using System;
using UnityEngine;
using DeskSlayer.GameState;

namespace DeskSlayer.InputHooking
{
    /// <summary>
    /// 供 KeyboardHook.GlobalKeyboardHookService／MouseHook.GlobalMouseHookService 共用的
    /// 「暫停感知的安全事件佇列分派」骨架。兩者的生命週期（啟停底層 Hook、訂閱 GameStateMachine、
    /// 逐幀消化執行緒安全佇列、例外隔離式事件分派）過去是逐行複製貼上的兩份程式碼，抽出這個
    /// 泛型基底類別後只維護一份：子類別只需要在 Awake 建立各自的 provider／佇列後，透過
    /// BindQueue／BindHookLifecycle 注入「怎麼取事件」與「怎麼啟停監聽」，並用自訂事件存取子
    /// 把 <see cref="Dispatched"/> 包裝成各自具名的公開事件（如 OnKeyPressed／OnMouseClicked），
    /// 對下游訂閱端完全透明——事件名稱、簽章、多播例外隔離語意都不變。
    /// </summary>
    /// <typeparam name="TEventData">佇列裡的事件資料型別（例如 KeyPressData、MouseClickData）。</typeparam>
    public abstract class PausableHookServiceBase<TEventData> : MonoBehaviour
    {
        /// <summary>比照 ConcurrentQueue&lt;T&gt;.TryDequeue 的簽章，讓基底類別可以消化任意型別的
        /// 事件佇列，而不需要認識佇列的具體型別（KeyboardEventQueue／MouseEventQueue）。</summary>
        public delegate bool TryDequeueHandler(out TEventData data);

        [SerializeField, Tooltip("每影格最多處理的事件數量，避免瞬間大量輸入造成單影格卡頓")]
        private int _maxEventsPerFrame = 50;

        /// <summary>供子類別用自訂事件存取子（add/remove）轉發成各自具名的公開事件；
        /// 只有本類別（宣告端）可以觸發（Invoke），子類別與外部只能訂閱/取消訂閱。</summary>
        protected event Action<TEventData> Dispatched;

        private TryDequeueHandler _tryDequeue;
        private Action _startListening;
        private Action _stopListening;
        private bool _isPaused;

        /// <summary>子類別於 Awake 建立佇列後呼叫，注入該佇列的 TryDequeue 方法。</summary>
        protected void BindQueue(TryDequeueHandler tryDequeue)
        {
            _tryDequeue = tryDequeue;
        }

        /// <summary>
        /// 子類別於 Awake 建立 provider 後呼叫，注入啟停監聽的方式。傳入的委派內部應自行處理
        /// provider 可能為 null 的情況（例如非 Windows 平台未建立任何 provider）。
        /// </summary>
        protected void BindHookLifecycle(Action startListening, Action stopListening)
        {
            _startListening = startListening;
            _stopListening = stopListening;
        }

        private void OnEnable()
        {
            if (_startListening != null)
            {
                try
                {
                    _startListening();
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
            _stopListening?.Invoke();

            if (GameStateMachine.Instance != null)
            {
                GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            }
        }

        private void OnApplicationQuit()
        {
            _stopListening?.Invoke();
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
            if (_tryDequeue == null)
            {
                return;
            }

            int processed = 0;
            while (processed < _maxEventsPerFrame && _tryDequeue(out TEventData data))
            {
                // 暫停中仍要把佇列排空，避免解除暫停瞬間把暫停期間累積的事件一次性補放；
                // 但不分派事件——直接捨棄，而不是延後處理，確保暫停時的輸入不會事後補觸發。
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
        private void DispatchEvent(TEventData data)
        {
            if (Dispatched == null)
            {
                return;
            }

            foreach (Delegate handler in Dispatched.GetInvocationList())
            {
                try
                {
                    ((Action<TEventData>)handler).Invoke(data);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
