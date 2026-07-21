using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 敵人 Prefab 端的命中特效錨點容器。輕/重攻擊粒子改掛在玩家角色身上（見 PlayerHitVfxAnchors），
    /// 這裡只保留「敵人受擊特效」——概念上這是敵人身體對命中的反應，理應跟著敵人本體定位，
    /// 而不是跟著攻擊來源。錨點為此 Prefab 底下的子物件，方便直接在場景/Prefab 編輯模式下
    /// 手動拖曳調整局部座標定位，不用程式計算精確碰撞座標。
    /// 敵人死亡溶解播放完成、即將被 Destroy() 前，主動請 HitVfxDirector 強制回收所有目前借出中的
    /// 特效物件——這些物件因為掛在本敵人底下，若不主動處理會隨著 Destroy() 被動一起銷毀，
    /// 從對應的 Object Pool 中永久流失，導致 Pool 容量隨遊玩時間遞減。
    /// </summary>
    [RequireComponent(typeof(EnemyDeathVisualDispatcher))]
    public sealed class EnemyHitVfxAnchors : MonoBehaviour
    {
        [SerializeField, Tooltip("敵人受擊特效的生成錨點")]
        private Transform _enemyHitAnchor;

        private EnemyDeathVisualDispatcher _deathVisualDispatcher;

        /// <summary>敵人受擊特效的生成錨點。</summary>
        public Transform EnemyHitAnchor => _enemyHitAnchor;

        private void Awake()
        {
            _deathVisualDispatcher = GetComponent<EnemyDeathVisualDispatcher>();
        }

        private void OnEnable()
        {
            _deathVisualDispatcher.OnDissolveComplete += HandleDissolveComplete;
        }

        private void OnDisable()
        {
            _deathVisualDispatcher.OnDissolveComplete -= HandleDissolveComplete;
        }

        private void HandleDissolveComplete()
        {
            HitVfxDirector.Instance?.ReleaseAllBorrowedVfx();
        }
    }
}
