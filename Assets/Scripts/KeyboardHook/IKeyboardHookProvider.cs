namespace DeskSlayer.KeyboardHook
{
    /// <summary>
    /// 鍵盤事件監聽提供者的抽象介面。
    /// 上層（GlobalKeyboardHookService）僅依賴此介面，不直接依賴任何平台專屬的實作，
    /// 未來若需支援其他平台或建立測試用 Mock，可直接抽換實作而不影響上層邏輯（依賴反轉）。
    /// </summary>
    public interface IKeyboardHookProvider
    {
        /// <summary>開始監聽全域鍵盤事件。</summary>
        void StartListening();

        /// <summary>停止監聽並釋放相關資源（Hook 控制代碼、背景執行緒等）。</summary>
        void StopListening();
    }
}
