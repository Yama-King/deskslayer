namespace DeskSlayer.Achievements
{
    /// <summary>
    /// 數值累積門檻型成就（ThresholdAchievementSO）可追蹤的數值來源。新增來源時，
    /// 只需要在這裡新增列舉值，並在 AchievementService 對應的事件訂閱處增加一個累加分支，
    /// 既有的門檻判定迴圈（CheckThresholds）不需要修改。
    /// </summary>
    public enum AchievementValueSource
    {
        /// <summary>累積有效打字字數（可列印字元），來源為 GlobalKeyboardHookService.OnKeyPressed。</summary>
        TypedCharacterCount,

        /// <summary>累積攻擊觸發次數（輕+重攻擊合計），來源為 TypingEnergySystem 的攻擊觸發事件。</summary>
        AttackTriggerCount
    }
}
