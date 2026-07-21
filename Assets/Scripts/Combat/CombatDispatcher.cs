using System;
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

        /// <summary>輕攻擊實際造成傷害時發出（無敵幀吞掉的命中不算），帶入受擊的敵人，供 HitVfxDirector 等純表現層模組訂閱。</summary>
        public event Action<EnemyController> OnLightAttackHit;

        /// <summary>重攻擊實際造成傷害時發出（無敵幀吞掉的命中不算），帶入受擊的敵人，供 HitVfxDirector 等純表現層模組訂閱。</summary>
        public event Action<EnemyController> OnHeavyAttackHit;

        /// <summary>任一攻擊（輕/重皆算）實際造成傷害時發出，帶入受擊的敵人，供 HitVfxDirector 等純表現層模組訂閱。</summary>
        public event Action<EnemyController> OnEnemyHit;

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

            EnemyController hitEnemy = _targetEnemy;
            int healthBeforeHit = hitEnemy.CurrentHealth;
            hitEnemy.TakeHit(result);

            // 命中特效的觸發時機不能只看 result.IsHit——命中仍可能因為敵人正處於無敵幀而被 EnemyController
            // 內部吞掉，不套用傷害、也不會發出 Hurt 動畫/傷害飄字/受擊音效依賴的 OnHit。若特效只依 IsHit
            // 觸發，連續打字時會在無敵幀期間持續閃爆特效，跟畫面上「有沒有真的打中」的其他回饋脫節。
            // 改用 TakeHit 前後的血量差判斷「這次是否真的造成傷害」，語意與既有受擊回饋保持一致。
            if (hitEnemy.CurrentHealth < healthBeforeHit)
            {
                OnEnemyHit?.Invoke(hitEnemy);

                if (weapon is HeavyWeaponSO)
                {
                    OnHeavyAttackHit?.Invoke(hitEnemy);
                }
                else
                {
                    OnLightAttackHit?.Invoke(hitEnemy);
                }
            }
        }
    }
}
