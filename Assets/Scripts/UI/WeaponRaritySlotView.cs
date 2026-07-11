using DeskSlayer.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 單一稀有度列項。目前僅完成綁定與基本名稱顯示，資料完整呈現（擁有/鎖定狀態、數值）
    /// 與裝備/合成互動留待後續切片實作。
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

        /// <summary>綁定這一列要顯示的稀有度資產。</summary>
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

            _nameLabel.text = weapon.WeaponName;
            _equipButton.interactable = false;
            _synthesizeButton.interactable = false;
        }
    }
}
