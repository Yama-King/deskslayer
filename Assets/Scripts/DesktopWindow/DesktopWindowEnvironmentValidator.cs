using UnityEngine;
using UnityEngine.Rendering;

namespace DeskSlayer.DesktopWindow
{
    /// <summary>
    /// 集中檢查透明桌面視窗仰賴的 Player Settings / Camera 前提是否成立，僅在 Console 留下警告，
    /// 不中斷遊戲、不彈窗——這是給開發者事後排查用的診斷，不是玩家會看到的內容。
    /// 之所以獨立成一個類別而不是散落在啟動流程裡，是因為這些前提（Graphics API、Flip Model
    /// Swapchain、Camera HDR）都是同一組「色鍵/Alpha 去背能否正常運作」的必要條件，理由相同、
    /// 變動時機相同，集中在一處才不會日後改 Player Settings 時要記得同步改好幾個地方。
    /// </summary>
    internal static class DesktopWindowEnvironmentValidator
    {
        /// <summary>
        /// 執行一次性檢查。targetCamera 傳入 UniWindowController 實際使用的攝影機，
        /// 因為 HDR Rendering 是逐 Camera 設定，不是全域的 Player Settings。
        /// </summary>
        public static void Validate(Camera targetCamera)
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11)
            {
                Debug.LogWarning(
                    $"[DesktopWindow] Graphics API 目前是 {SystemInfo.graphicsDeviceType}，預期應為 Direct3D11。" +
                    "透明去背需要鎖定 D3D11，請檢查 Player Settings > Player > Other Settings > Graphics APIs。");
            }

#if UNITY_EDITOR
            // Flip Model Swapchain 是純粹的 Build 設定（PlayerSettings.useFlipModelSwapchain），
            // 沒有對應的 Runtime API 可以在打包後的 Standalone Player 內讀取，因此這項檢查只能在 Editor
            // 內執行。Build 出來的執行檔若這個設定跑掉，不會有對應的 Console 警告，需要人工在
            // Player Settings 確認——這是刻意接受的已知限制，而非遺漏。
            if (UnityEditor.PlayerSettings.useFlipModelSwapchain)
            {
                Debug.LogWarning(
                    "[DesktopWindow] Flip Model Swapchain 目前是開啟狀態，必須關閉" +
                    "（PlayerSettings.useFlipModelSwapchain = false），否則 D3D Flip Model 會繞過 GDI 視窗合成、" +
                    "導致色鍵/Alpha 去背失效。此檢查僅在 Editor 執行，Build 後的執行檔請自行以 Player Settings 覆核。");
            }
#endif

            if (targetCamera != null && targetCamera.allowHDR)
            {
                Debug.LogWarning(
                    $"[DesktopWindow] Camera '{targetCamera.name}' 的 HDR Rendering 目前是開啟狀態，必須關閉，" +
                    "否則透明視窗的 Alpha 合成會不正確。");
            }
        }
    }
}
