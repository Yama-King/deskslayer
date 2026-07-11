using System.Collections.Generic;
using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 全部武器資產的主索引。掉落判定與背包 UI 都透過這份資產查詢「某家族某稀有度底下有哪些變體」，
    /// 避免使用 Resources.LoadAll 或硬編路徑——新增/移除武器資產時，只需要把資產拖進這裡的清單，
    /// 不需要改動任何程式碼。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "DeskSlayer/Combat/Weapon Database", order = 2)]
    public sealed class WeaponDatabaseSO : ScriptableObject
    {
        [SerializeField, Tooltip("所有武器資產（家族 x 變體 x 稀有度的每一種組合各一份）")]
        private List<WeaponDataSO> _allWeapons = new List<WeaponDataSO>();

        private Dictionary<(WeaponFamily family, WeaponRarity rarity), List<WeaponDataSO>> _variantsLookup;
        private Dictionary<WeaponFamily, List<int>> _variantNumbersLookup;

        /// <summary>所有已登錄的武器資產。</summary>
        public IReadOnlyList<WeaponDataSO> AllWeapons => _allWeapons;

        /// <summary>查詢指定家族、指定稀有度底下的所有變體。找不到對應組合時回傳空清單。</summary>
        public IReadOnlyList<WeaponDataSO> GetVariants(WeaponFamily family, WeaponRarity rarity)
        {
            EnsureLookupBuilt();

            if (_variantsLookup.TryGetValue((family, rarity), out List<WeaponDataSO> variants))
            {
                return variants;
            }

            return System.Array.Empty<WeaponDataSO>();
        }

        /// <summary>查詢指定家族、變體編號、稀有度的單一武器資產，找不到時回傳 null。</summary>
        public WeaponDataSO GetWeapon(WeaponFamily family, int variant, WeaponRarity rarity)
        {
            IReadOnlyList<WeaponDataSO> variants = GetVariants(family, rarity);
            foreach (WeaponDataSO weapon in variants)
            {
                if (weapon.Variant == variant)
                {
                    return weapon;
                }
            }

            return null;
        }

        /// <summary>
        /// 查詢指定家族底下實際存在的變體編號（升冪排序），供背包 UI 動態列出變體入口，
        /// 不需要在 UI 層寫死「固定 5 個變體」。找不到對應家族時回傳空清單。
        /// </summary>
        public IReadOnlyList<int> GetVariantNumbers(WeaponFamily family)
        {
            EnsureVariantNumbersLookupBuilt();

            if (_variantNumbersLookup.TryGetValue(family, out List<int> variants))
            {
                return variants;
            }

            return System.Array.Empty<int>();
        }

        private void OnEnable()
        {
            // 清空快取，確保 Domain Reload 或清單在 Inspector 被改動後，下次查詢會重新建立索引而非沿用舊資料。
            _variantsLookup = null;
            _variantNumbersLookup = null;
        }

        private void EnsureLookupBuilt()
        {
            if (_variantsLookup != null)
            {
                return;
            }

            _variantsLookup = new Dictionary<(WeaponFamily, WeaponRarity), List<WeaponDataSO>>();
            foreach (WeaponDataSO weapon in _allWeapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                var key = (weapon.Family, weapon.Rarity);
                if (!_variantsLookup.TryGetValue(key, out List<WeaponDataSO> variants))
                {
                    variants = new List<WeaponDataSO>();
                    _variantsLookup[key] = variants;
                }

                variants.Add(weapon);
            }
        }

        private void EnsureVariantNumbersLookupBuilt()
        {
            if (_variantNumbersLookup != null)
            {
                return;
            }

            var seen = new Dictionary<WeaponFamily, HashSet<int>>();
            foreach (WeaponDataSO weapon in _allWeapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                if (!seen.TryGetValue(weapon.Family, out HashSet<int> variantSet))
                {
                    variantSet = new HashSet<int>();
                    seen[weapon.Family] = variantSet;
                }

                variantSet.Add(weapon.Variant);
            }

            _variantNumbersLookup = new Dictionary<WeaponFamily, List<int>>();
            foreach (KeyValuePair<WeaponFamily, HashSet<int>> pair in seen)
            {
                List<int> sorted = new List<int>(pair.Value);
                sorted.Sort();
                _variantNumbersLookup[pair.Key] = sorted;
            }
        }
    }
}
