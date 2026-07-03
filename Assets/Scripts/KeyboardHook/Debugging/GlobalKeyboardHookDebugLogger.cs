#if UNITY_EDITOR
using UnityEngine;

namespace DeskSlayer.KeyboardHook.Debugging
{
    /// <summary>
    /// 開發階段驗證用的除錯監聽器：訂閱 GlobalKeyboardHookService 的事件並輸出到 Console，
    /// 用於手動測試「遊戲視窗未聚焦時仍能攔截全域鍵盤事件」。僅在 Editor 內編譯，不會進入正式版本。
    /// </summary>
    [RequireComponent(typeof(GlobalKeyboardHookService))]
    public sealed class GlobalKeyboardHookDebugLogger : MonoBehaviour
    {
        private GlobalKeyboardHookService _service;

        private void Awake()
        {
            _service = GetComponent<GlobalKeyboardHookService>();
        }

        private void OnEnable()
        {
            _service.OnKeyPressed += HandleKeyPressed;
        }

        private void OnDisable()
        {
            _service.OnKeyPressed -= HandleKeyPressed;
        }

        private void HandleKeyPressed(KeyPressData data)
        {
            Debug.Log($"[KeyboardHook] key='{data.Character}' ticks={data.TimestampTicks}");
        }
    }
}
#endif
