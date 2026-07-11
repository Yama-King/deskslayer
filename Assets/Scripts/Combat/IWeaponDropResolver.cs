namespace DeskSlayer.Combat
{
    /// <summary>
    /// 武器掉落判定介面。比照 ICombatResolver 的介面化模式，把「怎麼決定這次掉什麼」抽成可替換的
    /// 演算法，呼叫端（WeaponDropDispatcher）不需要因為掉落規則改變（例如之後改成保底計數、
    /// 活動加權等）而跟著修改。
    /// </summary>
    public interface IWeaponDropResolver
    {
        /// <summary>
        /// 判定一次武器掉落，依序決定家族 → 稀有度 → 變體。
        /// 若所有稀有度底下的變體皆已封頂（極端情況：整個資料庫都合成滿級），回傳 null 代表本次無掉落。
        /// </summary>
        WeaponDataSO ResolveDrop(WeaponDatabaseSO database, WeaponDropConfigSO config, IWeaponUpgradeLevelProvider upgradeLevels);
    }
}
