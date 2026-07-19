#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DeskSlayer.MouseHook
{
    /// <summary>
    /// 透過 Win32 SetWindowsHookEx(WH_MOUSE_LL) 實作的全域低階滑鼠監聽，只攔截左右鍵的「按下」事件
    /// （不含放開/移動/拖曳，右鍵目前無對應遊戲行為，僅保留偵測能力供未來擴充）。
    ///
    /// 技術對應關係：跟 KeyboardHook.Win32LowLevelKeyboardHook 是同一套已驗證過的執行緒安全模式——
    /// 獨立 STA 執行緒 + Win32 訊息迴圈（GetMessage/TranslateMessage/DispatchMessage）、
    /// 用 ManualResetEventSlim 讓 StartListening() 等到 Hook 真正安裝完成才返回、
    /// 用 PostThreadMessage(WM_QUIT) 讓訊息迴圈自然結束（比 Thread.Abort 安全，不會留下未釋放的 Hook）。
    /// 這裡刻意複製同一套模式、獨立成自己的檔案，不抽共用基底類別：鍵盤 Hook 是全專案技術風險最高、
    /// 且有過真實迴歸事故的模組（見該類別註解），為了 DRY 去重構一個已經正確運作的高風險系統不划算，
    /// 兩者各自獨立、日後各自演進互不影響才是更保守的做法。
    ///
    /// 滑鼠 Hook 比鍵盤 Hook 單純很多：不需要 ToUnicode 字元轉換，也不需要自行維護修飾鍵狀態表，
    /// 只需要判斷 wParam 訊息碼是否為 WM_LBUTTONDOWN/WM_RBUTTONDOWN。
    /// </summary>
    public sealed class Win32LowLevelMouseHook : IMouseHookProvider
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const uint WM_QUIT = 0x0012;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

        private readonly MouseEventQueue _eventQueue;

        // 保留 delegate 的強參考：若 delegate 被 GC 回收，原生端回呼會直接崩潰
        // （CallbackOnCollectedDelegate），這是 P/Invoke callback 的常見地雷。
        private readonly LowLevelMouseProc _hookProc;

        private readonly ManualResetEventSlim _hookReady = new ManualResetEventSlim(false);
        private Thread _messageLoopThread;
        private IntPtr _hookHandle = IntPtr.Zero;
        private volatile uint _messageLoopThreadId;
        private volatile bool _hookInstallFailed;

        public Win32LowLevelMouseHook(MouseEventQueue eventQueue)
        {
            _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
            _hookProc = HookCallback;
        }

        public void StartListening()
        {
            if (_messageLoopThread != null)
            {
                return; // 已在監聽中，避免重複安裝
            }

            _hookReady.Reset();
            _hookInstallFailed = false;

            _messageLoopThread = new Thread(MessageLoopThreadMain)
            {
                IsBackground = true,
                Name = "DeskSlayer.GlobalMouseHook"
            };
            _messageLoopThread.SetApartmentState(ApartmentState.STA);
            _messageLoopThread.Start();

            // 等待 Hook 安裝完成再返回，確保呼叫端結束 StartListening() 時監聽已經生效。
            _hookReady.Wait();

            if (_hookInstallFailed)
            {
                _messageLoopThread = null;
                throw new InvalidOperationException("SetWindowsHookEx(WH_MOUSE_LL) 安裝失敗，請確認執行環境權限。");
            }
        }

        public void StopListening()
        {
            if (_messageLoopThread == null)
            {
                return;
            }

            // 用 PostThreadMessage 送出 WM_QUIT 讓訊息迴圈自然結束，
            // 這是終止「擁有訊息迴圈的執行緒」的標準做法，比 Thread.Abort 安全（不會留下未釋放的 Hook）。
            PostThreadMessage(_messageLoopThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _messageLoopThread.Join(1000);
            _messageLoopThread = null;
        }

        private void MessageLoopThreadMain()
        {
            _messageLoopThreadId = GetCurrentThreadId();

            _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, GetModuleHandle(null), 0);
            _hookInstallFailed = _hookHandle == IntPtr.Zero;

            _hookReady.Set();

            if (!_hookInstallFailed)
            {
                MSG msg;
                int result;
                while ((result = GetMessage(out msg, IntPtr.Zero, 0, 0)) != 0)
                {
                    if (result == -1)
                    {
                        break; // GetMessage 發生錯誤，結束迴圈
                    }

                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            }

            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    int message = (int)wParam;

                    if (message == WM_LBUTTONDOWN)
                    {
                        _eventQueue.Enqueue(new MouseClickData(MouseButtonKind.Left, DateTime.UtcNow.Ticks));
                    }
                    else if (message == WM_RBUTTONDOWN)
                    {
                        _eventQueue.Enqueue(new MouseClickData(MouseButtonKind.Right, DateTime.UtcNow.Ticks));
                    }

                    // 其餘滑鼠訊息（移動、滾輪、放開等）不在本系統的偵測範圍內，直接忽略。
                }
            }
            catch
            {
                // Hook 回呼在背景執行緒的原生呼叫堆疊中執行，未攔截的例外會直接讓整個行程崩潰，
                // 因此這裡必須全面吞掉例外，確保單次事件解析失敗不會拖垮整個監聽服務。
            }

            // 無論是否處理，都必須呼叫 CallNextHookEx 讓事件繼續傳遞給下一個 Hook，
            // 否則會阻斷系統中其他監聽滑鼠事件的程式，包含使用者原本預期的點擊行為。
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }
    }
}
#endif
