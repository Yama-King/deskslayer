using UnityEngine;

namespace DeskSlayer.Achievements
{
    /// <summary>
    /// 集合完成型成就（目前僅「風雨無阻」一項）：體驗過天氣系統全部內部分類後解鎖一次。
    /// 判定邏輯（維護已見天氣集合、比對是否集齊）是這個型別專屬的獨立邏輯，
    /// 刻意不預先為「集合完成型」抽出通用框架，待未來真的出現第二個同類需求時再重構。
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeatherSetAchievement", menuName = "DeskSlayer/Achievements/Weather Set Achievement", order = 2)]
    public sealed class WeatherSetAchievementSO : AchievementDefinitionSO
    {
    }
}
