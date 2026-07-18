using UnityEngine;

namespace DeskSlayer.Juice
{
    /// <summary>
    /// 成就解鎖 Toast 的手感參數：進場彈跳、停留、淡出時長皆可調整，不寫死在程式碼裡。
    /// 成就沒有稀有度分級，因此不像 WeaponDropFeedbackConfigSO 需要依稀有度查表，單一組數值即可。
    /// </summary>
    [CreateAssetMenu(fileName = "AchievementToastConfig", menuName = "DeskSlayer/Achievements/Toast Config", order = 4)]
    public sealed class AchievementToastConfigSO : ScriptableObject
    {
        [SerializeField, Min(0.05f), Tooltip("進場縮放彈跳的時長（秒）")]
        private float _punchDuration = 0.25f;

        [SerializeField, Min(0.1f), Tooltip("完全顯示、尚未開始淡出的停留時長（秒）")]
        private float _holdDuration = 2.5f;

        [SerializeField, Min(0.05f), Tooltip("淡出時長（秒）")]
        private float _fadeDuration = 0.4f;

        /// <summary>進場縮放彈跳的時長（秒）。</summary>
        public float PunchDuration => _punchDuration;

        /// <summary>完全顯示、尚未開始淡出的停留時長（秒）。</summary>
        public float HoldDuration => _holdDuration;

        /// <summary>淡出時長（秒）。</summary>
        public float FadeDuration => _fadeDuration;
    }
}
