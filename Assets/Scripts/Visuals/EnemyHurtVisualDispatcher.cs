using UnityEngine;
using DeskSlayer.Enemy;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱同一個 GameObject 上 EnemyController 的 OnHit 事件，觸發 Hurt 動畫狀態。
    /// 播放完畢由 Animator 狀態機的 Exit Time 轉場自動回到 Idle，這裡不需要額外收尾邏輯。
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyHurtVisualDispatcher : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");

        private EnemyController _enemyController;

        private void Awake()
        {
            _enemyController = GetComponent<EnemyController>();
        }

        private void OnEnable()
        {
            _enemyController.OnHit += HandleHit;
        }

        private void OnDisable()
        {
            _enemyController.OnHit -= HandleHit;
        }

        private void HandleHit(int damage)
        {
            _animator.SetTrigger(HurtTriggerHash);
        }
    }
}
