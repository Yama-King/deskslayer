using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 浮動面板（武器背包、天氣選城市、成就清單、分享卡片）外框尺寸/位置的可調參數。
    ///
    /// 套用 _anchoredPosition/_baseSizeDelta/_baseContentScale 這幾個設計原始值到 RectTransform，
    /// 刻意改成只能透過 Inspector 右鍵選單的「套用設計原始值」手動觸發（見 ApplyDesignValues），
    /// 不會在 OnEnable 或 OnValidate 自動套用。早期版本 OnEnable／OnValidate 都會自動套用一次，
    /// 結果只要直接在 Scene 視圖拖曳 RectTransform、或改 RectTransform 元件自己的欄位（而不是
    /// 透過這個元件的欄位調整），改動當下看起來沒事，但接下來只要有任何時機讓這個
    /// [ExecuteAlways] 元件重新驗證，就會用還沒更新的舊序列化欄位把 RectTransform 悄悄蓋回去。
    /// 原本以為「只有使用者編輯這個元件自己的欄位才會觸發 OnValidate」，但實測用 Debug.Log 追蹤
    /// 發現：即使完全沒有編輯任何欄位，Unity 光是進入或離開 Play Mode，就會對場景裡所有
    /// [ExecuteAlways] 元件自動重新呼叫一次 OnValidate——這是實測抓到的真實 bug，不是假設性風險，
    /// 也是這個元件放棄「自動套用」、改成手動觸發的直接原因：手動觸發不受 Unity 什麼時候會自動
    /// 重新驗證 ExecuteAlways 元件這個内部時機影響，套用這件事永遠只發生在使用者真的按下按鈕
    /// 的當下，沒有第三種「背景自動觸發」的可能性。
    ///
    /// Build 執行期不需要自動套用：玩家看到的 RectTransform 值本來就是 Editor 存檔當下最後一次
    /// 手動按過「套用設計原始值」的結果，直接序列化在場景/Prefab 裡，不需要在 Awake/OnEnable
    /// 重新套用一次。
    ///
    /// 額外支援滑鼠中鍵滾輪縮放（IScrollHandler）：桌面透明視窗環境下無法重新 Build 就即時看到
    /// Inspector 欄位調整的結果，滾輪縮放讓使用者能直接在打包出來的 .exe 裡滑鼠移到面板上滾動
    /// 滑鼠中鍵即時試出想要的大小，不用重新 Build。這條路徑不受上面「移除自動套用」影響，
    /// OnScroll 呼叫 ApplySize 屬於使用者當下主動操作觸發，不是背景自動套用。
    ///
    /// 外框尺寸（_baseSizeDelta）與內部內容縮放（_baseContentScale）刻意用同一個 _zoomRatio 共同
    /// 驅動，而不是各自維護一組獨立的滾輪縮放上下限：早期版本讓兩者各自 clamp 在自己的
    /// min/max 範圍，滾輪縮到極端值時，其中一個先撞到自己的上限/下限就停住，另一個卻還在繼續
    /// 縮放，兩者的比例關係就跑掉了（外框尺寸跟裡面的內容物對不上、跑版）——這是實測抓到的真實
    /// bug，不是假設性風險。現在只有一個 _zoomRatio 會被 clamp（_minZoomRatio ~ _maxZoomRatio），
    /// 外框與內容永遠是「設計原始值 × 同一個 zoomRatio」，不管縮放到範圍內任何一點，兩者的比例
    /// 關係都跟原始設計（zoomRatio=1 時）完全一致，天生不會跑版。
    ///
    /// 位置（_anchoredPosition）刻意跟尺寸分開套用：手動按「套用設計原始值」會同時套用位置跟
    /// 尺寸，但滾輪縮放（OnScroll）只呼叫 ApplySize，不會重新套用位置。面板實際的即時位置在
    /// 使用者拖曳後（PanelDragHandle）是由 RectTransform.anchoredPosition 自己記著，並不會回寫
    /// 進這個欄位——如果 OnScroll 也跟著套用位置，玩家拖曳面板到別處後只要再滾一次滾輪，面板就會
    /// 瞬間跳回原本的設計位置，這是實測抓到的真實 bug，不是假設性風險。
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

        [Header("全域尺寸拉桿（可留空，不強制要求）")]
        [SerializeField, Tooltip("介面大小拉桿的廣播者，收到 OnGlobalScaleChanged 時會把倍率一併乘進尺寸計算")]
        private GlobalPanelScaleBroadcaster _globalScaleBroadcaster;

        private RectTransform _rectTransform;
        private float _zoomRatio = 1f;
        private float _globalScale = 1f;

        private void OnValidate()
        {
            _zoomRatio = Mathf.Clamp(_zoomRatio, _minZoomRatio, _maxZoomRatio);
        }

        private void OnEnable()
        {
            if (_globalScaleBroadcaster != null)
            {
                _globalScale = _globalScaleBroadcaster.CurrentScale;
                _globalScaleBroadcaster.OnGlobalScaleChanged += HandleGlobalScaleChanged;
            }
        }

        private void OnDisable()
        {
            if (_globalScaleBroadcaster != null)
            {
                _globalScaleBroadcaster.OnGlobalScaleChanged -= HandleGlobalScaleChanged;
            }
        }

        private void HandleGlobalScaleChanged(float scale)
        {
            _globalScale = scale;
            ApplySize();
        }

        /// <summary>
        /// 在 Inspector 對這個元件按右鍵選單，選「套用設計原始值」手動觸發：把
        /// _anchoredPosition/_baseSizeDelta/_baseContentScale 套用到 RectTransform，用來取代
        /// 舊版在 OnEnable/OnValidate 自動套用（見類別註解）。調完 _anchoredPosition/_baseSizeDelta
        /// /_baseContentScale 這幾個欄位後，記得手動按這裡才會在 Scene 看到套用結果。
        /// </summary>
        [ContextMenu("套用設計原始值 (Apply Design Values)")]
        private void ApplyDesignValues()
        {
            ApplyPosition();
            ApplySize();
        }

        /// <summary>
        /// 跟 ApplyDesignValues 方向相反：把「目前 RectTransform 手動調整出來的實際尺寸/位置」
        /// 存回 _baseSizeDelta/_anchoredPosition，取代原本序列化的設計值。
        ///
        /// 用途：滑鼠滾輪縮放（OnScroll）每次都是用 _baseSizeDelta × _zoomRatio 重新算尺寸，而
        /// _zoomRatio 是 private 執行期欄位、每次進 Play 都從 1 開始。如果使用者直接在 Scene/Game
        /// 視圖手動拖曳調整過面板大小（沒有透過這個元件的欄位），_baseSizeDelta 完全不知道這件事——
        /// 玩家一進 Play 只要滾一格滾輪，就會直接用舊的 _baseSizeDelta 算出全新尺寸，把剛剛手動調
        /// 好的大小整個蓋掉，畫面瞬間跳到不一樣的尺寸（這是實測抓到的真實 bug：手動調到 289×330，
        /// 一滾滑鼠中鍵就跳回 _baseSizeDelta≈420×480 附近，不是假設性風險）。
        ///
        /// 手動調整完大小後，在 Inspector 對這個元件按右鍵選「採用目前尺寸為設計值」，把當下的
        /// sizeDelta/anchoredPosition/內容縮放存回設計值欄位，並把 _zoomRatio 重置為 1，之後滾輪
        /// 縮放就會以這個新尺寸為基準，不會再跳掉。跟 ApplyDesignValues 一樣刻意只能手動觸發。
        /// </summary>
        [ContextMenu("採用目前尺寸為設計值 (Capture Current as Design Values)")]
        private void CaptureCurrentAsDesignValues()
        {
            if (_rectTransform == null)
            {
                _rectTransform = (RectTransform)transform;
            }

            _baseSizeDelta = _rectTransform.sizeDelta;
            _anchoredPosition = _rectTransform.anchoredPosition;

            Transform contentScaleRoot = FindDeepChild(transform, "ContentScaleRoot");
            if (contentScaleRoot != null)
            {
                _baseContentScale = contentScaleRoot.localScale.x;
            }

            _zoomRatio = 1f;
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

            _rectTransform.sizeDelta = _baseSizeDelta * _zoomRatio * _globalScale;

            Transform contentScaleRoot = FindDeepChild(transform, "ContentScaleRoot");
            if (contentScaleRoot != null)
            {
                contentScaleRoot.localScale = Vector3.one * (_baseContentScale * _zoomRatio * _globalScale);
            }
        }

        /// <summary>
        /// 遞迴往下找名為 <paramref name="name"/> 的子物件，取代原本只找「直接子物件」的
        /// <c>transform.Find(name)</c>。改用遞迴的原因：套用了邊框系統（CyberpunkPanel 系列）的面板，
        /// ContentScaleRoot 會被巢狀放在邊框 Prefab 自己的 ContentContainer 底下（多一層），
        /// 直接子物件查找會找不到，導致縮放時外框跟著滾輪縮放但內容物完全沒有等比縮放，
        /// 兩者比例對不上而互相蓋到（這是實測抓到的真實 bug，不是假設性風險）。沒套邊框系統的
        /// 舊面板（武器背包、成就清單、分享卡片）ContentScaleRoot 仍是直接子物件，遞迴查找對它們
        /// 是等價的，行為不受影響。
        /// </summary>
        private static Transform FindDeepChild(Transform parent, string name)
        {
            Transform direct = parent.Find(name);
            if (direct != null)
            {
                return direct;
            }

            foreach (Transform child in parent)
            {
                Transform found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
