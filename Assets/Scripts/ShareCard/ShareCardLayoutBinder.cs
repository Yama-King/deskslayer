using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡合成版面的純綁定元件：只負責把截圖背景與統計數據填進對應的 UI 元件，
    /// 版面位置/字型/顏色全部留在 Prefab／Inspector 調整，這裡完全不寫死任何座標或樣式數值。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShareCardLayoutBinder : MonoBehaviour
    {
        [SerializeField]
        private RawImage _backgroundImage;

        [SerializeField, Tooltip("顯示目前資料範圍的標籤（例如「本日戰績」／「累積戰績」）")]
        private TextMeshProUGUI _scopeLabelText;

        [SerializeField]
        private TextMeshProUGUI _typedCountText;

        [SerializeField]
        private TextMeshProUGUI _killCountText;

        [SerializeField, Tooltip("打字量文字的顯示格式，{0} 會被替換成實際數字")]
        private string _typedCountFormat = "打字量：{0}";

        [SerializeField, Tooltip("擊殺數文字的顯示格式，{0} 會被替換成實際數字")]
        private string _killCountFormat = "擊殺數：{0}";

        [SerializeField]
        private ShareCardStyleQuadrantView _styleQuadrantView;

        [SerializeField, Tooltip("風格原型主題資料庫，用來依 ArchetypeId 查找主題色/圖示/名稱/標語")]
        private StyleArchetypeThemeDatabaseSO _themeDatabase;

        [SerializeField, Tooltip("套用主題色的背景/邊框強調元件")]
        private Image _themeAccentImage;

        [SerializeField, Tooltip("原型專屬圖示，留空 Sprite 時會顯示純色色塊")]
        private Image _archetypeIconImage;

        [SerializeField]
        private TextMeshProUGUI _archetypeNameText;

        [SerializeField]
        private TextMeshProUGUI _archetypeSloganText;

        /// <summary>套用一次截圖背景與統計數據到版面上的各個 UI 元件。</summary>
        public void Bind(Texture background, ShareCardDisplayData data)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.texture = background;
            }

            if (_scopeLabelText != null)
            {
                _scopeLabelText.text = data.ScopeLabel;
            }

            if (_typedCountText != null)
            {
                _typedCountText.text = string.Format(_typedCountFormat, data.TypedCount);
            }

            if (_killCountText != null)
            {
                _killCountText.text = string.Format(_killCountFormat, data.KillCount);
            }

            if (_styleQuadrantView != null)
            {
                _styleQuadrantView.RefreshDisplay(data.LightAttackTendencyScore, data.RhythmStabilityScore);
            }

            BindArchetypeTheme(data.ArchetypeId);
        }

        /// <summary>依原型分類查表套用主題色、圖示、名稱、標語與標記點光暈。找不到主題資料時記錄警告並保留目前顯示，不中斷合成流程。</summary>
        private void BindArchetypeTheme(StyleArchetypeId archetypeId)
        {
            if (_themeDatabase == null || !_themeDatabase.TryGetTheme(archetypeId, out StyleArchetypeThemeSO theme))
            {
                Debug.LogWarning($"[ShareCardLayoutBinder] 找不到原型 {archetypeId} 對應的主題資料，圖卡主題呈現維持目前狀態。");
                return;
            }

            if (_themeAccentImage != null)
            {
                _themeAccentImage.color = theme.ThemeColor;
            }

            if (_archetypeIconImage != null)
            {
                _archetypeIconImage.sprite = theme.Icon;
                _archetypeIconImage.color = theme.ThemeColor;
            }

            if (_archetypeNameText != null)
            {
                _archetypeNameText.text = theme.DisplayName;
            }

            if (_archetypeSloganText != null)
            {
                _archetypeSloganText.text = theme.Slogan;
            }

            if (_styleQuadrantView != null)
            {
                _styleQuadrantView.ApplyGlowColor(theme.ThemeColor);
            }
        }
    }
}
