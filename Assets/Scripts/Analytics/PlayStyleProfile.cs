using System;
using System.Collections.Generic;

namespace DeskSlayer.Analytics
{
    /// <summary>
    /// 純 C# 統計引擎，不依賴 UnityEngine，方便未來以 EditMode 測試獨立驗證運算邏輯。
    /// 比照 IFrameGuard 的設計哲學：統計邏輯與 Unity 生命週期分離，由外部（PlayStyleAnalyzer）餵入資料並查詢結果。
    /// </summary>
    public sealed class PlayStyleProfile
    {
        private const int MaxIntervalSamples = 30;

        private readonly Queue<double> _recentIntervalsMs = new Queue<double>(MaxIntervalSamples);

        private bool _hasPreviousTimestamp;
        private long _previousTimestampTicks;

        private int _lightAttackCount;
        private int _heavyAttackCount;

        /// <summary>累積輕攻擊觸發次數。</summary>
        public int LightAttackCount => _lightAttackCount;

        /// <summary>累積重攻擊觸發次數。</summary>
        public int HeavyAttackCount => _heavyAttackCount;

        /// <summary>
        /// 輕攻擊傾向分數（0~100）。100 代表觸發紀錄全為輕攻擊，0 代表全為重攻擊。
        /// 尚未有任何攻擊觸發時回傳 50（中立值），避免一開局樣本不足就誤判風格。
        /// </summary>
        public float LightAttackTendencyScore
        {
            get
            {
                int total = _lightAttackCount + _heavyAttackCount;
                if (total == 0)
                {
                    return 50f;
                }

                return (float)_lightAttackCount / total * 100f;
            }
        }

        /// <summary>
        /// 打字節奏穩定度分數（0~100）。以按鍵間隔的變異係數（標準差 / 平均值）反推，
        /// 數值越高代表間隔越穩定（節奏型），越低代表忽快忽慢（爆發型）。
        /// 樣本不足兩筆時回傳 50（中立值）。
        /// </summary>
        public float RhythmStabilityScore
        {
            get
            {
                if (_recentIntervalsMs.Count < 2)
                {
                    return 50f;
                }

                (double mean, double stdDev) = CalculateMeanAndStdDev(_recentIntervalsMs);
                if (mean <= 0d)
                {
                    return 50f;
                }

                double coefficientOfVariation = stdDev / mean;
                double stability = 100d - coefficientOfVariation * 100d;
                return (float)Clamp(stability, 0d, 100d);
            }
        }

        /// <summary>
        /// 記錄一次按鍵時間戳記（DateTime.Ticks），內部換算為與前一次按鍵的間隔（毫秒）。
        /// 僅保留最近 <see cref="MaxIntervalSamples"/> 筆間隔，反映「目前」節奏而非開局以來的全域平均。
        /// </summary>
        public void RecordKeyTimestamp(long timestampTicks)
        {
            if (_hasPreviousTimestamp)
            {
                double intervalMs = TimeSpan.FromTicks(timestampTicks - _previousTimestampTicks).TotalMilliseconds;
                if (intervalMs >= 0d)
                {
                    if (_recentIntervalsMs.Count >= MaxIntervalSamples)
                    {
                        _recentIntervalsMs.Dequeue();
                    }

                    _recentIntervalsMs.Enqueue(intervalMs);
                }
            }

            _previousTimestampTicks = timestampTicks;
            _hasPreviousTimestamp = true;
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

        private static (double mean, double stdDev) CalculateMeanAndStdDev(Queue<double> samples)
        {
            double sum = 0d;
            foreach (double value in samples)
            {
                sum += value;
            }

            double mean = sum / samples.Count;

            double squaredDiffSum = 0d;
            foreach (double value in samples)
            {
                double diff = value - mean;
                squaredDiffSum += diff * diff;
            }

            double variance = squaredDiffSum / samples.Count;
            return (mean, Math.Sqrt(variance));
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
    }
}
