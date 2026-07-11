using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 預設的轉蛋式掉落判定實作：先依家族權重選家族，再依稀有度權重選稀有度，最後在該稀有度底下
    /// 隨機選一個變體。某稀有度底下「所有變體」都已合成封頂時，直接把該稀有度從權重池排除（權重視為 0），
    /// 剩餘稀有度自動瓜分機率，不需要手動重新分配百分比。
    /// </summary>
    public sealed class DefaultWeaponDropResolver : IWeaponDropResolver
    {
        private static readonly WeaponFamily[] AllFamilies = (WeaponFamily[])Enum.GetValues(typeof(WeaponFamily));
        private static readonly WeaponRarity[] AllRarities = (WeaponRarity[])Enum.GetValues(typeof(WeaponRarity));

        public WeaponDataSO ResolveDrop(WeaponDatabaseSO database, WeaponDropConfigSO config, IWeaponUpgradeLevelProvider upgradeLevels)
        {
            WeaponFamily family = PickFamily(config);
            WeaponRarity? rarity = PickRarity(database, config, upgradeLevels, family);
            if (rarity == null)
            {
                return null;
            }

            return PickVariant(database, upgradeLevels, family, rarity.Value);
        }

        private static WeaponFamily PickFamily(WeaponDropConfigSO config)
        {
            return WeightedPick(AllFamilies, config.GetFamilyWeight);
        }

        /// <summary>依權重選一個稀有度，排除該家族底下「沒有任何變體資產」或「所有變體皆已封頂」的稀有度。</summary>
        private static WeaponRarity? PickRarity(WeaponDatabaseSO database, WeaponDropConfigSO config, IWeaponUpgradeLevelProvider upgradeLevels, WeaponFamily family)
        {
            List<WeaponRarity> eligibleRarities = new List<WeaponRarity>(AllRarities.Length);

            foreach (WeaponRarity rarity in AllRarities)
            {
                if (config.GetRarityDropWeight(rarity) <= 0f)
                {
                    continue;
                }

                IReadOnlyList<WeaponDataSO> variants = database.GetVariants(family, rarity);
                if (variants.Count == 0 || IsFullyCapped(variants, upgradeLevels))
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

        private static WeaponDataSO PickVariant(WeaponDatabaseSO database, IWeaponUpgradeLevelProvider upgradeLevels, WeaponFamily family, WeaponRarity rarity)
        {
            IReadOnlyList<WeaponDataSO> variants = database.GetVariants(family, rarity);
            List<WeaponDataSO> availableVariants = new List<WeaponDataSO>(variants.Count);

            foreach (WeaponDataSO variant in variants)
            {
                if (upgradeLevels.GetUpgradeLevel(variant) < variant.MaxUpgradeLevel)
                {
                    availableVariants.Add(variant);
                }
            }

            if (availableVariants.Count == 0)
            {
                return null;
            }

            return availableVariants[UnityEngine.Random.Range(0, availableVariants.Count)];
        }

        private static bool IsFullyCapped(IReadOnlyList<WeaponDataSO> variants, IWeaponUpgradeLevelProvider upgradeLevels)
        {
            foreach (WeaponDataSO variant in variants)
            {
                if (upgradeLevels.GetUpgradeLevel(variant) < variant.MaxUpgradeLevel)
                {
                    return false;
                }
            }

            return true;
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
