namespace DeskSlayer.Enemy
{
    /// <summary>
    /// 冷卻式無敵幀（I-frame）計時器，非碰撞觸發，單純以時間倒數判斷是否忽略傷害。
    /// 刻意獨立成一般 C# 類別（不掛在 MonoBehaviour 生命週期上），
    /// 未來若玩家受擊等其他場景也需要同一套規則，可直接抽出重用，不需要重寫。
    /// </summary>
    public sealed class IFrameGuard
    {
        private readonly float _durationSeconds;
        private float _remainingSeconds;

        public IFrameGuard(float durationSeconds)
        {
            _durationSeconds = durationSeconds;
        }

        /// <summary>目前是否處於無敵幀期間。</summary>
        public bool IsActive => _remainingSeconds > 0f;

        /// <summary>每幀呼叫一次，倒數無敵幀剩餘時間。</summary>
        public void Tick(float deltaTime)
        {
            if (_remainingSeconds > 0f)
            {
                _remainingSeconds -= deltaTime;
            }
        }

        /// <summary>重新啟動一次無敵幀倒數。</summary>
        public void Trigger()
        {
            _remainingSeconds = _durationSeconds;
        }
    }
}
