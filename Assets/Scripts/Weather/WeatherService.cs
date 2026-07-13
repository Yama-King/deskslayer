using System;
using System.Collections;
using UnityEngine;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 天氣系統的協調者：定時透過 IWeatherDataProvider 取得資料、透過 WeatherConditionMapSO
    /// 轉換成遊戲內部分類、透過 WeatherModifierDatabaseSO 查詢對應數值，並在分類改變時發出事件。
    /// 職責僅限於流程協調——資料取得／代碼對照／數值查詢都委派給各自的類別，本身不認識
    /// OpenWeatherMap 的任何呼叫細節，也不參與下個切片的戰鬥數值套用（由訂閱端自行處理）。
    /// </summary>
    public sealed class WeatherService : MonoBehaviour
    {
        /// <summary>天氣分類改變時發出，帶入新的分類對應數值，供戰鬥/UI 系統訂閱而不需要每影格查詢。</summary>
        public event Action<WeatherModifierData> OnWeatherChanged;

        [SerializeField]
        private WeatherCityDatabaseSO _cityDatabase;

        [SerializeField]
        private WeatherConditionMapSO _conditionMap;

        [SerializeField]
        private WeatherModifierDatabaseSO _modifierDatabase;

        [SerializeField, Tooltip("找不到本機快取、且尚未收到第一次 API 回應時使用的預設天氣分類")]
        private WeatherCategory _defaultCategory = WeatherCategory.Cloudy;

        [SerializeField, Min(0f), Tooltip("遊戲啟動後，第一次天氣請求延遲的秒數")]
        private float _initialDelaySeconds = 5f;

        [SerializeField, Min(1f), Tooltip("每次天氣請求的間隔秒數，預設 3600 秒（1 小時）")]
        private float _pollIntervalSeconds = 3600f;

        private IWeatherProvider _provider;

        /// <summary>目前的天氣分類。</summary>
        public WeatherCategory CurrentCategory { get; private set; }

        /// <summary>目前天氣分類對應的數值資料。</summary>
        public WeatherModifierData CurrentModifier { get; private set; }

        private void Awake()
        {
            ApplyCategory(WeatherCacheStore.TryLoad(out WeatherCategory cached) ? cached : _defaultCategory, notify: false);

            WeatherApiKeyLoader.TryLoad(out string apiKey);
            _provider = new OpenWeatherMapProvider(apiKey);
        }

        private void OnEnable()
        {
            StartCoroutine(PollRoutine());
        }

        private IEnumerator PollRoutine()
        {
            yield return new WaitForSeconds(_initialDelaySeconds);

            while (true)
            {
                yield return FetchOnce();
                yield return new WaitForSeconds(_pollIntervalSeconds);
            }
        }

        private IEnumerator FetchOnce()
        {
            if (_cityDatabase == null || _conditionMap == null || _modifierDatabase == null)
            {
                Debug.LogWarning("[WeatherService] 尚未指派城市/對照表/數值資料庫資產，略過本次天氣請求");
                yield break;
            }

            WeatherCityDatabaseSO.CityEntry city = _cityDatabase.CurrentCity;

            WeatherFetchResult result = WeatherFetchResult.Failed;
            yield return _provider.FetchWeather(city.Latitude, city.Longitude, fetchResult => result = fetchResult);

            if (!result.Success)
            {
                Debug.LogWarning($"[WeatherService] 天氣資料取得失敗，沿用目前狀態：{CurrentCategory}");
                yield break;
            }

            WeatherCategory category = _conditionMap.GetCategory(result.ConditionId);
            Debug.Log($"[WeatherService] 取得天氣分類：{category}（城市：{city.DisplayName}，condition id：{result.ConditionId}）");

            ApplyCategory(category, notify: true);
            WeatherCacheStore.Save(category);
        }

        private void ApplyCategory(WeatherCategory category, bool notify)
        {
            bool changed = category != CurrentCategory;

            CurrentCategory = category;
            CurrentModifier = _modifierDatabase != null
                ? _modifierDatabase.GetModifier(category)
                : new WeatherModifierData(category, 0f, 0f, null, Color.white);

            if (notify && changed)
            {
                OnWeatherChanged?.Invoke(CurrentModifier);
            }
        }
    }
}
