using DG.Tweening;
using DeskSlayer.GameState;
using UnityEngine;

namespace DeskSlayer.Juice
{
    /// <summary>
    /// 命中頓幀（Hit-stop）控制器：命中瞬間將 Time.timeScale 短暫降低，製造打擊的重量感。
    /// 獨立於 Combat／Visuals 命名空間之外（單向被兩者依賴），避免 CombatDispatcher 觸發頓幀
    /// 卻又反過來被表現層參照造成循環相依。
    /// 回復計時使用 DOVirtual.DelayedCall 搭配 ignoreTimeScale，不受自己造成的 timeScale 影響，
    /// 確保頓幀時間到了一定能準時恢復，不會卡死遊戲。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitStopController : MonoBehaviour
    {
        private const string RestoreTweenId = "HitStopRestore";

        [SerializeField, Range(0.01f, 1f), Tooltip("頓幀期間 Time.timeScale 降到的目標值，刻意不設為 0，避免部分依賴 timeScale 的系統完全停滯")]
        private float _hitStopTimeScale = 0.05f;

        public static HitStopController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            SubscribeToGameStateMachine();
        }

        private void Start()
        {
            // Unity 只保證所有物件的 Awake 先於任何物件的 Start，不保證 OnEnable 的跨物件順序，
            // 這裡補一次訂閱，確保不論 GameStateMachine 的 Awake 相對順序為何都能訂閱成功。
            SubscribeToGameStateMachine();
        }

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
            {
                GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            }
        }

        private void SubscribeToGameStateMachine()
        {
            if (GameStateMachine.Instance == null)
            {
                return;
            }

            GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            GameStateMachine.Instance.OnGamePhaseChanged += HandleGamePhaseChanged;
        }

        /// <summary>
        /// 暫停時只取消尚未觸發的即時回復回呼（DOVirtual.DelayedCall 用 ignoreTimeScale: true，
        /// 暫停中仍會照跑），刻意不呼叫 RestoreTimeScale()——Time.timeScale 的最終值一律交由
        /// GameStateMachine 決定，避免頓幀回復把暫停中的 timeScale 悄悄改回 1。
        /// </summary>
        private void HandleGamePhaseChanged(GamePhase? previous, GamePhase current)
        {
            if (current == GamePhase.Paused)
            {
                DOTween.Kill(RestoreTweenId);
            }
        }

        /// <summary>
        /// 觸發一次頓幀，維持時間讀取呼叫端傳入的 duration（例如觸發本次攻擊的武器 HitStopDuration）。
        /// 若上一次頓幀的回復計時尚未執行，直接取消並以本次 duration 重新計時，讓連續快速命中
        /// （例如高速打字連續觸發輕攻擊）的頓幀效果彼此延續，不會被提早恢復打斷。
        /// </summary>
        public void Trigger(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            DOTween.Kill(RestoreTweenId);
            Time.timeScale = _hitStopTimeScale;
            DOVirtual.DelayedCall(duration, RestoreTimeScale, ignoreTimeScale: true)
                .SetId(RestoreTweenId);
        }

        private static void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }
    }
}
