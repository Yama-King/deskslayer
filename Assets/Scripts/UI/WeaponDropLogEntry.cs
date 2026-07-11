using DG.Tweening;
using DeskSlayer.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 掉落記錄清單中的單一列項：顯示圖示＋名稱＋稀有度色條，進場播放縮放彈跳，
    /// 停留一段時間後自動淡出並銷毀。清單上限的強制移除交由 WeaponDropLogPanel
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

        [SerializeField, Tooltip("進場縮放彈跳的時長（秒）")]
        private float _punchDuration = 0.25f;

        [SerializeField, Tooltip("淡出前的停留總時長（秒），含淡出時間")]
        private float _lifetime = 3f;

        [SerializeField, Tooltip("淡出所需時間（秒），需小於等於停留總時長")]
        private float _fadeDuration = 0.4f;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>顯示本次掉落內容並開始進場動畫與自動消失倒數。</summary>
        public void Show(WeaponDataSO weapon, WeaponRarityDisplayConfigSO.RarityStyle style)
        {
            _icon.sprite = weapon.Icon;
            _nameLabel.text = weapon.WeaponName;
            _rarityColorTag.color = style.Color;

            _canvasGroup.alpha = 1f;

            transform.DOKill();
            transform.localScale = Vector3.one * 0.7f;
            transform.DOScale(1f, _punchDuration).SetEase(Ease.OutBack).SetUpdate(true);

            _canvasGroup.DOFade(0f, _fadeDuration)
                .SetDelay(Mathf.Max(0f, _lifetime - _fadeDuration))
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}
