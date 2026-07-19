namespace DeskSlayer.MouseHook
{
    /// <summary>
    /// 全域滑鼠 Hook 目前支援偵測的按鍵種類。只涵蓋左右鍵的「按下」事件，
    /// 不含放開/移動/拖曳（見 Win32LowLevelMouseHook 類別註解）。
    /// </summary>
    public enum MouseButtonKind
    {
        Left,
        Right
    }
}
