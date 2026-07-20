using UnityEngine;
using DeskSlayer.Enemy;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱同一個 GameObject 上 EnemyController 的 OnHealthChanged 事件，
    /// 轉發給子物件的 EnemyHealthBarView 更新血條顯示。比照 EnemyHurtVisualDispatcher 的作法，
    /// 純粹轉發資料，不參與任何戰鬥數值運算，也不直接讀寫 EnemyController 的內部狀態。
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyHealthBarDispatcher : MonoBehaviour
    {
        [SerializeField]
        private EnemyHealthBarView _healthBarView;

        private EnemyController _enemyController;

        private void Awake()
        {
            _enemyController = GetComponent<EnemyController>();
        }

        private void OnEnable()
        {
            _enemyController.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            _enemyController.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int currentHealth, int maxHealth)
        {
            if (_healthBarView == null)
            {
                return;
            }

            _healthBarView.UpdateHealth(currentHealth, maxHealth);
        }
    }
}
