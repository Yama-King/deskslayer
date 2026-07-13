namespace DeskSlayer.Weather
{
    /// <summary>
    /// 遊戲內部的天氣分類。戰鬥與 UI 系統只認識這份分類，不直接依賴任何天氣 API 供應商的原始代碼，
    /// 之後更換供應商時只需要調整 WeatherConditionMapSO 的對照資料，不需要動到這個列舉以外的任何地方。
    /// </summary>
    public enum WeatherCategory
    {
        Clear,
        Cloudy,
        Rain,
        Thunderstorm,
        Snow
    }
}
