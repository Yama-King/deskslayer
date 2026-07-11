using DG.Tweening;
using DeskSlayer.Combat;
using DeskSlayer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.Juice
{
    /// <summary>
    /// 掉落提示：在敵人死亡位置顯示一次掉落武器的圖示＋名稱＋稀有度色框，用 DOTween 做
    /// 「進場縮放彈跳＋往上飄移＋淡出」，動畫結束後自行銷毀。比照 DamagePopup 的作法採
    /// World Space Canvas，讓稀有度色框／圖示／文字可以用標準 UI 元件排版，同時沿用
    /// CanvasGroup.DOFade（WeaponInventoryPanelController 已驗證過的 DOTween UI 模組用法）。
    /// 尺寸／飄移距離／時長皆由 WeaponDropFeedbackConfigSO 依稀有度決定，這裡不寫死數值。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WeaponDropPopup : MonoBehaviour
    {
        [SerializeField, Tooltip("縮放彈跳＋飄移動畫套用的內容根節點")]
        private RectTransform _contentRoot;

        [SerializeField]
        private Image _rarityFrame;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>顯示本次掉落內容並依稀有度強度曲線開始進場＋飄移＋淡出動畫。已封頂武器轉換成碎片時，
        /// 名稱文字改顯示「轉換為碎片」，讓玩家清楚知道這次重複品沒有被浪費掉，動畫本身不做任何區分。</summary>
        public void Show(WeaponDataSO weapon, WeaponRarityDisplayConfigSO.RarityStyle style, WeaponDropFeedbackConfigSO.RarityFeedback feedback, WeaponDropOutcome outcome)
        {
            _icon.sprite = weapon.Icon;
            _nameLabel.text = outcome == WeaponDropOutcome.ShardConverted
                ? $"{weapon.WeaponName}（轉換為碎片）"
                : weapon.WeaponName;
            _rarityFrame.color = style.Color;

            _canvasGroup.alpha = 1f;

            _contentRoot.DOKill();
            _contentRoot.localScale = Vector3.one * feedback.ContentScale * 0.6f;
            _contentRoot.DOScale(feedback.ContentScale, feedback.PunchDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            transform.DOMoveY(transform.position.y + feedback.FloatDistance, feedback.DisplayDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

            _canvasGroup.DOFade(0f, feedback.FadeDuration)
                .SetDelay(Mathf.Max(0f, feedback.DisplayDuration - feedback.FadeDuration))
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}
