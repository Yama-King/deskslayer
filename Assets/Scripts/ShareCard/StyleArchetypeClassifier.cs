namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 純 C# 靜態風格原型判定邏輯，只讀入兩個分數與門檻設定，不依賴 UnityEngine 以外的型別，
    /// 方便未來以 EditMode 測試獨立驗證（比照 PlayStyleProfile 的設計哲學）。
    /// 只讀取呼叫端傳入的分數與門檻資料，不直接參照 PlayStyleAnalyzer，
    /// 與該既有系統之間完全零耦合。
    /// </summary>
    public static class StyleArchetypeClassifier
    {
        /// <summary>
        /// 依規格書 2.1/2.2 的優先順序判定原型：先檢查兩軸分數是否同時落在平衡型帶寬內
        /// （含邊界），符合則直接歸類為平衡型；不符合才依兩軸各自以分界值為準的四象限邏輯判定。
        /// </summary>
        public static StyleArchetypeId Classify(float rhythmStabilityScore, float lightAttackTendencyScore, StyleArchetypeThresholdConfigSO config)
        {
            float bandMin = config.BalancedBandwidthMin;
            float bandMax = config.BalancedBandwidthMax;
            float split = config.QuadrantSplitValue;

            bool rhythmInBand = rhythmStabilityScore >= bandMin && rhythmStabilityScore <= bandMax;
            bool lightInBand = lightAttackTendencyScore >= bandMin && lightAttackTendencyScore <= bandMax;

            if (rhythmInBand && lightInBand)
            {
                return StyleArchetypeId.Balanced;
            }

            bool rhythmHigh = rhythmStabilityScore >= split;
            bool lightHigh = lightAttackTendencyScore >= split;

            if (rhythmHigh && lightHigh)
            {
                return StyleArchetypeId.AgileAssassin;
            }

            if (rhythmHigh)
            {
                return StyleArchetypeId.SteadyHeavy;
            }

            if (lightHigh)
            {
                return StyleArchetypeId.FlexibleGuerrilla;
            }

            return StyleArchetypeId.Berserker;
        }
    }
}
