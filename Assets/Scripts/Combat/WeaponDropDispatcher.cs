using System;
using UnityEngine;
using DeskSlayer.Enemy;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 武器掉落系統的橋接元件：訂閱 EnemyController 的死亡事件，透過 IWeaponDropResolver 判定
    /// 本次掉落的武器後交給 WeaponInventoryService 收下。比照 CombatDispatcher 的 Mediator 模式，
    /// EnemyController 完全不需要認識武器／掉落系統的存在，也不需要修改它的核心邏輯。
    /// </summary>
    public sealed class WeaponDropDispatcher : MonoBehaviour
    {
        /// <summary>掉落判定完成且成功加入背包後發出，帶入掉落的武器資料、死亡位置與本次實際結果
        /// （取得新武器／重複品／轉換為碎片），供純表現層模組（掉落提示、記錄清單、音效等）訂閱，
        /// 不參與任何掉落判定邏輯。</summary>
        public event Action<WeaponDataSO, Vector3, WeaponDropOutcome> OnWeaponDropped;

        [SerializeField]
        private EnemyController _targetEnemy;

        [SerializeField]
        private WeaponDatabaseSO _database;

        [SerializeField]
        private WeaponDropConfigSO _dropConfig;

        [SerializeField]
        private WeaponInventoryService _inventoryService;

        private IWeaponDropResolver _dropResolver;

        private void Awake()
        {
            _dropResolver = new DefaultWeaponDropResolver();
        }

        private void OnEnable()
        {
            if (_targetEnemy != null)
            {
                _targetEnemy.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (_targetEnemy != null)
            {
                _targetEnemy.OnDeath -= HandleDeath;
            }
        }

        /// <summary>切換目前訂閱死亡事件的敵人，供 EnemyRotationManager 在生成新敵人後轉移目標使用。</summary>
        public void SetTarget(EnemyController target)
        {
            if (_targetEnemy != null)
            {
                _targetEnemy.OnDeath -= HandleDeath;
            }

            _targetEnemy = target;

            if (_targetEnemy != null)
            {
                _targetEnemy.OnDeath += HandleDeath;
            }
        }

        private void HandleDeath()
        {
            if (_inventoryService == null || _database == null || _dropConfig == null)
            {
                return;
            }

            WeaponDataSO drop = _dropResolver.ResolveDrop(_database, _dropConfig);
            if (drop == null)
            {
                Debug.Log("[WeaponDropDispatcher] 本次無武器掉落（該家族尚未建置任何武器資產）");
                return;
            }

            WeaponDropOutcome outcome;
            if (_inventoryService.IsWeaponMaxed(drop))
            {
                _inventoryService.AddShard(drop.Family, drop.Rarity);
                outcome = WeaponDropOutcome.ShardConverted;
            }
            else
            {
                outcome = _inventoryService.AddDrop(drop);
            }

            Debug.Log($"[WeaponDropDispatcher] 掉落武器：{drop.WeaponName}（{drop.Family}/{drop.Rarity}/變體{drop.Variant}），結果：{outcome}");
            OnWeaponDropped?.Invoke(drop, _targetEnemy.transform.position, outcome);
        }
    }
}
