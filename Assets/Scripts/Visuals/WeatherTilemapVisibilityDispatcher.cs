using System;
using UnityEngine;
using DeskSlayer.Weather;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱 WeatherService.OnWeatherChanged，依目前天氣分類切換 Rain/Snow/Thunderstorm
    /// Tilemap 的顯示狀態。每個分類對應唯一一張 Tilemap，同一時間最多顯示一張，其餘（含未列於
    /// _entries 的分類，例如 Clear／Cloudy）一律隱藏。不判斷天氣分類的邏輯本身——分類判斷仍完全
    /// 交給 WeatherService，這裡只負責把分類結果反映成顯示/隱藏。
    /// </summary>
    public sealed class WeatherTilemapVisibilityDispatcher : MonoBehaviour
    {
        [Serializable]
        private struct TilemapEntry
        {
            [SerializeField]
            private WeatherCategory _category;

            [SerializeField]
            private GameObject _target;

            public WeatherCategory Category => _category;
            public GameObject Target => _target;
        }

        [SerializeField]
        private WeatherService _weatherService;

        [SerializeField, Tooltip("各天氣分類對應要顯示的 Tilemap GameObject；未列出的分類一律隱藏所有項目")]
        private TilemapEntry[] _entries;

        private void Start()
        {
            if (_weatherService == null)
            {
                Debug.LogWarning("[WeatherTilemapVisibilityDispatcher] 尚未指派 WeatherService，無法切換 Tilemap 顯示狀態");
                return;
            }

            ApplyVisibility(_weatherService.CurrentCategory);
            _weatherService.OnWeatherChanged += HandleWeatherChanged;
        }

        private void OnDisable()
        {
            if (_weatherService != null)
            {
                _weatherService.OnWeatherChanged -= HandleWeatherChanged;
            }
        }

        private void HandleWeatherChanged(WeatherModifierData modifier)
        {
            ApplyVisibility(modifier.Category);
        }

        private void ApplyVisibility(WeatherCategory category)
        {
            foreach (TilemapEntry entry in _entries)
            {
                if (entry.Target != null)
                {
                    entry.Target.SetActive(entry.Category == category);
                }
            }
        }
    }
}
