using DG.Tweening;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 設定面板的開關生命週期，完全比照 WeaponInventoryPanelController 的模式：這是疊加在遊戲畫面上的
    /// 浮動面板，不是正式暫停選單，刻意不使用 Time.timeScale = 0——開啟時全域鍵盤/滑鼠 Hook 監聽、
    /// TypingEnergySystem 的能量累積與戰鬥判定都必須維持正常運作，呼應桌面陪伴軟體「隨時可調整」的
    /// 核心體驗。開關方式刻意只透過畫面按鈕點擊，不綁定任何鍵盤熱鍵，避免與打字觸發攻擊的輸入邏輯
    /// 產生衝突疑慮。內容邏輯（滑鼠開關/音量滑桿的實際讀寫）拆到 SettingsMenuController，這裡只負責
    /// 面板本身的開關與過場動畫。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [SerializeField, Tooltip("淡入淡出過場時長（秒）")]
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

        /// <summary>切換面板開關狀態。</summary>
        public void Toggle()
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>開啟面板：允許互動並播放淡入 + 縮放回彈過場。</summary>
        public void Open()
        {
            if (_isOpen)
            {
                return;
            }

            _isOpen = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            GetComponent<PanelDragHandle>()?.BringToFront();

            _canvasGroup.DOKill();
            _panelRoot.DOKill();
            _canvasGroup.DOFade(1f, _fadeDuration);
            _panelRoot.localScale = Vector3.one * 0.95f;
            _panelRoot.DOScale(1f, _fadeDuration).SetEase(Ease.OutBack);
        }

        /// <summary>關閉面板：播放淡出過場，結束後停止阻擋滑鼠事件。</summary>
        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;

            _canvasGroup.DOKill();
            _panelRoot.DOKill();
            _canvasGroup.DOFade(0f, _fadeDuration)
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    _canvasGroup.interactable = false;
                });
            _panelRoot.DOScale(0.95f, _fadeDuration);
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
