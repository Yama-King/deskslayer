using System;
using UnityEngine;
using DeskSlayer.KeyboardHook;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 打字連擊計數器的邏輯核心：單向訂閱同一個 GameObject 上 GlobalKeyboardHookService 的
    /// OnKeyPressed 事件（與驅動 TypingEnergySystem 的同一個既有輸入來源），不經過
    /// AttackInputAggregator 彙整層，因為滑鼠點擊不算「打字」、且攻擊觸發次數與實際打字字元數
    /// 並非一對一關係。純粹自行維護計數與閒置計時狀態，不與 PlayStyleAnalyzer、成就系統、
    /// CombatDispatcher 等其他系統產生任何依賴，僅對外公開兩個事件供表現層訂閱轉發。
    /// </summary>
    [RequireComponent(typeof(GlobalKeyboardHookService))]
    public sealed class TypingComboTracker : MonoBehaviour
    {
        [SerializeField]
        private TypingComboConfigSO _config;

        /// <summary>每次計數遞增時觸發，攜帶遞增後的最新計數值。</summary>
        public event Action<int> OnComboIncremented;

        /// <summary>閒置逾時、計數已歸零時觸發。</summary>
        public event Action OnComboExpired;

        private GlobalKeyboardHookService _hookService;
        private int _comboCount;
        private float _idleTimer;

        private void Awake()
        {
            _hookService = GetComponent<GlobalKeyboardHookService>();
        }

        private void OnEnable()
        {
            _hookService.OnKeyPressed -= HandleKeyPressed;
            _hookService.OnKeyPressed += HandleKeyPressed;
        }

        private void OnDisable()
        {
            _hookService.OnKeyPressed -= HandleKeyPressed;
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

        private void HandleKeyPressed(KeyPressData data)
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
        /// Editor-only 測試輔助：手動注入一次按鍵事件以驗證計數／閒置計時邏輯，
        /// 不參與正式執行流程，僅供編輯器內驗證使用。
        /// </summary>
        public void DebugSimulateKeyPress(char character)
        {
            HandleKeyPressed(new KeyPressData(character, DateTime.UtcNow.Ticks));
        }
#endif
    }
}
