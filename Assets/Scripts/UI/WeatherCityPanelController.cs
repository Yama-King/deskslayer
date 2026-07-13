using DG.Tweening;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 天氣城市選擇面板的開關生命週期。比照 WeaponInventoryPanelController 的作法：
    /// 疊加在遊戲畫面上的浮動面板，不使用 Time.timeScale = 0，開啟時鍵盤監聽與打字觸發攻擊
    /// 仍正常運作；開關方式只透過畫面按鈕點擊，不綁定鍵盤熱鍵。獨立成專屬類別而非重用
    /// WeaponInventoryPanelController，是為了保持命名與職責清楚對應各自的功能面板。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WeatherCityPanelController : MonoBehaviour
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
