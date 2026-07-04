namespace DeskSlayer.Combat
{
    /// <summary>
    /// 單次攻擊判定的結果，由 ICombatResolver 計算後回傳給呼叫端套用傷害。
    /// 使用 readonly struct 避免額外配置，因為判定會隨打字頻率高頻觸發。
    /// </summary>
    public readonly struct CombatResult
    {
        /// <summary>本次攻擊是否命中。</summary>
        public readonly bool IsHit;

        /// <summary>命中時造成的傷害，未命中固定為 0。</summary>
        public readonly int Damage;

        public CombatResult(bool isHit, int damage)
        {
            IsHit = isHit;
            Damage = damage;
        }
    }
}
