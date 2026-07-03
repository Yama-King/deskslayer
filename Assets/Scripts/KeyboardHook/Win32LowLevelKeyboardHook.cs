#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DeskSlayer.KeyboardHook
{
    /// <summary>
    /// 透過 Win32 SetWindowsHookEx(WH_KEYBOARD_LL) 實作的全域低階鍵盤監聽。
    /// 在獨立的 STA 執行緒上安裝 Hook 並執行訊息迴圈，確保不阻塞 Unity 主執行緒；
    /// 這裡只負責「監聽並丟進執行緒安全佇列」，刻意不呼叫任何 Unity API
    /// （Unity API 只能在主執行緒呼叫，背景執行緒呼叫會直接拋例外或造成未定義行為）。
    /// </summary>
    public sealed class Win32LowLevelKeyboardHook : IKeyboardHookProvider
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const uint WM_QUIT = 0x0012;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
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

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

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
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff,
            int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

        private readonly KeyboardEventQueue _eventQueue;

        // 保留 delegate 的強參考：若 delegate 被 GC 回收，原生端回呼會直接崩潰
        // （CallbackOnCollectedDelegate），這是 P/Invoke callback 的常見地雷。
        private readonly LowLevelKeyboardProc _hookProc;

        private readonly ManualResetEventSlim _hookReady = new ManualResetEventSlim(false);
        private Thread _messageLoopThread;
        private IntPtr _hookHandle = IntPtr.Zero;
        private volatile uint _messageLoopThreadId;
        private volatile bool _hookInstallFailed;

        public Win32LowLevelKeyboardHook(KeyboardEventQueue eventQueue)
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
                Name = "DeskSlayer.GlobalKeyboardHook"
            };
            _messageLoopThread.SetApartmentState(ApartmentState.STA);
            _messageLoopThread.Start();

            // 等待 Hook 安裝完成再返回，確保呼叫端結束 StartListening() 時監聽已經生效。
            _hookReady.Wait();

            if (_hookInstallFailed)
            {
                _messageLoopThread = null;
                throw new InvalidOperationException("SetWindowsHookEx(WH_KEYBOARD_LL) 安裝失敗，請確認執行環境權限。");
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
            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(null), 0);
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
                if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                    if (TryResolveCharacter(hookStruct.vkCode, hookStruct.scanCode, out char character))
                    {
                        _eventQueue.Enqueue(new KeyPressData(character, DateTime.UtcNow.Ticks));
                    }
                }
            }
            catch
            {
                // Hook 回呼在背景執行緒的原生呼叫堆疊中執行，未攔截的例外會直接讓整個行程崩潰，
                // 因此這裡必須全面吞掉例外，確保單次按鍵解析失敗不會拖垮整個監聽服務。
            }

            // 無論是否處理，都必須呼叫 CallNextHookEx 讓事件繼續傳遞給下一個 Hook，
            // 否則會阻斷系統中其他監聽鍵盤事件的程式，包含使用者原本預期的輸入行為。
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        /// <summary>
        /// 將虛擬鍵碼轉換為實際字元，並過濾掉非可列印字元（如 Shift、Ctrl、方向鍵、功能鍵等）。
        /// </summary>
        private static bool TryResolveCharacter(uint vkCode, uint scanCode, out char character)
        {
            character = default;

            var keyboardState = new byte[256];
            if (!GetKeyboardState(keyboardState))
            {
                return false;
            }

            var buffer = new StringBuilder(2);
            int translated = ToUnicode(vkCode, scanCode, keyboardState, buffer, buffer.Capacity, 0);

            if (translated != 1)
            {
                // 0：無對應字元；負數：死鍵（如注音符號）；>1：多字元組合，皆非本系統需要的單一可列印字元。
                return false;
            }

            character = buffer[0];
            return !char.IsControl(character);
        }
    }
}
#endif
