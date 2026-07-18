using System;
using System.Globalization;

namespace DeskSlayer.Analytics
{
    /// <summary>
    /// 按鍵發生當下裝備的武器類型分類，供 <see cref="PlayStyleProfile.RecordEquippedWeaponKeyPress"/> 統計按鍵歸屬。
    /// 定義在此（純 C#）而非直接使用 WeaponDataSO 型別，是為了維持 PlayStyleProfile 不依賴 UnityEngine 的設計；
    /// 實際的武器型別判斷交由 PlayStyleAnalyzer（Unity 宿主層）解析後傳入。
    /// </summary>
    public enum WeaponCategory
    {
        /// <summary>尚未裝備武器，或裝備的武器不屬於輕/重任一類別，不計入 LightAttackTendencyScore 統計。</summary>
        None,

        /// <summary>輕武器。</summary>
        Light,

        /// <summary>重武器。</summary>
        Heavy
    }

    /// <summary>
    /// 純 C# 統計引擎，不依賴 UnityEngine，方便未來以 EditMode 測試獨立驗證運算邏輯。
    /// 比照 IFrameGuard 的設計哲學：統計邏輯與 Unity 生命週期分離，由外部（PlayStyleAnalyzer）餵入資料並查詢結果。
    ///
    /// LightAttackTendencyScore、RhythmStabilityScore 皆輸出「當日」「累積」兩個獨立版本，呼應分享卡系統既有的
    /// 當日/累積切換功能。兩個版本的跨日惰性歸零判斷共用同一個 <see cref="LastRecordedDate"/> 欄位（規格允許），
    /// 但這個欄位是 PlayStyleProfile 自己獨立維護的狀態，不與 ShareCardStatsTracker 的日期欄位共用或互相參照——
    /// 兩個系統各自維護自己的「上次記錄日期」，即使語意相同、格式相同，也刻意不合併成共用元件，
    /// 比照整個專案「各系統獨立維護自身狀態，不因為語意相似就耦合」的既有原則。
    ///
    /// RhythmStabilityScore 不再使用固定筆數的滾動樣本窗口，改用線上（online）統計累加器
    /// （Welford's Online Algorithm）：當日／累積各自只維護「樣本數、目前平均值、平方差累加值」三個數值，
    /// 不保留任何原始樣本本身。當日版本完整涵蓋當天從開始到現在的所有段落內間隔，累積版本完整涵蓋
    /// 玩家整個遊戲生涯的節奏特徵，兩者都不會被任何隱性筆數上限截斷。
    /// </summary>
    public sealed class PlayStyleProfile
    {
        /// <summary>段落切分閾值（毫秒）預設值，可由 <see cref="SegmentBreakThresholdMs"/> 調整。</summary>
        public const double DefaultSegmentBreakThresholdMs = 2000d;

        /// <summary>段落內樣本數防禦下限預設值，可由 <see cref="MinSegmentSampleCount"/> 調整。</summary>
        public const int DefaultMinSegmentSampleCount = 5;

        private const float NeutralRhythmStabilityScore = 50f;
        private const string DateFormat = "yyyy-MM-dd";

        private readonly RhythmAccumulator _dailyRhythmAccumulator = new RhythmAccumulator();
        private readonly RhythmAccumulator _totalRhythmAccumulator = new RhythmAccumulator();

        private bool _hasPreviousTimestamp;
        private long _previousTimestampTicks;

        private int _lightAttackCount;
        private int _heavyAttackCount;

        private int _dailyLightWeaponKeyPressCount;
        private int _dailyHeavyWeaponKeyPressCount;
        private long _totalLightWeaponKeyPressCount;
        private long _totalHeavyWeaponKeyPressCount;

        private double _segmentBreakThresholdMs = DefaultSegmentBreakThresholdMs;
        private int _minSegmentSampleCount = DefaultMinSegmentSampleCount;

        private float _cachedDailyRhythmStabilityScore = NeutralRhythmStabilityScore;
        private float _cachedTotalRhythmStabilityScore = NeutralRhythmStabilityScore;

        private string _lastRecordedDate = string.Empty;

