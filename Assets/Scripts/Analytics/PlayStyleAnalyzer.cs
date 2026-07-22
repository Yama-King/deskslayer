using UnityEngine;
using DeskSlayer.AttackInput;
using DeskSlayer.Combat;
using DeskSlayer.Persistence;

namespace DeskSlayer.Analytics
{
    /// <summary>
    /// 統計玩家打字/點擊節奏與輕重攻擊觸發比例，映射成「戰鬥風格」傾向分數（當日／累積兩個版本），供 UI 查詢。
    /// 純粹訂閱既有事件做統計，不修改 AttackInputAggregator 或 TypingEnergySystem。
    /// 統計運算拆到 PlayStyleProfile（純 C# class），此類別負責 Unity 生命週期、事件轉接，
    /// 以及與 SaveLifecycleController 之間的存檔讀寫橋接（PlayStyleProfile 本身不依賴 Persistence，
    /// 比照 IFrameGuard（純邏輯）與 EnemyController（Unity 宿主）的分工方式。
    ///
    /// 滑鼠點擊與鍵盤字元輸入透過 AttackInputAggregator 彙整成同一種 AttackInputData 事件，
    /// 這裡完全不區分來源，兩者對節奏統計與武器歸屬統計一視同仁。PlayStyleProfile 內部方法
    /// （RecordKeyTimestamp 等）與存檔欄位（dailyLightWeaponKeyPressCount 等）仍沿用「按鍵」字樣命名，
    /// 是刻意選擇不重新命名——避免異動既有存檔 JSON 欄位造成相容性風險，語意上現在涵蓋所有攻擊輸入來源。
    /// </summary>
    [RequireComponent(typeof(AttackInputAggregator))]
    [RequireComponent(typeof(TypingEnergySystem))]
    [RequireComponent(typeof(WeaponSwitcher))]
    public sealed class PlayStyleAnalyzer : MonoBehaviour
    {
        [SerializeField, Tooltip("Debug.Log 定期輸出目前風格分數的間隔秒數")]
        private float _logIntervalSeconds = 5f;

        [SerializeField, Tooltip("打字段落切分閾值（毫秒）。按鍵間隔超過此值視為玩家離開打字動作，該次間隔不列入節奏穩定度計算母體，需依實際試玩感受微調")]
        private float _segmentBreakThresholdMs = (float)PlayStyleProfile.DefaultSegmentBreakThresholdMs;

        [SerializeField, Tooltip("計算節奏穩定度所需的段落內樣本數下限。低於此下限時分數維持前次結果不更新，避免極少樣本算出失真分數")]
        private int _minSegmentSampleCount = PlayStyleProfile.DefaultMinSegmentSampleCount;

        private AttackInputAggregator _attackInputAggregator;
        private TypingEnergySystem _typingEnergySystem;
        private WeaponSwitcher _weaponSwitcher;
        private readonly PlayStyleProfile _profile = new PlayStyleProfile();
        private float _logTimer;

        /// <summary>當日輕攻擊傾向分數（0~100）。數值越高代表今天越傾向輕攻擊流，越低代表越傾向重攻擊流。</summary>
        public float DailyLightAttackTendencyScore => _debugLightOverride ?? _profile.DailyLightAttackTendencyScore;

        /// <summary>累積（全生涯）輕攻擊傾向分數（0~100）。</summary>
        public float TotalLightAttackTendencyScore => _debugLightOverride ?? _profile.TotalLightAttackTendencyScore;

        /// <summary>當日打字節奏穩定度分數（0~100）。數值越高代表今天的按鍵間隔越穩定（節奏型），越低代表忽快忽慢（爆發型）。</summary>
        public float DailyRhythmStabilityScore => _debugRhythmOverride ?? _profile.DailyRhythmStabilityScore;

        /// <summary>累積打字節奏穩定度分數（0~100），代表近期整體節奏特徵，不特別區分日期界線。</summary>
        public float TotalRhythmStabilityScore => _debugRhythmOverride ?? _profile.TotalRhythmStabilityScore;

#if UNITY_EDITOR
        private float? _debugLightOverride;
        private float? _debugRhythmOverride;

        /// <summary>
        /// 僅供 Editor 測試工具使用：強制覆蓋當日/累積分數（不分開覆蓋，兩者一律回傳同一組覆蓋值），
        /// 讓分享卡風格揭曉可以不必真的打字磨出對應分數就能立即測試五種原型。比照 WeatherService.
        /// DebugForceCategory 的做法，以 #if UNITY_EDITOR 包住，不會被打包進正式 Build，也完全不觸碰
        /// PlayStyleProfile 內部狀態或存檔資料，純粹是讀取端的顯示層覆蓋。
        /// </summary>
        public void DebugForceScores(float lightAttackTendencyScore, float rhythmStabilityScore)
        {
            _debugLightOverride = lightAttackTendencyScore;
            _debugRhythmOverride = rhythmStabilityScore;
        }

        /// <summary>清除強制覆蓋，恢復讀取 PlayStyleProfile 真實計算出來的分數。</summary>
        public void DebugClearForcedScores()
        {
            _debugLightOverride = null;
            _debugRhythmOverride = null;
        }
#endif

