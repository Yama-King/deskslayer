using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 預設的轉蛋式掉落判定實作：先依家族權重選家族，再依稀有度權重選稀有度，最後在該稀有度底下
    /// 隨機選一個變體。刻意不依合成等級排除任何稀有度/變體——即使某把武器已封頂，仍可能被抽中，
    /// 由 WeaponDropDispatcher 事後判斷該次重複品要轉換成武器碎片還是合成材料，避免整個資料庫
    /// 封頂後直接停止掉落。
    /// </summary>
    public sealed class DefaultWeaponDropResolver : IWeaponDropResolver
    {
        private static readonly WeaponFamily[] AllFamilies = (WeaponFamily[])Enum.GetValues(typeof(WeaponFamily));
        private static readonly WeaponRarity[] AllRarities = (WeaponRarity[])Enum.GetValues(typeof(WeaponRarity));

        public WeaponDataSO ResolveDrop(WeaponDatabaseSO database, WeaponDropConfigSO config)
        {
            WeaponFamily family = PickFamily(config);
            WeaponRarity? rarity = PickRarity(database, config, family);
            if (rarity == null)
            {
                return null;
            }

            return PickVariant(database, family, rarity.Value);
        }

        private static WeaponFamily PickFamily(WeaponDropConfigSO config)
        {
            return WeightedPick(AllFamilies, config.GetFamilyWeight);
        }

        /// <summary>依權重選一個稀有度，只排除該家族底下「沒有任何變體資產」的稀有度（資料尚未建置）。</summary>
        private static WeaponRarity? PickRarity(WeaponDatabaseSO database, WeaponDropConfigSO config, WeaponFamily family)
        {
            List<WeaponRarity> eligibleRarities = new List<WeaponRarity>(AllRarities.Length);

            foreach (WeaponRarity rarity in AllRarities)
            {
                if (config.GetRarityDropWeight(rarity) <= 0f)
                {
                    continue;
                }

                if (database.GetVariants(family, rarity).Count == 0)
                {
                    continue;
                }

                eligibleRarities.Add(rarity);
            }

            if (eligibleRarities.Count == 0)
            {
                return null;
            }

            return WeightedPick(eligibleRarities.ToArray(), config.GetRarityDropWeight);
        }

        private static WeaponDataSO PickVariant(WeaponDatabaseSO database, WeaponFamily family, WeaponRarity rarity)
        {
            IReadOnlyList<WeaponDataSO> variants = database.GetVariants(family, rarity);
            if (variants.Count == 0)
            {
                return null;
            }

            return variants[UnityEngine.Random.Range(0, variants.Count)];
        }

        /// <summary>依權重從候選清單中抽一個項目，權重總和為 0 時退回均勻隨機，避免除以 0。</summary>
        private static T WeightedPick<T>(T[] candidates, Func<T, float> weightSelector)
        {
            float totalWeight = 0f;
            foreach (T candidate in candidates)
            {
                totalWeight += Mathf.Max(0f, weightSelector(candidate));
            }

            if (totalWeight <= 0f)
            {
                return candidates[UnityEngine.Random.Range(0, candidates.Length)];
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (T candidate in candidates)
            {
                cumulative += Mathf.Max(0f, weightSelector(candidate));
                if (roll <= cumulative)
                {
                    return candidate;
                }
            }

            return candidates[candidates.Length - 1];
        }
    }
}
