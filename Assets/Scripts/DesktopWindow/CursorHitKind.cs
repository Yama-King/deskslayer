namespace DeskSlayer.DesktopWindow
{
    /// <summary>
    /// 每幀點擊穿透判定的結果分類，供 <see cref="DesktopWindowClickThroughMediator"/> 對外暴露，
    /// 讓後續步驟（拖曳互動）可以直接讀取，不需要重複做一次判斷。
    /// </summary>
    public enum CursorHitKind
    {
        /// <summary>游標下方純透明區域，判定結果為點擊穿透。</summary>
        None,

        /// <summary>游標在 UI 元件上，優先權最高，一律不穿透。</summary>
        UI,

        /// <summary>游標世界座標位置命中 DesktopHitTest 圖層上的 Collider2D，不穿透。</summary>
        GameObject
    }
}
