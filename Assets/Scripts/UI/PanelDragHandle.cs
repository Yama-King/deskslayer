using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 彈出面板（武器背包、天氣選城市）的拖曳：掛在 PanelRoot 本身——這個 GameObject 本來就有一張
    /// 鋪滿整個面板的背景 Image（RaycastTarget 開啟），按鈕、格子等互動元件是疊在它前面的子物件，
    /// 會先攔截自己範圍內的輸入，因此不需要另外刻一份「排除清單」去判斷「這是不是按鈕」——
    /// 拖曳偵測天然只在按鈕/格子以外的背景空白處生效。
    ///
    /// 只位移自己這個 RectTransform 的 anchoredPosition，完全不引用 GameWorldDragCoordinator，
    /// 天生不會影響 GameWorldRoot 或任何錨定按鈕。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PanelDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform _rectTransform;
        private Canvas _canvas;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
            _rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }
    }
}
