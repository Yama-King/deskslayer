using UnityEngine;

namespace DeskSlayer.Enemy
{
    /// <summary>
    /// 敵人數值資料。比照 WeaponDataSO 的作法採 ScriptableObject，
    /// 讓數值平衡調整不需重新編譯程式碼，同時方便日後以資產形式管理多種敵人。
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "DeskSlayer/Enemy/Enemy Data", order = 0)]
    public sealed class EnemyDataSO : ScriptableObject
    {
        [SerializeField]
        private string _enemyName;

        [SerializeField, Min(1)]
        private int _maxHealth = 100;

        [SerializeField, Min(0)]
        private int _defense = 5;

        [SerializeField, Min(0f), Tooltip("受擊後的無敵幀時間（秒），期間內的攻擊不重複計算傷害")]
        private float _iFrameDuration = 0.3f;

        [SerializeField, Min(0.1f), Tooltip("死亡溶解動畫總時長（秒），供 EnemyDeathVisualDispatcher 播放溶解效果使用")]
        private float _dissolveDuration = 1.2f;

        /// <summary>敵人顯示名稱。</summary>
        public string EnemyName => _enemyName;

        /// <summary>最大生命值。</summary>
        public int MaxHealth => _maxHealth;

        /// <summary>防禦力，供 ICombatResolver 計算命中率與傷害使用。</summary>
        public int Defense => _defense;

        /// <summary>無敵幀時間（秒），供 EnemyController 建立 IFrameGuard 使用。</summary>
        public float IFrameDuration => _iFrameDuration;

        /// <summary>死亡溶解動畫總時長（秒），供 EnemyDeathVisualDispatcher 使用。</summary>
        public float DissolveDuration => _dissolveDuration;
    }
}
