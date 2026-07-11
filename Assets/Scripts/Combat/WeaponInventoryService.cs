using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 管理玩家擁有的武器實例與重複品數量，負責判定「是否可合成」並執行合成。
    /// 比照 AudioManager／HitStopController 的靜態單例作法：WeaponDropDispatcher 掛在各敵人身上，
    /// 無法在 Prefab 編輯階段直接參照場景中唯一的背包服務，透過 Instance 存取即可，不需要手動接線。
    /// 合成消耗與加成規則完全讀取 WeaponDropConfigSO，這裡不寫死任何數值。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponInventoryService : MonoBehaviour, IWeaponUpgradeLevelProvider
    {
        [SerializeField]
        private WeaponDatabaseSO _database;

        [SerializeField]
        private WeaponDropConfigSO _dropConfig;

        public static WeaponInventoryService Instance { get; private set; }

        private readonly Dictionary<WeaponDataSO, WeaponInstance> _ownedWeapons = new Dictionary<WeaponDataSO, WeaponInstance>();
        private readonly Dictionary<WeaponDataSO, int> _duplicateCounts = new Dictionary<WeaponDataSO, int>();

        /// <summary>首次取得一把新武器（背包中原本沒有）時發出，供未來背包 UI 顯示「新武器」提示。</summary>
        public event Action<WeaponInstance> OnWeaponObtained;

        /// <summary>取得已擁有武器的重複品時發出，帶入該武器目前的重複品總數。</summary>
        public event Action<WeaponDataSO, int> OnDuplicateObtained;

        /// <summary>武器合成升級成功時發出。</summary>
        public event Action<WeaponInstance> OnWeaponUpgraded;

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

        /// <summary>查詢指定武器目前的合成等級，尚未擁有時視為 0 級，供 IWeaponDropResolver 判斷封頂使用。</summary>
        public int GetUpgradeLevel(WeaponDataSO weapon)
        {
            return _ownedWeapons.TryGetValue(weapon, out WeaponInstance instance) ? instance.UpgradeLevel : 0;
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
        /// 已擁有時視為「重複品」，累加重複品數量供之後合成消耗。
        /// </summary>
        public void AddDrop(WeaponDataSO weapon)
        {
            if (weapon == null)
            {
                return;
            }

            if (!_ownedWeapons.ContainsKey(weapon))
            {
                WeaponInstance instance = new WeaponInstance(weapon);
                _ownedWeapons[weapon] = instance;
                OnWeaponObtained?.Invoke(instance);
                return;
            }

            int newCount = GetDuplicateCount(weapon) + 1;
            _duplicateCounts[weapon] = newCount;
            OnDuplicateObtained?.Invoke(weapon, newCount);
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
