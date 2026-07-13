using System;
using UnityEngine;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 可選擇的城市清單資料（顯示名稱＋經緯度）。城市選擇 UI 是後續切片的範圍，
    /// 本切片先固定使用 _selectedCityIndex 指定的城市，供之後接上 UI 時直接沿用同一份資產。
    /// </summary>
    [CreateAssetMenu(fileName = "WeatherCityDatabase", menuName = "DeskSlayer/Weather/Weather City Database", order = 1)]
    public sealed class WeatherCityDatabaseSO : ScriptableObject
    {
        [Serializable]
        public struct CityEntry
        {
            [SerializeField]
            private string _displayName;

            [SerializeField]
            private float _latitude;

            [SerializeField]
            private float _longitude;

            public CityEntry(string displayName, float latitude, float longitude)
            {
                _displayName = displayName;
                _latitude = latitude;
                _longitude = longitude;
            }

            public string DisplayName => _displayName;
            public float Latitude => _latitude;
            public float Longitude => _longitude;
        }

        [SerializeField, Tooltip("可選擇的城市清單（顯示名稱＋經緯度），城市選擇 UI 為後續切片範圍")]
        private CityEntry[] _cities =
        {
            new CityEntry("台北市", 25.0478f, 121.5319f),
            new CityEntry("台中市", 24.1477f, 120.6736f),
            new CityEntry("高雄市", 22.6273f, 120.3014f),
            new CityEntry("花蓮縣", 23.9871f, 121.6015f),
            new CityEntry("斗六市", 23.7092f, 120.5433f)
        };

        [SerializeField, Min(0), Tooltip("W1 階段固定使用的城市索引，城市選擇 UI 完成前僅能在此手動調整")]
        private int _selectedCityIndex;

        /// <summary>目前固定選用的城市。索引超出範圍時回傳清單第一筆並印出 Warning，清單為空時回傳預設值並印出 Warning。</summary>
        public CityEntry CurrentCity
        {
            get
            {
                if (_cities == null || _cities.Length == 0)
                {
                    Debug.LogWarning("[WeatherCityDatabaseSO] 城市清單為空，無法提供任何城市資料");
                    return default;
                }

                if (_selectedCityIndex < 0 || _selectedCityIndex >= _cities.Length)
                {
                    Debug.LogWarning($"[WeatherCityDatabaseSO] 選定的城市索引 {_selectedCityIndex} 超出範圍，改用清單第一筆");
                    return _cities[0];
                }

                return _cities[_selectedCityIndex];
            }
        }
    }
}
