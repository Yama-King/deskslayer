using DG.Tweening;
using DeskSlayer.Achievements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.Juice
{
    /// <summary>
    /// 單一成就解鎖 Toast 彈窗：顯示圖示＋名稱＋描述，進場播放縮放彈跳，停留一段時間後
    /// 自動淡出並銷毀。完全比照 WeaponDropLogEntry 的 DOTween 進場/淡出寫法，
    /// 不另外發明新的彈出動畫模式。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class AchievementToastEntry : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        [SerializeField]
        private TextMeshProUGUI _descriptionLabel;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>顯示指定成就內容並依設定的手感參數開始進場動畫與自動消失倒數。</summary>
        public void Show(AchievementDefinitionSO achievement, AchievementToastConfigSO config)
        {
            if (_icon != null)
            {
                _icon.sprite = achievement.Icon;
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = achievement.DisplayName;
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = achievement.Description;
            }

            _canvasGroup.alpha = 1f;

            transform.DOKill();
            transform.localScale = Vector3.one * 0.7f;
            transform.DOScale(1f, config.PunchDuration).SetEase(Ease.OutBack).SetUpdate(true);

            _canvasGroup.DOFade(0f, config.FadeDuration)
                .SetDelay(config.HoldDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}
