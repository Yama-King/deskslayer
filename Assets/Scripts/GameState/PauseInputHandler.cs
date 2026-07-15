using UnityEngine;
using UnityEngine.InputSystem;

namespace DeskSlayer.GameState
{
    /// <summary>
    /// 職責僅限於偵測 Esc 鍵並呼叫 GameStateMachine 切換階段，不持有任何狀態、
    /// 不認識 UI 或其他系統。比照 GlobalKeyboardHookService（輸入來源）與
    /// TypingEnergySystem（邏輯消費者）的職責切分：這裡只是輸入來源。
    /// 使用 Unity Input System（本專案 Player Settings 的 Active Input Handling 已設為
    /// 「Input System Package (New)」，舊版 UnityEngine.Input 在此設定下會直接拋例外）
    /// 而非全域鍵盤 Hook——暫停選單只在遊戲視窗聚焦時才需要回應，這是暫停選單的標準慣例，
    /// 與「打字驅動戰鬥」刻意設計成視窗未聚焦也能運作的需求不同。
    /// </summary>
    public sealed class PauseInputHandler : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                GameStateMachine.Instance?.TogglePause();
            }
        }
    }
}
