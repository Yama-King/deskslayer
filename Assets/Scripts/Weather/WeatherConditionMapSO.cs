using System;
using UnityEngine;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// OpenWeatherMap condition id 對照遊戲內部 WeatherCategory 的資料驅動對照表。
    /// 對照規則以區間資料呈現而非寫死在 if/switch 裡，之後更換天氣 API 供應商時，
    /// 理想狀況下只需要調整這份資產（或另外提供一份對應資產），不需要修改任何判斷邏輯。
    /// </summary>
    [CreateAssetMenu(fileName = "WeatherConditionMap", menuName = "DeskSlayer/Weather/Weather Condition Map", order = 2)]
    public sealed class WeatherConditionMapSO : ScriptableObject
    {
        [Serializable]
        public struct ConditionRange
        {
            [SerializeField, Tooltip("此區間的最小 condition id（含）")]
            private int _minId;

            [SerializeField, Tooltip("此區間的最大 condition id（含）")]
            private int _maxId;

            [SerializeField]
            private WeatherCategory _category;

            public ConditionRange(int minId, int maxId, WeatherCategory category)
            {
                _minId = minId;
                _maxId = maxId;
                _category = category;
            }

            public int MinId => _minId;
            public int MaxId => _maxId;
            public WeatherCategory Category => _category;

            public bool Contains(int id) => id >= _minId && id <= _maxId;
        }

        [SerializeField, Tooltip("OpenWeatherMap condition id 對照表，之後更換 API 供應商時只需要調整這份資料")]
        private ConditionRange[] _ranges =
        {
            new ConditionRange(200, 232, WeatherCategory.Thunderstorm),
            new ConditionRange(300, 321, WeatherCategory.Rain),
            new ConditionRange(500, 531, WeatherCategory.Rain),
            new ConditionRange(600, 622, WeatherCategory.Snow),
            new ConditionRange(701, 781, WeatherCategory.Cloudy),
            new ConditionRange(800, 800, WeatherCategory.Clear),
            new ConditionRange(801, 804, WeatherCategory.Cloudy)
        };

        [SerializeField, Tooltip("找不到對應區間時的預設分類")]
        private WeatherCategory _fallbackCategory = WeatherCategory.Cloudy;

        /// <summary>依 OpenWeatherMap condition id 查對照分類，找不到對應區間時回傳 fallback 分類並印出 Warning，不會拋出例外。</summary>
        public WeatherCategory GetCategory(int conditionId)
        {
            foreach (ConditionRange range in _ranges)
            {
                if (range.Contains(conditionId))
                {
                    return range.Category;
                }
            }

            Debug.LogWarning($"[WeatherConditionMapSO] 未涵蓋的天氣 condition id {conditionId}，預設歸類為 {_fallbackCategory}");
            return _fallbackCategory;
        }
    }
}