        private void Awake()
        {
            _attackInputAggregator = GetComponent<AttackInputAggregator>();
            _typingEnergySystem = GetComponent<TypingEnergySystem>();
            _weaponSwitcher = GetComponent<WeaponSwitcher>();
            _profile.SegmentBreakThresholdMs = _segmentBreakThresholdMs;
            _profile.MinSegmentSampleCount = _minSegmentSampleCount;

            PlayStyleSaveData saveData = SaveLifecycleController.CurrentSaveData.playStyle;
            _profile.InitializePersistedAttackState(
                saveData.dailyLightWeaponKeyPressCount,
                saveData.dailyHeavyWeaponKeyPressCount,
                saveData.totalLightWeaponKeyPressCount,
                saveData.totalHeavyWeaponKeyPressCount,
                saveData.lastRecordedDate);
            _profile.InitializePersistedRhythmState(
                saveData.dailyRhythmSampleCount,
                saveData.dailyRhythmMean,
                saveData.dailyRhythmM2,
                saveData.totalRhythmSampleCount,
                saveData.totalRhythmMean,
                saveData.totalRhythmM2);
            _profile.RefreshDailyRolloverIfNeeded();

            SyncPersistedPlayStyleState();
        }

        private void OnEnable()
        {
            _attackInputAggregator.OnAttackInputTriggered += HandleAttackInput;
            _typingEnergySystem.OnLightAttackTriggered += HandleLightAttackTriggered;
            _typingEnergySystem.OnHeavyAttackTriggered += HandleHeavyAttackTriggered;
        }

        private void OnDisable()
        {
            _attackInputAggregator.OnAttackInputTriggered -= HandleAttackInput;
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
            Debug.Log($"[PlayStyleAnalyzer] 輕攻擊傾向(當日/累積)={DailyLightAttackTendencyScore:F1}/{TotalLightAttackTendencyScore:F1} 分，節奏穩定度(當日/累積)={DailyRhythmStabilityScore:F1}/{TotalRhythmStabilityScore:F1} 分");
        }

        /// <summary>
        /// 比對目前日期與上次記錄日期，跨日時歸零當日計數與節奏線上累加器。供分享卡生成流程等外部呼叫端
        /// 在讀取當日分數前主動呼叫，確保跨日後第一次讀取就是正確數字（比照 ShareCardStatsTracker 的作法，
        /// 但這裡驅動的是 PlayStyleProfile 自己獨立維護的日期欄位，不影響 ShareCardStatsTracker）。
        /// </summary>
        public void RefreshDailyRolloverIfNeeded()
        {
            _profile.RefreshDailyRolloverIfNeeded();
            SyncPersistedPlayStyleState();
        }

        /// <summary>
        /// 記錄時間戳記以計算節奏，並依當下裝備的武器類型記錄輸入歸屬（供 LightAttackTendencyScore 使用）。
        /// 攻擊「觸發」計數則一律交由 TypingEnergySystem 的事件負責，避免重複判斷有效輸入。
        /// </summary>
        private void HandleAttackInput(AttackInputData data)
        {
            _profile.RecordKeyTimestamp(data.TimestampTicks);
            _profile.RecordEquippedWeaponKeyPress(ResolveWeaponCategory(_weaponSwitcher.CurrentWeapon));
            SyncPersistedPlayStyleState();
        }

        private void HandleLightAttackTriggered(LightWeaponSO weapon)
        {
            _profile.RecordLightAttack();
        }

        private void HandleHeavyAttackTriggered(HeavyWeaponSO weapon)
        {
            _profile.RecordHeavyAttack();
        }

        private static WeaponCategory ResolveWeaponCategory(WeaponDataSO weapon)
        {
            switch (weapon)
            {
                case LightWeaponSO _:
                    return WeaponCategory.Light;

                case HeavyWeaponSO _:
                    return WeaponCategory.Heavy;

                default:
                    return WeaponCategory.None;
            }
        }

        /// <summary>
        /// 將 PlayStyleProfile 目前的當日/累積按鍵歸屬計數、節奏線上累加器狀態與上次記錄日期同步寫回
        /// SaveData，讓 PlayStyleProfile 本身維持不依賴 Persistence 的純 C# 設計。只寫入記憶體中的
        /// SaveLifecycleController.CurrentSaveData，實際落盤仍由既有的存檔時機（App 關閉、RequestSave）負責。
        /// </summary>
        private void SyncPersistedPlayStyleState()
        {
            PlayStyleSaveData saveData = SaveLifecycleController.CurrentSaveData.playStyle;
            saveData.dailyLightWeaponKeyPressCount = _profile.DailyLightWeaponKeyPressCount;
            saveData.dailyHeavyWeaponKeyPressCount = _profile.DailyHeavyWeaponKeyPressCount;
            saveData.totalLightWeaponKeyPressCount = _profile.TotalLightWeaponKeyPressCount;
            saveData.totalHeavyWeaponKeyPressCount = _profile.TotalHeavyWeaponKeyPressCount;
            saveData.lastRecordedDate = _profile.LastRecordedDate;
            saveData.dailyRhythmSampleCount = _profile.DailyRhythmSampleCount;
            saveData.dailyRhythmMean = _profile.DailyRhythmMean;
            saveData.dailyRhythmM2 = _profile.DailyRhythmM2;
            saveData.totalRhythmSampleCount = _profile.TotalRhythmSampleCount;
            saveData.totalRhythmMean = _profile.TotalRhythmMean;
            saveData.totalRhythmM2 = _profile.TotalRhythmM2;
        }
    }
}
