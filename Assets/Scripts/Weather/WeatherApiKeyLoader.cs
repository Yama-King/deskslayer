using System;
using System.IO;
using UnityEngine;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 讀取 StreamingAssets 底下的天氣 API Key 設定檔。真實 Key 不進版控——版控內只有
    /// weather-config.example.json 範本，實際的 weather-config.json 由開發者自行建立。
    /// 檔案不存在、內容無法解析、或 Key 為空字串一律視為讀取失敗，印出 Warning 後回傳 false，不拋出例外。
    /// </summary>
    public static class WeatherApiKeyLoader
    {
        private const string ConfigFileName = "weather-config.json";

        [Serializable]
        private class ConfigDto
        {
            public string apiKey;
        }

        public static bool TryLoad(out string apiKey)
        {
            apiKey = null;
            string path = Path.Combine(Application.streamingAssetsPath, ConfigFileName);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[WeatherApiKeyLoader] 找不到設定檔 {path}，請參考 weather-config.example.json 自行建立並填入 API Key");
                return false;
            }

            try
            {
                ConfigDto config = JsonUtility.FromJson<ConfigDto>(File.ReadAllText(path));
                if (config == null || string.IsNullOrEmpty(config.apiKey))
                {
                    Debug.LogWarning("[WeatherApiKeyLoader] 設定檔內的 apiKey 為空，請填入有效的 OpenWeatherMap API Key");
                    return false;
                }

                apiKey = config.apiKey;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[WeatherApiKeyLoader] 設定檔解析失敗：{exception.Message}");
                return false;
            }
        }
    }
}
