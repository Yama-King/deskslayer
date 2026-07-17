using UnityEngine;
using DeskSlayer.KeyboardHook;
using DeskSlayer.DesktopWindow;

namespace DeskSlayer.Performance
{
    /// <summary>
    /// 依「視窗焦點」與「使用者是否仍在互動」套用 Application.targetFrameRate 的唯一權責入口。
    /// 背景常駐是桌面陪伴軟體的生存要求，CPU Profiler 實測顯示專案先前完全沒有鎖定幀率，
    /// 導致所有逐幀系統（腳本 Update／渲染／UI Raycast）都以螢幕更新率被重複放大執行，
    /// 是目前量到的最大單一效能槓桿點。
    ///
    /// 刻意不用「視窗一失焦就降頻」這種簡單規則：本專案的核心使用情境就是玩家在其他視窗
    /// 打字時，DeskSlayer 仍在背景無焦點接收全域鍵盤事件觸發戰鬥（見 GlobalKeyboardHookService），
    /// 若失焦當下立刻降頻，會讓背景打字戰鬥的畫面回饋變頓。因此改成「失焦後，等最後一次
    /// 互動經過 IdleThresholdSeconds 都沒有新互動才降頻」，互動訊號來自既有的
    /// GlobalKeyboardHookService.OnKeyPressed（打字）與 GameWorldDragCoordinator 的拖曳/縮放事件
    /// （兩者都已經是事件驅動，這裡純粹訂閱，不新增任何每幀輪詢）。視窗有焦點時一律維持
    /// ActiveTargetFrameRate，不對閒置滑鼠停留做降頻判斷，避免使用者盯著視窗時感覺卡頓。
    ///
    /// 降頻只影響渲染與 Update() 呼叫頻率，不影響任何戰鬥判定/存讀檔等邏輯正確性——
    /// 全域鍵盤事件本身在背景執行緒累積於佇列，GlobalKeyboardHookService.Update() 在降頻期間
    /// 只是被呼叫得比較不頻繁，佇列不會遺失事件，只是延遲攤還（見 _maxEventsPerFrame 設計）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AdaptiveFrameRateController : MonoBehaviour
    {
        [SerializeField]
        private FrameRatePolicySO _policy;

        [SerializeField, Tooltip("打字觸發降頻重置的訊號來源，缺少時仍可運作，只是失去打字互動訊號")]
        private GlobalKeyboardHookService _keyboardHookService;

        [SerializeField, Tooltip("拖曳/縮放觸發降頻重置的訊號來源，缺少時仍可運作，只是失去拖曳/縮放互動訊號")]
        private GameWorldDragCoordinator _dragCoordinator;

        private void Awake()
        {
            // Application.targetFrameRate 唯有 vSyncCount=0 時才會真正生效，這裡明確設一次，
            // 不假設 Quality Settings 資產本身的預設值不會被之後的設定異動覆蓋。
            QualitySettings.vSyncCount = 0;
        }

        private void OnEnable()
        {
            Application.focusChanged += HandleFocusChanged;

            if (_keyboardHookService != null)
            {
                _keyboardHookService.OnKeyPressed += HandleActivitySignal;
            }

            if (_dragCoordinator != null)
            {
                _dragCoordinator.OnGameWorldRootMoved += HandleActivitySignal;
                _dragCoordinator.OnWorldScaleChanged += HandleActivitySignal;
            }

            ApplyForCurrentState();
        }

        private void OnDisable()
        {
            Application.focusChanged -= HandleFocusChanged;

            if (_keyboardHookService != null)
            {
                _keyboardHookService.OnKeyPressed -= HandleActivitySignal;
            }

            if (_dragCoordinator != null)
            {
                _dragCoordinator.OnGameWorldRootMoved -= HandleActivitySignal;
                _dragCoordinator.OnWorldScaleChanged -= HandleActivitySignal;
            }

            CancelInvoke(nameof(ApplyIdleFrameRate));
        }

        private void HandleFocusChanged(bool hasFocus)
        {
            ApplyForCurrentState();
        }

        // 訂閱多種簽章不同的活動事件（KeyPressData／Vector2／float），內容本身不重要，
        // 只當作「有互動發生」的訊號，因此三個 Handle 多載都導向同一個處理方法。
        private void HandleActivitySignal(KeyPressData _) => HandleActivitySignal();
        private void HandleActivitySignal(Vector2 _) => HandleActivitySignal();
        private void HandleActivitySignal(float _) => HandleActivitySignal();

        private void HandleActivitySignal()
        {
            ApplyForCurrentState();
        }

        private void ApplyForCurrentState()
        {
            if (_policy == null)
            {
                Debug.LogWarning("[AdaptiveFrameRateController] 尚未指派 FrameRatePolicySO，維持引擎預設幀率設定", this);
                return;
            }

            Application.targetFrameRate = _policy.ActiveTargetFrameRate;
            CancelInvoke(nameof(ApplyIdleFrameRate));

            if (!Application.isFocused)
            {
                Invoke(nameof(ApplyIdleFrameRate), _policy.IdleThresholdSeconds);
            }
        }

        private void ApplyIdleFrameRate()
        {
            if (_policy == null || Application.isFocused)
            {
                return;
            }

            Application.targetFrameRate = _policy.IdleTargetFrameRate;
        }
    }
}
