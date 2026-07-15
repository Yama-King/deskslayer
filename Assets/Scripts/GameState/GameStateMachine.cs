using System;
using UnityEngine;

namespace DeskSlayer.GameState
{
    /// <summary>
    /// 全域遊戲階段的唯一權責入口：維護目前是 Playing 還是 Paused，並在切換時廣播事件。
    /// 其他系統（輸入分派、頓幀、音效、暫停 UI）一律訂閱 <see cref="OnGamePhaseChanged"/> 來反應，
    /// 不應該各自維護一份 isPaused 旗標，避免狀態分裂成多個真相來源。
    /// Time.timeScale 的寫入永遠放在事件廣播之後執行，確保不管訂閱者在回呼中做了什麼
    /// （例如 HitStopController 取消它自己尚未觸發的頓幀回復），這裡的寫入都是最後一手、擁有最終權威。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameStateMachine : MonoBehaviour
    {
        public static GameStateMachine Instance { get; private set; }

        public GamePhase CurrentPhase { get; private set; } = GamePhase.Playing;

        /// <summary>
        /// 階段切換事件，攜帶 (先前階段, 目前階段)。開機時會以 previous = null 觸發一次，
        /// 作為未來 Save/Load 系統「App 啟動、正式進入 Playing 之前」掛載讀檔邏輯的擴充點，
        /// 不需要另外設計一套轉場掛勾機制——訂閱這個事件並檢查 previous == null 即可辨識開機時機。
        /// </summary>
        public event Action<GamePhase?, GamePhase> OnGamePhaseChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 開機通知：所有場景常駐物件的 Awake/OnEnable 都保證已在 Start 之前執行完畢，
            // 因此這裡觸發時，任何訂閱者（包含未來的 Save/Load 系統）都已完成訂閱，不會漏接。
            OnGamePhaseChanged?.Invoke(null, CurrentPhase);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>在 Playing 與 Paused 之間切換，供 Esc 鍵等輸入來源呼叫。</summary>
        public void TogglePause()
        {
            SetPhase(CurrentPhase == GamePhase.Playing ? GamePhase.Paused : GamePhase.Playing);
        }

        /// <summary>顯式指定目標階段，保留給未來非 Esc 觸發來源（例如 UI 按鈕、Save/Load 流程）使用。</summary>
        public void SetPhase(GamePhase newPhase)
        {
            if (newPhase == CurrentPhase)
            {
                return;
            }

            GamePhase previous = CurrentPhase;
            CurrentPhase = newPhase;
            OnGamePhaseChanged?.Invoke(previous, newPhase);
            Time.timeScale = newPhase == GamePhase.Paused ? 0f : 1f;
        }
    }
}
