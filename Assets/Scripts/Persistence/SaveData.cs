using System;
using System.Collections.Generic;
using DeskSlayer.Combat;
using DeskSlayer.Weather;

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

        public AchievementSaveData achievements = new AchievementSaveData();

        public ShareCardSaveData shareCard = new ShareCardSaveData();

        public PlayStyleSaveData playStyle = new PlayStyleSaveData();

        public GameSettingsSaveData gameSettings = new GameSettingsSaveData();
    }

    /// <summary>
    /// 成就系統的存檔資料：已解鎖成就的識別碼清單，以及數值累積門檻型成就依賴的底層累積數值
    /// （打字字數、攻擊觸發次數）與集合完成型成就依賴的已體驗天氣分類集合。這些累積數值本身
    /// 不存在於 TypingEnergySystem／PlayStyleAnalyzer／WeatherService 等既有系統中（皆為純
    /// session 記憶體狀態），因此由成就系統獨立持久化，不影響既有系統的存檔範圍。
    /// 新增門檻階段資產時，只要 unlockedAchievementIds 裡沒有對應 id 就視為未解鎖，
    /// 讀取舊存檔不會因為缺少新 id 而失敗。
    /// </summary>
    [Serializable]
    public sealed class AchievementSaveData
    {
        public List<string> unlockedAchievementIds = new List<string>();

        public long typedCharacterCount;

        public int attackTriggerCount;

        public List<WeatherCategory> seenWeatherCategories = new List<WeatherCategory>();
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

    /// <summary>
    /// 分享卡系統的存檔資料：當日／累積打字量、當日／累積擊殺數，以及跨日惰性歸零判斷用的
    /// 「上次記錄日期」。這份計數與成就系統的 typedCharacterCount／attackTriggerCount 完全獨立
    /// （即使部分來源事件相同），不共用、不參照成就系統的存檔欄位或計數邏輯。
    /// lastRecordedDate 為空字串代表尚未有任何一次事件或分享卡生成觸發過惰性判斷，
    /// 第一次觸發時會視為「跟今天不同」而直接初始化，不需要特別處理初始值。
    /// </summary>
    [Serializable]
    public sealed class ShareCardSaveData
    {
        public int dailyTypedCount;

        public int dailyKillCount;

        public long totalTypedCount;

        public long totalKillCount;

        public string lastRecordedDate = string.Empty;
    }

    /// <summary>
    /// PlayStyleAnalyzer（打字節奏／輕重攻擊傾向統計）的存檔資料：當日／累積輕重武器按鍵歸屬次數，
    /// 當日／累積節奏線上統計累加器（Welford's Online Algorithm 的樣本數、平均值、平方差累加值），
    /// 以及跨日惰性歸零判斷用的「上次記錄日期」。這個日期欄位與 <see cref="ShareCardSaveData.lastRecordedDate"/>
    /// 語意相同、格式相同，但各自獨立維護、不共用也不互相參照，比照兩系統一貫的獨立原則。
    /// 節奏累加器只保留三個數值、不保留任何原始樣本，資料量極小，因此當日／累積皆納入存檔範圍，
    /// 確保玩家同一天內重啟遊戲時，當日統計能正確接續而非從零重新開始。
    /// </summary>
    [Serializable]
    public sealed class PlayStyleSaveData
    {
        public int dailyLightWeaponKeyPressCount;

        public int dailyHeavyWeaponKeyPressCount;

        public long totalLightWeaponKeyPressCount;

        public long totalHeavyWeaponKeyPressCount;

        public string lastRecordedDate = string.Empty;

        public long dailyRhythmSampleCount;

        public double dailyRhythmMean;

        public double dailyRhythmM2;

        public long totalRhythmSampleCount;

        public double totalRhythmMean;

        public double totalRhythmM2;
    }

    /// <summary>
    /// 玩家可調整設定的存檔資料：滑鼠偵測開關（是否將滑鼠點擊視為攻擊輸入）與主音量。
    /// 比照 WeatherCityPreferenceStore 現行的做法直接掛在 SaveData 底下，不使用 PlayerPrefs——
    /// 城市選擇資料已從 PlayerPrefs 遷移至此，這裡從一開始就採用相同的持久化路徑。
    /// </summary>
    [Serializable]
    public sealed class GameSettingsSaveData
    {
        public bool mouseAttackInputEnabled = true;

        public float masterVolume = 1f;

        /// <summary>是否透過 Windows 登錄檔 Run 機碼開機自動啟動，預設關閉——背景常駐程式
        /// 不應該未經玩家同意就跟著開機啟動。</summary>
        public bool launchOnStartupEnabled;

        /// <summary>視窗是否維持在所有視窗最上層，預設開啟——沿用改版前「永遠置頂」的既有行為，
        /// 這次只是新增「可以關閉」的能力，不是改變預設值。</summary>
        public bool windowTopmostEnabled = true;
    }
}
