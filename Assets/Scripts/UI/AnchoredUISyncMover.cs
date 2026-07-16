using DeskSlayer.DesktopWindow;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 常駐錨定 UI 元素（按鈕或純顯示圖示）跟著 GameWorldRoot 同步移動／縮放的登記機制：訂閱
    /// GameWorldDragCoordinator.OnGameWorldRootMoved，收到螢幕像素差時換算成這個 Screen Space -
    /// Overlay Canvas 的本地單位套用到 anchoredPosition。這類 Canvas 不吃 Transform 位置，
    /// 所以「跟著移動」只能透過改寫 anchoredPosition 達成，換算除以 Canvas.scaleFactor 是
    /// CanvasScaler(ScaleWithScreenSize) 底下 Unity 標準公式。
    ///
    /// 掛在按鈕上時，一併把 GameWorldRoot 的縮放設定套用到 ButtonHoverPunch 的基準縮放（透過
    /// SetBaseScale，不直接改 localScale，理由見 ButtonHoverPunch 註解）；沒有 ButtonHoverPunch
    /// 的純顯示元素（例如天氣圖示）則直接設定自己的 localScale。之後 GameWorldRoot 因為滾輪縮放
    /// 而改變時（GameWorldDragCoordinator.OnWorldScaleChanged），套用邏輯完全比照初始縮放，
    /// 只是縮放來源從 ScriptableObject 的固定值換成事件帶出的即時值。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnchoredUISyncMover : MonoBehaviour
    {
        [SerializeField, Tooltip("GameWorldRoot 位移時要訂閱的協調者")]
        private GameWorldDragCoordinator _coordinator;

        [SerializeField, Tooltip("與 GameWorldRoot 共用的縮放設定")]
        private DesktopWorldPresentationSettingsSO _presentationSettings;

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private ButtonHoverPunch _hoverPunch;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            _hoverPunch = GetComponent<ButtonHoverPunch>();
        }

        /// <summary>
        /// 初始縮放刻意延到 Start 才套用，不是 Awake：Unity 不保證同一個 GameObject 上不同元件的
        /// Awake() 執行順序，這裡呼叫的 ButtonHoverPunch.SetBaseScale 依賴 ButtonHoverPunch 自己的
        /// _rectTransform 已經在它自己的 Awake() 內設定好——Start 保證所有物件的 Awake 都已跑完，
        /// 才能安全依賴這個前提。
        /// </summary>
        private void Start()
        {
            ApplyInitialScale();
        }

        private void OnEnable()
        {
            if (_coordinator != null)
            {
                _coordinator.OnGameWorldRootMoved += HandleGameWorldRootMoved;
                _coordinator.OnWorldScaleChanged += HandleWorldScaleChanged;
            }
        }

        private void OnDisable()
        {
            if (_coordinator != null)
            {
                _coordinator.OnGameWorldRootMoved -= HandleGameWorldRootMoved;
                _coordinator.OnWorldScaleChanged -= HandleWorldScaleChanged;
            }
        }

        private void ApplyInitialScale()
        {
            if (_presentationSettings == null)
            {
                return;
            }

            ApplyScale(_presentationSettings.WorldScale);
        }

        private void HandleGameWorldRootMoved(Vector2 screenPixelDelta)
        {
            float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
            _rectTransform.anchoredPosition += screenPixelDelta / scaleFactor;
        }

        private void HandleWorldScaleChanged(float scale)
        {
            ApplyScale(scale);
        }

        private void ApplyScale(float scale)
        {
            if (_hoverPunch != null)
            {
                _hoverPunch.SetBaseScale(scale);
            }
            else
            {
                _rectTransform.localScale = Vector3.one * scale;
            }
        }
    }
}