        /// <summary>累積輕攻擊觸發次數（實際觸發的攻擊事件次數，非按鍵歸屬次數）。</summary>
        public int LightAttackCount => _lightAttackCount;

        /// <summary>累積重攻擊觸發次數（實際觸發的攻擊事件次數，非按鍵歸屬次數）。</summary>
        public int HeavyAttackCount => _heavyAttackCount;

        /// <summary>
        /// 打字段落切分閾值（毫秒）。任兩次按鍵間隔超過此閾值，視為玩家離開打字動作去做別的事，
        /// 該次間隔視為「段落之間」，完全不列入 RhythmStabilityScore 的計算母體（不封頂、不採計）。
        /// 人類使用電腦本來就會反覆停下來，這類停頓會規律性地多次發生，不是偶發離群值，
        /// 若把停頓時間也算進統計母體，幾乎所有正常使用模式都會被誤判為「爆發型」，指標失去區分度。
        /// 設為可調整參數而非寫死常數，因為此數值需依實際試玩感受微調。傳入非正值時維持原設定不變。
        /// </summary>
        public double SegmentBreakThresholdMs
        {
            get => _segmentBreakThresholdMs;
            set
            {
                if (value > 0d)
                {
                    _segmentBreakThresholdMs = value;
                }
            }
        }

        /// <summary>
        /// 計算 RhythmStabilityScore 所需的段落內樣本數下限（讀取線上累加器的樣本數欄位判斷，
        /// 不再讀取佇列長度）。樣本數低於此下限時，分數維持前次計算結果不更新（防禦性處理），
        /// 避免用極少樣本算出的分數過度波動。設為可調整參數而非寫死常數，理由同
        /// <see cref="SegmentBreakThresholdMs"/>。傳入小於 2 的值時維持原設定不變。
        /// </summary>
        public int MinSegmentSampleCount
        {
            get => _minSegmentSampleCount;
            set
            {
                if (value >= 2)
                {
                    _minSegmentSampleCount = value;
                }
            }
        }

        /// <summary>當日輕武器按鍵歸屬次數，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public int DailyLightWeaponKeyPressCount => _dailyLightWeaponKeyPressCount;

        /// <summary>當日重武器按鍵歸屬次數，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public int DailyHeavyWeaponKeyPressCount => _dailyHeavyWeaponKeyPressCount;

        /// <summary>累積（全生涯）輕武器按鍵歸屬次數，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public long TotalLightWeaponKeyPressCount => _totalLightWeaponKeyPressCount;

        /// <summary>累積（全生涯）重武器按鍵歸屬次數，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public long TotalHeavyWeaponKeyPressCount => _totalHeavyWeaponKeyPressCount;

        /// <summary>跨日惰性歸零判斷用的「上次記錄日期」，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public string LastRecordedDate => _lastRecordedDate;

        /// <summary>當日節奏線上累加器目前已處理樣本數，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public long DailyRhythmSampleCount => _dailyRhythmAccumulator.SampleCount;

        /// <summary>當日節奏線上累加器目前平均值，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public double DailyRhythmMean => _dailyRhythmAccumulator.Mean;

        /// <summary>當日節奏線上累加器目前平方差累加值，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public double DailyRhythmM2 => _dailyRhythmAccumulator.M2;

        /// <summary>累積節奏線上累加器目前已處理樣本數，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public long TotalRhythmSampleCount => _totalRhythmAccumulator.SampleCount;

        /// <summary>累積節奏線上累加器目前平均值，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public double TotalRhythmMean => _totalRhythmAccumulator.Mean;

        /// <summary>累積節奏線上累加器目前平方差累加值，供 PlayStyleAnalyzer 同步寫回存檔使用。</summary>
        public double TotalRhythmM2 => _totalRhythmAccumulator.M2;

        /// <summary>
        /// 當日輕攻擊傾向分數（0~100），計算依據僅限「今天」歸屬的按鍵次數，公式與
        /// <see cref="TotalLightAttackTendencyScore"/> 相同，差別只在計數來源。
        /// </summary>
        public float DailyLightAttackTendencyScore => ComputeLightAttackTendency(_dailyLightWeaponKeyPressCount, _dailyHeavyWeaponKeyPressCount);

