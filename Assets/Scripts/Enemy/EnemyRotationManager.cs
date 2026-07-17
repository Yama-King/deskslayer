using UnityEngine;
using DeskSlayer.Combat;
using DeskSlayer.Visuals;
using DeskSlayer.Weather;

namespace DeskSlayer.Enemy
{
    /// <summary>
    /// 單一敵人死亡輪換管理器：場上永遠只有一隻敵人，死亡溶解播放完成、GameObject 銷毀後，
    /// 從三種敵人 Prefab 中隨機挑一種在同一出生點生成下一隻，並將 CombatDispatcher、WeaponDropDispatcher
    /// 的目標一併轉移到新敵人。不做波次、難度遞增等額外管理邏輯。
    /// </summary>
    public sealed class EnemyRotationManager : MonoBehaviour
    {
        [SerializeField, Tooltip("三種敵人的 Prefab 根物件（皆需掛有 EnemyController 與 EnemyDeathVisualDispatcher）")]
        private EnemyController[] _enemyPrefabs;

        [SerializeField]
        private Transform _spawnPoint;

        [SerializeField, Tooltip("新敵人要 parent 到的世界根節點，讓拖曳 GameWorldRoot 時場上的敵人也會一起跟著移動")]
        private Transform _gameWorldRoot;

        [SerializeField]
        private CombatDispatcher _combatDispatcher;

        [SerializeField, Tooltip("武器掉落系統的橋接元件，敵人輪換時需一併轉移目標，否則新敵人死亡不會觸發掉落判定")]
        private WeaponDropDispatcher _weaponDropDispatcher;

        [SerializeField, Tooltip("天氣服務來源，敵人生成後注入給其 SpriteWeatherTintDispatcher，留空時新敵人不套用天氣色調")]
        private WeatherService _weatherService;

        private void Start()
        {
            SpawnRandomEnemy();
        }

        private void SpawnRandomEnemy()
        {
            EnemyController prefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Length)];
            EnemyController instance = Instantiate(prefab, _spawnPoint.position, prefab.transform.rotation, _gameWorldRoot);

            EnemyDeathVisualDispatcher deathDispatcher = instance.GetComponent<EnemyDeathVisualDispatcher>();
            deathDispatcher.OnDissolveComplete += HandleDefeated;

            SpriteWeatherTintDispatcher tintDispatcher = instance.GetComponent<SpriteWeatherTintDispatcher>();
            if (tintDispatcher != null)
            {
                tintDispatcher.Initialize(_weatherService);
            }

            _combatDispatcher.SetTarget(instance);

            if (_weaponDropDispatcher != null)
            {
                _weaponDropDispatcher.SetTarget(instance);
            }

            void HandleDefeated()
            {
                deathDispatcher.OnDissolveComplete -= HandleDefeated;
                SpawnRandomEnemy();
            }
        }
    }
}
