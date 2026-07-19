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
    ///
    /// 修飾鍵狀態（Shift/Ctrl/Alt/CapsLock）由本類別自行維護，刻意不呼叫 GetKeyboardState：
    /// 該 API 讀到的是「呼叫執行緒」的同步鍵盤狀態，而這裡的訊息迴圈執行緒從未真正收派過
    /// 實體 WM_KEYDOWN／WM_KEYUP，讓這個背景執行緒去依賴跟訊息佇列綁定的系統狀態表本身就不可靠。
    ///
    /// 補充：「視窗取得焦點後完全打不出字」這個症狀後來查出真正根因是專案曾經把
    /// ProjectSettings.activeInputHandler 設為新版 Input System Only——新版 Input System 的
    /// Native Backend 會在 DeskSlayer 視窗取得 OS 焦點時跟這裡的全域 Hook 搶鍵盤輸入，導致
    /// HookCallback 完全收不到事件。專案現在固定用 Input Manager (Old)（見
    /// DesktopWorldDragInputController.cs 類別註解），不是這個類別能單獨解決的問題；
    /// 這裡維護自己的鍵盤狀態表仍然是正確做法，只是不要誤以為它是那個症狀的根因。
    /// </summary>
    public sealed class Win32LowLevelKeyboardHook : IKeyboardHookProvider
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const uint WM_QUIT = 0x0012;

        // LLKHF_EXTENDED：KBDLLHOOKSTRUCT.flags 的 bit 0，用來分辨 Ctrl/Alt 是左鍵還是右鍵。
        // 低階 Hook 對 Shift 會直接給左右各自的虛擬鍵碼（VK_LSHIFT/VK_RSHIFT），
        // 但 Ctrl/Alt 一律回報通用鍵碼（VK_CONTROL/VK_MENU），要靠這個旗標才能分辨左右。
        private const uint LLKHF_EXTENDED = 0x00000001;

        private const uint VK_SHIFT = 0x10;
        private const uint VK_CONTROL = 0x11;
        private const uint VK_MENU = 0x12;
        private const uint VK_CAPITAL = 0x14;
        private const uint VK_LSHIFT = 0xA0;
        private const uint VK_RSHIFT = 0xA1;
        private const uint VK_LCONTROL = 0xA2;
        private const uint VK_RCONTROL = 0xA3;
        private const uint VK_LMENU = 0xA4;
        private const uint VK_RMENU = 0xA5;

        // ToUnicode 判定鍵盤狀態的位元慣例：bit7(0x80) 代表「目前按著」，bit0(0x01) 代表鎖定鍵的切換狀態。
        private const byte KeyDownFlag = 0x80;
        private const byte ToggleFlag = 0x01;

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
        private static extern short GetKeyState(int nVirtKey);

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

        // 自行維護的鍵盤狀態表，只在 Hook 的訊息迴圈執行緒上讀寫，不需要額外同步。
        // 只有 Shift/Ctrl/Alt/CapsLock 這幾個索引會被更新，其餘索引 ToUnicode 用不到，不需要維護。
        private readonly byte[] _keyboardState = new byte[256];
        private bool _capsLockPhysicallyDown;

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

            Array.Clear(_keyboardState, 0, _keyboardState.Length);
            _capsLockPhysicallyDown = false;
            InitializeCapsLockState();

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
                if (nCode >= 0)
                {
                    int message = (int)wParam;
                    bool isKeyDown = message == WM_KEYDOWN || message == WM_SYSKEYDOWN;
                    bool isKeyUp = message == WM_KEYUP || message == WM_SYSKEYUP;

                    if (isKeyDown || isKeyUp)
                    {
                        KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                        // 修飾鍵的按下/放開都要追蹤，才能正確反映放開時的狀態；
                        // 一般按鍵放開事件對 ToUnicode 無意義，UpdateModifierState 內部會直接忽略。
                        UpdateModifierState(hookStruct.vkCode, hookStruct.flags, isKeyDown);

                        if (isKeyDown && TryResolveCharacter(hookStruct.vkCode, hookStruct.scanCode, out char character))
                        {
                            _eventQueue.Enqueue(new KeyPressData(character, DateTime.UtcNow.Ticks));
                        }
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
        /// 根據 Hook 串流看到的按鍵事件，更新自行維護的 _keyboardState。
        /// 只處理 ToUnicode 會用到的修飾鍵，其餘按鍵不需要、也不應該寫進這張表。
        /// </summary>
        private void UpdateModifierState(uint vkCode, uint flags, bool isKeyDown)
        {
            byte state = isKeyDown ? KeyDownFlag : (byte)0x00;

            switch (vkCode)
            {
                case VK_LSHIFT:
                case VK_RSHIFT:
                    // 低階 Hook 對 Shift 直接給左右各自的鍵碼，可以直接寫入對應索引。
                    _keyboardState[vkCode] = state;
                    _keyboardState[VK_SHIFT] = Any(_keyboardState[VK_LSHIFT], _keyboardState[VK_RSHIFT]);
                    break;

                case VK_CONTROL:
                    // 左右 Ctrl 在低階 Hook 一律回報通用鍵碼，要靠 LLKHF_EXTENDED 分辨是哪一邊，
                    // 否則放開右 Ctrl 時會誤判成「左右都放開」或反過來誤判成「還按著」。
                    uint specificCtrl = (flags & LLKHF_EXTENDED) != 0 ? VK_RCONTROL : VK_LCONTROL;
                    _keyboardState[specificCtrl] = state;
                    _keyboardState[VK_CONTROL] = Any(_keyboardState[VK_LCONTROL], _keyboardState[VK_RCONTROL]);
                    break;

                case VK_MENU:
                    uint specificAlt = (flags & LLKHF_EXTENDED) != 0 ? VK_RMENU : VK_LMENU;
                    _keyboardState[specificAlt] = state;
                    _keyboardState[VK_MENU] = Any(_keyboardState[VK_LMENU], _keyboardState[VK_RMENU]);
                    break;

                case VK_CAPITAL:
                    // CapsLock 是切換鍵，只有「實際被壓下的那一瞬間」才切換一次；
                    // 長按會持續送出多次 WM_KEYDOWN（一般按鍵的鍵盤重複輸入），若每次都切換會誤判成連續開關。
                    if (isKeyDown && !_capsLockPhysicallyDown)
                    {
                        _keyboardState[VK_CAPITAL] ^= ToggleFlag;
                    }
                    _capsLockPhysicallyDown = isKeyDown;
                    break;
            }
        }

        private static byte Any(byte a, byte b) => (byte)((a | b) != 0 ? KeyDownFlag : 0x00);

        /// <summary>
        /// 開機當下讀取一次 CapsLock 的實際開關狀態，作為 _keyboardState 的起始值。
        /// GetKeyState 的「切換狀態」(回傳值 bit0) 是系統層級的全域狀態，不像 GetKeyboardState
        /// 那樣綁定呼叫執行緒的訊息佇列，因此這裡可以放心從 Hook 的訊息迴圈執行緒讀取。
        /// </summary>
        private void InitializeCapsLockState()
        {
            short capsLockState = GetKeyState((int)VK_CAPITAL);
            if ((capsLockState & 0x0001) != 0)
            {
                _keyboardState[VK_CAPITAL] |= ToggleFlag;
            }
        }

        /// <summary>
        /// 將虛擬鍵碼轉換為實際字元，並過濾掉非可列印字元（如 Shift、Ctrl、方向鍵、功能鍵等）。
        /// </summary>
        private bool TryResolveCharacter(uint vkCode, uint scanCode, out char character)
        {
            character = default;

            var buffer = new StringBuilder(2);
            int translated = ToUnicode(vkCode, scanCode, _keyboardState, buffer, buffer.Capacity, 0);

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
