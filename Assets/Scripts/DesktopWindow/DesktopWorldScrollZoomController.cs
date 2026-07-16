using UnityEngine;
using UnityEngine.InputSystem;

namespace DeskSlayer.DesktopWindow
{
    /// <summary>
    /// 世界成員（地塊/角色/敵人，DesktopHitTest 圖層上的 Collider2D）滾輪縮放輸入偵測。UI（錨定按鈕）
    /// 走 uGUI 自己的 IScrollHandler 事件（見 AnchoredUIDragSource），不經過這裡——這裡只負責
    /// mediator.LastHitKind 為 GameObject 的情況，兩條路徑最終都收斂到同一個
    /// GameWorldDragCoordinator.ApplyWorldScaleDelta，見該類別註解。架構上完全比照
    /// DesktopWorldDragInputController 處理拖曳輸入的既有模式，只是這裡讀的是滾輪而不是左鍵。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DesktopWindowClickThroughMediator))]
    [DefaultExecutionOrder(100)]
    public sealed class DesktopWorldScrollZoomController : MonoBehaviour
    {
        [SerializeField, Tooltip("滾輪縮放時實際縮放 GameWorldRoot 的協調者")]
        private GameWorldDragCoordinator _coordinator;

        private DesktopWindowClickThroughMediator _mediator;

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

            if (_mediator.LastHitKind != CursorHitKind.GameObject)
            {
                return;
            }

            float scrollY = mouse.scroll.ReadValue().y;
            if (scrollY == 0f)
            {
                return;
            }

            _coordinator.ApplyWorldScaleDelta(scrollY);
        }
    }
}
