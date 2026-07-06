using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 重武器數據。累積按鍵次數達到 _keyPressThreshold 時觸發一次重攻擊。
    /// </summary>
    [CreateAssetMenu(fileName = "NewHeavyWeapon", menuName = "DeskSlayer/Combat/Heavy Weapon", order = 1)]
    public sealed class HeavyWeaponSO : WeaponDataSO
    {
        [SerializeField, Min(1), Tooltip("累積多少次按鍵才觸發一次重攻擊")]
        private int _keyPressThreshold = 20;

        private int _accumulatedKeyPressCount;

        /// <summary>觸發重攻擊所需的累積按鍵次數。</summary>
        public int KeyPressThreshold => _keyPressThreshold;

        /// <summary>
        /// 重武器的觸發規則：內部累積按鍵次數，達到 <see cref="KeyPressThreshold"/> 才回傳 true 並重置計數。
        /// ScriptableObject 資產本質是共享、跨場景持久的資料容器，這裡刻意借用它與 MonoBehaviour 相同的
        /// OnEnable 生命週期（Domain Reload / 進入 Play Mode 時都會呼叫）重置累積值，避免上一次執行的殘留狀態
        /// 污染下一次遊玩；但若同一個武器資產被多個裝備者同時參照，累積計數會被共用，此設計假設同一時間
        /// 只有一名玩家裝備同一把武器。
        /// </summary>
        public override bool TryTriggerAttack()
        {
            _accumulatedKeyPressCount++;
            if (_accumulatedKeyPressCount < _keyPressThreshold)
            {
                return false;
            }

            _accumulatedKeyPressCount = 0;
            return true;
        }

        private void OnEnable()
        {
            _accumulatedKeyPressCount = 0;
        }
    }
}
