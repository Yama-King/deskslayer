using UnityEngine;
using DeskSlayer.Combat;
using DeskSlayer.Visuals;

namespace DeskSlayer.Enemy
{
    /// <summary>
    /// 單一敵人死亡輪換管理器：場上永遠只有一隻敵人，死亡溶解播放完成、GameObject 銷毀後，
    /// 從三種敵人 Prefab 中隨機挑一種在同一出生點生成下一隻，並將 CombatDispatcher 的攻擊目標
    /// 轉移到新敵人。不做波次、難度遞增等額外管理邏輯。
    /// </summary>
    public sealed class EnemyRotationManager : MonoBehaviour
    {
        [SerializeField, Tooltip("三種敵人的 Prefab 根物件（皆需掛有 EnemyController 與 EnemyDeathVisualDispatcher）")]
        private EnemyController[] _enemyPrefabs;

        [SerializeField]
        private Transform _spawnPoint;

        [SerializeField]
        private CombatDispatcher _combatDispatcher;

        private void Start()
        {
            SpawnRandomEnemy();
        }

        private void SpawnRandomEnemy()
        {
            EnemyController prefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Length)];
            EnemyController instance = Instantiate(prefab, _spawnPoint.position, prefab.transform.rotation);

            EnemyDeathVisualDispatcher deathDispatcher = instance.GetComponent<EnemyDeathVisualDispatcher>();
            deathDispatcher.OnDissolveComplete += HandleDefeated;

            _combatDispatcher.SetTarget(instance);

            void HandleDefeated()
            {
                deathDispatcher.OnDissolveComplete -= HandleDefeated;
                SpawnRandomEnemy();
            }
        }
    }
}
