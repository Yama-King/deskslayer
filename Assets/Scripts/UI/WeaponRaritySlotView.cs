using DG.Tweening;
using DeskSlayer.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 單一稀有度列項：顯示該稀有度的擁有狀態與數值，並提供裝備／合成互動。
    /// 未擁有時只顯示鎖定剪影，不洩漏具體數值；已擁有時讀取 WeaponInstance 的加成後數值。
    /// 合成的可互動判定與執行完全交給 WeaponInventoryService，這裡只負責把結果轉譯成畫面文字。
    /// </summary>
    public sealed class WeaponRaritySlotView : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;

        [SerializeField]
        private Image _rarityColorTag;

        [SerializeField]
        private TextMeshProUGUI _rarityLabel;

        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        [SerializeField]
        private TextMeshProUGUI _levelLabel;

        [SerializeField]
        private TextMeshProUGUI _duplicateLabel;

        [SerializeField]
        private TextMeshProUGUI _damageLabel;

        [SerializeField, Tooltip("永久保底武器的額外標籤區塊，僅在 IsPermanentStarter 時顯示")]
        private GameObject _permanentStarterBadge;

        [SerializeField]
        private Button _equipButton;

        [SerializeField]
        private Button _synthesizeButton;

        [SerializeField, Tooltip("無法合成時顯示原因，例如「還需 X 個」或「已達上限」")]
        private TextMeshProUGUI _synthesizeReasonLabel;

        private WeaponDataSO _weapon;
        private WeaponInventoryService _inventoryService;
        private WeaponDropConfigSO _dropConfig;
        private WeaponSwitcher _weaponSwitcher;
        private WeaponRarityDisplayConfigSO _displayConfig;
        private bool _isBound;

        /// <summary>綁定這一列要顯示的稀有度資產，並訂閱背包事件以便即時刷新。</summary>
        public void Bind(
            WeaponDataSO weapon,
            WeaponInventoryService inventoryService,
            WeaponDropConfigSO dropConfig,
            WeaponSwitcher weaponSwitcher,
            WeaponRarityDisplayConfigSO displayConfig)
        {
            _weapon = weapon;
            _inventoryService = inventoryService;
            _dropConfig = dropConfig;
            _weaponSwitcher = weaponSwitcher;
            _displayConfig = displayConfig;

            _equipButton.onClick.AddListener(HandleEquipClicked);
            _synthesizeButton.onClick.AddListener(HandleSynthesizeClicked);
            _inventoryService.OnWeaponObtained += HandleWeaponObtained;
            _inventoryService.OnDuplicateObtained += HandleDuplicateObtained;
            _inventoryService.OnWeaponUpgraded += HandleWeaponUpgraded;
            _isBound = true;

            Refresh();
        }

        private void OnDestroy()
        {
            if (!_isBound)
            {
                return;
            }

            _equipButton.onClick.RemoveListener(HandleEquipClicked);
            _synthesizeButton.onClick.RemoveListener(HandleSynthesizeClicked);
            _inventoryService.OnWeaponObtained -= HandleWeaponObtained;
            _inventoryService.OnDuplicateObtained -= HandleDuplicateObtained;
            _inventoryService.OnWeaponUpgraded -= HandleWeaponUpgraded;
        }

        private void HandleWeaponObtained(WeaponInstance instance)
        {
            if (instance.Data == _weapon)
            {
                Refresh();
            }
        }

        private void HandleDuplicateObtained(WeaponDataSO weapon, int _)
        {
            if (weapon == _weapon)
            {
                Refresh();
            }
        }

        private void HandleWeaponUpgraded(WeaponInstance instance)
        {
            if (instance.Data != _weapon)
            {
                return;
            }

            Refresh();
            PlaySynthesizeFeedback();
        }

        private void HandleEquipClicked()
        {
            _weaponSwitcher.EquipWeapon(_weapon);
        }

        private void HandleSynthesizeClicked()
        {
            _inventoryService.TryUpgrade(_weapon);
        }

        private void Refresh()
        {
            WeaponRarityDisplayConfigSO.RarityStyle style = _displayConfig.GetStyle(_weapon.Rarity);
            _rarityLabel.text = style.DisplayName;

            if (_rarityColorTag != null)
            {
                _rarityColorTag.color = style.Color;
            }

            if (_permanentStarterBadge != null)
            {
                _permanentStarterBadge.SetActive(_weapon.IsPermanentStarter);
            }

            bool owned = _inventoryService.IsOwned(_weapon);
            if (!owned)
            {
                RefreshLocked();
                return;
            }

            RefreshOwned();
        }

        private void RefreshLocked()
        {
            _nameLabel.text = "???";
            _icon.sprite = _displayConfig.LockedIcon;
            _icon.color = Color.gray;
            _levelLabel.text = string.Empty;
            _duplicateLabel.text = string.Empty;
            _damageLabel.text = string.Empty;

            _equipButton.interactable = false;
            _synthesizeButton.interactable = false;
            if (_synthesizeReasonLabel != null)
            {
                _synthesizeReasonLabel.text = "尚未擁有";
            }
        }

        private void RefreshOwned()
        {
            WeaponInstance instance = _inventoryService.GetOwnedInstance(_weapon);

            _nameLabel.text = _weapon.WeaponName;
            _icon.sprite = _weapon.Icon;
            _icon.color = Color.white;
            _levelLabel.text = $"Lv.{instance.UpgradeLevel}";

            int duplicateCount = _inventoryService.GetDuplicateCount(_weapon);
            _duplicateLabel.text = $"重複 x{duplicateCount}";
            _damageLabel.text = $"傷害 {Mathf.RoundToInt(_weapon.BaseDamage * instance.DamageMultiplier)}";

            _equipButton.interactable = true;
            RefreshSynthesisState(instance, duplicateCount);
        }

        private void RefreshSynthesisState(WeaponInstance instance, int duplicateCount)
        {
            if (instance.IsAtMaxUpgradeLevel)
            {
                _synthesizeButton.interactable = false;
                if (_synthesizeReasonLabel != null)
                {
                    _synthesizeReasonLabel.text = "已達上限";
                }

                return;
            }

            int fusionCost = _dropConfig.GetFusionCost(_weapon.Rarity);
            int missing = fusionCost - duplicateCount;
            if (missing > 0)
            {
                _synthesizeButton.interactable = false;
                if (_synthesizeReasonLabel != null)
                {
                    _synthesizeReasonLabel.text = $"還需 {missing} 個";
                }

                return;
            }

            _synthesizeButton.interactable = true;
            if (_synthesizeReasonLabel != null)
            {
                _synthesizeReasonLabel.text = string.Empty;
            }
        }

        private void PlaySynthesizeFeedback()
        {
            RectTransform levelTransform = _levelLabel.rectTransform;
            levelTransform.DOKill();
            levelTransform.localScale = Vector3.one * 1.3f;
            levelTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
    }
}
