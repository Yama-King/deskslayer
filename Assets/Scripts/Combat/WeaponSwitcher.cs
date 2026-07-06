using System;
using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 管理玩家目前裝備的單一武器。任何來源（UI 按鈕、未來的拾取/商店系統）都能呼叫
    /// EquipWeapon 切換裝備，不需要認識任何特定武器類型；TypingEnergySystem 只向這裡
    /// 查詢「目前裝備武器」，不再自己持有 light/heavy 兩個獨立欄位。
    /// </summary>
    public sealed class WeaponSwitcher : MonoBehaviour
    {
        [SerializeField, Tooltip("遊戲開始時預設裝備的武器")]
        private WeaponDataSO _defaultWeapon;

        /// <summary>目前裝備武器變更時發出，供 UI 等表現層更新顯示。</summary>
        public event Action<WeaponDataSO> OnWeaponEquipped;

        private WeaponDataSO _currentWeapon;

        /// <summary>目前裝備的武器，遊戲一開始尚未指定預設武器時為 null。</summary>
        public WeaponDataSO CurrentWeapon => _currentWeapon;

        private void Awake()
        {
            if (_defaultWeapon != null)
            {
                _currentWeapon = _defaultWeapon;
            }
        }

        /// <summary>
        /// 切換目前裝備的武器。傳入 null 或與目前裝備相同的武器視為無效操作，不會觸發事件。
        /// </summary>
        public void EquipWeapon(WeaponDataSO weapon)
        {
            if (weapon == null || weapon == _currentWeapon)
            {
                return;
            }

            _currentWeapon = weapon;
            Debug.Log($"[WeaponSwitcher] 裝備武器切換為 {weapon.WeaponName}");
            OnWeaponEquipped?.Invoke(weapon);
        }
    }
}
