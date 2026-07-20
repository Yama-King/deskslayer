using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 敵人血條的時長／顏色參數資產。三種敵人造型共用同一份設定，
    /// 避免各自 Prefab 上分別調整導致外觀與節奏不一致。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyHealthBarConfig", menuName = "DeskSlayer/Visuals/Enemy Health Bar Config", order = 5)]
    public sealed class EnemyHealthBarConfigSO : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("血量填充值補間到新數值的時長（秒）")]
        private float _fillTweenDuration = 0.25f;

        [SerializeField, Min(0f), Tooltip("血條淡入顯示的時長（秒）")]
        private float _fadeInDuration = 0.15f;

        [SerializeField, Min(0f), Tooltip("淡入完成後，未再受到傷害時的可見保留時長（秒），到期後開始淡出")]
        private float _holdDuration = 2f;

        [SerializeField, Min(0f), Tooltip("血條淡出隱藏的時長（秒）")]
        private float _fadeOutDuration = 0.4f;

        [SerializeField, Tooltip("血條填充顏色")]
        private Color _fillColor = new Color(0.85f, 0.15f, 0.15f);

        [SerializeField, Tooltip("血條背景底色")]
        private Color _backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        /// <summary>血量填充值補間到新數值的時長（秒）。</summary>
        public float FillTweenDuration => _fillTweenDuration;

        /// <summary>血條淡入顯示的時長（秒）。</summary>
        public float FadeInDuration => _fadeInDuration;

        /// <summary>淡入完成後，未再受到傷害時的可見保留時長（秒）。</summary>
        public float HoldDuration => _holdDuration;

        /// <summary>血條淡出隱藏的時長（秒）。</summary>
        public float FadeOutDuration => _fadeOutDuration;

        /// <summary>血條填充顏色。</summary>
        public Color FillColor => _fillColor;

        /// <summary>血條背景底色。</summary>
        public Color BackgroundColor => _backgroundColor;
    }
}
