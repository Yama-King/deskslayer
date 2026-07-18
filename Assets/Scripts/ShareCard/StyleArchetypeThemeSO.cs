using UnityEngine;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 單一風格原型的專屬主題資料：顯示名稱、主題色、圖示、標語。新增或調整任一原型的主題時，
    /// 只需要修改這份資產的欄位，不需要修改任何判定邏輯或呈現邏輯的程式碼。
    /// 圖示留空時，套用它的 Image 元件會自然顯示純色色塊（無 Sprite 時 Image 以 color 純色渲染），
    /// 天然滿足「先用簡單佔位圖形」的需求，不需要另外準備美術資源。
    /// </summary>
    [CreateAssetMenu(fileName = "StyleArchetypeTheme", menuName = "DeskSlayer/ShareCard/Style Archetype Theme", order = 4)]
    public sealed class StyleArchetypeThemeSO : ScriptableObject
    {
        [SerializeField]
        private StyleArchetypeId _archetypeId;

        [SerializeField]
        private string _displayName;

        [SerializeField, Tooltip("此原型的主題色，用於背景/邊框/強調色與標記點光暈")]
        private Color _themeColor = Color.white;

        [SerializeField, Tooltip("此原型的專屬圖示/徽章，留空時對應 Image 會顯示純色色塊")]
        private Sprite _icon;

        [SerializeField, Tooltip("此原型的專屬風格標語，內容由使用者於 Editor 內手動填寫")]
        private string _slogan;

        /// <summary>此主題對應的原型識別碼。</summary>
        public StyleArchetypeId ArchetypeId => _archetypeId;

        /// <summary>原型顯示名稱。</summary>
        public string DisplayName => _displayName;

        /// <summary>主題色。</summary>
        public Color ThemeColor => _themeColor;

        /// <summary>專屬圖示，可能為 null。</summary>
        public Sprite Icon => _icon;

        /// <summary>專屬風格標語，可能為空字串。</summary>
        public string Slogan => _slogan;
    }
}
