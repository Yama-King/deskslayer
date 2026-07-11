namespace DeskSlayer.Combat
{
    /// <summary>
    /// 查詢指定武器目前合成等級的介面。抽出這個小介面讓 IWeaponDropResolver 判斷「某稀有度底下的
    /// 變體是否全數封頂」時，不需要直接依賴 WeaponInventoryService 的具體實作，兩者只透過這個
    /// 查詢契約溝通，符合高內聚低耦合的架構原則。
    /// </summary>
    public interface IWeaponUpgradeLevelProvider
    {
        /// <summary>查詢指定武器資料目前的合成等級，尚未擁有該武器時視為 0 級。</summary>
        int GetUpgradeLevel(WeaponDataSO weapon);
    }
}
