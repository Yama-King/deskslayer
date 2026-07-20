using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 打字連擊計數器的純視覺元件：不認識 TypingComboTracker，只透過公開方法 ShowCount / Hide
    /// 接收資料，資料如何來完全交給 TypingComboCounterDispatcher 負責轉發。
    /// 計數器預設不可見（呼應桌面陪伴「非侵入」調性），每次計數遞增就淡入顯示並播放一次
    /// 跳出（punch scale）動畫；閒置逾時歸零時改為淡出消失，而非瞬間隱藏。
    /// </summary>
    public sealed class TypingComboCounterView : MonoBehaviour
    {
        [SerializeField]
        private TypingComboConfigSO _config;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private TextMeshProUGUI _countText;

        private Tween _fadeTween;
        private Tween _punchTween;
        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = _countText.rectTransform.localScale;

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        /// <summary>顯示（或更新）目前的連擊計數，並播放一次跳出動畫。</summary>
        public void ShowCount(int count)
        {
            _countText.text = $"x{count}";

            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(1f, _config.FadeInDuration);

            _punchTween?.Kill();
            _countText.rectTransform.localScale = _baseScale;
            _punchTween = _countText.rectTransform.DOPunchScale(
                Vector3.one * _config.PunchScaleStrength,
                _config.PunchScaleDuration);
        }

        /// <summary>淡出隱藏計數器。</summary>
        public void Hide()
        {
            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(0f, _config.FadeOutDuration);
        }
    }
}
