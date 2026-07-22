using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeskSlayer.Analytics;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡面板：玩家手動觸發生成流程的 UI 入口。開關方式比照 AchievementListPanelController
    /// （CanvasGroup 淡入淡出 + RectTransform 縮放回彈），「當日/累積」切換比照 WeaponFamilyTabGroup
    /// 的雙頁籤高亮模式。流程進行中鎖住產生按鈕並顯示處理中文字，避免玩家重複觸發造成流程衝突。
    /// 只負責 UI 反應與流程串接，實際擷取交給 ShareCardCaptureFlowController、合成交給
    /// ShareCardComposer、檔案輸出交給 ShareCardFileExporter，各自單一職責。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ShareCardPanelController : MonoBehaviour
    {
        [SerializeField]
        private ShareCardStatsTracker _statsTracker;

        [SerializeField]
        private PlayStyleAnalyzer _playStyleAnalyzer;

        [SerializeField]
        private ShareCardCaptureFlowController _captureFlowController;

        [SerializeField]
        private ShareCardComposer _composer;

        [SerializeField]
        private StyleArchetypeThresholdConfigSO _thresholdConfig;

        [SerializeField]
        private ShareCardRevealAnimator _revealAnimator;

        [SerializeField, Tooltip("面板縮放過場用的根節點")]
        private RectTransform _panelRoot;

        [SerializeField, Tooltip("淡入淡出過場時長（秒）")]
        private float _fadeDuration = 0.2f;

        [SerializeField]
        private Button _dailyScopeButton;

        [SerializeField]
        private Button _totalScopeButton;

        [SerializeField, Tooltip("目前選取「當日」頁籤的高亮圖示（可留空，不強制要求視覺區隔）")]
        private Image _dailyScopeHighlight;

        [SerializeField, Tooltip("目前選取「累積」頁籤的高亮圖示（可留空，不強制要求視覺區隔）")]
        private Image _totalScopeHighlight;

        [SerializeField]
        private Button _generateButton;

        [SerializeField, Tooltip("產生按鈕上的文字（可留空，不強制要求文字提示）")]
        private TextMeshProUGUI _generateButtonLabel;

        [SerializeField]
        private string _generateIdleLabel = "產生分享卡";

        [SerializeField]
        private string _generateProcessingLabel = "處理中...";

        [SerializeField]
        private string _dailyScopeLabel = "本日戰績";

        [SerializeField]
        private string _totalScopeLabel = "累積戰績";

        private CanvasGroup _canvasGroup;
        private bool _isOpen;
        private bool _showDailyScope = true;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            SetClosedImmediate();
        }

        private void OnEnable()
        {
            _dailyScopeButton.onClick.AddListener(SelectDailyScope);
            _totalScopeButton.onClick.AddListener(SelectTotalScope);
            _generateButton.onClick.AddListener(HandleGenerateClicked);

            if (_captureFlowController != null)
            {
                _captureFlowController.OnCaptureSucceeded += HandleCaptureSucceeded;
                _captureFlowController.OnCaptureFailed += HandleCaptureFailed;
            }

            SelectDailyScope();
            SetGeneratingState(false);
        }

        private void OnDisable()
        {
            _dailyScopeButton.onClick.RemoveListener(SelectDailyScope);
            _totalScopeButton.onClick.RemoveListener(SelectTotalScope);
            _generateButton.onClick.RemoveListener(HandleGenerateClicked);

            if (_captureFlowController != null)
            {
                _captureFlowController.OnCaptureSucceeded -= HandleCaptureSucceeded;
                _captureFlowController.OnCaptureFailed -= HandleCaptureFailed;
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
            GetComponent<DeskSlayer.UI.PanelDragHandle>()?.BringToFront();

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

        private void SelectDailyScope()
        {
            _showDailyScope = true;
            UpdateScopeHighlight();
        }

        private void SelectTotalScope()
        {
            _showDailyScope = false;
            UpdateScopeHighlight();
        }

        private void UpdateScopeHighlight()
        {
            if (_dailyScopeHighlight != null)
            {
                _dailyScopeHighlight.enabled = _showDailyScope;
            }

            if (_totalScopeHighlight != null)
            {
                _totalScopeHighlight.enabled = !_showDailyScope;
            }
        }

        private void HandleGenerateClicked()
        {
            if (_captureFlowController == null || _captureFlowController.IsCapturing)
            {
                return;
            }

            SetGeneratingState(true);
            _captureFlowController.BeginCapture();
        }

        private void HandleCaptureSucceeded(Texture2D background)
        {
            ShareCardDisplayData data = BuildDisplayData();

            if (_composer != null && _composer.TryCompose(background, data, out byte[] pngBytes))
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                ShareCardFileExporter.SaveAndReveal(pngBytes);
#else
                Debug.LogWarning("[ShareCardPanelController] 目前平台不支援自動開啟檔案總管，分享卡未輸出。");
#endif
            }
            else
            {
                Debug.LogWarning("[ShareCardPanelController] 圖卡合成失敗，未輸出檔案。");
            }

            Destroy(background);

            if (_revealAnimator != null)
            {
                _revealAnimator.PlayReveal(data);
            }

            SetGeneratingState(false);
        }

        private void HandleCaptureFailed()
        {
            Debug.LogWarning("[ShareCardPanelController] 螢幕擷取失敗，未產生分享卡。");
            SetGeneratingState(false);
        }

        private ShareCardDisplayData BuildDisplayData()
        {
            _statsTracker?.RefreshDailyRolloverIfNeeded();
            _playStyleAnalyzer?.RefreshDailyRolloverIfNeeded();

            long typedCount = 0;
            long killCount = 0;

            if (_statsTracker != null)
            {
                typedCount = _showDailyScope ? _statsTracker.DailyTypedCount : _statsTracker.TotalTypedCount;
                killCount = _showDailyScope ? _statsTracker.DailyKillCount : _statsTracker.TotalKillCount;
            }

            float lightScore = 0f;
            float rhythmScore = 0f;

            if (_playStyleAnalyzer != null)
            {
                lightScore = _showDailyScope ? _playStyleAnalyzer.DailyLightAttackTendencyScore : _playStyleAnalyzer.TotalLightAttackTendencyScore;
                rhythmScore = _showDailyScope ? _playStyleAnalyzer.DailyRhythmStabilityScore : _playStyleAnalyzer.TotalRhythmStabilityScore;
            }

            string scopeLabel = _showDailyScope ? _dailyScopeLabel : _totalScopeLabel;

            StyleArchetypeId archetypeId = _thresholdConfig != null
                ? StyleArchetypeClassifier.Classify(rhythmScore, lightScore, _thresholdConfig)
                : StyleArchetypeId.Balanced;

            return new ShareCardDisplayData(typedCount, killCount, lightScore, rhythmScore, scopeLabel, archetypeId);
        }

        private void SetGeneratingState(bool isGenerating)
        {
            _generateButton.interactable = !isGenerating;

            if (_generateButtonLabel != null)
            {
                _generateButtonLabel.text = isGenerating ? _generateProcessingLabel : _generateIdleLabel;
            }
        }
    }
}
