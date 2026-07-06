using UnityEngine;
using DeskSlayer.Enemy;

namespace DeskSlayer.Juice
{
    /// <summary>
    /// 表現層橋接元件：訂閱同一個 GameObject 上 EnemyController 的 OnHit 事件，
    /// 在敵人頭頂位置生成一個 DamagePopup 顯示本次傷害。比照 EnemyHurtVisualDispatcher 的作法，
    /// 純粹轉發表現效果，不參與任何戰鬥數值運算。
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class DamagePopupDispatcher : MonoBehaviour
    {
        [SerializeField]
        private DamagePopup _damagePopupPrefab;

        [SerializeField, Tooltip("相對於敵人 Transform 的生成偏移量（世界座標），預設往上偏移到頭頂位置")]
        private Vector3 _spawnOffset = new Vector3(0f, 1.6f, 0f);

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
            if (_damagePopupPrefab == null)
            {
                return;
            }

            DamagePopup popup = Instantiate(_damagePopupPrefab, transform.position + _spawnOffset, Quaternion.identity);
            popup.Show(damage);
        }
    }
}
