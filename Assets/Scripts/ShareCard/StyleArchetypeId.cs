namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡風格揭曉的五種固定原型分類。固定列舉（非開放式集合），因為原型種類本身由規格書定案，
    /// 不像武器/成就那樣會持續新增項目，enum 比陣列查找更直接。
    /// </summary>
    public enum StyleArchetypeId
    {
        /// <summary>平衡型：兩軸分數皆落在中段帶寬內，優先於四象限判定。</summary>
        Balanced,

        /// <summary>敏捷刺客型：節奏穩定度高、輕攻擊傾向高。</summary>
        AgileAssassin,

        /// <summary>沉穩重砲型：節奏穩定度高、輕攻擊傾向低（偏重攻擊）。</summary>
        SteadyHeavy,

        /// <summary>靈活遊擊型：節奏穩定度低、輕攻擊傾向高。</summary>
        FlexibleGuerrilla,

        /// <summary>狂戰士型：節奏穩定度低、輕攻擊傾向低（偏重攻擊）。</summary>
        Berserker
    }
}
