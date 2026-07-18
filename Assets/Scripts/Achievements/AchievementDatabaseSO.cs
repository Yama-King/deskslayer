using UnityEngine;

namespace DeskSlayer.Achievements
{
    /// <summary>
    /// 所有成就定義的目錄資產，供 AchievementService 判定門檻與還原存檔時查找完整清單，
    /// 也供成就選單枚舉「全部成就」（含尚未解鎖的項目）使用。新增成就（含新的門檻階段資產）
    /// 只需要把資產拖進這裡的陣列，不需要修改任何程式碼。
    /// </summary>
    [CreateAssetMenu(fileName = "AchievementDatabase", menuName = "DeskSlayer/Achievements/Database", order = 3)]
    public sealed class AchievementDatabaseSO : ScriptableObject
    {
        [SerializeField]
        private AchievementDefinitionSO[] _allAchievements;

        /// <summary>所有成就定義（含各型別、各門檻階段），順序即為選單顯示順序。</summary>
        public AchievementDefinitionSO[] AllAchievements => _allAchievements;
    }
}
