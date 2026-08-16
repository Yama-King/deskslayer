using System;
using UnityEngine;
using DeskSlayer.InputHooking;

namespace DeskSlayer.MouseHook
{
    /// <summary>
    /// 橋接全域滑鼠 Hook（背景執行緒）與 Unity 主執行緒的服務，比照 KeyboardHook.GlobalKeyboardHookService：
    /// 職責僅限於建立平台專屬的 provider／佇列，並把兩者注入共用骨架
    /// <see cref="PausableHookServiceBase{TEventData}"/>，不處理任何戰鬥/統計邏輯
    /// （交由 AttackInput.AttackInputAggregator 訂閱後彙整）。
    ///
    /// 與點擊穿透機制（DesktopWindowClickThroughMediator）刻意保持獨立：該機制只用
    /// UniWindowController.GetCursorPosition() 做逐幀「位置」判定，完全不消費滑鼠按鍵事件，
    /// 因此這裡的「按鍵事件」偵測與它在事件層級沒有交集，不需要、也不應該合併為同一個服務。
    /// </summary>
    public sealed class GlobalMouseHookService : PausableHookServiceBase<MouseClickData>
    {
        /// <summary>每當一次滑鼠點擊事件從佇列被取出時觸發，供遊戲系統（如 AttackInputAggregator）訂閱。</summary>
        public event Action<MouseClickData> OnMouseClicked
        {
            add => Dispatched += value;
            remove => Dispatched -= value;
        }

        private MouseEventQueue _eventQueue;
        private IMouseHookProvider _hookProvider;

        private void Awake()
        {
            _eventQueue = new MouseEventQueue();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _hookProvider = new Win32LowLevelMouseHook(_eventQueue);
#else
            Debug.LogWarning("GlobalMouseHookService: 目前平台不支援 Win32 全域滑鼠 Hook，服務將不會啟動監聽。");
#endif

            BindQueue(_eventQueue.TryDequeue);
            BindHookLifecycle(() => _hookProvider?.StartListening(), () => _hookProvider?.StopListening());
        }
    }
}
