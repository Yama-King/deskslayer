using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 玩家角色端的命中特效錨點容器。輕/重攻擊粒子代表的是「攻擊來源」的揮擊效果，
    /// 因此掛在玩家角色身上而非敵人身上——跟敵人受擊特效（EnemyHitVfxAnchors，掛在敵人身上）
    /// 是刻意分開的兩端。兩個 Transform 皆為玩家角色底下的子物件，方便直接在場景編輯模式下
    /// 手動拖曳調整局部座標定位，不用程式計算精確碰撞座標。
    /// 玩家角色是場上唯一、不會被銷毀重生的持久物件，因此不需要比照 EnemyHitVfxAnchors
    /// 處理「物件即將被 Destroy() 前強制回收借出特效」的情境。
    /// </summary>
    public sealed class PlayerHitVfxAnchors : MonoBehaviour
    {
        [SerializeField, Tooltip("輕攻擊粒子特效的生成錨點")]
        private Transform _lightAttackAnchor;

        [SerializeField, Tooltip("重攻擊粒子特效的生成錨點")]
        private Transform _heavyAttackAnchor;

        /// <summary>輕攻擊粒子特效的生成錨點。</summary>
        public Transform LightAttackAnchor => _lightAttackAnchor;

        /// <summary>重攻擊粒子特效的生成錨點。</summary>
        public Transform HeavyAttackAnchor => _heavyAttackAnchor;
    }
}
