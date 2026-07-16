using Kirurobo;
using UnityEngine;

namespace DeskSlayer.DesktopWindow
{
    /// <summary>
    /// 桌面透明視窗「視窗本體生命週期」的唯一權責入口：開機時計算涵蓋所有實體螢幕的虛擬桌面範圍，
    /// 並把 UniWindowController 的視窗大小/位置設為這個範圍，設定完成後不再變動。
    /// 點擊穿透判定與拖曳互動屬於後續步驟，刻意不在這個類別處理，避免職責膨脹成 God Class。
    ///
    /// 大小/位置只能在 UniWinCore 實際「附著」到本機視窗控制代碼之後才呼叫，太早呼叫會是無效操作
    /// （原生視窗代碼尚未取得）。UniWindowController 在附著完成的同一個 Update() 內，會呼叫一次
    /// OnMonitorChanged 事件（其原始用途是給內建的 FitToMonitor 用），這是套件本身唯一保證「視窗已可
    /// 安全設定大小/位置」的時機點，因此借用這個事件當作 ready 訊號，而不是自行猜測要等幾個 Update frame。
    /// 用完立即取消訂閱，確保只套用一次——多螢幕環境下執行期間插拔螢幕不會重新計算，見下方已知限制。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UniWindowController))]
    public sealed class DesktopWindowLifecycleController : MonoBehaviour
    {
        private UniWindowController _windowController;
        private bool _hasAppliedStartupBounds;

        private void Awake()
        {
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

        private void Start()
        {
            // currentCamera 沿用套件本身「未指定則使用 Camera.main」的既有慣例（見 UniWindowController.Awake），
            // 專案內目前也沒有另一套取得主攝影機的機制，所以這裡不引入新的取得方式，只是在缺席時做同樣的回退。
            Camera camera = _windowController.currentCamera != null ? _windowController.currentCamera : Camera.main;
            DesktopWindowEnvironmentValidator.Validate(camera);
        }

        /// <summary>
        /// 已知限制：涵蓋範圍只在遊戲啟動、視窗第一次附著完成時計算一次。執行期間插拔螢幕或改變螢幕
        /// 排列不會觸發重新計算，需要重啟遊戲才會套用新的螢幕配置。多螢幕情境下此為刻意取捨——
        /// 動態偵測需要持續比對監視器組態是否有變（OnMonitorChanged 之後續觸發即可作為訊號來源），
        /// 但那會讓「視窗設定完成後保持固定」這個下一步驟（拖曳互動）仰賴的前提變得不穩定，
        /// 投入產出比不划算，故不在這份規格範圍內實作。
        /// </summary>
        private void HandleWindowReady()
        {
            if (_hasAppliedStartupBounds)
            {
                return;
            }

            _hasAppliedStartupBounds = true;
            _windowController.OnMonitorChanged -= HandleWindowReady;

            ApplyStartupCoverageBounds();
        }

        private void ApplyStartupCoverageBounds()
        {
            int monitorCount = UniWindowController.GetMonitorCount();
            if (monitorCount <= 0)
            {
                Debug.LogWarning("[DesktopWindow] 未偵測到任何螢幕，視窗大小/位置維持不變。");
                return;
            }

            Rect bounds = UniWindowController.GetMonitorRect(0);
            for (int i = 1; i < monitorCount; i++)
            {
                bounds = EncapsulateRect(bounds, UniWindowController.GetMonitorRect(i));
            }

            _windowController.windowPosition = bounds.position;
            _windowController.windowSize = bounds.size;
        }

        /// <summary>回傳能同時涵蓋兩個矩形的最小外接矩形，用來把多台螢幕的個別範圍合併成虛擬桌面範圍。</summary>
        private static Rect EncapsulateRect(Rect a, Rect b)
        {
            float xMin = Mathf.Min(a.xMin, b.xMin);
            float yMin = Mathf.Min(a.yMin, b.yMin);
            float xMax = Mathf.Max(a.xMax, b.xMax);
            float yMax = Mathf.Max(a.yMax, b.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
