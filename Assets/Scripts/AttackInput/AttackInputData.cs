namespace DeskSlayer.AttackInput
{
    /// <summary>
    /// 彙整後的攻擊觸發事件資料，統一鍵盤字元輸入與滑鼠點擊兩種來源。
    /// 刻意不帶鍵盤專屬的 Character 欄位——下游只需要「一次有效攻擊輸入發生了」與時間戳記，
    /// 不需要、也不應該知道實際按了哪個鍵或哪個滑鼠鍵。
    /// </summary>
    public readonly struct AttackInputData
    {
        /// <summary>事件發生時間（UTC, DateTime.Ticks），供 PlayStyleAnalyzer 分析打字/點擊節奏使用。</summary>
        public readonly long TimestampTicks;

        /// <summary>來源標記，僅供除錯/分析用途，見 AttackInputSource 註解。</summary>
        public readonly AttackInputSource Source;

        public AttackInputData(long timestampTicks, AttackInputSource source)
        {
            TimestampTicks = timestampTicks;
            Source = source;
        }
    }
}
