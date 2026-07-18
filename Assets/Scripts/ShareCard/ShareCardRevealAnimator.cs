using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡互動面板的風格揭曉動畫：兩軸分數數字跳動、象限標記點補間移動、主題色淡入、
    /// 原型名稱縮放回彈強調，四段動畫一次呼叫 PlayReveal() 觸發。純粹負責播放動畫，
    /// 不參與截圖/合成/存檔等既有流程，由 ShareCardPanelController 在既有流程完成後額外呼叫。
    /// 每次呼叫都先 DOKill 進行中的序列，避免玩家連續點擊產生的動畫互相疊加。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShareCardRevealAnimator : MonoBehaviour
    {
        [SerializeField]
        private StyleArchetypeThemeDatabaseSO _themeDatabase;

        [SerializeField]
        private ShareCardStyleQuadrantView _quadrantView;

        [SerializeField]
        private TextMeshProUGUI _rhythmScoreText;

        [SerializeField]
        private TextMeshProUGUI _lightScoreText;

        [SerializeField, Tooltip("分數文字的顯示格式，{0} 會被替換成 0~100 的數字")]
        private string _rhythmScoreFormat = "節奏穩定度：{0}";

        [SerializeField, Tooltip("武器傾向文字的顯示格式，{0} 會被替換成輕攻擊佔比、{1} 會被替換成重攻擊佔比（兩者相加為 100），同時顯示兩側避免玩家用重武器時看到單一輕攻擊數字不知所以然")]
        private string _lightScoreFormat = "輕攻擊 {0}％・重攻擊 {1}％";

        [SerializeField, Tooltip("套用主題色的背景/邊框強調元件")]
        private Image _themeAccentImage;

        [SerializeField]
        private TextMeshProUGUI _archetypeNameText;

        [SerializeField, Tooltip("分數跳動與標記點移動的時長（秒）")]
        private float _revealDuration = 0.6f;

        [SerializeField, Tooltip("主題色淡入的時長（秒）")]
        private float _themeFadeDuration = 0.3f;

        [SerializeField, Tooltip("原型名稱縮放回彈的時長（秒）")]
        private float _nameScaleDuration = 0.35f;

        [SerializeField, Tooltip("原型名稱回彈動畫開始時的縮放倍率")]
        private float _nameStartScale = 1.6f;

        private Sequence _activeSequence;

        /// <summary>播放一次風格揭曉動畫，數據與原型分類皆由呼叫端（ShareCardPanelController）決定好傳入。</summary>
        public void PlayReveal(ShareCardDisplayData data)
        {
            _activeSequence?.Kill();

            if (!_themeDatabase.TryGetTheme(data.ArchetypeId, out StyleArchetypeThemeSO theme))
            {
                Debug.LogWarning($"[ShareCardRevealAnimator] 找不到原型 {data.ArchetypeId} 對應的主題資料，跳過揭曉動畫。");
                return;
            }

            _activeSequence = DOTween.Sequence();

            if (_rhythmScoreText != null)
            {
                var rhythmCounter = new FloatCounter();
                _activeSequence.Join(DOTween.To(() => rhythmCounter.Value, x => rhythmCounter.Value = x, data.RhythmStabilityScore, _revealDuration)
                    .OnUpdate(() => _rhythmScoreText.text = string.Format(_rhythmScoreFormat, Mathf.RoundToInt(rhythmCounter.Value))));
            }

            if (_lightScoreText != null)
            {
                var lightCounter = new FloatCounter();
                _activeSequence.Join(DOTween.To(() => lightCounter.Value, x => lightCounter.Value = x, data.LightAttackTendencyScore, _revealDuration)
                    .OnUpdate(() =>
                    {
                        int lightPercent = Mathf.RoundToInt(lightCounter.Value);
                        int heavyPercent = 100 - lightPercent;
                        _lightScoreText.text = string.Format(_lightScoreFormat, lightPercent, heavyPercent);
                    }));
            }

            if (_quadrantView != null)
            {
                Tween markerTween = _quadrantView.AnimateMarkerTo(data.LightAttackTendencyScore, data.RhythmStabilityScore, _revealDuration, Ease.OutBack);
                if (markerTween != null)
                {
                    _activeSequence.Join(markerTween);
                }
            }

            if (_themeAccentImage != null)
            {
                _activeSequence.Append(_themeAccentImage.DOColor(theme.ThemeColor, _themeFadeDuration));
            }

            if (_archetypeNameText != null)
            {
                _archetypeNameText.text = theme.DisplayName;
                _archetypeNameText.rectTransform.localScale = Vector3.one * _nameStartScale;
                _activeSequence.Join(_archetypeNameText.rectTransform.DOScale(1f, _nameScaleDuration).SetEase(Ease.OutBack));
            }
        }

        /// <summary>DOTween.To 需要一個可讀寫的浮點數欄位/屬性，用這個小型包裝類別承載跳動中的當前值。</summary>
        private sealed class FloatCounter
        {
            public float Value;
        }
    }
}
