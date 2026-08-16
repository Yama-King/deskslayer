using System;
using UnityEngine;
using DeskSlayer.InputHooking;

namespace DeskSlayer.KeyboardHook
{
    /// <summary>
    /// 橋接全域鍵盤 Hook（背景執行緒）與 Unity 主執行緒的服務。
    /// 職責僅限於建立平台專屬的 provider／佇列，並把兩者注入共用骨架
    /// <see cref="PausableHookServiceBase{TEventData}"/>（啟停監聽、暫停感知、逐幀分派、
    /// 例外隔離皆由基底類別統一處理，比照 MouseHook.GlobalMouseHookService）。
    /// 不處理任何戰鬥／能量邏輯（單一職責，交由 TypingEnergySystem 等訂閱者實作）。
    /// </summary>
    public sealed class GlobalKeyboardHookService : PausableHookServiceBase<KeyPressData>
    {
        /// <summary>每當一次有效按鍵事件從佇列被取出時觸發，供遊戲系統（如 TypingEnergySystem）訂閱。</summary>
        public event Action<KeyPressData> OnKeyPressed
        {
            add => Dispatched += value;
            remove => Dispatched -= value;
        }

        private KeyboardEventQueue _eventQueue;
        private IKeyboardHookProvider _hookProvider;

        private void Awake()
        {
            _eventQueue = new KeyboardEventQueue();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _hookProvider = new Win32LowLevelKeyboardHook(_eventQueue);
#else
            Debug.LogWarning("GlobalKeyboardHookService: 目前平台不支援 Win32 全域鍵盤 Hook，服務將不會啟動監聽。");
#endif

            BindQueue(_eventQueue.TryDequeue);
            BindHookLifecycle(() => _hookProvider?.StartListening(), () => _hookProvider?.StopListening());
        }
    }
}
