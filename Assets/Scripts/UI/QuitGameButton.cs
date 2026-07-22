using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 主選單「離開」按鈕的行為：直接結束遊戲，不彈確認對話框（沒有另外被要求要有確認流程）。
    /// Editor 內 Application.Quit() 不會真的結束 Play Mode，這裡額外處理讓 Editor 測試時也能看到
    /// 對應效果。
    /// </summary>
    public sealed class QuitGameButton : MonoBehaviour
    {
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