        /// <summary>
        /// 累積（全生涯）輕攻擊傾向分數（0~100）。100 代表打字量全部歸屬於輕武器裝備期間，0 代表全部歸屬於重武器。
        /// 計算依據為「有效按鍵發生當下裝備的武器類型」（見 <see cref="RecordEquippedWeaponKeyPress"/>），
        /// 而非攻擊觸發次數——輕/重攻擊的觸發機制天生存在數量級落差（輕攻擊每次按鍵即觸發，重攻擊需累積
        /// 多次按鍵才觸發一次），若以觸發次數計算會結構性偏向輕攻擊傾向，無法反映實際打字量分配。
        /// 尚未有任何按鍵歸屬時回傳 50（中立值），避免一開局樣本不足就誤判風格。
        /// </summary>
        public float TotalLightAttackTendencyScore => ComputeLightAttackTendency(_totalLightWeaponKeyPressCount, _totalHeavyWeaponKeyPressCount);

        /// <summary>
        /// 當日打字節奏穩定度分數（0~100），計算依據為當日線上累加器，完整涵蓋今天從開始到現在的
        /// 所有段落內按鍵間隔，不受任何筆數上限截斷。邏輯與 <see cref="TotalRhythmStabilityScore"/> 相同
        /// （段落切分、樣本數防禦、快取前次結果皆適用），差別只在累加器各自獨立，且會在跨日時整組重置。
        /// </summary>
        public float DailyRhythmStabilityScore => _cachedDailyRhythmStabilityScore;

        /// <summary>
        /// 累積打字節奏穩定度分數（0~100）。以「段落內」按鍵間隔的變異係數（標準差 / 平均值）反推，
        /// 數值越高代表間隔越穩定（節奏型），越低代表忽快忽慢（爆發型）。這裡的「累積」代表玩家從
        /// 開始遊玩至今的完整節奏特徵，永久累加、不因任何時間邊界重置，非跨日重置的當日版本。
        /// 這個分數衡量的語意是「玩家實際打字動作進行當下，節奏是否穩定」，不衡量「玩家是否長時間
        /// 持續打字未離開」——超過 <see cref="SegmentBreakThresholdMs"/> 的停頓（段落之間）完全不列入
        /// 計算母體，因為人類使用電腦本來就會反覆停下來做別的事，這是任何正常使用模式都會有的行為，
        /// 對「節奏穩不穩定」這個問題不具區分度，混入母體只會讓幾乎所有玩家都被誤判為爆發型。
        /// 樣本數低於 <see cref="MinSegmentSampleCount"/> 時，回傳快取住的前次計算結果，
        /// 避免極少樣本算出失真的極端分數——這不是「樣本不足時固定回傳某個值」的特殊規則，
        /// 單純只是「維持前次結果」在還沒有任何前次結果可維持時，自然落回初始值 50（中立）。
        /// 樣本數只會單調增加（見 <see cref="RecordKeyTimestamp"/>），一旦第一次累積到門檻以上，
        /// 這個分支之後不會再被觸發，因此 50 分實務上只會出現在剛開始統計的短暫期間。
        /// 這是真正用來判斷玩家「戰鬥風格」的依據——按鍵間隔反映的是打字當下的自然節奏，
        /// 不像 LightAttackTendencyScore 會被玩家手動切換武器的主觀選擇干擾。
        /// </summary>
        public float TotalRhythmStabilityScore => _cachedTotalRhythmStabilityScore;

