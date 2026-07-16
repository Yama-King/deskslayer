using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 通用按鈕互動回饋：滑鼠移入時輕微放大，移出時恢復；點擊瞬間做一個短暫縮小回彈，
    /// 提供「有點到」的觸覺回饋感。刻意獨立於 WeaponSwitchUI 之外，
    /// 純粹是視覺回饋、不認識武器切換邏輯，掛在任何 Button 上都能重複使用。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ButtonHoverPunch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField, Tooltip("滑鼠移入時的放大倍率")]
        private float _hoverScale = 1.1f;

        [SerializeField, Tooltip("縮放過渡動畫時長（秒）")]
        private float _scaleDuration = 0.15f;

        [SerializeField, Tooltip("點擊瞬間縮小的倍率")]
        private float _clickScale = 0.9f;

        [SerializeField, Tooltip("點擊回彈動畫時長（秒）")]
        private float _clickPunchDuration = 0.25f;

        private RectTransform _rectTransform;
        private Vector3 _originalScale;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _originalScale = _rectTransform.localScale;
        }

        /// <summary>
        /// 更新 hover/click 回饋動畫要回彈的基準縮放倍率並立即套用（非 hover 狀態下）。
        /// 這個元件是 RectTransform.localScale 唯一擁有者：外部（例如常駐錨定按鈕的整組縮放設定）
        /// 需要改變按鈕基礎大小時，透過這個方法交給這裡統一處理，不要直接改寫 localScale，
        /// 否則會跟這裡的 DOTween 回饋動畫互搶同一個欄位、產生時序競態。
        /// </summary>
        public void SetBaseScale(float multiplier)
        {
            _originalScale = Vector3.one * multiplier;
            _rectTransform.DOKill();
            _rectTransform.localScale = _originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _rectTransform.DOKill();
            _rectTransform.DOScale(_originalScale * _hoverScale, _scaleDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _rectTransform.DOKill();
            _rectTransform.DOScale(_originalScale, _scaleDuration);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _rectTransform.DOKill();
            _rectTransform.DOScale(_originalScale * _clickScale, _clickPunchDuration * 0.4f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => _rectTransform.DOScale(_originalScale * _hoverScale, _clickPunchDuration * 0.6f).SetEase(Ease.OutBack));
        }
    }
}
