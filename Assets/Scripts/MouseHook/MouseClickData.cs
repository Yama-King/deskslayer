namespace DeskSlayer.MouseHook
{
    /// <summary>
    /// 單次滑鼠點擊事件的資料結構，比照 KeyboardHook.KeyPressData 使用 readonly struct
    /// 避免高頻率觸發時的 GC 配置。
    /// </summary>
    public readonly struct MouseClickData
    {
        /// <summary>觸發事件的按鍵。</summary>
        public readonly MouseButtonKind Button;

        /// <summary>事件發生時間（UTC, DateTime.Ticks）。</summary>
        public readonly long TimestampTicks;

        public MouseClickData(MouseButtonKind button, long timestampTicks)
        {
            Button = button;
            TimestampTicks = timestampTicks;
        }
    }
}
