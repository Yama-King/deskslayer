using DeskSlayer.Combat;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 目前裝備武器圖示的統籌者：訂閱 WeaponSwitcher.OnWeaponEquipped，把新裝備武器的圖示轉發給
    /// WeaponIconView 顯示。只負責轉發顯示內容，不參與任何武器切換邏輯，比照 WeatherIconDispatcher
    /// 的統籌者角色。
    /// </summary>
    public sealed class WeaponIconDispatcher : MonoBehaviour
    {
        [SerializeField]
        private WeaponSwitcher _weaponSwitcher;

        [SerializeField]
        private WeaponIconView _iconView;

        private void OnEnable()
        {
            if (_weaponSwitcher == null)
            {
                Debug.LogWarning("[WeaponIconDispatcher] 尚未指派 WeaponSwitcher，武器圖示無法更新");
                return;
            }

            _weaponSwitcher.OnWeaponEquipped += HandleWeaponEquipped;
        }

        private void OnDisable()
        {
            if (_weaponSwitcher != null)
            {
                _weaponSwitcher.OnWeaponEquipped -= HandleWeaponEquipped;
            }
        }

        /// <summary>
        /// 讀取 WeaponSwitcher 目前裝備（跨 GameObject 的初始狀態）刻意放在 Start 而非 OnEnable，
        /// 比照 WeaponSwitchUI 既有的雙重保護做法：Unity 不保證不同物件間 Awake 執行順序，Start
        /// 才能保證 WeaponSwitcher.Awake() 設定的預設武器已經就緒。
        /// </summary>
        private void Start()
        {
            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeapon != null)
            {
                _iconView.SetIcon(_weaponSwitcher.CurrentWeapon.Icon);
            }
        }

        private void HandleWeaponEquipped(WeaponDataSO weapon)
        {
            _iconView.SetIcon(weapon != null ? weapon.Icon : null);
        }
    }
}
