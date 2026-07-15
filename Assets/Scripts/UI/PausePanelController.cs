using DG.Tweening;
using DeskSlayer.GameState;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 暫停疊加層的顯示/隱藏。與 WeaponInventoryPanelController／WeatherCityPanelController
    /// 同樣採 CanvasGroup + DOTween 的殼，但職責切分刻意不同：那兩個面板由 UI 按鈕點擊
    /// 主動觸發開關，這裡的 Open/Close 一律是 private，只能由訂閱 GameStateMachine 事件的
    /// HandleGamePhaseChanged 呼叫——UI 只能「反應」狀態變化，不能反過來決定或修改遊戲狀態。
    /// 過場動畫一律加上 SetUpdate(true)：暫停期間 Time.timeScale 為 0，若不獨立於 timeScale，
    /// 淡入/縮放的開場動畫會在播放到一半時直接凍結，疊加層永遠無法完整顯示。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PausePanelController : MonoBehaviour
    {
        [SerializeField, Tooltip("淡入淡出過場時長（秒，使用 Realtime，不受暫停時 timeScale=0 影響）")]
        private float _fadeDuration = 0.2f;

        [SerializeField, Tooltip("面板縮放過場用的根節點")]
        private RectTransform _panelRoot;

        private CanvasGroup _canvasGroup;
        private bool _isOpen;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            SetClosedImmediate();
        }

        private void OnEnable()
        {
            SubscribeToGameStateMachine();
        }

        private void Start()
        {
            // Unity 只保證「所有物件的 Awake 都先於任何物件的 Start」，不保證 OnEnable 的跨物件順序，
            // 因此 OnEnable 當下 GameStateMachine.Instance 有可能還沒被賦值。這裡在 Start 補一次訂閱，
            // 確保無論兩者執行順序為何，最終一定會成功訂閱到事件。
            SubscribeToGameStateMachine();
        }

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
            {
                GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            }
        }

        private void SubscribeToGameStateMachine()
        {
            if (GameStateMachine.Instance == null)
            {
                return;
            }

            // 先移除再訂閱，確保 OnEnable／Start 都呼叫到這裡時，事件上只會掛一份委派
            GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            GameStateMachine.Instance.OnGamePhaseChanged += HandleGamePhaseChanged;

            // 元件啟用當下直接對齊目前實際階段，避免與真正的遊戲狀態脫節
            if (GameStateMachine.Instance.CurrentPhase == GamePhase.Paused)
            {
                Open();
            }
            else
            {
                SetClosedImmediate();
            }
        }

        private void HandleGamePhaseChanged(GamePhase? previous, GamePhase current)
        {
            if (current == GamePhase.Paused)
            {
                Open();
            }
            else
            {
                Close();
            }
        }

        private void Open()
        {
            if (_isOpen)
            {
                return;
            }

            _isOpen = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            _canvasGroup.DOKill();
            _panelRoot.DOKill();
            _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
            _panelRoot.localScale = Vector3.one * 0.95f;
            _panelRoot.DOScale(1f, _fadeDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;

            _canvasGroup.DOKill();
            _panelRoot.DOKill();
            _canvasGroup.DOFade(0f, _fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    _canvasGroup.interactable = false;
                });
            _panelRoot.DOScale(0.95f, _fadeDuration).SetUpdate(true);
        }

        private void SetClosedImmediate()
        {
            _isOpen = false;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            if (_panelRoot != null)
            {
                _panelRoot.localScale = Vector3.one * 0.95f;
            }
        }
    }
}
