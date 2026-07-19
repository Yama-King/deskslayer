using System;
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
        /// 目前蓄力進度，正規化為 0~1（累積按鍵次數 / 閾值），純粹由既有累積值換算，不影響觸發判定本身。
        /// 供表現層（例如蓄力發光/抖動視覺回饋）查詢，未蓄力或閾值設定異常時回傳 0。
        /// </summary>
        public float ChargeProgress01 => _keyPressThreshold > 0
            ? Mathf.Clamp01((float)_accumulatedKeyPressCount / _keyPressThreshold)
            : 0f;

        /// <summary>
        /// 每次累積按鍵後廣播一次目前蓄力進度（0~1），供表現層即時同步視覺效果，不等到真正觸發才通知。
        /// </summary>
        public event Action<float> OnChargeProgressChanged;

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
            OnChargeProgressChanged?.Invoke(ChargeProgress01);

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
