namespace DeskSlayer.Combat
{
    /// <summary>
    /// 玩家實際持有的武器個體：指向對應 WeaponDataSO 的參照 + 目前合成等級。
    /// 比照 IFrameGuard 的設計哲學，刻意獨立成一般 C# 類別（非 MonoBehaviour、非 ScriptableObject），
    /// 職責單一，只負責記錄與推進「這一份持有物」的合成進度，不認識背包、掉落等其他系統。
    /// </summary>
    public sealed class WeaponInstance
    {
        private readonly WeaponDataSO _data;
        private int _upgradeLevel;

        public WeaponInstance(WeaponDataSO data, int upgradeLevel = 0)
        {
            _data = data;
            _upgradeLevel = upgradeLevel;
        }

        /// <summary>指向的武器數據資產。</summary>
        public WeaponDataSO Data => _data;

        /// <summary>目前合成等級，0 代表尚未合成過。</summary>
        public int UpgradeLevel => _upgradeLevel;

        /// <summary>目前是否已達最大合成等級，達上限後不可再合成。</summary>
        public bool IsAtMaxUpgradeLevel => _upgradeLevel >= _data.MaxUpgradeLevel;

        /// <summary>目前合成等級對應的傷害加成倍率，直接讀取 WeaponDataSO 的遞減收益曲線計算結果。</summary>
        public float DamageMultiplier => _data.GetUpgradeDamageMultiplier(_upgradeLevel);

        /// <summary>
        /// 合成升一級。呼叫端（WeaponInventoryService）需自行完成「重複品數量是否足夠」的資格判定與消耗，
        /// 這裡只單純負責等級推進，已達上限時安全地不做任何事。
        /// </summary>
        public void LevelUp()
        {
            if (IsAtMaxUpgradeLevel)
            {
                return;
            }

            _upgradeLevel++;
        }
    }
}
