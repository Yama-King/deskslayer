using System;
using System.Collections.Generic;
using DeskSlayer.Weather;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 城市選擇清單：依 WeatherCityDatabaseSO 目前的城市筆數動態生成 WeatherCityEntryView
    /// （筆數由資料庫決定，不寫死清單長度），並在選取項目改變時更新高亮標示。
    /// 只負責顯示與收集玩家輸入，實際切換城市、查詢天氣、寫入偏好設定一律交給
    /// WeatherCitySelectionController 呼叫 WeatherService 處理。
    /// </summary>
    public sealed class WeatherCityListView : MonoBehaviour
    {
        [SerializeField]
        private WeatherCityDatabaseSO _database;

        [SerializeField]
        private Transform _contentParent;

        [SerializeField]
        private WeatherCityEntryView _entryPrefab;

        /// <summary>清單中某個城市項目被選取時發出（帶入該城市在資料庫中的索引）。</summary>
        public event Action<int> OnCitySelected;

        private readonly List<WeatherCityEntryView> _spawnedEntries = new List<WeatherCityEntryView>();
        private int _selectedIndex;

        /// <summary>依資料庫目前的城市清單重新生成項目，並標示指定索引為選取狀態。</summary>
        public void Populate(int selectedIndex)
        {
            ClearEntries();
            _selectedIndex = selectedIndex;

            if (_database == null)
            {
                Debug.LogWarning("[WeatherCityListView] 尚未指派城市資料庫，無法生成城市清單");
                return;
            }

            IReadOnlyList<WeatherCityDatabaseSO.CityEntry> cities = _database.Cities;
            for (int i = 0; i < cities.Count; i++)
            {
                WeatherCityEntryView entry = Instantiate(_entryPrefab, _contentParent);
                entry.Bind(cities[i].DisplayName, i == selectedIndex);

                int capturedIndex = i;
                entry.OnClicked += () => OnCitySelected?.Invoke(capturedIndex);

                _spawnedEntries.Add(entry);
            }
        }

        /// <summary>更新目前選取的城市索引，只切換高亮標示，不重新生成整份清單。</summary>
        public void SetSelectedIndex(int index)
        {
            _selectedIndex = index;

            for (int i = 0; i < _spawnedEntries.Count; i++)
            {
                _spawnedEntries[i].SetSelected(i == _selectedIndex);
            }
        }

        private void ClearEntries()
        {
            foreach (WeatherCityEntryView entry in _spawnedEntries)
            {
                if (entry != null)
                {
                    Destroy(entry.gameObject);
                }
            }

            _spawnedEntries.Clear();
        }
    }
}
