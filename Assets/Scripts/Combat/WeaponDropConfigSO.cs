using System;
using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 武器掉落與合成的可調數值設定檔。所有數字皆可直接在 Inspector 調整，不需要改程式碼，
    /// 供之後平衡數值微調使用。掉落判定的演算法本身在 IWeaponDropResolver，這裡只提供數值。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDropConfig", menuName = "DeskSlayer/Combat/Weapon Drop Config", order = 3)]
    public sealed class WeaponDropConfigSO : ScriptableObject
    {
        [Serializable]
        public struct RarityConfig
        {
            [SerializeField]
            private WeaponRarity _rarity;

            [SerializeField, Min(0f), Tooltip("此稀有度的基礎掉落權重，數字越大越常掉落")]
            private float _dropWeight;

            [SerializeField, Min(1), Tooltip("此稀有度合成升一級需要消耗的重複品數量")]
            private int _fusionCost;

            public RarityConfig(WeaponRarity rarity, float dropWeight, int fusionCost)
            {
                _rarity = rarity;
                _dropWeight = dropWeight;
                _fusionCost = fusionCost;
            }

            public WeaponRarity Rarity => _rarity;
            public float DropWeight => _dropWeight;
            public int FusionCost => _fusionCost;
        }

        [Serializable]
        public struct FamilyConfig
        {
            [SerializeField]
            private WeaponFamily _family;

            [SerializeField, Min(0f), Tooltip("此家族被選中的權重，數字越大越常掉落")]
            private float _weight;

            public FamilyConfig(WeaponFamily family, float weight)
            {
                _family = family;
                _weight = weight;
            }

            public WeaponFamily Family => _family;
            public float Weight => _weight;
        }

        [SerializeField, Tooltip("各稀有度的掉落權重與合成消耗數量")]
        private RarityConfig[] _rarityConfigs =
        {
            new RarityConfig(WeaponRarity.Common, 80f, 3),
            new RarityConfig(WeaponRarity.Rare, 15f, 5),
            new RarityConfig(WeaponRarity.Epic, 4f, 8),
            new RarityConfig(WeaponRarity.Legendary, 1f, 10)
        };

        [SerializeField, Tooltip("各家族被選中的權重")]
        private FamilyConfig[] _familyConfigs =
        {
            new FamilyConfig(WeaponFamily.Dagger, 1f),
            new FamilyConfig(WeaponFamily.Greatsword, 1f)
        };

        /// <summary>查詢指定稀有度的基礎掉落權重，找不到對應設定時回傳 0（視為不會掉落）。</summary>
        public float GetRarityDropWeight(WeaponRarity rarity)
        {
            foreach (RarityConfig config in _rarityConfigs)
            {
                if (config.Rarity == rarity)
                {
                    return config.DropWeight;
                }
            }

            return 0f;
        }

        /// <summary>查詢指定稀有度合成升一級所需的重複品數量，找不到對應設定時回傳預設值 1。</summary>
        public int GetFusionCost(WeaponRarity rarity)
        {
            foreach (RarityConfig config in _rarityConfigs)
            {
                if (config.Rarity == rarity)
                {
                    return config.FusionCost;
                }
            }

            return 1;
        }

        /// <summary>查詢指定家族被選中的權重，找不到對應設定時回傳 0（視為不會掉落）。</summary>
        public float GetFamilyWeight(WeaponFamily family)
        {
            foreach (FamilyConfig config in _familyConfigs)
            {
                if (config.Family == family)
                {
                    return config.Weight;
                }
            }

            return 0f;
        }

        private void Reset()
        {
            _rarityConfigs = new[]
            {
                new RarityConfig(WeaponRarity.Common, 80f, 3),
                new RarityConfig(WeaponRarity.Rare, 15f, 5),
                new RarityConfig(WeaponRarity.Epic, 4f, 8),
                new RarityConfig(WeaponRarity.Legendary, 1f, 10)
            };

            _familyConfigs = new[]
            {
                new FamilyConfig(WeaponFamily.Dagger, 1f),
                new FamilyConfig(WeaponFamily.Greatsword, 1f)
            };
        }
    }
}
