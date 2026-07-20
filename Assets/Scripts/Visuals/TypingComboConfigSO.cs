using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 打字連擊計數器的時長／閾值參數資產。淡入淡出、跳出動畫與閒置歸零的節奏皆可在此調整，
    /// 不需要改程式碼、不需要重新編譯。
    /// </summary>
    [CreateAssetMenu(fileName = "TypingComboConfig", menuName = "DeskSlayer/Visuals/Typing Combo Config", order = 6)]
    public sealed class TypingComboConfigSO : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("最後一次有效按鍵輸入後，超過此時間（秒）未有新輸入即視為連擊中斷")]
        private float _idleTimeoutSeconds = 1.2f;

        [SerializeField, Min(0f), Tooltip("計數每次遞增時，跳出動畫的放大幅度（以 1 為基準的額外縮放量）")]
        private float _punchScaleStrength = 0.25f;

        [SerializeField, Min(0f), Tooltip("跳出動畫的時長（秒）")]
        private float _punchScaleDuration = 0.2f;

        [SerializeField, Min(0f), Tooltip("計數器淡入顯示的時長（秒）")]
        private float _fadeInDuration = 0.12f;

        [SerializeField, Min(0f), Tooltip("閒置逾時後，計數器淡出隱藏的時長（秒）")]
        private float _fadeOutDuration = 0.4f;

        /// <summary>最後一次有效按鍵輸入後，超過此時間（秒）未有新輸入即視為連擊中斷。</summary>
        public float IdleTimeoutSeconds => _idleTimeoutSeconds;

        /// <summary>計數每次遞增時，跳出動畫的放大幅度。</summary>
        public float PunchScaleStrength => _punchScaleStrength;

        /// <summary>跳出動畫的時長（秒）。</summary>
        public float PunchScaleDuration => _punchScaleDuration;

        /// <summary>計數器淡入顯示的時長（秒）。</summary>
        public float FadeInDuration => _fadeInDuration;

        /// <summary>閒置逾時後，計數器淡出隱藏的時長（秒）。</summary>
        public float FadeOutDuration => _fadeOutDuration;
    }
}
