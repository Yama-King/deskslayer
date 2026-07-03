namespace DeskSlayer.KeyboardHook
{
    /// <summary>
    /// 單次按鍵事件的資料結構。使用 readonly struct 避免額外的 GC 配置，
    /// 因為打字過程中此事件會被高頻率建立。
    /// </summary>
    public readonly struct KeyPressData
    {
        /// <summary>觸發事件的可列印字元。</summary>
        public readonly char Character;

        /// <summary>事件發生時間（UTC, DateTime.Ticks），供未來 PlayStyleAnalyzer 分析打字節奏使用。</summary>
        public readonly long TimestampTicks;

        public KeyPressData(char character, long timestampTicks)
        {
            Character = character;
            TimestampTicks = timestampTicks;
        }
    }
}
