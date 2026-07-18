using UnityEngine;

namespace DeskSlayer.Achievements
{
    /// <summary>
    /// 成就定義的抽象基底類別，定義所有成就共通的顯示資料與存檔識別碼。
    /// 三種成就型別（事件觸發型、數值累積門檻型、集合完成型）各自的判定邏輯不同，
    /// 但都共用這份基底：顯示名稱／描述／圖示交給成就選單顯示，Id 交給存檔與解鎖判定使用。
    /// </summary>
    public abstract class AchievementDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("存檔用的唯一識別碼，建議用英文小寫+底線命名（例如 first_kill），一經上線後不要更改，否則舊存檔的解鎖紀錄會對不上")]
        private string _id;

        [SerializeField]
        private string _displayName;

        [SerializeField, TextArea]
        private string _description;

        [SerializeField]
        private Sprite _icon;

        /// <summary>存檔用的唯一識別碼。</summary>
        public string Id => _id;

        /// <summary>成就顯示名稱，供 Toast 與成就選單使用。</summary>
        public string DisplayName => _displayName;

        /// <summary>成就描述文字，供成就選單使用。</summary>
        public string Description => _description;

        /// <summary>成就圖示，供 Toast 與成就選單使用。</summary>
        public Sprite Icon => _icon;
    }
}
