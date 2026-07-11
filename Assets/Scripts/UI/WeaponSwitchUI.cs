using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeskSlayer.Combat;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 常駐的武器切換按鈕。滑鼠點擊觸發，刻意與全域鍵盤監聽的按鍵輸入分開，
    /// 避免日常打字操作跟武器切換混在一起。W1 階段只驗證「點擊→呼叫 WeaponSwitcher.EquipWeapon→
    /// 顯示更新」的邏輯鏈是否正確，美術與版面留待後續階段。
    /// </summary>
    public sealed class WeaponSwitchUI : MonoBehaviour
    {
        [SerializeField]
        private WeaponSwitcher _weaponSwitcher;

        [SerializeField]
        private Button _lightWeaponButton;

        [SerializeField]
        private Button _heavyWeaponButton;

        [SerializeField]
        private LightWeaponSO _lightWeapon;

        [SerializeField]
        private HeavyWeaponSO _heavyWeapon;

        [SerializeField]
        private TextMeshProUGUI _currentWeaponLabel;

        private void OnEnable()
        {
            _lightWeaponButton.onClick.AddListener(EquipLightWeapon);
            _heavyWeaponButton.onClick.AddListener(EquipHeavyWeapon);
            _weaponSwitcher.OnWeaponEquipped += HandleWeaponEquipped;
        }

        private void OnDisable()
        {
            _lightWeaponButton.onClick.RemoveListener(EquipLightWeapon);
            _heavyWeaponButton.onClick.RemoveListener(EquipHeavyWeapon);
            _weaponSwitcher.OnWeaponEquipped -= HandleWeaponEquipped;
        }

        /// <summary>
        /// 讀取 WeaponSwitcher 目前裝備（跨 GameObject 的初始狀態）刻意放在 Start 而非 OnEnable：
        /// Unity 不保證不同物件間 Awake/OnEnable 的呼叫順序，WeaponSwitcher.Awake() 設定預設武器
        /// 可能晚於這裡的 OnEnable 執行；但 Start 保證所有物件的 Awake/OnEnable 都已跑完，讀到的
        /// CurrentWeapon 才是可靠的初始值。
        /// </summary>
        private void Start()
        {
            RefreshLabel(_weaponSwitcher.CurrentWeapon);
        }

        private void EquipLightWeapon()
        {
            _weaponSwitcher.EquipWeapon(_lightWeapon);
        }

        private void EquipHeavyWeapon()
        {
            _weaponSwitcher.EquipWeapon(_heavyWeapon);
        }

        private void HandleWeaponEquipped(WeaponDataSO weapon)
        {
            RefreshLabel(weapon);
        }

        private void RefreshLabel(WeaponDataSO weapon)
        {
            if (_currentWeaponLabel == null)
            {
                return;
            }

            _currentWeaponLabel.text = weapon != null ? $"目前裝備：{weapon.WeaponName}" : "目前裝備：無";
        }
    }
}
