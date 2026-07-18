using UnityEngine;

namespace DeskSlayer.Achievements
{
    /// <summary>
    /// 數值累積門檻型成就（例如打字量里程碑、風格覺醒）：追蹤指定數值來源的累積值，
    /// 達到門檻即解鎖。新增一個門檻階段（例如打字量新增 5000/10000 字兩個階段）只需要
    /// 新增一份這個類別的資產、指定來源與門檻值，AchievementService 的判定邏輯完全通用，
    /// 不需要新增或修改任何程式碼。
    /// </summary>
    [CreateAssetMenu(fileName = "NewThresholdAchievement", menuName = "DeskSlayer/Achievements/Threshold Achievement", order = 1)]
    public sealed class ThresholdAchievementSO : AchievementDefinitionSO
    {
        [SerializeField, Tooltip("要追蹤的數值來源")]
        private AchievementValueSource _sourceType;

        [SerializeField, Min(1), Tooltip("達到此累積值即解鎖")]
        private long _threshold = 1;

        /// <summary>要追蹤的數值來源。</summary>
        public AchievementValueSource SourceType => _sourceType;

        /// <summary>達到此累積值即解鎖。</summary>
        public long Threshold => _threshold;
    }
}
