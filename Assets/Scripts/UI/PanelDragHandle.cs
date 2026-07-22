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
    ///
    /// 所有浮動面板目前預設都是 sortingOrder=0（互相打平手），EventSystem 對打平手的 Raycast
    /// 結果排序取決於 Canvas 建立順序而非「哪個面板視覺上疊在最上層」，導致面板互相重疊時，
    /// 拖曳有時候會抓到「看起來在下層」的那個面板——不是兩個面板同時被拖動，而是抓錯目標。
    /// 修法比照一般視窗系統的「點擊哪個視窗，哪個視窗就浮到最上層」慣例：每次開始拖曳，就把
    /// 自己的 Canvas.sortingOrder 蓋過所有浮動面板目前用過的最高值，讓「最後被點的面板」永遠是
    /// 下一次 Raycast 排序的優先命中對象，之後同一個重疊區域內的拖曳就會穩定抓到同一個、使用者
    /// 剛剛實際點中的面板，不會再抓到底下那層。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PanelDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        private static int s_topSortingOrder;

        private RectTransform _rectTransform;
        private Canvas _canvas;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// 不只在開始拖曳時才浮到最上層——單純點擊（不拖曳）也該讓面板浮到最上層，符合一般視窗
        /// 「點了就置頂」的直覺，拖曳只是額外的動作，不是置頂唯一的觸發時機。
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            BringToFront();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
            _rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }

        /// <summary>
        /// 供各面板自己的 PanelController 在 Open() 時主動呼叫：面板「被打開」本身也該讓它浮到最上層，
        /// 不能只靠使用者事後點擊才置頂——否則剛打開的面板一開始就疊在別的面板底下，使用者第一次
        /// 想拖動/點擊就會直接誤觸到底下那層，這正是實測抓到的真實 bug，不是假設性風險。
        /// </summary>
        public void BringToFront()
        {
            // 外部（各 PanelController.Open()）呼叫的時機不保證晚於這裡自己的 Awake()，
            // 保險起見在這裡也做一次延遲抓取，避免因為元件執行順序偶發抓不到 Canvas。
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas == null)
            {
                return;
            }

            s_topSortingOrder++;
            _canvas.sortingOrder = s_topSortingOrder;
        }
    }
}
