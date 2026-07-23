using System;
using UnityEngine;
using DeskSlayer.AttackInput;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 打字連擊計數器的邏輯核心：訂閱同一個 GameObject 上 AttackInputAggregator 的
    /// OnAttackInputTriggered 事件，反映「有效攻擊次數」而非單純的「有效打字次數」——
    /// 滑鼠攻擊是否計入連擊，交由彙整層既有的滑鼠偵測開關判定（GameSettingsPreferenceStore.
    /// MouseAttackInputEnabled），這裡不重複判斷輸入來源，未來彙整層新增第三種輸入來源時
    /// 也不需要回頭修改這裡。純粹自行維護計數與閒置計時狀態，不與 PlayStyleAnalyzer、成就系統、
    /// CombatDispatcher 等其他系統產生任何依賴，僅對外公開兩個事件供表現層訂閱轉發。
    /// </summary>
    [RequireComponent(typeof(AttackInputAggregator))]
    public sealed class TypingComboTracker : MonoBehaviour
    {
        [SerializeField]
        private TypingComboConfigSO _config;

        /// <summary>每次計數遞增時觸發，攜帶遞增後的最新計數值。</summary>
        public event Action<int> OnComboIncremented;

        /// <summary>閒置逾時、計數已歸零時觸發。</summary>
        public event Action OnComboExpired;

        private AttackInputAggregator _attackInputAggregator;
        private int _comboCount;
        private float _idleTimer;

        private void Awake()
        {
            _attackInputAggregator = GetComponent<AttackInputAggregator>();
        }

        private void OnEnable()
        {
            _attackInputAggregator.OnAttackInputTriggered -= HandleAttackInputTriggered;
            _attackInputAggregator.OnAttackInputTriggered += HandleAttackInputTriggered;
        }

        private void OnDisable()
        {
            _attackInputAggregator.OnAttackInputTriggered -= HandleAttackInputTriggered;
        }

        private void Update()
        {
            if (_comboCount == 0)
            {
                return;
            }

            _idleTimer += Time.deltaTime;

            if (_idleTimer >= _config.IdleTimeoutSeconds)
            {
                ExpireCombo();
            }
        }

        private void HandleAttackInputTriggered(AttackInputData data)
        {
            _comboCount++;
            _idleTimer = 0f;
            OnComboIncremented?.Invoke(_comboCount);
        }

        private void ExpireCombo()
        {
            _comboCount = 0;
            _idleTimer = 0f;
            OnComboExpired?.Invoke();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only 測試輔助：手動注入一次攻擊輸入事件以驗證計數／閒置計時邏輯，
        /// 不參與正式執行流程，僅供編輯器內驗證使用。
        /// </summary>
        public void DebugSimulateKeyPress()
        {
            HandleAttackInputTriggered(new AttackInputData(DateTime.UtcNow.Ticks, AttackInputSource.Keyboard));
        }
#endif
    }
}
