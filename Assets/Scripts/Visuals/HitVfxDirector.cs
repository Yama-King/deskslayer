using UnityEngine;
using DeskSlayer.Combat;
using DeskSlayer.Enemy;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 命中特效系統的橋接元件：訂閱 CombatDispatcher 新增的命中事件，交給對應的
    /// PooledSpriteAnimationPool 播放。輕/重攻擊粒子的錨點固定讀取玩家角色身上的
    /// PlayerHitVfxAnchors（攻擊來源，場上唯一且不會被銷毀重生，直接用 Inspector 參照即可）；
    /// 敵人受擊特效的錨點則依事件帶入的受擊 EnemyController 動態查找其身上的 EnemyHitVfxAnchors
    /// （敵人會被銷毀重生，無法用固定參照）。
    /// 純粹轉發表現效果，不參與任何戰鬥數值運算，也不認識輸入系統/風格分析系統等既有系統，
    /// 維持單向依賴——只訂閱戰鬥判定結果，不影響上游。
    /// 比照 AudioManager 提供靜態 Instance 存取入口，讓場上唯一一隻敵人的 EnemyHitVfxAnchors
    /// 在死亡前可以直接呼叫 ReleaseAllBorrowedVfx()，不需要額外的雙向參照佈線
    /// （不比照 CombatDispatcher/WeaponDropDispatcher 由 EnemyRotationManager 逐一 SetTarget 的作法，
    /// 因為本系統不需要持有「目前目標」狀態，事件本身已經帶入受擊的 EnemyController）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitVfxDirector : MonoBehaviour
    {
        [SerializeField]
        private CombatDispatcher _combatDispatcher;

        [SerializeField, Tooltip("玩家角色身上的命中特效錨點容器，提供輕/重攻擊粒子的生成錨點")]
        private PlayerHitVfxAnchors _playerAnchors;

        [SerializeField, Tooltip("輕攻擊粒子特效的 Object Pool")]
        private PooledSpriteAnimationPool _lightAttackPool;

        [SerializeField, Tooltip("重攻擊粒子特效的 Object Pool")]
        private PooledSpriteAnimationPool _heavyAttackPool;

        [SerializeField, Tooltip("敵人受擊特效的 Object Pool")]
        private PooledSpriteAnimationPool _enemyHitPool;

        /// <summary>簡易靜態存取入口，供 EnemyHitVfxAnchors 在敵人死亡前呼叫 ReleaseAllBorrowedVfx。</summary>
        public static HitVfxDirector Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[HitVfxDirector] 場上已存在另一個 HitVfxDirector 實例，{name} 將被停用", this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            if (_combatDispatcher == null)
            {
                Debug.LogWarning($"[HitVfxDirector] {name} 未指定 _combatDispatcher，命中特效系統停用", this);
                return;
            }

            _combatDispatcher.OnLightAttackHit += HandleLightAttackHit;
            _combatDispatcher.OnHeavyAttackHit += HandleHeavyAttackHit;
            _combatDispatcher.OnEnemyHit += HandleEnemyHit;
        }

        private void OnDisable()
        {
            if (_combatDispatcher == null)
            {
                return;
            }

            _combatDispatcher.OnLightAttackHit -= HandleLightAttackHit;
            _combatDispatcher.OnHeavyAttackHit -= HandleHeavyAttackHit;
            _combatDispatcher.OnEnemyHit -= HandleEnemyHit;
        }

        /// <summary>強制回收三個 Pool 目前所有借出中的物件，供 EnemyHitVfxAnchors 在敵人死亡前呼叫。</summary>
        public void ReleaseAllBorrowedVfx()
        {
            _lightAttackPool?.ReleaseAllBorrowed();
            _heavyAttackPool?.ReleaseAllBorrowed();
            _enemyHitPool?.ReleaseAllBorrowed();
        }

        private void HandleLightAttackHit(EnemyController target)
        {
            if (_playerAnchors != null)
            {
                _lightAttackPool?.TrySpawn(_playerAnchors.LightAttackAnchor);
            }
        }

        private void HandleHeavyAttackHit(EnemyController target)
        {
            if (_playerAnchors != null)
            {
                _heavyAttackPool?.TrySpawn(_playerAnchors.HeavyAttackAnchor);
            }
        }

        private void HandleEnemyHit(EnemyController target)
        {
            EnemyHitVfxAnchors anchors = target != null ? target.GetComponent<EnemyHitVfxAnchors>() : null;
            if (anchors != null)
            {
                _enemyHitPool?.TrySpawn(anchors.EnemyHitAnchor);
            }
        }
    }
}
