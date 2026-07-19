using System;
using DeskSlayer.GameState;
using UnityEngine;

namespace DeskSlayer.MouseHook
{
    /// <summary>
    /// 橋接全域滑鼠 Hook（背景執行緒）與 Unity 主執行緒的服務，比照 KeyboardHook.GlobalKeyboardHookService：
    /// 職責僅限於啟停底層監聽、每影格從佇列取出事件並分派給訂閱者，不處理任何戰鬥/統計邏輯
    /// （交由 AttackInput.AttackInputAggregator 訂閱後彙整）。
    ///
    /// 與點擊穿透機制（DesktopWindowClickThroughMediator）刻意保持獨立：該機制只用
    /// UniWindowController.GetCursorPosition() 做逐幀「位置」判定，完全不消費滑鼠按鍵事件，
    /// 因此這裡新增的「按鍵事件」偵測與它在事件層級沒有交集，不需要、也不應該合併為同一個服務。
    /// </summary>
    public sealed class GlobalMouseHookService : MonoBehaviour
    {
        [SerializeField, Tooltip("每影格最多處理的滑鼠事件數量，避免瞬間大量點擊造成單影格卡頓")]
        private int _maxEventsPerFrame = 50;

        /// <summary>每當一次滑鼠點擊事件從佇列被取出時觸發，供遊戲系統（如 AttackInputAggregator）訂閱。</summary>
        public event Action<MouseClickData> OnMouseClicked;

        private MouseEventQueue _eventQueue;
        private IMouseHookProvider _hookProvider;
        private bool _isPaused;

        private void Awake()
        {
            _eventQueue = new MouseEventQueue();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _hookProvider = new Win32LowLevelMouseHook(_eventQueue);
#else
            Debug.LogWarning("GlobalMouseHookService: 目前平台不支援 Win32 全域滑鼠 Hook，服務將不會啟動監聽。");
#endif
        }

        private void OnEnable()
        {
            if (_hookProvider != null)
            {
                try
                {
                    _hookProvider.StartListening();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            SubscribeToGameStateMachine();
        }

        private void Start()
        {
            // Unity 只保證所有物件的 Awake 先於任何物件的 Start，不保證 OnEnable 的跨物件順序，
            // 這裡補一次訂閱，確保不論 GameStateMachine 的 Awake 相對順序為何都能訂閱成功。
            SubscribeToGameStateMachine();
        }

        private void OnDisable()
        {
            _hookProvider?.StopListening();

            if (GameStateMachine.Instance != null)
            {
                GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            }
        }

        private void OnApplicationQuit()
        {
            _hookProvider?.StopListening();
        }

        private void SubscribeToGameStateMachine()
        {
            if (GameStateMachine.Instance == null)
            {
                return;
            }

            // 訂閱前先同步一次目前階段，避免訂閱完成前的極短暫視窗誤判為未暫停
            _isPaused = GameStateMachine.Instance.CurrentPhase == GamePhase.Paused;
            GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            GameStateMachine.Instance.OnGamePhaseChanged += HandleGamePhaseChanged;
        }

        private void HandleGamePhaseChanged(GamePhase? previous, GamePhase current)
        {
            _isPaused = current == GamePhase.Paused;
        }

        private void Update()
        {
            if (_eventQueue == null)
            {
                return;
            }

            int processed = 0;
            while (processed < _maxEventsPerFrame && _eventQueue.TryDequeue(out MouseClickData data))
            {
                // 暫停中仍要把佇列排空，避免解除暫停瞬間把暫停期間累積的點擊一次性補放；
                // 但不分派事件——直接捨棄，而不是延後處理，比照 GlobalKeyboardHookService 的作法。
                if (!_isPaused)
                {
                    DispatchEvent(data);
                }

                processed++;
            }
        }

        /// <summary>
        /// 逐一呼叫訂閱者並個別隔離例外，避免單一訂閱者拋出例外中斷整條事件分派流程。
        /// </summary>
        private void DispatchEvent(MouseClickData data)
        {
            if (OnMouseClicked == null)
            {
                return;
            }

            foreach (Delegate handler in OnMouseClicked.GetInvocationList())
            {
                try
                {
                    ((Action<MouseClickData>)handler).Invoke(data);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
