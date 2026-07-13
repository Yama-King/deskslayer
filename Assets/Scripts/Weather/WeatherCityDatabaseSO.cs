using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 可選擇的城市清單資料（顯示名稱＋經緯度）。清單本身唯讀，供城市選擇 UI 動態生成項目；
    /// 目前選定的城市索引則透過 SetSelectedCityIndex 寫入，僅供 WeatherService 呼叫
    /// （UI 不應直接修改索引，需透過 WeatherService.SelectCity 才會一併觸發持久化與重新查詢）。
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

        [SerializeField, Min(0), Tooltip("目前選定的城市索引，預設值供尚未讀取到任何存檔偏好時使用")]
        private int _selectedCityIndex;

        /// <summary>唯讀城市清單，供城市選擇 UI 依筆數動態生成項目，不寫死清單長度。</summary>
        public IReadOnlyList<CityEntry> Cities => _cities;

        /// <summary>目前選定的城市索引。</summary>
        public int SelectedCityIndex => _selectedCityIndex;

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

        /// <summary>寫入目前選定的城市索引。僅供 WeatherService 呼叫，索引超出範圍時印出 Warning 並略過。</summary>
        public void SetSelectedCityIndex(int index)
        {
            if (_cities == null || index < 0 || index >= _cities.Length)
            {
                Debug.LogWarning($"[WeatherCityDatabaseSO] 嘗試設定的城市索引 {index} 超出範圍，略過");
                return;
            }

            _selectedCityIndex = index;
        }
    }
}
