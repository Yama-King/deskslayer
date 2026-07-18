using UnityEngine;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 所有風格原型主題的目錄資產，供靜態合成與互動揭曉動畫依 StyleArchetypeId 查找對應主題。
    /// 新增/調整主題時只需要修改對應的 StyleArchetypeThemeSO 資產或這裡的清單，不需要改程式碼。
    /// </summary>
    [CreateAssetMenu(fileName = "StyleArchetypeThemeDatabase", menuName = "DeskSlayer/ShareCard/Style Archetype Theme Database", order = 5)]
    public sealed class StyleArchetypeThemeDatabaseSO : ScriptableObject
    {
        [SerializeField]
        private StyleArchetypeThemeSO[] _allThemes;

        /// <summary>查找指定原型對應的主題資料。找不到時回傳 false。</summary>
        public bool TryGetTheme(StyleArchetypeId archetypeId, out StyleArchetypeThemeSO theme)
        {
            theme = null;

            if (_allThemes == null)
            {
                return false;
            }

            foreach (StyleArchetypeThemeSO candidate in _allThemes)
            {
                if (candidate != null && candidate.ArchetypeId == archetypeId)
                {
                    theme = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
