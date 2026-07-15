using System;
using System.Collections.Generic;
using DeskSlayer.Persistence;
using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 管理玩家擁有的武器實例、重複品數量與武器碎片數量，負責判定「是否可合成／兌換」並執行。
    /// 比照 AudioManager／HitStopController 的靜態單例作法：WeaponDropDispatcher 掛在各敵人身上，
    /// 無法在 Prefab 編輯階段直接參照場景中唯一的背包服務，透過 Instance 存取即可，不需要手動接線。
    /// 合成消耗、碎片兌換消耗與加成規則完全讀取 WeaponDropConfigSO，這裡不寫死任何數值。
    ///
    /// 存檔串接：Awake 時透過 SaveLifecycleController.CurrentSaveData（靜態、惰性載入，不依賴任何物件的
    /// Awake 執行順序）還原背包內容；之後每次背包狀態異動（掉落、合成、碎片兌換）都直接同步更新
    /// CurrentSaveData 對應的清單項目，讓存檔內容隨時保持最新，不需要等到寫入當下才整包重新掃描。
    /// DTO（WeaponSaveEntry／ShardSaveEntry）與執行期物件（WeaponInstance）之間的轉換完全封裝在這個類別內，
    /// SaveService／SaveLifecycleController 全程不需要認識任何武器系統的型別。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponInventoryService : MonoBehaviour
    {
        [SerializeField]
        private WeaponDatabaseSO _database;

        [SerializeField]
        private WeaponDropConfigSO _dropConfig;

        [SerializeField, Tooltip("目前裝備武器的來源，用於還原/持久化上次離線前裝備的武器。留空則不處理裝備狀態的存讀檔")]
        private WeaponSwitcher _weaponSwitcher;

        public static WeaponInventoryService Instance { get; private set; }

        private readonly Dictionary<WeaponDataSO, WeaponInstance> _ownedWeapons = new Dictionary<WeaponDataSO, WeaponInstance>();
        private readonly Dictionary<WeaponDataSO, int> _duplicateCounts = new Dictionary<WeaponDataSO, int>();
        private readonly Dictionary<(WeaponFamily, WeaponRarity), int> _shardCounts = new Dictionary<(WeaponFamily, WeaponRarity), int>();

        /// <summary>首次取得一把新武器（背包中原本沒有）時發出，供未來背包 UI 顯示「新武器」提示。</summary>
        public event Action<WeaponInstance> OnWeaponObtained;

        /// <summary>取得已擁有武器的重複品時發出，帶入該武器目前的重複品總數。</summary>
        public event Action<WeaponDataSO, int> OnDuplicateObtained;

        /// <summary>武器合成升級成功時發出。</summary>
        public event Action<WeaponInstance> OnWeaponUpgraded;

        /// <summary>指定家族+稀有度的武器碎片數量變動時發出（累積或兌換消耗皆會觸發），帶入變動後的總數。</summary>
        public event Action<WeaponFamily, WeaponRarity, int> OnShardCountChanged;

        /// <summary>目前擁有的所有武器實例，供未來背包 UI 查詢使用。</summary>
        public IReadOnlyCollection<WeaponInstance> OwnedWeapons => _ownedWeapons.Values;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOwnedWeaponsFromSaveData();
            GrantPermanentStarterWeapons();
        }

        private void OnEnable()
        {
            SubscribeToWeaponSwitcher();
        }

        private void OnDisable()
        {
            if (_weaponSwitcher != null)
            {
                _weaponSwitcher.OnWeaponEquipped -= HandleWeaponEquippedForSave;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 還原上次離線前裝備的武器，刻意放在 Start() 而非 Awake()：Unity 不保證不同物件間的 Awake 執行順序，
        /// WeaponSwitcher.Awake() 會把裝備設回 Inspector 的預設武器，若這裡的還原也在 Awake 執行，
        /// 有可能先跑完就被 WeaponSwitcher 隨後的 Awake 蓋掉。Start() 保證所有物件的 Awake 都已跑完，
        /// 這裡呼叫 EquipWeapon 才是最終、不會被覆蓋的結果（比照 WeaponSwitchUI.Start() 的既有作法）。
        /// </summary>
        private void Start()
        {
            // 比照 PausePanelController／AudioManager 等既有系統對「跨物件事件訂閱」的雙重保護作法，
            // 在 Start() 補呼叫一次 SubscribeToWeaponSwitcher()：_weaponSwitcher 本身是序列化欄位參照
            // （場景反序列化階段就已賦值完成，理論上不受 Awake/OnEnable 執行順序影響，不是那幾個既有案例
            // 依賴的「靜態 Instance 要等對方 Awake 跑完才賦值」那種寫法），但仍在這裡補一次訂閱，
            // 避免日後這個欄位改成透過某個 Instance 靜態單例取得參照時，需要重新排查這段邏輯。
            SubscribeToWeaponSwitcher();
            RestoreEquippedWeapon();
        }

        /// <summary>訂閱 WeaponSwitcher 的裝備變更事件，先移除再訂閱以避免 OnEnable／Start 都呼叫到這裡時重複掛上同一個委派。</summary>
        private void SubscribeToWeaponSwitcher()
        {
            if (_weaponSwitcher == null)
            {
                return;
            }

            _weaponSwitcher.OnWeaponEquipped -= HandleWeaponEquippedForSave;
            _weaponSwitcher.OnWeaponEquipped += HandleWeaponEquippedForSave;
        }

        /// <summary>查詢指定武器目前持有的重複品數量（尚未消耗掉的合成材料）。</summary>
        public int GetDuplicateCount(WeaponDataSO weapon)
        {
            return _duplicateCounts.TryGetValue(weapon, out int count) ? count : 0;
        }

        /// <summary>是否已擁有指定武器（至少持有 1 把，不論合成等級）。</summary>
        public bool IsOwned(WeaponDataSO weapon)
        {
            return _ownedWeapons.ContainsKey(weapon);
        }

        /// <summary>查詢指定武器目前擁有的實例（等級、傷害倍率等），尚未擁有時回傳 null，供背包 UI 顯示已擁有武器的詳細數值。</summary>
        public WeaponInstance GetOwnedInstance(WeaponDataSO weapon)
        {
            return _ownedWeapons.TryGetValue(weapon, out WeaponInstance instance) ? instance : null;
        }

        /// <summary>
        /// 收下一次掉落。尚未擁有該武器時視為「首次取得」，直接建立 0 級的武器實例；
        /// 已擁有時視為「重複品」，累加重複品數量供之後合成消耗。呼叫前應先用 IsWeaponMaxed
        /// 判斷是否已封頂——已封頂的重複品應改呼叫 AddShard，這裡不重複判斷封頂邏輯。
        /// </summary>
        public WeaponDropOutcome AddDrop(WeaponDataSO weapon)
        {
            if (weapon == null)
            {
                return WeaponDropOutcome.None;
            }

            if (!_ownedWeapons.ContainsKey(weapon))
            {
                WeaponInstance instance = new WeaponInstance(weapon);
                _ownedWeapons[weapon] = instance;
                SyncOwnedWeaponEntry(weapon);
                OnWeaponObtained?.Invoke(instance);
                return WeaponDropOutcome.NewWeapon;
            }

            int newCount = GetDuplicateCount(weapon) + 1;
            _duplicateCounts[weapon] = newCount;
            SyncOwnedWeaponEntry(weapon);
            OnDuplicateObtained?.Invoke(weapon, newCount);
            return WeaponDropOutcome.Duplicate;
        }

        /// <summary>判斷指定武器是否已擁有且已達最大合成等級，供 WeaponDropDispatcher 決定重複品要走合成材料還是碎片路線。</summary>
        public bool IsWeaponMaxed(WeaponDataSO weapon)
        {
            return weapon != null && _ownedWeapons.TryGetValue(weapon, out WeaponInstance instance) && instance.IsAtMaxUpgradeLevel;
        }

        /// <summary>查詢指定家族+稀有度目前累積的武器碎片數量，尚未累積過時回傳 0。</summary>
        public int GetShardCount(WeaponFamily family, WeaponRarity rarity)
        {
            return _shardCounts.TryGetValue((family, rarity), out int count) ? count : 0;
        }

        /// <summary>
        /// 累積一枚指定家族+稀有度的武器碎片，供已封頂武器的重複品掉落時呼叫。
        /// 碎片依家族+稀有度分開記錄，不同稀有度的碎片不互通。
        /// </summary>
        public void AddShard(WeaponFamily family, WeaponRarity rarity)
        {
            int newCount = GetShardCount(family, rarity) + 1;
            _shardCounts[(family, rarity)] = newCount;
            SyncShardEntry(family, rarity);
            OnShardCountChanged?.Invoke(family, rarity, newCount);
        }

        /// <summary>
        /// 判斷是否可以用碎片兌換指定武器：該武器必須尚未擁有（兌換範圍限定同家族、同稀有度底下
        /// 尚未收集的變體），且對應家族+稀有度的碎片數量達到 WeaponDropConfigSO 設定的兌換消耗數量。
        /// </summary>
        public bool CanExchangeShard(WeaponDataSO weapon)
        {
            if (weapon == null || IsOwned(weapon))
            {
                return false;
            }

            int cost = _dropConfig.GetShardExchangeCost(weapon.Rarity);
            return GetShardCount(weapon.Family, weapon.Rarity) >= cost;
        }

        /// <summary>
        /// 執行一次碎片兌換：資格判定失敗時安全地不做任何事並回傳 false；成功時扣除對應家族+稀有度
        /// 的碎片、把武器加入背包（0 級起）並發出 OnWeaponObtained，讓既有的背包 UI 直接視為「取得新武器」刷新。
        /// </summary>
        public bool TryExchangeShard(WeaponDataSO weapon)
        {
            if (!CanExchangeShard(weapon))
            {
                return false;
            }

            int cost = _dropConfig.GetShardExchangeCost(weapon.Rarity);
            int newCount = GetShardCount(weapon.Family, weapon.Rarity) - cost;
            _shardCounts[(weapon.Family, weapon.Rarity)] = newCount;
            SyncShardEntry(weapon.Family, weapon.Rarity);
            OnShardCountChanged?.Invoke(weapon.Family, weapon.Rarity, newCount);

            WeaponInstance instance = new WeaponInstance(weapon);
            _ownedWeapons[weapon] = instance;
            SyncOwnedWeaponEntry(weapon);
            OnWeaponObtained?.Invoke(instance);
            return true;
        }

        /// <summary>
        /// 判斷指定武器目前是否可以合成升級：必須已擁有、尚未達最大合成等級，
        /// 且重複品數量達到 WeaponDropConfigSO 針對該稀有度設定的合成消耗數量。
        /// </summary>
        public bool CanUpgrade(WeaponDataSO weapon)
        {
            if (weapon == null || !_ownedWeapons.TryGetValue(weapon, out WeaponInstance instance))
            {
                return false;
            }

            if (instance.IsAtMaxUpgradeLevel)
            {
                return false;
            }

            int fusionCost = _dropConfig.GetFusionCost(weapon.Rarity);
            return GetDuplicateCount(weapon) >= fusionCost;
        }

        /// <summary>
        /// 執行一次合成：資格判定失敗時安全地不做任何事並回傳 false；成功時依 WeaponDropConfigSO
        /// 設定的消耗數量扣除重複品、將武器實例升一級並發出 OnWeaponUpgraded。
        /// </summary>
        public bool TryUpgrade(WeaponDataSO weapon)
        {
            if (!CanUpgrade(weapon))
            {
                return false;
            }

            int fusionCost = _dropConfig.GetFusionCost(weapon.Rarity);
            _duplicateCounts[weapon] = GetDuplicateCount(weapon) - fusionCost;

            WeaponInstance instance = _ownedWeapons[weapon];
            instance.LevelUp();
            SyncOwnedWeaponEntry(weapon);
            OnWeaponUpgraded?.Invoke(instance);
            return true;
        }

        /// <summary>
        /// 遊戲啟動時，把資料庫中所有標記為永久保底武器的資產直接發放給玩家（0 級起）。
        /// 只依 WeaponDataSO.IsPermanentStarter 旗標判斷，不寫死特定武器資產，之後新增/更換保底武器
        /// 只需要在 Inspector 打勾，不需要改這裡的程式碼。
        /// </summary>
        private void GrantPermanentStarterWeapons()
        {
            if (_database == null)
            {
                return;
            }

            foreach (WeaponDataSO weapon in _database.AllWeapons)
            {
                if (weapon == null || !weapon.IsPermanentStarter || _ownedWeapons.ContainsKey(weapon))
                {
                    continue;
                }

                WeaponInstance instance = new WeaponInstance(weapon);
                _ownedWeapons[weapon] = instance;
                SyncOwnedWeaponEntry(weapon);
                OnWeaponObtained?.Invoke(instance);
            }
        }

        /// <summary>
        /// 從存檔還原武器背包：把 SaveData 的 DTO 清單轉換回執行期的 _ownedWeapons／_duplicateCounts／
        /// _shardCounts。找不到存檔（首次啟動）或清單為空時，這些集合維持空白，交由後續的
        /// GrantPermanentStarterWeapons 補上保底武器，不視為錯誤。清單中若有欄位格式錯誤或缺漏
        /// （JsonUtility 會填入型別預設值，例如 variant 變成 0），GetWeapon 會查無資產、安全跳過該筆。
        /// 刻意不在這裡觸發 OnWeaponObtained 等事件：這是「還原」不是「取得」，避免背包 UI 在遊戲一開始
        /// 就誤判為新武器提示；UI 一律在面板開啟、實際綁定時透過 IsOwned/GetOwnedInstance 查詢當下狀態。
        /// </summary>
        private void LoadOwnedWeaponsFromSaveData()
        {
            if (_database == null)
            {
                return;
            }

            SaveData saveData = SaveLifecycleController.CurrentSaveData;

            if (saveData.ownedWeapons != null)
            {
                foreach (WeaponSaveEntry entry in saveData.ownedWeapons)
                {
                    WeaponDataSO weapon = _database.GetWeapon(entry.family, entry.variant, entry.rarity);
                    if (weapon == null)
                    {
                        Debug.LogWarning($"[WeaponInventoryService] 存檔中的武器（{entry.family}/變體{entry.variant}/{entry.rarity}）在資料庫中已找不到對應資產，略過這筆存檔資料");
                        continue;
                    }

                    _ownedWeapons[weapon] = new WeaponInstance(weapon, entry.upgradeLevel);
                    if (entry.duplicateCount > 0)
                    {
                        _duplicateCounts[weapon] = entry.duplicateCount;
                    }
                }
            }

            if (saveData.weaponShards != null)
            {
                foreach (ShardSaveEntry entry in saveData.weaponShards)
                {
                    if (entry.shardCount > 0)
                    {
                        _shardCounts[(entry.family, entry.rarity)] = entry.shardCount;
                    }
                }
            }
        }

        /// <summary>
        /// 還原上次離線前裝備的武器。equippedWeaponVariant 為 0（尚未存過任何裝備紀錄）或資料庫查無對應資產
        /// 時，安全地不做任何事，維持 WeaponSwitcher 自己 Awake() 設定的預設武器。
        /// </summary>
        private void RestoreEquippedWeapon()
        {
            if (_weaponSwitcher == null || _database == null)
            {
                return;
            }

            SaveData saveData = SaveLifecycleController.CurrentSaveData;
            if (saveData.equippedWeaponVariant <= 0)
            {
                return;
            }

            WeaponDataSO weapon = _database.GetWeapon(saveData.equippedWeaponFamily, saveData.equippedWeaponVariant, saveData.equippedWeaponRarity);
            if (weapon == null)
            {
                Debug.LogWarning($"[WeaponInventoryService] 存檔中裝備的武器（{saveData.equippedWeaponFamily}/變體{saveData.equippedWeaponVariant}/{saveData.equippedWeaponRarity}）在資料庫中已找不到對應資產，維持預設裝備武器");
                return;
            }

            _weaponSwitcher.EquipWeapon(weapon);
        }

        /// <summary>裝備切換時同步寫入存檔（僅更新記憶體中的 CurrentSaveData，不主動觸發落盤，
        /// 落盤時機沿用既定的 OnApplicationQuit／RequestSave）。</summary>
        private void HandleWeaponEquippedForSave(WeaponDataSO weapon)
        {
            SaveData saveData = SaveLifecycleController.CurrentSaveData;
            saveData.equippedWeaponFamily = weapon.Family;
            saveData.equippedWeaponVariant = weapon.Variant;
            saveData.equippedWeaponRarity = weapon.Rarity;
        }

        /// <summary>
        /// 將指定武器目前的執行期狀態（合成等級、重複品數量）同步寫入 CurrentSaveData 對應的清單項目，
        /// 找不到既有項目時新增一筆。呼叫端負責在每次背包狀態異動後呼叫，避免存檔內容要等到寫入當下
        /// 才整包重新掃描背包。
        /// </summary>
        private void SyncOwnedWeaponEntry(WeaponDataSO weapon)
        {
            if (!_ownedWeapons.TryGetValue(weapon, out WeaponInstance instance))
            {
                return;
            }

            List<WeaponSaveEntry> entries = SaveLifecycleController.CurrentSaveData.ownedWeapons;
            for (int i = 0; i < entries.Count; i++)
            {
                WeaponSaveEntry entry = entries[i];
                if (entry.family == weapon.Family && entry.variant == weapon.Variant && entry.rarity == weapon.Rarity)
                {
                    entry.upgradeLevel = instance.UpgradeLevel;
                    entry.duplicateCount = GetDuplicateCount(weapon);
                    entries[i] = entry;
                    return;
                }
            }

            entries.Add(new WeaponSaveEntry
            {
                family = weapon.Family,
                variant = weapon.Variant,
                rarity = weapon.Rarity,
                upgradeLevel = instance.UpgradeLevel,
                duplicateCount = GetDuplicateCount(weapon)
            });
        }

        /// <summary>將指定家族+稀有度目前的碎片數量同步寫入 CurrentSaveData 對應的清單項目，
        /// 找不到既有項目時新增一筆。</summary>
        private void SyncShardEntry(WeaponFamily family, WeaponRarity rarity)
        {
            int shardCount = GetShardCount(family, rarity);
            List<ShardSaveEntry> entries = SaveLifecycleController.CurrentSaveData.weaponShards;
            for (int i = 0; i < entries.Count; i++)
            {
                ShardSaveEntry entry = entries[i];
                if (entry.family == family && entry.rarity == rarity)
                {
                    entry.shardCount = shardCount;
                    entries[i] = entry;
                    return;
                }
            }

            entries.Add(new ShardSaveEntry { family = family, rarity = rarity, shardCount = shardCount });
        }
    }
}
