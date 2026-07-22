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

        /// <summary>
        /// 選定城市改變時發出，帶入新城市的顯示名稱。刻意跟 OnWeatherChanged 分開：切換城市當下就該
        /// 更新「目前選擇城市」的顯示，不能依賴 OnWeatherChanged（新城市剛好跟舊城市同一種天氣分類時
        /// OnWeatherChanged 不會觸發，但城市名稱本身已經換了，UI 不能沿用舊城市名稱）。
        /// </summary>
        public event Action<string> OnCityChanged;

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
        private Coroutine _immediateFetchCoroutine;

        /// <summary>目前的天氣分類。</summary>
        public WeatherCategory CurrentCategory { get; private set; }

        /// <summary>目前天氣分類對應的數值資料。</summary>
        public WeatherModifierData CurrentModifier { get; private set; }

        /// <summary>目前選定城市的顯示名稱；尚未指派城市資料庫時回傳空字串。</summary>
        public string CurrentCityDisplayName => _cityDatabase != null ? _cityDatabase.CurrentCity.DisplayName : string.Empty;

        private void Awake()
        {
            // 先套用上次選定的城市（若有存檔），確保第一次天氣查詢就用玩家選的城市，
            // 不會先用預設城市查一次、讀到存檔後又要再查第二次。
            if (_cityDatabase != null && WeatherCityPreferenceStore.TryLoad(out int savedCityIndex))
            {
                _cityDatabase.SetSelectedCityIndex(savedCityIndex);
            }

            ApplyCategory(WeatherCacheStore.TryLoad(out WeatherCategory cached) ? cached : _defaultCategory, notify: false);

            WeatherApiKeyLoader.TryLoad(out string apiKey);
            _provider = new OpenWeatherMapProvider(apiKey);
        }

        private void OnEnable()
        {
            StartCoroutine(PollRoutine());
        }

        /// <summary>
        /// 供城市選擇 UI 呼叫：切換目前選定的城市、寫入偏好設定，並立即重新查詢天氣，
        /// 不需要等到下一次排程輪詢（預設週期 _pollIntervalSeconds）才反映新城市的天氣。
        /// </summary>
        public void SelectCity(int cityIndex)
        {
            if (_cityDatabase == null)
            {
                Debug.LogWarning("[WeatherService] 尚未指派城市資料庫，無法切換城市");
                return;
            }

            _cityDatabase.SetSelectedCityIndex(cityIndex);
            WeatherCityPreferenceStore.Save(cityIndex);
            OnCityChanged?.Invoke(_cityDatabase.CurrentCity.DisplayName);

            if (_immediateFetchCoroutine != null)
            {
                StopCoroutine(_immediateFetchCoroutine);
            }
            _immediateFetchCoroutine = StartCoroutine(FetchOnce());
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

#if UNITY_EDITOR
        /// <summary>
        /// 僅供 Editor 測試使用：略過 API 請求與城市查詢流程，直接套用指定天氣分類並在有變化時觸發
        /// OnWeatherChanged，讓「風雨無阻」等訂閱端能被同一套正式判定邏輯驗證，不需要真的等待/僞造
        /// API 回應。以 #if UNITY_EDITOR 包住，不會被打包進正式 Build。
        /// </summary>
        public void DebugForceCategory(WeatherCategory category)
        {
            ApplyCategory(category, notify: true);
        }
#endif
    }
}
