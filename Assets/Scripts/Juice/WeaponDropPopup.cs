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

        [SerializeField, Tooltip("往上飄移的總距離（世界座標單位）")]
        private float _floatDistance = 1.1f;

        [SerializeField, Tooltip("顯示動畫的總時長（秒），淡出在時長尾端播放")]
        private float _duration = 0.8f;

        [SerializeField, Tooltip("淡出所需時間（秒），需小於等於總時長")]
        private float _fadeDuration = 0.35f;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>顯示本次掉落內容並開始進場＋飄移＋淡出動畫。</summary>
        public void Show(WeaponDataSO weapon, WeaponRarityDisplayConfigSO.RarityStyle style)
        {
            _icon.sprite = weapon.Icon;
            _nameLabel.text = weapon.WeaponName;
            _rarityFrame.color = style.Color;

            _canvasGroup.alpha = 1f;

            _contentRoot.DOKill();
            _contentRoot.localScale = Vector3.one * 0.6f;
            _contentRoot.DOScale(1f, _duration * 0.4f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            transform.DOMoveY(transform.position.y + _floatDistance, _duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

            _canvasGroup.DOFade(0f, _fadeDuration)
                .SetDelay(Mathf.Max(0f, _duration - _fadeDuration))
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}
