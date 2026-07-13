using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// OpenWeatherMap 免費版 Current Weather API（data/2.5/weather，經緯度查詢）的資料取得實作。
    /// 刻意不使用 One Call 3.0/4.0（需要信用卡）。網路錯誤、逾時、非預期回應內容、JSON 解析失敗
    /// 一律視為本次取得失敗，透過 onComplete 回報，不拋出未處理的例外。
    /// </summary>
    public sealed class OpenWeatherMapProvider : IWeatherProvider
    {
        private const string ApiBaseUrl = "https://api.openweathermap.org/data/2.5/weather";

        [Serializable]
        private class WeatherEntryDto
        {
            public int id;
        }

        [Serializable]
        private class ResponseDto
        {
            public WeatherEntryDto[] weather;
            public string name;
        }

        private readonly string _apiKey;

        public OpenWeatherMapProvider(string apiKey)
        {
            _apiKey = apiKey;
        }

        public IEnumerator FetchWeather(float latitude, float longitude, Action<WeatherFetchResult> onComplete)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                Debug.LogWarning("[OpenWeatherMapProvider] 尚未設定有效的 API Key，略過本次天氣請求");
                onComplete?.Invoke(WeatherFetchResult.Failed);
                yield break;
            }

            string url = $"{ApiBaseUrl}?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[OpenWeatherMapProvider] 天氣請求失敗（{request.responseCode}）：{request.error}");
                    onComplete?.Invoke(WeatherFetchResult.Failed);
                    yield break;
                }

                WeatherFetchResult result = ParseResponse(request.downloadHandler.text);
                onComplete?.Invoke(result);
            }
        }

        private static WeatherFetchResult ParseResponse(string json)
        {
            try
            {
                ResponseDto response = JsonUtility.FromJson<ResponseDto>(json);
                if (response?.weather == null || response.weather.Length == 0)
                {
                    Debug.LogWarning("[OpenWeatherMapProvider] 天氣回應內容不完整，缺少 weather 欄位");
                    return WeatherFetchResult.Failed;
                }

                return new WeatherFetchResult(true, response.weather[0].id, response.name);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[OpenWeatherMapProvider] 天氣回應解析失敗：{exception.Message}");
                return WeatherFetchResult.Failed;
            }
        }
    }
}
