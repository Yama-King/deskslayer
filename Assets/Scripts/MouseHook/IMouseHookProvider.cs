namespace DeskSlayer.MouseHook
{
    /// <summary>
    /// 滑鼠事件監聽提供者的抽象介面，比照 KeyboardHook.IKeyboardHookProvider：
    /// 上層（GlobalMouseHookService）僅依賴此介面，不直接依賴任何平台專屬的實作。
    /// </summary>
    public interface IMouseHookProvider
    {
        /// <summary>開始監聽全域滑鼠事件。</summary>
        void StartListening();

        /// <summary>停止監聽並釋放相關資源（Hook 控制代碼、背景執行緒等）。</summary>
        void StopListening();
    }
}
