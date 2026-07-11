using DG.Tweening;
using DeskSlayer.Combat;
using DeskSlayer.Juice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 掉落記錄清單中的單一列項：顯示圖示＋名稱＋稀有度色條，進場播放縮放彈跳，
    /// 停留一段時間後自動淡出並銷毀，強度曲線與原地跳出提示共用同一份
    /// WeaponDropFeedbackConfigSO。清單上限的強制移除交由 WeaponDropLogPanel
    /// 直接操作 Transform 子物件數量處理，這裡只負責自己的顯示與自動消失。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WeaponDropLogEntry : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;

        [SerializeField]
        private Image _rarityColorTag;

        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>顯示本次掉落內容並依稀有度強度曲線開始進場動畫與自動消失倒數。已封頂武器轉換成碎片時，
        /// 名稱文字改顯示「轉換為碎片」，跟原地跳出提示共用同一套判斷規則。</summary>
        public void Show(WeaponDataSO weapon, WeaponRarityDisplayConfigSO.RarityStyle style, WeaponDropFeedbackConfigSO.RarityFeedback feedback, WeaponDropOutcome outcome)
        {
            _icon.sprite = weapon.Icon;
            _nameLabel.text = outcome == WeaponDropOutcome.ShardConverted
                ? $"{weapon.WeaponName}（轉換為碎片）"
                : weapon.WeaponName;
            _rarityColorTag.color = style.Color;

            _canvasGroup.alpha = 1f;

            transform.DOKill();
            transform.localScale = Vector3.one * 0.7f;
            transform.DOScale(1f, feedback.PunchDuration).SetEase(Ease.OutBack).SetUpdate(true);

            _canvasGroup.DOFade(0f, feedback.LogFadeDuration)
                .SetDelay(Mathf.Max(0f, feedback.LogLifetime - feedback.LogFadeDuration))
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}
