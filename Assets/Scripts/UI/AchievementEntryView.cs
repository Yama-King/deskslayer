using DeskSlayer.Achievements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 成就選單的單一列項：純功能性資料綁定（名稱／描述／已解鎖狀態），不做排版與視覺設計，
    /// 留給之後美術套版時直接沿用同一份 Bind() 介面替換顯示樣式。
    /// </summary>
    public sealed class AchievementEntryView : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        [SerializeField]
        private TextMeshProUGUI _descriptionLabel;

        [SerializeField]
        private TextMeshProUGUI _statusLabel;

        /// <summary>此項目對應的成就識別碼，供 AchievementListPanelController 查找要即時更新的項目使用。</summary>
        public string AchievementId { get; private set; }

        /// <summary>依成就定義與目前解鎖狀態刷新顯示內容。</summary>
        public void Bind(AchievementDefinitionSO achievement, bool unlocked)
        {
            AchievementId = achievement.Id;

            if (_icon != null)
            {
                _icon.sprite = achievement.Icon;
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = achievement.DisplayName;
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = achievement.Description;
            }

            SetUnlocked(unlocked);
        }

        /// <summary>更新已解鎖/未解鎖的狀態文字，供解鎖事件觸發時即時刷新。</summary>
        public void SetUnlocked(bool unlocked)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = unlocked ? "已解鎖" : "未解鎖";
            }
        }
    }
}
