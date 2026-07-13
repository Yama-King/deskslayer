using UnityEngine;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 玩家選定城市的輕量持久化：直接用 PlayerPrefs 存一個整數索引。
    /// 此為暫時性做法，待正式 Save/Load 系統設計後應重新評估——專案完整的存檔系統（W3 待辦）
    /// 尚未實作，先用 PlayerPrefs 讓城市選擇能跨遊戲重啟保留，避免「城市選擇」與未來的
    /// 正式存檔變成兩套彼此不知道對方存在的資料來源。
    /// </summary>
    public static class WeatherCityPreferenceStore
    {
        private const string SelectedCityIndexKey = "DeskSlayer.Weather.SelectedCityIndex";

        /// <summary>嘗試讀取玩家上次選定的城市索引，從未儲存過時回傳 false。</summary>
        public static bool TryLoad(out int cityIndex)
        {
            if (!PlayerPrefs.HasKey(SelectedCityIndexKey))
            {
                cityIndex = default;
                return false;
            }

            cityIndex = PlayerPrefs.GetInt(SelectedCityIndexKey);
            return true;
        }

        /// <summary>將玩家選定的城市索引寫入 PlayerPrefs。</summary>
        public static void Save(int cityIndex)
        {
            PlayerPrefs.SetInt(SelectedCityIndexKey, cityIndex);
            PlayerPrefs.Save();
        }
    }
}
