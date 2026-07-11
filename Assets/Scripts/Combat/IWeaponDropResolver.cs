namespace DeskSlayer.Combat
{
    /// <summary>
    /// 武器掉落判定介面。比照 ICombatResolver 的介面化模式，把「怎麼決定這次掉什麼」抽成可替換的
    /// 演算法，呼叫端（WeaponDropDispatcher）不需要因為掉落規則改變（例如之後改成保底計數、
    /// 活動加權等）而跟著修改。判定本身不考慮合成等級是否封頂——已封頂的武器仍可被選中，
    /// 由呼叫端（WeaponDropDispatcher）事後判斷是否要把這次重複品轉換成武器碎片，避免玩家所有
    /// 變體都封頂後直接停止掉落、白白浪費掉獎勵節奏。
    /// </summary>
    public interface IWeaponDropResolver
    {
        /// <summary>
        /// 判定一次武器掉落，依序決定家族 → 稀有度 → 變體。
        /// 只有在該家族底下完全沒有任何武器資產（資料尚未建置）時才回傳 null 代表本次無掉落。
        /// </summary>
        WeaponDataSO ResolveDrop(WeaponDatabaseSO database, WeaponDropConfigSO config);
    }
}
