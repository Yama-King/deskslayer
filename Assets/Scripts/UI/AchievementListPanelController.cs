using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using DeskSlayer.Achievements;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 成就選單面板：依 AchievementDatabaseSO 的順序生成所有成就項目（含尚未解鎖的），
    /// 並訂閱 AchievementService 的解鎖事件即時刷新對應項目的狀態，不需要每次開啟面板重新整包查詢。
    /// 開關方式比照 WeaponInventoryPanelController：疊加在畫面上的浮動面板，用 CanvasGroup
    /// 淡入淡出＋RectTransform 縮放回彈呈現，不使用 GameObject.SetActive（避免每次開關都重新
    /// Populate／重新訂閱事件），也不使用 Time.timeScale，只透過畫面按鈕點擊開關。
    /// 純功能性面板，不做排版與視覺設計。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class AchievementListPanelController : MonoBehaviour
    {
        [SerializeField]
        private AchievementDatabaseSO _database;

        [SerializeField]
        private AchievementService _achievementService;

        [SerializeField]
        private AchievementEntryView _entryPrefab;

        [SerializeField, Tooltip("清單項目的父節點")]
        private Transform _listContainer;

        [SerializeField, Tooltip("淡入淡出過場時長（秒）")]
        private float _fadeDuration = 0.2f;

        [SerializeField, Tooltip("面板縮放過場用的根節點")]
        private RectTransform _panelRoot;

        private readonly Dictionary<string, AchievementEntryView> _entriesById = new Dictionary<string, AchievementEntryView>();

        private CanvasGroup _canvasGroup;
        private bool _isOpen;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            SetClosedImmediate();
        }

        private void OnEnable()
        {
            Populate();

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

        /// <summary>切換面板開關狀態。</summary>
        public void Toggle()
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>開啟面板：允許互動並播放淡入 + 縮放回彈過場。</summary>
        public void Open()
        {
            if (_isOpen)
            {
                return;
            }

            _isOpen = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            GetComponent<PanelDragHandle>()?.BringToFront();

            _canvasGroup.DOKill();
            _canvasGroup.DOFade(1f, _fadeDuration);

            if (_panelRoot != null)
            {
                _panelRoot.DOKill();
                _panelRoot.localScale = Vector3.one * 0.95f;
                _panelRoot.DOScale(1f, _fadeDuration).SetEase(Ease.OutBack);
            }
        }

        /// <summary>關閉面板：播放淡出過場，結束後停止阻擋滑鼠事件。</summary>
        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;

            _canvasGroup.DOKill();
            _canvasGroup.DOFade(0f, _fadeDuration)
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    _canvasGroup.interactable = false;
                });

            if (_panelRoot != null)
            {
                _panelRoot.DOKill();
                _panelRoot.DOScale(0.95f, _fadeDuration);
            }
        }

        private void SetClosedImmediate()
        {
            _isOpen = false;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            if (_panelRoot != null)
            {
                _panelRoot.localScale = Vector3.one * 0.95f;
            }
        }

        private void Populate()
        {
            if (_database == null || _database.AllAchievements == null || _entryPrefab == null || _listContainer == null)
            {
                return;
            }

            foreach (Transform child in _listContainer)
            {
                Destroy(child.gameObject);
            }
            _entriesById.Clear();

            foreach (AchievementDefinitionSO achievement in _database.AllAchievements)
            {
                if (achievement == null)
                {
                    continue;
                }

                AchievementEntryView entry = Instantiate(_entryPrefab, _listContainer);
                bool unlocked = _achievementService != null && _achievementService.IsUnlocked(achievement.Id);
                entry.Bind(achievement, unlocked);
                _entriesById[achievement.Id] = entry;
            }
        }

        private void HandleAchievementUnlocked(AchievementDefinitionSO achievement)
        {
            if (achievement != null && _entriesById.TryGetValue(achievement.Id, out AchievementEntryView entry))
            {
                entry.SetUnlocked(true);
            }
        }
    }
}
