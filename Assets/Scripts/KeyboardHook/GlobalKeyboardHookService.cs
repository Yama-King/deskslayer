using System;
using DeskSlayer.GameState;
using UnityEngine;

namespace DeskSlayer.KeyboardHook
{
    /// <summary>
    /// 橋接全域鍵盤 Hook（背景執行緒）與 Unity 主執行緒的服務。
    /// 職責僅限於啟停底層監聽、每影格從佇列取出事件並分派給遊戲系統訂閱者，
    /// 不處理任何戰鬥／能量邏輯（單一職責，交由未來的 TypingEnergySystem 等訂閱者實作）。
    /// </summary>
    public sealed class GlobalKeyboardHookService : MonoBehaviour
    {
        [SerializeField, Tooltip("每影格最多處理的按鍵事件數量，避免瞬間大量輸入造成單影格卡頓")]
        private int _maxEventsPerFrame = 50;

        /// <summary>每當一次有效按鍵事件從佇列被取出時觸發，供遊戲系統（如未來的 TypingEnergySystem）訂閱。</summary>
        public event Action<KeyPressData> OnKeyPressed;

        private KeyboardEventQueue _eventQueue;
        private IKeyboardHookProvider _hookProvider;
        private bool _isPaused;

        private void Awake()
        {
            _eventQueue = new KeyboardEventQueue();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _hookProvider = new Win32LowLevelKeyboardHook(_eventQueue);
#else
            Debug.LogWarning("GlobalKeyboardHookService: 目前平台不支援 Win32 全域鍵盤 Hook，服務將不會啟動監聽。");
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
            while (processed < _maxEventsPerFrame && _eventQueue.TryDequeue(out KeyPressData data))
            {
                // 暫停中仍要把佇列排空，避免解除暫停瞬間把暫停期間累積的按鍵一次性補放；
                // 但不分派事件——直接捨棄，而不是延後處理，確保暫停時打的字不會事後補觸發攻擊。
                if (!_isPaused)
                {
                    DispatchEvent(data);
                }

                processed++;
            }
        }

        /// <summary>
        /// 逐一呼叫訂閱者並個別隔離例外，避免單一訂閱者拋出例外中斷整條事件分派流程
        /// （直接呼叫 multicast delegate 的話，前面訂閱者拋例外會導致後面訂閱者完全收不到事件）。
        /// </summary>
        private void DispatchEvent(KeyPressData data)
        {
            if (OnKeyPressed == null)
            {
                return;
            }

            foreach (Delegate handler in OnKeyPressed.GetInvocationList())
            {
                try
                {
                    ((Action<KeyPressData>)handler).Invoke(data);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
