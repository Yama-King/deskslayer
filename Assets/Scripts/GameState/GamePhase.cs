namespace DeskSlayer.GameState
{
    /// <summary>
    /// 遊戲目前所處的執行階段。刻意只有兩種狀態：不做主選單、不做死亡/勝利結算，
    /// 對應本次 W3 範疇（App 啟動即進入 Playing，敵人輪換無限迴圈，沒有分關卡結算）。
    /// </summary>
    public enum GamePhase
    {
        Playing,
        Paused
    }
}
