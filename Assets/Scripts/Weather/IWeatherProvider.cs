using System;
using System.Collections;

namespace DeskSlayer.Weather
{
    /// <summary>
    /// 天氣資料取得介面。抽出介面是為了保留未來更換 API 供應商的彈性——WeatherService 只認識這個介面，
    /// 不直接依賴任何供應商的呼叫細節，換供應商時只需要新增另一個實作。
    /// </summary>
    public interface IWeatherProvider
    {
        /// <summary>
        /// 依經緯度取得目前天氣資料。回傳型別為 IEnumerator，供呼叫端以 StartCoroutine 執行；
        /// 無論成功或失敗，執行結束前一定會呼叫一次 onComplete，不會拋出未處理的例外。
        /// </summary>
        IEnumerator FetchWeather(float latitude, float longitude, Action<WeatherFetchResult> onComplete);
    }
}
