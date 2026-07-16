using System.Collections.Generic;
using Kirurobo;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskSlayer.DesktopWindow
{
    /// <summary>
    /// 桌面透明視窗「點擊穿透判定」的唯一權責入口：每幀決定游標下方屬於 UI／具體 Collider2D／
    /// 純透明區域三者之一，並據此設定 UniWindowController.isClickThrough。取代套件內建的
    /// Opacity／Raycast 自動判斷（原因見〈技術決策紀錄_桌面透明視窗整合架構設計.md〉，不在此重述），
    /// 因此要求場景上的 UniWindowController.isHitTestEnabled 必須先手動設為 false，讓判定完全交給這裡——
    /// 這個前提是 Inspector 設定值本身，不在程式碼內覆寫，維持單一事實來源，寫法比照
    /// <see cref="DesktopWindowLifecycleController"/> 直接在場景 prefab instance 上設定旗標的既有慣例。
    ///
    /// 座標轉換刻意不沿用 UniWindowController 內部私有的 GetClientCursorPosition()：那個方法只服務套件
    /// 自己 Opacity 模式的 ReadPixels 取樣。這裡改用 UniWindowController.GetCursorPosition() 搭配
    /// windowPosition 反推游標的視窗內本地座標——兩者（連同步驟1用到的 GetMonitorRect）都是同一個
    /// LibUniWinC 原生外掛函式家族，經比對原生原始碼（libuniwinc.cpp）與 Build 實測（Mouse.current.position
    /// 逐幀比對）驗證過：全部已經是「原點左下、Y 向上」的 Unity 慣例座標（原生端統一用
    /// primaryMonitorHeight - rawY 換算過），不是 Win32 原始的左上原點/Y向下座標。因此兩者相減
    /// 就是正確的 Unity Screen 座標，不需要、也不能再額外做一次 Y 軸翻轉——早期版本誤以為這裡是
    /// Win32 原始座標而多做了一次翻轉，導致大部分 UI 位置的點擊判定被鏡像到錯誤的 Y 座標，這個假設
    /// 已經修正，之後若要改動這段轉換邏輯，務必先照這個方式重新對照原生原始碼或實機驗證，不要單憑直覺。
    ///
    /// 正因為座標轉換依賴步驟1套用好的 windowPosition/windowSize，這裡必須讀取
    /// DesktopWindowLifecycleController.HasAppliedStartupBounds 作為明確的完成訊號：涵蓋範圍套用完成前，
    /// windowPosition 還是 OS 一開始給的預設視窗位置，跟後續實際涵蓋全螢幕的位置不是同一個值，這段期間
    /// 算出來的座標會是錯的。不用「等固定幀數」這種猜測性做法，因為 DesktopWindowLifecycleController
    /// 已經有可靠的完成旗標可以直接依賴。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UniWindowController))]
    [RequireComponent(typeof(DesktopWindowLifecycleController))]
    public sealed class DesktopWindowClickThroughMediator : MonoBehaviour
    {
        [SerializeField, Tooltip("Physics2D.OverlapPoint 判定用的圖層名稱，只有這個圖層上的 Collider2D 會參與點擊穿透判定")]
        private string _hitTestLayerName = "DesktopHitTest";

        private UniWindowController _windowController;
        private DesktopWindowLifecycleController _lifecycleController;
        private Camera _camera;
        private int _hitTestLayerMask;
        private PointerEventData _pointerEventData;

        // 每幀重複使用，避免 GC Allocation——判定邏輯每幀都要跑，這是捨棄套件內建 Opacity 模式、
        // 換成自己控制的核心理由之一，這裡的實作也要延續同樣的效能要求。
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

        /// <summary>本幀判定結果分類，供步驟3（拖曳互動）直接讀取，不需要重複做一次判斷。</summary>
        public CursorHitKind LastHitKind { get; private set; } = CursorHitKind.None;

        /// <summary>本幀命中的 Collider2D；LastHitKind 不是 GameObject 時一律為 null。</summary>
        public Collider2D LastHitCollider { get; private set; }

        private void Awake()
        {
            _windowController = GetComponent<UniWindowController>();
            _lifecycleController = GetComponent<DesktopWindowLifecycleController>();
            _hitTestLayerMask = LayerMask.GetMask(_hitTestLayerName);
        }

        private void Start()
        {
            // currentCamera 的取得時機、回退規則比照 DesktopWindowLifecycleController：
            // 在 Start 才讀取，避開 Awake 執行順序在同一個 GameObject 上不保證先後的問題。
            _camera = _windowController.currentCamera != null ? _windowController.currentCamera : Camera.main;

            _pointerEventData = new PointerEventData(EventSystem.current);
        }

        private void Update()
        {
            if (!_lifecycleController.HasAppliedStartupBounds)
            {
                // 涵蓋範圍還沒套用完成，windowPosition 不可信任，這幾幀寧可維持不穿透的安全預設，
                // 也不要用還沒校正好的座標做判定。
                LastHitKind = CursorHitKind.None;
                LastHitCollider = null;
                _windowController.isClickThrough = false;
                return;
            }

            Vector2 screenPoint = ToUnityScreenPoint(UniWindowController.GetCursorPosition());
            EvaluateCursorTarget(screenPoint);

            _windowController.isClickThrough = (LastHitKind == CursorHitKind.None);
        }

        /// <summary>
        /// 把原生游標座標換算成視窗內的本地 Unity Screen 座標。GetCursorPosition() 與 windowPosition
        /// 都已經是「原點左下、Y 向上」的座標（見類別註解），單純相減即可，兩者用同一個原點基準，
        /// 多螢幕環境下（含左側/上方有負座標的螢幕）才不會算偏。
        /// </summary>
        private Vector2 ToUnityScreenPoint(Vector2 nativeCursorPosition)
        {
            return nativeCursorPosition - _windowController.windowPosition;
        }

        /// <summary>
        /// 三層優先序判定：UI > Collider2D > 純透明區域。拆成獨立方法是為了能在 Editor Play Mode
        /// 用反射搭配合成座標驗證判定邏輯本身，不需要依賴實際可用的透明視窗效果（那個只能在 Build 驗證）。
        /// </summary>
        private void EvaluateCursorTarget(Vector2 screenPoint)
        {
            if (EventSystem.current != null)
            {
                _pointerEventData.position = screenPoint;
                _uiRaycastResults.Clear();
                EventSystem.current.RaycastAll(_pointerEventData, _uiRaycastResults);

                if (_uiRaycastResults.Count > 0)
                {
                    LastHitKind = CursorHitKind.UI;
                    LastHitCollider = null;
                    return;
                }
            }

            if (_camera != null)
            {
                Vector3 worldPoint = _camera.ScreenToWorldPoint(
                    new Vector3(screenPoint.x, screenPoint.y, -_camera.transform.position.z));

                Collider2D hit = Physics2D.OverlapPoint(worldPoint, _hitTestLayerMask);
                if (hit != null)
                {
                    LastHitKind = CursorHitKind.GameObject;
                    LastHitCollider = hit;
                    return;
                }
            }

            LastHitKind = CursorHitKind.None;
            LastHitCollider = null;
        }
    }
}
