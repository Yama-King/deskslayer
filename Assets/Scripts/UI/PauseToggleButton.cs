using DeskSlayer.GameState;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 暫停切換按鈕的視覺狀態呈現：依 GameStateMachine 目前階段切換圖示，讓玩家一眼判斷目前是否
    /// 處於暫停狀態（狀態指示，不是「按下去會怎樣」的動作提示）。實際的暫停/繼續切換動作由
    /// Button.OnClick 透過 Inspector persistent call 直接呼叫 GameStateMachine.TogglePause()，
    /// 這裡不重複處理點擊邏輯，只單純反應狀態變化——比照 DesktopLockToggleButton 用單一 Image
    /// 切換兩張圖示的做法。
    /// </summary>
    public sealed class PauseToggleButton : MonoBehaviour
    {
        [SerializeField]
        private Image _iconImage;

        [SerializeField, Tooltip("Playing（遊戲進行中）狀態顯示的圖示")]
        private Sprite _playingIcon;

        [SerializeField, Tooltip("Paused（暫停中）狀態顯示的圖示")]
        private Sprite _pausedIcon;

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
            _iconImage.sprite = isPaused ? _pausedIcon : _playingIcon;
        }
    }
}
