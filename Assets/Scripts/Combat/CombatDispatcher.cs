using UnityEngine;
using DeskSlayer.Enemy;
using DeskSlayer.Juice;

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

        private TypingEnergySystem _typingEnergySystem;
        private ICombatResolver _combatResolver;

        private void Awake()
        {
            _typingEnergySystem = GetComponent<TypingEnergySystem>();
            _combatResolver = new DefaultCombatResolver();
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

        private void HandleAttackTriggered(WeaponDataSO weapon)
        {
            if (_targetEnemy == null)
            {
                return;
            }

            CombatResult result = _combatResolver.Resolve(weapon, _targetEnemy.EnemyData);

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
