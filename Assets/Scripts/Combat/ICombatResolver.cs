using DeskSlayer.Enemy;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 戰鬥判定介面。W1 階段採數值/機率運算取代即時 Hitbox/Hurtbox 碰撞，
    /// 呼應背景常駐軟體對 CPU 佔用率的嚴格要求。
    /// 抽出介面是為了保留未來替換成即時碰撞判定的彈性，呼叫端（如 CombatDispatcher）
    /// 不需要因為判定方式改變而跟著修改。
    /// </summary>
    public interface ICombatResolver
    {
        /// <summary>計算武器攻擊敵人的判定結果。</summary>
        /// <param name="weapon">發動攻擊的武器數據。</param>
        /// <param name="enemy">受擊敵人的數據。</param>
        CombatResult Resolve(WeaponDataSO weapon, EnemyDataSO enemy);
    }
}
