namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡合成當下要顯示的資料快照：已經由呼叫端（ShareCardPanelController）決定好要顯示
    /// 「當日」或「累積」範圍，這裡只單純攜帶最終要顯示的數字，不參與任何範圍判斷邏輯。
    /// </summary>
    public readonly struct ShareCardDisplayData
    {
        public readonly long TypedCount;
        public readonly long KillCount;
        public readonly float LightAttackTendencyScore;
        public readonly float RhythmStabilityScore;
        public readonly string ScopeLabel;
        public readonly StyleArchetypeId ArchetypeId;

        public ShareCardDisplayData(long typedCount, long killCount, float lightAttackTendencyScore, float rhythmStabilityScore, string scopeLabel, StyleArchetypeId archetypeId)
        {
            TypedCount = typedCount;
            KillCount = killCount;
            LightAttackTendencyScore = lightAttackTendencyScore;
            RhythmStabilityScore = rhythmStabilityScore;
            ScopeLabel = scopeLabel;
            ArchetypeId = archetypeId;
        }
    }
}
