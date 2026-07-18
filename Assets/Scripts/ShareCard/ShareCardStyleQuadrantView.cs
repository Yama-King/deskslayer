using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 純讀取 PlayStyleAnalyzer 既有對外公開的兩個唯讀屬性（輕攻擊傾向、節奏穩定度），
    /// 換算成一個 2 軸座標點在方形象限區域內的位置呈現，不新建任何統計、不修改 PlayStyleAnalyzer。
    /// _quadrantArea 需要是 pivot/anchor 皆置中（0.5, 0.5）的 RectTransform，
    /// 這樣 anchoredPosition (0,0) 才會對應方形區域正中央。
    ///
    /// 提供瞬間定位（RefreshDisplay，靜態合成用，行為不變）與補間動畫（AnimateMarkerTo，互動揭曉
    /// 動畫用）兩種套用方式，內部共用同一份座標換算，避免兩處各自計算可能算出不同結果。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShareCardStyleQuadrantView : MonoBehaviour
    {
        [SerializeField, Tooltip("方形象限背景區域，pivot/anchor 需置中 (0.5, 0.5)")]
        private RectTransform _quadrantArea;

        [SerializeField, Tooltip("代表目前風格分數位置的標記點")]
        private RectTransform _marker;

        [SerializeField, Tooltip("標記點的光暈效果，可留空不強制要求")]
        private Image _markerGlow;

        /// <summary>依兩個 0~100 分數重新計算標記點在方形區域內的位置，瞬間套用（供靜態合成使用）。</summary>
        public void RefreshDisplay(float lightAttackTendencyScore, float rhythmStabilityScore)
        {
            if (_quadrantArea == null || _marker == null)
            {
                return;
            }

            _marker.anchoredPosition = ComputeMarkerPosition(lightAttackTendencyScore, rhythmStabilityScore);
        }

        /// <summary>套用標記點光暈顏色（供靜態合成依主題色套用，未指派光暈元件時安全地不做任何事）。</summary>
        public void ApplyGlowColor(Color color)
        {
            if (_markerGlow != null)
            {
                _markerGlow.color = color;
            }
        }

        /// <summary>
        /// 依兩個 0~100 分數，用補間動畫把標記點移動到目標位置（供互動揭曉動畫使用），
        /// 回傳的 Tween 供呼叫端串接或等待播放完成。
        /// </summary>
        public Tween AnimateMarkerTo(float lightAttackTendencyScore, float rhythmStabilityScore, float duration, Ease ease)
        {
            if (_quadrantArea == null || _marker == null)
            {
                return null;
            }

            Vector2 target = ComputeMarkerPosition(lightAttackTendencyScore, rhythmStabilityScore);
            _marker.DOKill();
            return _marker.DOAnchorPos(target, duration).SetEase(ease);
        }

        private Vector2 ComputeMarkerPosition(float lightAttackTendencyScore, float rhythmStabilityScore)
        {
            Vector2 areaSize = _quadrantArea.rect.size;
            float normalizedX = Mathf.Clamp01(lightAttackTendencyScore / 100f);
            float normalizedY = Mathf.Clamp01(rhythmStabilityScore / 100f);

            return new Vector2(
                (normalizedX - 0.5f) * areaSize.x,
                (normalizedY - 0.5f) * areaSize.y);
        }
    }
}
