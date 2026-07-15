using DeskSlayer.Persistence;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 玩家選定城市的輕量持久化外觀：對外簽名（TryLoad/Save）維持不變，讓 WeatherService 完全不需要
    /// 改動。內部已從 PlayerPrefs 遷移到正式的 Save/Load 系統，改讀寫 SaveLifecycleController 共用的
    /// SaveData.selectedWeatherCityIndex 欄位（-1 代表玩家從未手動選過城市），不再讀寫 PlayerPrefs 裡
    /// 舊有的 "DeskSlayer.Weather.SelectedCityIndex" 鍵值。
    ///
    /// 讀寫都透過 SaveLifecycleController.CurrentSaveData（靜態、惰性載入），不需要判斷
    /// SaveLifecycleController 是否已完成 Awake——不論 WeatherService.Awake() 相對於
    /// SaveLifecycleController 的執行順序為何，第一次存取 CurrentSaveData 都會保證讀到正確的存檔資料。
    /// </summary>
    public static class WeatherCityPreferenceStore
    {
        /// <summary>嘗試讀取玩家上次選定的城市索引，從未儲存過時回傳 false。</summary>
        public static bool TryLoad(out int cityIndex)
        {
            SaveData data = SaveLifecycleController.CurrentSaveData;

            if (data.selectedWeatherCityIndex < 0)
            {
                cityIndex = default;
                return false;
            }

            cityIndex = data.selectedWeatherCityIndex;
            return true;
        }

        /// <summary>將玩家選定的城市索引寫入存檔。</summary>
        public static void Save(int cityIndex)
        {
            SaveLifecycleController.CurrentSaveData.selectedWeatherCityIndex = cityIndex;
            SaveLifecycleController.RequestSave();
        }
    }
}
