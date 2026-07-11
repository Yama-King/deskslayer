using System;
using UnityEngine;
using DeskSlayer.Combat;

namespace DeskSlayer.Enemy
{
    /// <summary>
    /// 敵人控制器。持有目前生命值並提供 TakeHit 套用 ICombatResolver 的判定結果，
    /// 內建冷卻式無敵幀（I-frame）避免同一波攻擊被重複計算傷害。
    /// 生命值歸零時發出一次性的 OnDeath 事件，死亡後不再接受傷害判定；
    /// 銷毀與重生交由表現層（EnemyDeathVisualDispatcher）與 EnemyRotationManager 處理。
    /// </summary>
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField]
        private EnemyDataSO _enemyData;

        private int _currentHealth;
        private bool _isDead;
        private IFrameGuard _iFrameGuard;

        /// <summary>實際受到傷害時發出，帶入本次造成的傷害值，供 AudioDispatcher、DamagePopupDispatcher 等純表現層模組訂閱。</summary>
        public event Action<int> OnHit;

        /// <summary>生命值歸零時發出一次，供死亡表現層（如 EnemyDeathVisualDispatcher）訂閱。</summary>
        public event Action OnDeath;

        /// <summary>此敵人的數值資料，供 CombatDispatcher 呼叫 ICombatResolver.Resolve() 使用。</summary>
        public EnemyDataSO EnemyData => _enemyData;

        /// <summary>目前生命值。</summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>是否已死亡，死亡後不再接受傷害判定。</summary>
        public bool IsDead => _isDead;

        private void Awake()
        {
            if (_enemyData == null)
            {
                Debug.LogWarning($"[EnemyController] {name} 未指定 _enemyData，敵人邏輯將停用", this);
                return;
            }

            _currentHealth = _enemyData.MaxHealth;
            _iFrameGuard = new IFrameGuard(_enemyData.IFrameDuration);
        }

        private void Update()
        {
            _iFrameGuard?.Tick(Time.deltaTime);
        }

        /// <summary>
        /// 套用一次 ICombatResolver 的判定結果。未命中或處於無敵幀期間皆不扣血。
        /// </summary>
        public void TakeHit(CombatResult result)
        {
            if (_isDead || _enemyData == null)
            {
                return;
            }

            if (!result.IsHit)
            {
                Debug.Log($"[EnemyController] {_enemyData.EnemyName} 閃避了攻擊，未命中");
                return;
            }

            if (_iFrameGuard.IsActive)
            {
                Debug.Log($"[EnemyController] {_enemyData.EnemyName} 處於無敵幀，本次傷害不計算");
                return;
            }

            _currentHealth -= result.Damage;
            _iFrameGuard.Trigger();

            Debug.Log($"[EnemyController] {_enemyData.EnemyName} 受到 {result.Damage} 點傷害，剩餘生命值 {_currentHealth}/{_enemyData.MaxHealth}");
            OnHit?.Invoke(result.Damage);

            if (_currentHealth <= 0 && !_isDead)
            {
                _isDead = true;
                Debug.Log($"[EnemyController] {_enemyData.EnemyName} 死亡");
                OnDeath?.Invoke();
            }
        }
    }
}
