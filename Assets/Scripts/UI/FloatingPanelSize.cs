using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 浮動面板（武器背包、天氣選城市）外框尺寸/位置的可調參數。[ExecuteAlways] + OnValidate
    /// 讓 Inspector 數值變動時 Edit Mode 就直接套用，不需要進 Play Mode 才能預覽調整結果——
    /// 面板最終大小是實機試玩後的主觀判斷（見規格書已知限制），這裡只負責讓調整這件事本身
    /// 快速，不是預先算出正確答案。
    ///
    /// 額外支援滑鼠中鍵滾輪縮放（IScrollHandler）：桌面透明視窗環境下無法重新 Build 就即時看到
    /// Inspector 欄位調整的結果（欄位調整仍然只能在 Editor 裡預覽），滾輪縮放讓使用者能直接在
    /// 打包出來的 .exe 裡滑鼠移到面板上滾動滑鼠中鍵即時試出想要的大小，不用重新 Build。
    ///
    /// 外框尺寸（_baseSizeDelta）與內部內容縮放（_baseContentScale）刻意用同一個 _zoomRatio 共同
    /// 驅動，而不是各自維護一組獨立的滾輪縮放上下限：早期版本讓兩者各自 clamp 在自己的
    /// min/max 範圍，滾輪縮到極端值時，其中一個先撞到自己的上限/下限就停住，另一個卻還在繼續
    /// 縮放，兩者的比例關係就跑掉了（外框尺寸跟裡面的內容物對不上、跑版）——這是實測抓到的真實
    /// bug，不是假設性風險。現在只有一個 _zoomRatio 會被 clamp（_minZoomRatio ~ _maxZoomRatio），
    /// 外框與內容永遠是「設計原始值 × 同一個 zoomRatio」，不管縮放到範圍內任何一點，兩者的比例
    /// 關係都跟原始設計（zoomRatio=1 時）完全一致，天生不會跑版。
    ///
    /// 位置（_anchoredPosition）刻意跟尺寸分開套用：這個欄位只在 OnEnable／Inspector 編輯時
    /// （OnValidate）套用一次，滾輪縮放（OnScroll）只呼叫 ApplySize，不會重新套用位置。
    /// 面板實際的即時位置在使用者拖曳後（PanelDragHandle）是由 RectTransform.anchoredPosition
    /// 自己記著，並不會回寫進這個欄位——如果 OnScroll 也跟著套用位置，玩家拖曳面板到別處後只要
    /// 再滾一次滾輪，面板就會瞬間跳回原本的設計位置，這是實測抓到的真實 bug，不是假設性風險。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class FloatingPanelSize : MonoBehaviour, IScrollHandler
    {
        [Header("設計原始值（zoomRatio = 1 時的樣子）")]
        [SerializeField, Tooltip("面板外框尺寸（寬, 高），這是設計原始值，不是縮放後的即時值")]
        private Vector2 _baseSizeDelta = new Vector2(520f, 380f);

        [SerializeField, Tooltip("面板錨定位置（相對螢幕中心的偏移量，anchorMin/Max 固定為 0.5,0.5），不受縮放影響")]
        private Vector2 _anchoredPosition = new Vector2(412f, 22f);

        [SerializeField, Tooltip("內部既有內容整體縮放倍率的設計原始值，只有面板底下有 ContentScaleRoot 子物件時才會用到，沒有就略過")]
        private float _baseContentScale = 0.45f;

        [Header("滑鼠滾輪縮放")]
        [SerializeField, Tooltip("滑鼠停在面板上滾動滾輪時，每一格滾動改變的比例")]
        private float _scrollStep = 0.05f;

        [SerializeField, Range(0.1f, 1f), Tooltip("縮放倍率下限，太小會讓文字/按鈕小到看不清楚或點不到（跑版），依實機試玩調整")]
        private float _minZoomRatio = 0.6f;

        [SerializeField, Range(1f, 3f), Tooltip("縮放倍率上限，太大可能超出螢幕範圍，依實機試玩調整")]
        private float _maxZoomRatio = 1.6f;

        private RectTransform _rectTransform;
        private float _zoomRatio = 1f;

        private void OnEnable()
        {
            ApplyPosition();
            ApplySize();
        }

        private void OnValidate()
        {
            _zoomRatio = Mathf.Clamp(_zoomRatio, _minZoomRatio, _maxZoomRatio);

#if UNITY_EDITOR
            // RectTransform 的 sizeDelta/anchoredPosition 改動會觸發版面配置系統用 SendMessage 通知
            // 相關元件，但 OnValidate 執行期間 Unity 不允許呼叫 SendMessage，直接在這裡套用會在
            // Console 噴出無害但吵雜的警告。延到下一個 Editor tick 再套用，避開這個時機限制——
            // 純粹是 Editor 內 Inspector 編輯時的時機問題，Build 裡 OnValidate 本來就不會被呼叫，
            // 不影響玩家看到的行為。delayCall 排進佇列後如果剛好遇到程式重新編譯（domain reload），
            // 這個元件實例會被銷毀重建，佇列裡留著對舊實例的參照，執行時就會噴
            // MissingReferenceException——用 `this != null`（Unity 對已銷毀物件覆寫過的判斷）
            // 擋掉這個情況，而不是假設 delayCall 一定會在同一個實例活著的時候執行。
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    ApplyPosition();
                    ApplySize();
                }
            };
#else
            ApplyPosition();
            ApplySize();
#endif
        }

        /// <summary>
        /// 滑鼠停在面板背景上滾動滾輪時觸發（跟 PanelDragHandle 的拖曳判定同一個道理：這個事件
        /// 掛在 PanelRoot 上，按鈕/格子等互動元件的可點擊區域會擋在前面優先接收輸入，滾輪縮放
        /// 天然只在空白背景區域生效）。只呼叫 ApplySize，刻意不呼叫 ApplyPosition——見類別註解。
        /// </summary>
        public void OnScroll(PointerEventData eventData)
        {
            float direction = Mathf.Sign(eventData.scrollDelta.y);
            if (direction == 0f)
            {
                return;
            }

            float factor = 1f + direction * _scrollStep;
            _zoomRatio = Mathf.Clamp(_zoomRatio * factor, _minZoomRatio, _maxZoomRatio);

            ApplySize();
        }

        private void ApplyPosition()
        {
            if (_rectTransform == null)
            {
                _rectTransform = (RectTransform)transform;
            }

            _rectTransform.anchoredPosition = _anchoredPosition;
        }

        private void ApplySize()
        {
            if (_rectTransform == null)
            {
                _rectTransform = (RectTransform)transform;
            }

            _rectTransform.sizeDelta = _baseSizeDelta * _zoomRatio;

            Transform contentScaleRoot = transform.Find("ContentScaleRoot");
            if (contentScaleRoot != null)
            {
                contentScaleRoot.localScale = Vector3.one * (_baseContentScale * _zoomRatio);
            }
        }
    }
}
