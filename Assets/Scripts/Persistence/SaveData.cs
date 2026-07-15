using System;
using System.Collections.Generic;
using DeskSlayer.Combat;

namespace DeskSlayer.Persistence
{
    /// <summary>
    /// 存檔檔案的完整內容。只放「可反查回原始資料的識別碼」與數值進度，
    /// 不直接序列化 ScriptableObject 或執行期物件（例如 WeaponInstance），
    /// 避免資產參照或非 [Serializable] 物件被誤存進存檔。
    /// saveVersion 供未來版本遷移邏輯比對使用，這次不實作遷移，僅確保欄位存在並被寫入/讀取。
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int saveVersion = SaveService.CurrentSaveVersion;

        public int highScore;

        /// <summary>-1 代表玩家尚未手動選過城市，沿用 WeatherCityDatabaseSO 的 Inspector 預設值。</summary>
        public int selectedWeatherCityIndex = -1;

        public List<WeaponSaveEntry> ownedWeapons = new List<WeaponSaveEntry>();

        public List<ShardSaveEntry> weaponShards = new List<ShardSaveEntry>();

        /// <summary>目前裝備武器的識別碼。variant 為 0 代表玩家從未手動裝備過武器，
        /// 交由 WeaponSwitcher 的 Inspector 預設武器決定（比照 selectedWeatherCityIndex 用 -1 表示未設定的作法，
        /// 這裡因 Variant 欄位本身是 1~5 的合法範圍，改用 0 作為「未設定」的哨兵值）。</summary>
        public WeaponFamily equippedWeaponFamily;

        public int equippedWeaponVariant;

        public WeaponRarity equippedWeaponRarity;
    }

    /// <summary>
    /// 一筆已擁有武器的存檔資料。以 (family, variant, rarity) 三元組作為邏輯 ID，
    /// 透過 WeaponDatabaseSO.GetWeapon() 即可反查回實際的 WeaponDataSO 資產，
    /// 不需要在 WeaponDataSO 上額外新增 GUID 欄位。
    /// </summary>
    [Serializable]
    public struct WeaponSaveEntry
    {
        public WeaponFamily family;
        public int variant;
        public WeaponRarity rarity;
        public int upgradeLevel;
        public int duplicateCount;
    }

    /// <summary>一筆「家族＋稀有度」武器碎片的存檔資料。</summary>
    [Serializable]
    public struct ShardSaveEntry
    {
        public WeaponFamily family;
        public WeaponRarity rarity;
        public int shardCount;
    }
}
