using UnityEngine;
using DeskSlayer.Achievements;

namespace DeskSlayer.Juice
{
    /// <summary>
    /// 訂閱 AchievementService 的解鎖事件，在固定的 Canvas 錨點下生成一個 Toast 彈窗提示。
    /// 比照 WeaponDropLogPanel 對 WeaponDropDispatcher 的訂閱模式，職責僅限於「收到解鎖事件時生成一個 Toast」，
    /// 不參與任何成就判定邏輯。_toastAnchor 需掛 Vertical Layout Group（比照 WeaponDropLogPanel 的
    /// _listContainer 設定），讓短時間內連續解鎖時多個 Toast 依序疊成清單，而不是全部生成在同一個座標
    /// 互相重疊、只看得到最上面那一個。
    /// </summary>
    public sealed class AchievementToastDispatcher : MonoBehaviour
    {
        [SerializeField]
        private AchievementService _achievementService;

        [SerializeField]
        private AchievementToastEntry _toastPrefab;

        [SerializeField, Tooltip("Toast 生成的父節點（需掛 Vertical Layout Group，讓多個 Toast 疊列而非互相重疊）")]
        private Transform _toastAnchor;

        [SerializeField]
        private AchievementToastConfigSO _config;

        private void OnEnable()
        {
            if (_achievementService != null)
            {
                _achievementService.OnAchievementUnlocked += HandleAchievementUnlocked;
            }
        }

        private void OnDisable()
        {
            if (_achievementService != null)
            {
                _achievementService.OnAchievementUnlocked -= HandleAchievementUnlocked;
            }
        }

        private void HandleAchievementUnlocked(AchievementDefinitionSO achievement)
        {
            if (_toastPrefab == null || _toastAnchor == null || _config == null)
            {
                return;
            }

            AchievementToastEntry toast = Instantiate(_toastPrefab, _toastAnchor);
            toast.transform.SetAsFirstSibling();
            toast.Show(achievement, _config);
        }
    }
}
