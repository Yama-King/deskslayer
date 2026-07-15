using DeskSlayer.GameState;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 常駐暫停切換按鈕的視覺狀態呈現：依 GameStateMachine 目前階段切換圖示符號與底色，
    /// 讓玩家一眼判斷目前是否處於暫停狀態（狀態指示，不是「按下去會怎樣」的動作提示）。
    /// 實際的暫停/繼續切換動作由 Button.OnClick 透過 Inspector persistent call 直接呼叫
    /// GameStateMachine.TogglePause()（比照 PauseCanvas 底下 ResumeButton 既有的接線方式），
    /// 這裡不重複處理點擊邏輯，只單純反應狀態變化。
    ///
    /// 這個元件刻意放在 PauseCanvas 底下、與 PanelRoot（暫停疊加層本體）同層的兄弟節點，
    /// 不是 PanelRoot 的子節點：PanelRoot 的 CanvasGroup 在 Playing（關閉）時會整個淡出且
    /// blocksRaycasts/interactable 為 false，若這顆按鈕掛在它底下，遊戲進行中時就會連按鈕本身
    /// 都一併隱藏、點不到；Paused 時 CanvasGroup 雖然可互動，但仍不該讓「常駐可點擊」的入口
    /// 依附在會被開關的疊加層生命週期上。
    /// </summary>
    public sealed class PauseToggleButton : MonoBehaviour
    {
        [SerializeField]
        private Image _iconBackground;

        [SerializeField]
        private TextMeshProUGUI _iconLabel;

        [SerializeField, Tooltip("Playing（遊戲進行中）狀態顯示的符號，純圖示不是說明文字")]
        private string _playingGlyph = ">";

        [SerializeField, Tooltip("Paused（暫停中）狀態顯示的符號，純圖示不是說明文字")]
        private string _pausedGlyph = "II";

        [SerializeField]
        private Color _playingColor = Color.white;

        [SerializeField]
        private Color _pausedColor = new Color(1f, 0.55f, 0.2f);

        private void OnEnable()
        {
            SubscribeToGameStateMachine();
        }

        private void Start()
        {
            // 比照 PausePanelController 既有的雙重訂閱保護：GameStateMachine.Instance 要等它自己的
            // Awake() 跑完才會被賦值，OnEnable 當下有機率還沒賦值；Start 補訂一次確保無論跨物件
            // 執行順序為何，最終一定會成功訂閱到事件。
            SubscribeToGameStateMachine();
        }

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
            {
                GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            }
        }

        private void SubscribeToGameStateMachine()
        {
            if (GameStateMachine.Instance == null)
            {
                return;
            }

            // 先移除再訂閱，確保 OnEnable／Start 都呼叫到這裡時，事件上只會掛一份委派
            GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            GameStateMachine.Instance.OnGamePhaseChanged += HandleGamePhaseChanged;

            RefreshVisual(GameStateMachine.Instance.CurrentPhase);
        }

        private void HandleGamePhaseChanged(GamePhase? previous, GamePhase current)
        {
            RefreshVisual(current);
        }

        private void RefreshVisual(GamePhase phase)
        {
            bool isPaused = phase == GamePhase.Paused;
            _iconLabel.text = isPaused ? _pausedGlyph : _playingGlyph;
            _iconBackground.color = isPaused ? _pausedColor : _playingColor;
        }
    }
}