        /// <summary>
        /// 以先前讀取的存檔資料初始化輕重攻擊按鍵歸屬的持久化狀態（當日/累積次數、上次記錄日期）。
        /// 只在 PlayStyleAnalyzer 啟動時呼叫一次，需與 <see cref="InitializePersistedRhythmState"/>
        /// 搭配使用，兩者都設定完成後再呼叫 <see cref="RefreshDailyRolloverIfNeeded"/> 統一執行跨日檢查，
        /// 確保存檔日期若已是昨天以前，遊戲一啟動當日數字就會正確歸零，不需要等到第一次按鍵才修正。
        /// </summary>
        public void InitializePersistedAttackState(int dailyLightWeaponKeyPressCount, int dailyHeavyWeaponKeyPressCount,
            long totalLightWeaponKeyPressCount, long totalHeavyWeaponKeyPressCount, string lastRecordedDate)
        {
            _dailyLightWeaponKeyPressCount = dailyLightWeaponKeyPressCount;
            _dailyHeavyWeaponKeyPressCount = dailyHeavyWeaponKeyPressCount;
            _totalLightWeaponKeyPressCount = totalLightWeaponKeyPressCount;
            _totalHeavyWeaponKeyPressCount = totalHeavyWeaponKeyPressCount;
            _lastRecordedDate = lastRecordedDate ?? string.Empty;
        }

        /// <summary>
        /// 以先前讀取的存檔資料初始化節奏線上累加器的持久化狀態（當日/累積各自的樣本數、平均值、
        /// 平方差累加值），讓玩家同一天內重啟遊戲時，當日統計能正確接續，不從零重新開始。
        /// 呼叫後會重新計算一次當日/累積分數快取，反映還原後的累加器狀態。
        /// </summary>
        public void InitializePersistedRhythmState(long dailySampleCount, double dailyMean, double dailyM2,
            long totalSampleCount, double totalMean, double totalM2)
        {
            _dailyRhythmAccumulator.Restore(dailySampleCount, dailyMean, dailyM2);
            _totalRhythmAccumulator.Restore(totalSampleCount, totalMean, totalM2);

            RecalculateRhythmScore(_dailyRhythmAccumulator, isDaily: true);
            RecalculateRhythmScore(_totalRhythmAccumulator, isDaily: false);
        }

        /// <summary>
        /// 比對「目前日期」與「上次記錄日期」，不同則歸零當日按鍵歸屬計數、重置當日節奏線上累加器
        /// 並將當日節奏分數快取重置為中立值，再更新記錄日期。做法比照 ShareCardStatsTracker 的
        /// 惰性跨日判斷（不用常駐計時器輪詢），但這裡的日期欄位是本類別獨立維護的狀態，
        /// 不與 ShareCardStatsTracker 共用或互相參照。累積累加器與累積按鍵歸屬計數不受影響。
        /// 供 <see cref="RecordKeyTimestamp"/>、<see cref="RecordEquippedWeaponKeyPress"/> 內部呼叫，
        /// 也可供外部（例如分享卡生成流程）在讀取當日分數前主動呼叫，確保跨日後第一次讀取就是正確數字。
        /// </summary>
        public void RefreshDailyRolloverIfNeeded()
        {
            string today = DateTime.Today.ToString(DateFormat, CultureInfo.InvariantCulture);
            if (_lastRecordedDate == today)
            {
                return;
            }

            _dailyLightWeaponKeyPressCount = 0;
            _dailyHeavyWeaponKeyPressCount = 0;
            _dailyRhythmAccumulator.Reset();
            _cachedDailyRhythmStabilityScore = NeutralRhythmStabilityScore;
            _lastRecordedDate = today;
        }

        /// <summary>
        /// 記錄一次按鍵時間戳記（DateTime.Ticks），內部換算為與前一次按鍵的間隔（毫秒）。
        /// 間隔超過 <see cref="SegmentBreakThresholdMs"/> 時視為段落結束（玩家離開打字動作），
        /// 該次間隔不列入當日/累積 RhythmStabilityScore 的計算母體，也不觸發分數重算；
        /// 未超過閾值的間隔視為同一段落內的樣本，會同時餵入當日、累積兩個線上累加器並各自觸發分數重算。
        /// 兩個累加器都完整涵蓋各自範圍內的所有段落內間隔，不受任何筆數上限截斷。
        /// </summary>
        public void RecordKeyTimestamp(long timestampTicks)
        {
            RefreshDailyRolloverIfNeeded();

            if (_hasPreviousTimestamp)
            {
                double intervalMs = TimeSpan.FromTicks(timestampTicks - _previousTimestampTicks).TotalMilliseconds;
                if (intervalMs >= 0d && intervalMs <= _segmentBreakThresholdMs)
                {
                    _dailyRhythmAccumulator.Update(intervalMs);
                    _totalRhythmAccumulator.Update(intervalMs);
                    RecalculateRhythmScore(_dailyRhythmAccumulator, isDaily: true);
                    RecalculateRhythmScore(_totalRhythmAccumulator, isDaily: false);
                }
            }

            _previousTimestampTicks = timestampTicks;
            _hasPreviousTimestamp = true;
        }

