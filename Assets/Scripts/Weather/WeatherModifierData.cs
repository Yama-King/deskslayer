using UnityEngine;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 單一天氣分類對應的數值資料，由 WeatherModifierDatabaseSO 查詢後回傳。
    /// 使用 readonly struct 避免額外配置，供下個切片的戰鬥銜接（攻擊力/防禦力修正）與 UI（Icon/色調）使用。
    /// </summary>
    public readonly struct WeatherModifierData
    {
        /// <summary>本次資料對應的天氣分類。</summary>
        public readonly WeatherCategory Category;

        /// <summary>攻擊力修正百分比，例如 -15 代表 -15%。</summary>
        public readonly float AttackModifierPercent;

        /// <summary>防禦力修正百分比。</summary>
        public readonly float DefenseModifierPercent;

        /// <summary>UI 顯示用圖示，W1 階段可能為 null，供後續切片使用。</summary>
        public readonly Sprite Icon;

        /// <summary>UI 顯示用色調，供後續切片使用。</summary>
        public readonly Color TintColor;

        public WeatherModifierData(WeatherCategory category, float attackModifierPercent, float defenseModifierPercent, Sprite icon, Color tintColor)
        {
            Category = category;
            AttackModifierPercent = attackModifierPercent;
            DefenseModifierPercent = defenseModifierPercent;
            Icon = icon;
            TintColor = tintColor;
        }
    }
}
