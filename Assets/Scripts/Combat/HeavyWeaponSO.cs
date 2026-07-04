using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 重武器數據。累積按鍵次數達到 _keyPressThreshold 時觸發一次重攻擊。
    /// </summary>
    [CreateAssetMenu(fileName = "NewHeavyWeapon", menuName = "DeskSlayer/Combat/Heavy Weapon", order = 1)]
    public sealed class HeavyWeaponSO : WeaponDataSO
    {
        [SerializeField, Min(1), Tooltip("累積多少次按鍵才觸發一次重攻擊")]
        private int _keyPressThreshold = 20;

        /// <summary>觸發重攻擊所需的累積按鍵次數。</summary>
        public int KeyPressThreshold => _keyPressThreshold;
    }
}
