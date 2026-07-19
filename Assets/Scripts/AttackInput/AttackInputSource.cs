namespace DeskSlayer.AttackInput
{
    /// <summary>
    /// AttackInputData 的來源標記。僅供除錯/未來分析用途附加參考，
    /// 下游消費端（TypingEnergySystem、PlayStyleAnalyzer）不得依賴這個標記做分流判斷——
    /// 兩種來源必須被視為完全對等的攻擊觸發輸入。
    /// </summary>
    public enum AttackInputSource
    {
        Keyboard,
        Mouse
    }
}
