using System;
using UnityEngine;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 各天氣分類對應的可調數值資料。比照 WeaponDropConfigSO 的作法，把數值集中放在一份資產裡，
    /// 平衡調整（含未來的 Icon／色調視覺欄位）都在 Inspector 完成，不需要改程式碼或重新編譯。
    /// </summary>
    [CreateAssetMenu(fileName = "WeatherModifierDatabase", menuName = "DeskSlayer/Weather/Weather Modifier Database", order = 0)]
    public sealed class WeatherModifierDatabaseSO : ScriptableObject
    {
        [Serializable]
        public struct ModifierEntry
        {
            [SerializeField]
            private WeatherCategory _category;

            [SerializeField, Tooltip("攻擊力修正百分比，例如 -15 代表 -15%")]
            private float _attackModifierPercent;

            [SerializeField, Tooltip("防禦力修正百分比")]
            private float _defenseModifierPercent;

            [SerializeField, Tooltip("UI 顯示用圖示，W1 階段可留空，供後續切片使用")]
            private Sprite _icon;

            [SerializeField, Tooltip("UI 顯示用色調，供後續切片使用")]
            private Color _tintColor;

            public ModifierEntry(WeatherCategory category, float attackModifierPercent, float defenseModifierPercent)
            {
                _category = category;
                _attackModifierPercent = attackModifierPercent;
                _defenseModifierPercent = defenseModifierPercent;
                _icon = null;
                _tintColor = Color.white;
            }

            public WeatherCategory Category => _category;
            public float AttackModifierPercent => _attackModifierPercent;
            public float DefenseModifierPercent => _defenseModifierPercent;
            public Sprite Icon => _icon;
            public Color TintColor => _tintColor;
        }

        [SerializeField, Tooltip("各天氣分類對應的數值與視覺呈現資料")]
        private ModifierEntry[] _entries =
        {
            new ModifierEntry(WeatherCategory.Clear, -15f, -15f),
            new ModifierEntry(WeatherCategory.Cloudy, 0f, 0f),
            new ModifierEntry(WeatherCategory.Rain, 10f, 5f),
            new ModifierEntry(WeatherCategory.Thunderstorm, 20f, 15f),
            new ModifierEntry(WeatherCategory.Snow, 35f, 25f)
        };

        /// <summary>查詢指定天氣分類的數值資料，找不到對應設定時回傳中性數值（0% / 0%）並印出 Warning。</summary>
        public WeatherModifierData GetModifier(WeatherCategory category)
        {
            foreach (ModifierEntry entry in _entries)
            {
                if (entry.Category == category)
                {
                    return new WeatherModifierData(entry.Category, entry.AttackModifierPercent, entry.DefenseModifierPercent, entry.Icon, entry.TintColor);
                }
            }

            Debug.LogWarning($"[WeatherModifierDatabaseSO] 找不到天氣分類 {category} 對應的數值設定，改用中性數值");
            return new WeatherModifierData(category, 0f, 0f, null, Color.white);
        }

        private void Reset()
        {
            _entries = new[]
            {
                new ModifierEntry(WeatherCategory.Clear, -15f, -15f),
                new ModifierEntry(WeatherCategory.Cloudy, 0f, 0f),
                new ModifierEntry(WeatherCategory.Rain, 10f, 5f),
                new ModifierEntry(WeatherCategory.Thunderstorm, 20f, 15f),
                new ModifierEntry(WeatherCategory.Snow, 35f, 25f)
            };
        }
    }
}
