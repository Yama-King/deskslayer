using System;
using UnityEngine;

namespace DeskSlayer.DesktopWindow
{
    /// <summary>
    /// 「移動／縮放 GameWorldRoot」這兩個動作唯一的權責入口。不管是抓到世界成員（Collider2D）還是抓到
    /// 常駐錨定按鈕發起拖曳/滾輪縮放，輸入路徑最終都只呼叫這裡的 ApplyScreenDelta／ApplyWorldScaleDelta，
    /// 本身不判斷來源、不重複實作位移/縮放邏輯——「抓世界成員或抓錨定按鈕最終效果一致」是這個共用
    /// 入口的自然結果。GameWorldRoot 本身不需要額外包一層縮放用的 wrapper：這裡改的就是它自己的
    /// localScale，沒有其他系統（例如 DOTween 動畫）在跟這個欄位搶，跟兩個 UI 浮動面板需要另外包一層
    /// ContentScaleRoot 是不同狀況（面板的 PanelRoot.localScale 已經被開關動畫佔用，見 FloatingPanelSize
    /// 與 ContentScaleRoot 的既有設計）。
    ///
    /// 螢幕像素差換算世界位移量時，每次呼叫都即時用 Camera.ScreenToWorldPoint 重新投影，不快取任何
    /// 「像素↔世界單位」的固定比例，因此 GameWorldRoot 之後縮放不會讓這裡的換算跟著跑掉——本來就
    /// 沒有假設過縮放前後這個比例不變。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameWorldDragCoordinator : MonoBehaviour
    {
        [SerializeField, Tooltip("拖曳世界成員／錨定按鈕時實際位移的根節點")]
        private Transform _gameWorldRoot;

        [SerializeField, Tooltip("GameWorldRoot 與常駐錨定按鈕共用的初始縮放設定")]
        private DesktopWorldPresentationSettingsSO _presentationSettings;

        [Header("滑鼠滾輪縮放")]
        [SerializeField, Tooltip("滑鼠停在世界成員／錨定按鈕上滾動滾輪時，每一格滾動改變的比例")]
        private float _scrollStep = 0.05f;

        [SerializeField, Tooltip("滾輪縮放的下限")]
        private float _minWorldScale = 0.1f;

        [SerializeField, Tooltip("滾輪縮放的上限")]
        private float _maxWorldScale = 1.5f;

        private Camera _camera;
        private float _currentWorldScale = 1f;

        /// <summary>
        /// GameWorldRoot 每次因拖曳而位移時廣播，帶出的是原始螢幕像素差（不是世界位移量）——
        /// 訂閱者（錨定按鈕）需要的是螢幕像素量去換算自己的 anchoredPosition，不是世界單位量。
        /// </summary>
        public event Action<Vector2> OnGameWorldRootMoved;

        /// <summary>GameWorldRoot 每次因滾輪縮放而改變時廣播，帶出縮放後的絕對倍率（不是差量）。</summary>
        public event Action<float> OnWorldScaleChanged;

        private void Start()
        {
            _camera = Camera.main;
            _currentWorldScale = 1f;

            if (_gameWorldRoot != null && _presentationSettings != null)
            {
                ApplyWorldScale(_presentationSettings.WorldScale);
            }
        }

        /// <summary>
        /// 滑鼠滾輪縮放的入口：世界成員的滾輪偵測（DesktopWorldScrollZoomController）與錨定按鈕的
        /// 滾輪偵測（AnchoredUIDragSource）都呼叫這裡，不各自維護縮放邏輯。scrollY 只取正負號決定
        /// 放大或縮小方向，一次呼叫視為一格滾動，改變幅度固定為 _scrollStep，不受滾輪原始數值大小
        /// 影響（不同平台/滑鼠每格滾動回報的原始數值差異很大，取正負號才能有一致的縮放手感）。
        /// </summary>
        public void ApplyWorldScaleDelta(float scrollY)
        {
            float direction = Mathf.Sign(scrollY);
            if (direction == 0f)
            {
                return;
            }

            float factor = 1f + direction * _scrollStep;
            float newScale = Mathf.Clamp(_currentWorldScale * factor, _minWorldScale, _maxWorldScale);
            ApplyWorldScale(newScale);
        }

        /// <summary>
        /// 縮放 GameWorldRoot 時繞著攝影機視野中心縮放，而不是繞著 GameWorldRoot 自己的 Transform
        /// 位置縮放——GameWorldRoot 這個空物件本身的位置只是既有的階層整理慣例留下的任意值，不代表
        /// 內容實際的視覺中心（子物件用很大的負向 local 偏移量抵消回攝影機視野，只有在 scale=1 時才會
        /// 剛好抵消）。如果直接改 localScale 不補償 position，縮放的樞紐點會是這個離視覺中心很遠的
        /// 任意位置，子物件會被甩出攝影機視野——這是實測 Play Mode 時抓到的真實 bug，不是假設性風險。
        ///
        /// 用「新舊縮放比例」而不是絕對縮放值去換算位移量，讓這個方法可以從任意目前縮放狀態安全地
        /// 呼叫任意次（滾輪縮放需要反覆呼叫），不是只能從 scale=1 的起始狀態呼叫一次。
        /// </summary>
        private void ApplyWorldScale(float newScale)
        {
            float oldScale = _currentWorldScale > 0f ? _currentWorldScale : 1f;
            float ratio = newScale / oldScale;

            Vector3 pivot = _camera != null
                ? new Vector3(_camera.transform.position.x, _camera.transform.position.y, _gameWorldRoot.position.z)
                : Vector3.zero;

            _gameWorldRoot.position = pivot + (_gameWorldRoot.position - pivot) * ratio;
            _gameWorldRoot.localScale = Vector3.one * newScale;
            _currentWorldScale = newScale;

            OnWorldScaleChanged?.Invoke(newScale);
        }

        /// <summary>
        /// 把螢幕像素差換算成世界位移量套用到 GameWorldRoot，並把原始螢幕像素差廣播出去。
        /// 世界物件拖曳（DesktopWorldDragInputController）與錨定按鈕拖曳（AnchoredUIDragSource）
        /// 都呼叫這個方法，不各自維護位移邏輯。
        /// </summary>
        public void ApplyScreenDelta(Vector2 screenPixelDelta)
        {
            if (_gameWorldRoot == null || _camera == null)
            {
                return;
            }

            float depth = -_camera.transform.position.z;
            Vector3 worldFrom = _camera.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 worldTo = _camera.ScreenToWorldPoint(new Vector3(screenPixelDelta.x, screenPixelDelta.y, depth));
            _gameWorldRoot.position += worldTo - worldFrom;

            OnGameWorldRootMoved?.Invoke(screenPixelDelta);
        }
    }
}
