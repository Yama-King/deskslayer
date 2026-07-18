using UnityEngine;

namespace DeskSlayer.Achievements
{
    /// <summary>
    /// 事件觸發型成就（例如初戰告捷、軍火商）：特定事件第一次發生時解鎖，不需要任何額外數值，
    /// 觸發來源直接寫死在 AchievementService 裡對應到這份資產的 Id。這類成就天生就是單一事件、
    /// 不做多階段擴充，因此不像 ThresholdAchievementSO 需要資料驅動的數值來源設定。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSimpleAchievement", menuName = "DeskSlayer/Achievements/Simple Achievement", order = 0)]
    public sealed class SimpleAchievementSO : AchievementDefinitionSO
    {
    }
}
