using UnityEngine;
using DeskSlayer.KeyboardHook;
using DeskSlayer.Combat;

namespace DeskSlayer.Analytics
{
    /// <summary>
    /// 統計玩家打字節奏與輕重攻擊觸發比例，映射成「戰鬥風格」傾向分數，供未來 UI 查詢。
    /// 純粹訂閱既有事件做統計，不修改 GlobalKeyboardHookService 或 TypingEnergySystem。
    /// 統計運算拆到 PlayStyleProfile（純 C# class），此類別僅負責 Unity 生命週期與事件轉接，
    /// 比照 IFrameGuard（純邏輯）與 EnemyController（Unity 宿主）的分工方式。
    /// </summary>
    [RequireComponent(typeof(GlobalKeyboardHookService))]
    [RequireComponent(typeof(TypingEnergySystem))]
    public sealed class PlayStyleAnalyzer : MonoBehaviour
    {
        [SerializeField, Tooltip("Debug.Log 定期輸出目前風格分數的間隔秒數")]
        private float _logIntervalSeconds = 5f;

        private GlobalKeyboardHookService _hookService;
        private TypingEnergySystem _typingEnergySystem;
        private readonly PlayStyleProfile _profile = new PlayStyleProfile();
        private float _logTimer;

        /// <summary>輕攻擊傾向分數（0~100）。數值越高代表越傾向輕攻擊流，越低代表越傾向重攻擊流。</summary>
        public float LightAttackTendencyScore => _profile.LightAttackTendencyScore;

        /// <summary>打字節奏穩定度分數（0~100）。數值越高代表按鍵間隔越穩定（節奏型），越低代表忽快忽慢（爆發型）。</summary>
        public float RhythmStabilityScore => _profile.RhythmStabilityScore;

        private void Awake()
        {
            _hookService = GetComponent<GlobalKeyboardHookService>();
            _typingEnergySystem = GetComponent<TypingEnergySystem>();
        }

        private void OnEnable()
        {
            _hookService.OnKeyPressed += HandleKeyPressed;
            _typingEnergySystem.OnLightAttackTriggered += HandleLightAttackTriggered;
            _typingEnergySystem.OnHeavyAttackTriggered += HandleHeavyAttackTriggered;
        }

        private void OnDisable()
        {
            _hookService.OnKeyPressed -= HandleKeyPressed;
            _typingEnergySystem.OnLightAttackTriggered -= HandleLightAttackTriggered;
            _typingEnergySystem.OnHeavyAttackTriggered -= HandleHeavyAttackTriggered;
        }

        private void Update()
        {
            _logTimer += Time.deltaTime;
            if (_logTimer < _logIntervalSeconds)
            {
                return;
            }

            _logTimer = 0f;
            Debug.Log($"[PlayStyleAnalyzer] 輕攻擊傾向={LightAttackTendencyScore:F1} 分，節奏穩定度={RhythmStabilityScore:F1} 分");
        }

        /// <summary>僅用於記錄時間戳記以計算節奏，攻擊觸發計數一律交由 TypingEnergySystem 的事件負責，避免重複判斷有效按鍵。</summary>
        private void HandleKeyPressed(KeyPressData data)
        {
            _profile.RecordKeyTimestamp(data.TimestampTicks);
        }

        private void HandleLightAttackTriggered(LightWeaponSO weapon)
        {
            _profile.RecordLightAttack();
        }

        private void HandleHeavyAttackTriggered(HeavyWeaponSO weapon)
        {
            _profile.RecordHeavyAttack();
        }
    }
}
