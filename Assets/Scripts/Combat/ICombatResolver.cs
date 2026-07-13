namespace DeskSlayer.Combat
{
    /// <summary>
    /// 戰鬥判定介面。W1 階段採數值/機率運算取代即時 Hitbox/Hurtbox 碰撞，
    /// 呼應背景常駐軟體對 CPU 佔用率的嚴格要求。
    /// 抽出介面是為了保留未來替換成即時碰撞判定的彈性，呼叫端（如 CombatDispatcher）
    /// 不需要因為判定方式改變而跟著修改。
    /// 只接收「已經彙整完所有加成」的最終數值，不需要知道攻擊力/防禦力是怎麼算出來的
    /// （武器基礎值？天氣修正？未來的裝備/buff？），數值來源改變時本介面完全不受影響。
    /// </summary>
    public interface ICombatResolver
    {
        /// <summary>計算判定結果。</summary>
        /// <param name="attackPower">呼叫端已彙整完所有加成的最終攻擊力。</param>
        /// <param name="defensePower">呼叫端已彙整完所有加成的最終防禦力。</param>
        CombatResult Resolve(int attackPower, int defensePower);
    }
}