        private void RecalculateRhythmScore(RhythmAccumulator accumulator, bool isDaily)
        {
            if (accumulator.SampleCount < _minSegmentSampleCount)
            {
                // 樣本不足：不更新快取，維持現有值（可能是先前算出的真實分數，也可能是尚未計算過的
                // 初始中立值 50）。不是「樣本不足就回傳 50」的專屬規則，見類別註解說明。
                return;
            }

            if (accumulator.Mean <= 0d)
            {
                return;
            }

            double coefficientOfVariation = accumulator.StdDev / accumulator.Mean;
            double stability = 100d - coefficientOfVariation * 100d;
            float score = (float)Clamp(stability, 0d, 100d);

            if (isDaily)
            {
                _cachedDailyRhythmStabilityScore = score;
            }
            else
            {
                _cachedTotalRhythmStabilityScore = score;
            }
        }

        /// <summary>記錄一次輕攻擊觸發。</summary>
        public void RecordLightAttack()
        {
            _lightAttackCount++;
        }

        /// <summary>記錄一次重攻擊觸發。</summary>
        public void RecordHeavyAttack()
        {
            _heavyAttackCount++;
        }

        /// <summary>
        /// 記錄一次有效按鍵當下裝備的武器類型，同時計入當日、累積兩組 LightAttackTendencyScore 的
        /// 按鍵歸屬統計。不論這次按鍵是否實際觸發了一次攻擊都要呼叫——歸屬依據是「當下裝備哪種武器」，
        /// 非「是否觸發攻擊」。傳入 <see cref="WeaponCategory.None"/> 時不計入任何一方（例如尚未裝備武器）。
        /// </summary>
        public void RecordEquippedWeaponKeyPress(WeaponCategory category)
        {
            RefreshDailyRolloverIfNeeded();

            switch (category)
            {
                case WeaponCategory.Light:
                    _dailyLightWeaponKeyPressCount++;
                    _totalLightWeaponKeyPressCount++;
                    break;

                case WeaponCategory.Heavy:
                    _dailyHeavyWeaponKeyPressCount++;
                    _totalHeavyWeaponKeyPressCount++;
                    break;
            }
        }

        private static float ComputeLightAttackTendency(long lightCount, long heavyCount)
        {
            long total = lightCount + heavyCount;
            if (total == 0)
            {
                return 50f;
            }

            return (float)lightCount / total * 100f;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>
        /// 節奏穩定度的線上（online）統計累加器，採用 Welford's Online Algorithm 逐筆更新平均值與
        /// 平方差累加值，數值穩定性優於「先加總、事後除以樣本數」的簡易寫法，且不需要保留任何原始樣本，
        /// 記憶體與存檔佔用量固定為三個數值，不隨樣本數增長。
        /// </summary>
        private sealed class RhythmAccumulator
        {
            public long SampleCount { get; private set; }
            public double Mean { get; private set; }
            public double M2 { get; private set; }

            /// <summary>母體標準差（除以樣本數，非樣本數減一），與修正前「一次性計算」版本的公式一致。</summary>
            public double StdDev => SampleCount > 0 ? Math.Sqrt(M2 / SampleCount) : 0d;

            public void Update(double value)
            {
                SampleCount++;
                double delta = value - Mean;
                Mean += delta / SampleCount;
                double delta2 = value - Mean;
                M2 += delta * delta2;
            }

            public void Reset()
            {
                SampleCount = 0;
                Mean = 0d;
                M2 = 0d;
            }

            /// <summary>從存檔資料還原累加器狀態，供同一天內重啟遊戲時接續統計使用。</summary>
            public void Restore(long sampleCount, double mean, double m2)
            {
                SampleCount = sampleCount;
                Mean = mean;
                M2 = m2;
            }
        }
    }
}
