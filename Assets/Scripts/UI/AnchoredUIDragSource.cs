using DeskSlayer.DesktopWindow;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 常駐錨定按鈕（背包開關、天氣開關、暫停按鈕、武器快切等）的拖曳來源：抓這顆按鈕拖曳時，
    /// 帶動 GameWorldRoot 與其他錨定按鈕整組一起移動，效果與拖曳世界成員一致（見
    /// GameWorldDragCoordinator 註解）。
    ///
    /// 「點一下 vs 按住拖曳」的意圖判斷刻意不自己刻閾值：uGUI 內建的
    /// EventSystem.pixelDragThreshold（預設 10px）本身就是業界標準做法，一旦超過閾值觸發
    /// OnBeginDrag，Button.onClick 就會被 EventSystem 自動抑制，不需要額外程式碼判斷。
    ///
    /// 這顆按鈕自己的位移完全不在這裡處理，一律透過 AnchoredUISyncMover 訂閱
    /// GameWorldDragCoordinator.OnGameWorldRootMoved 來移動，避免「自己觸發的拖曳 + 廣播回饋」
    /// 疊加移動兩倍距離。滾輪縮放（IScrollHandler）同一個道理：這顆按鈕自己的縮放也不在這裡處理，
    /// 一律透過 AnchoredUISyncMover 訂閱 OnWorldScaleChanged 來套用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnchoredUIDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        [SerializeField, Tooltip("拖曳／滾輪縮放時實際位移/縮放 GameWorldRoot 的協調者")]
        private GameWorldDragCoordinator _coordinator;

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            _coordinator.ApplyScreenDelta(eventData.delta);
        }

        public void OnScroll(PointerEventData eventData)
        {
            _coordinator.ApplyWorldScaleDelta(eventData.scrollDelta.y);
        }
    }
}
