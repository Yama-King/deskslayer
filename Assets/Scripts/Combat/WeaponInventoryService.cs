using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 管理玩家擁有的武器實例、重複品數量與武器碎片數量，負責判定「是否可合成／兌換」並執行。
    /// 比照 AudioManager／HitStopController 的靜態單例作法：WeaponDropDispatcher 掛在各敵人身上，
    /// 無法在 Prefab 編輯階段直接參照場景中唯一的背包服務，透過 Instance 存取即可，不需要手動接線。
    /// 合成消耗、碎片兌換消耗與加成規則完全讀取 WeaponDropConfigSO，這裡不寫死任何數值。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponInventoryService : MonoBehaviour
    {
        [SerializeField]
        private WeaponDatabaseSO _database;

        [SerializeField]
        private WeaponDropConfigSO _dropConfig;

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
            GrantPermanentStarterWeapons();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
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
                OnWeaponObtained?.Invoke(instance);
                return WeaponDropOutcome.NewWeapon;
            }

            int newCount = GetDuplicateCount(weapon) + 1;
            _duplicateCounts[weapon] = newCount;
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
            OnShardCountChanged?.Invoke(weapon.Family, weapon.Rarity, newCount);

            WeaponInstance instance = new WeaponInstance(weapon);
            _ownedWeapons[weapon] = instance;
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
                OnWeaponObtained?.Invoke(instance);
            }
        }
    }
}
