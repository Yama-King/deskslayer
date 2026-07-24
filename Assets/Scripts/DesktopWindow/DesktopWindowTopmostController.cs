using DeskSlayer.Settings;
using Kirurobo;
using UnityEngine;

namespace DeskSlayer.DesktopWindow
{
    /// <summary>
    /// 桌面透明視窗「是否維持最上層」的唯一權責入口：開機時套用玩家上次的存檔選擇，
    /// 並讓設定選單能即時切換。取代先前單純靠 Prefab Override 寫死 UniWindowController
    /// 的 _isTopmost 序列化欄位（那只是套件附著前的暫時預設值，實際狀態一律以 SaveData 為準）。
    ///
    /// 職責切分與掛載位置比照 <see cref="DesktopWindowLifecycleController"/>：同樣掛在
    /// UniWindowController 所在的 GameObject，同樣借用 OnMonitorChanged 事件當作「視窗已附著、
    /// 可安全設定」的 ready 訊號——UniWindowController.isTopmost 的 setter 在 _uniWinCore 尚未
    /// 附著時會直接 no-op（見套件原始碼 SetTopmost），Awake/Start 階段呼叫等於沒呼叫。
    ///
    /// 對外提供 static Instance 讓不同 GameObject 上的 SettingsMenuController 能即時切換，
    /// 比照 AudioManager.Instance 供 UI 跨階層呼叫的既有慣例；不需要 DontDestroyOnLoad——
    /// 本專案只有單一常駐場景，掛載此元件的 GameObject 本身也不做跨場景保留。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UniWindowController))]
    public sealed class DesktopWindowTopmostController : MonoBehaviour
    {
        public static DesktopWindowTopmostController Instance { get; private set; }

        private UniWindowController _windowController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _windowController = GetComponent<UniWindowController>();
        }

        private void OnEnable()
        {
            _windowController.OnMonitorChanged += HandleWindowReady;
        }

        private void OnDisable()
        {
            _windowController.OnMonitorChanged -= HandleWindowReady;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>視窗附著完成後的第一個 ready 訊號，套用一次存檔設定值後立即取消訂閱，
        /// 比照 DesktopWindowLifecycleController.HandleWindowReady 只套用一次的作法。</summary>
        private void HandleWindowReady()
        {
            _windowController.OnMonitorChanged -= HandleWindowReady;
            _windowController.isTopmost = GameSettingsPreferenceStore.WindowTopmostEnabled;
        }

        /// <summary>供設定選單切換開關時即時套用，不需要重開程式或等待下一次啟動。</summary>
        public void SetTopmost(bool enabled)
        {
            _windowController.isTopmost = enabled;
        }
    }
}
