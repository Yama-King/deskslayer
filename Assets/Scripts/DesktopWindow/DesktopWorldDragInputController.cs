using UnityEngine;
using UnityEngine.InputSystem;

namespace DeskSlayer.DesktopWindow
{
    /// <summary>
    /// 世界成員（地塊/角色/敵人，DesktopHitTest 圖層上的 Collider2D）拖曳輸入偵測。UI（含錨定按鈕、
    /// 彈出面板）走 uGUI 自己的 IBeginDragHandler/IDragHandler 事件（見 AnchoredUIDragSource／
    /// PanelDragHandle），不經過這裡——這裡只負責 mediator.LastHitKind 為 GameObject 的情況，
    /// 兩條路徑最終都收斂到同一個 GameWorldDragCoordinator.ApplyScreenDelta，見該類別註解。
    ///
    /// 專案 Active Input Handler 設為新版 Input System Only（ProjectSettings.activeInputHandler=1），
    /// 因此用 Mouse.current 讀取按鍵狀態，不能用舊版 Input.GetMouseButton 系列。
    ///
    /// 這裡每幀讀的 mediator.LastHitKind/LastScreenPoint 是 mediator 自己 Update() 當幀算出來的，
    /// 必須確保這裡的 Update() 在 mediator 的 Update() 之後執行，否則會讀到上一幀的舊值——Unity 不
    /// 保證同一個 GameObject 上不同元件的 Update() 執行順序（跟 AnchoredUISyncMover 呼叫
    /// ButtonHoverPunch 時踩過的 Awake 順序問題是同一類風險，見該類別註解），因此用
    /// DefaultExecutionOrder 明確排在預設順序（0）之後，不依賴元件在 Inspector 清單裡剛好排在
    /// 後面這種不保證的巧合。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DesktopWindowClickThroughMediator))]
    [DefaultExecutionOrder(100)]
    public sealed class DesktopWorldDragInputController : MonoBehaviour
    {
        [SerializeField, Tooltip("拖曳世界成員時實際位移 GameWorldRoot 的協調者")]
        private GameWorldDragCoordinator _coordinator;

        private DesktopWindowClickThroughMediator _mediator;
        private bool _isDragging;
        private Vector2 _previousScreenPoint;

        private void Awake()
        {
            _mediator = GetComponent<DesktopWindowClickThroughMediator>();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (_mediator.LastHitKind == CursorHitKind.GameObject)
                {
                    _isDragging = true;
                    _previousScreenPoint = _mediator.LastScreenPoint;
                }

                return;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
                return;
            }

            if (!_isDragging)
            {
                return;
            }

            if (!mouse.leftButton.isPressed)
            {
                _isDragging = false;
                return;
            }

            Vector2 currentScreenPoint = _mediator.LastScreenPoint;
            Vector2 delta = currentScreenPoint - _previousScreenPoint;
            _previousScreenPoint = currentScreenPoint;

            if (delta != Vector2.zero)
            {
                _coordinator.ApplyScreenDelta(delta);
            }
        }
    }
}
