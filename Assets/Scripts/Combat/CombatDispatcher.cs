using UnityEngine;
using DeskSlayer.Enemy;
using DeskSlayer.Juice;
using DeskSlayer.Weather;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 戰鬥系統的橋接元件：訂閱 TypingEnergySystem 的輕/重攻擊觸發事件，
    /// 透過 ICombatResolver 計算判定結果後套用到目標敵人。
    /// 讓 TypingEnergySystem（輸入→觸發）與 EnemyController（敵人狀態）互不直接依賴，
    /// 兩者只透過事件與 CombatResult 這類資料溝通。
    /// </summary>
    [RequireComponent(typeof(TypingEnergySystem))]
    public sealed class CombatDispatcher : MonoBehaviour
    {
        [SerializeField]
        private EnemyController _targetEnemy;

        [SerializeField, Tooltip("天氣數值彙整來源，留空時視為無天氣修正")]
        private WeatherService _weatherService;

        private TypingEnergySystem _typingEnergySystem;
        private ICombatResolver _combatResolver;
        private PlayerCombatStatsProvider _combatStatsProvider;

        private void Awake()
        {
            _typingEnergySystem = GetComponent<TypingEnergySystem>();
            _combatResolver = new DefaultCombatResolver();
        }

        private void Start()
        {
            // 延後到 Start 才建立，確保 WeatherService.Awake()（套用快取天氣分類）已經執行完畢，
            // 避免依 Script Execution Order 而讀到尚未套用快取的初始值。
            _combatStatsProvider = new PlayerCombatStatsProvider(_weatherService);
        }

        private void OnEnable()
        {
            _typingEnergySystem.OnLightAttackTriggered += HandleAttackTriggered;
            _typingEnergySystem.OnHeavyAttackTriggered += HandleAttackTriggered;
        }

        private void OnDisable()
        {
            _typingEnergySystem.OnLightAttackTriggered -= HandleAttackTriggered;
            _typingEnergySystem.OnHeavyAttackTriggered -= HandleAttackTriggered;
        }

        private void OnDestroy()
        {
            _combatStatsProvider?.Dispose();
        }

        /// <summary>切換目前的攻擊目標，供 EnemyRotationManager 在生成新敵人後轉移目標使用。</summary>
        public void SetTarget(EnemyController target)
        {
            _targetEnemy = target;
        }

        private void HandleAttackTriggered(WeaponDataSO weapon)
        {
            if (_targetEnemy == null)
            {
                return;
            }

            int finalAttackPower = _combatStatsProvider.GetFinalAttackPower(weapon.BaseDamage);
            int finalDefensePower = _targetEnemy.EnemyData.Defense;

            CombatResult result = _combatResolver.Resolve(finalAttackPower, finalDefensePower);

            Debug.Log(result.IsHit
                ? $"[CombatDispatcher] {weapon.WeaponName} 命中，傷害 {result.Damage}"
                : $"[CombatDispatcher] {weapon.WeaponName} 未命中");

            if (result.IsHit)
            {
                // 頓幀時長讀取「這次觸發攻擊的武器」自己的 HitStopDuration，輕/重攻擊各自獨立、不共用同一數值。
                HitStopController.Instance?.Trigger(weapon.HitStopDuration);
            }

            _targetEnemy.TakeHit(result);
        }
    }
}
