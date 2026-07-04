using UnityEngine;
using DeskSlayer.Enemy;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 預設的數值/機率戰鬥判定實作。
    /// 命中率 = 攻擊力 / (攻擊力 + 防禦力)，clamp 在 [MinHitChance, MaxHitChance]
    /// 避免出現必中或必不中的極端情況；命中傷害 = max(1, 攻擊力 - 防禦力)，確保命中至少造成 1 點傷害。
    /// </summary>
    public sealed class DefaultCombatResolver : ICombatResolver
    {
        private const float MinHitChance = 0.3f;
        private const float MaxHitChance = 0.95f;

        public CombatResult Resolve(WeaponDataSO weapon, EnemyDataSO enemy)
        {
            int attack = weapon.BaseDamage;
            int defense = enemy.Defense;

            float rawHitChance = (float)attack / (attack + defense);
            float hitChance = Mathf.Clamp(rawHitChance, MinHitChance, MaxHitChance);

            if (Random.value > hitChance)
            {
                return new CombatResult(false, 0);
            }

            int damage = Mathf.Max(1, attack - defense);
            return new CombatResult(true, damage);
        }
    }
}
