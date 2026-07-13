using System;
using System.IO;
using UnityEngine;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 把最後一次成功取得的天氣分類快取到本機存檔目錄。遊戲啟動時、第一次 API 回應抵達前，
    /// 先用這份快取當作初始天氣狀態，避免每次開機都要等待網路請求才有天氣可用。
    /// 讀寫失敗（檔案不存在、內容無法解析、寫入被拒絕等）一律印出 Warning，不拋出例外、不影響遊戲繼續執行。
    /// </summary>
    public static class WeatherCacheStore
    {
        private const string CacheFileName = "weather-cache.json";

        [Serializable]
        private class CacheDto
        {
            public string category;
        }

        private static string CachePath => Path.Combine(Application.persistentDataPath, CacheFileName);

        /// <summary>嘗試讀取上次快取的天氣分類，找不到檔案（例如首次啟動）或內容無法解析時回傳 false。</summary>
        public static bool TryLoad(out WeatherCategory category)
        {
            category = default;
            string path = CachePath;

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                CacheDto dto = JsonUtility.FromJson<CacheDto>(File.ReadAllText(path));
                if (dto == null || !Enum.TryParse(dto.category, out category))
                {
                    Debug.LogWarning("[WeatherCacheStore] 快取內容無法解析，忽略本次快取");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[WeatherCacheStore] 讀取天氣快取失敗：{exception.Message}");
                return false;
            }
        }

        /// <summary>將目前的天氣分類寫入本機快取，寫入失敗時只印出 Warning，不影響遊戲繼續執行。</summary>
        public static void Save(WeatherCategory category)
        {
            try
            {
                CacheDto dto = new CacheDto { category = category.ToString() };
                File.WriteAllText(CachePath, JsonUtility.ToJson(dto));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[WeatherCacheStore] 寫入天氣快取失敗：{exception.Message}");
            }
        }
    }
}
