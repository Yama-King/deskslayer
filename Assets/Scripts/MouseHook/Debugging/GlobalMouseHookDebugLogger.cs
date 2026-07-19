#if UNITY_EDITOR
using UnityEngine;

namespace DeskSlayer.MouseHook.Debugging
{
    /// <summary>
    /// 開發階段驗證用的除錯監聽器：訂閱 GlobalMouseHookService 的事件並輸出到 Console，
    /// 用於手動測試「遊戲視窗未聚焦時仍能攔截全域滑鼠點擊」。僅在 Editor 內編譯，不會進入正式版本。
    /// 比照 KeyboardHook.Debugging.GlobalKeyboardHookDebugLogger。
    /// </summary>
    [RequireComponent(typeof(GlobalMouseHookService))]
    public sealed class GlobalMouseHookDebugLogger : MonoBehaviour
    {
        private GlobalMouseHookService _service;

        private void Awake()
        {
            _service = GetComponent<GlobalMouseHookService>();
        }

        private void OnEnable()
        {
            _service.OnMouseClicked += HandleMouseClicked;
        }

        private void OnDisable()
        {
            _service.OnMouseClicked -= HandleMouseClicked;
        }

        private void HandleMouseClicked(MouseClickData data)
        {
            Debug.Log($"[MouseHook] button={data.Button} ticks={data.TimestampTicks}");
        }
    }
}
#endif
