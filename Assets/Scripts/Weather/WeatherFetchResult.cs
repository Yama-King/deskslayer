namespace DeskSlayer.Weather
{
    /// <summary>單次天氣資料取得的結果。Success 為 false 時其餘欄位無意義，呼叫端應保留原本的天氣狀態。</summary>
    public readonly struct WeatherFetchResult
    {
        /// <summary>本次取得失敗時使用的固定值，避免各處重複建構失敗結果。</summary>
        public static readonly WeatherFetchResult Failed = new WeatherFetchResult(false, 0, null);

        public readonly bool Success;
        public readonly int ConditionId;
        public readonly string CityName;

        public WeatherFetchResult(bool success, int conditionId, string cityName)
        {
            Success = success;
            ConditionId = conditionId;
            CityName = cityName;
        }
    }
}
