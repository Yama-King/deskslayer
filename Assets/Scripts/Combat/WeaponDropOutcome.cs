namespace DeskSlayer.Combat
{
    /// <summary>
    /// 單次掉落判定實際造成的結果，供表現層（掉落提示、記錄清單）決定要顯示「取得新武器」
    /// 「取得重複品」還是「轉換為碎片」，不需要自己重新判斷背包狀態。
    /// </summary>
    public enum WeaponDropOutcome
    {
        /// <summary>本次判定沒有實際掉落（例如武器資料為 null），不應顯示任何提示。</summary>
        None,

        /// <summary>背包中原本沒有這把武器，首次取得。</summary>
        NewWeapon,

        /// <summary>已擁有這把武器且尚未封頂，取得的是可用於合成的重複品。</summary>
        Duplicate,

        /// <summary>這把武器已達最大合成等級，重複品改為累積成武器碎片，不浪費掉。</summary>
        ShardConverted
    }
}
