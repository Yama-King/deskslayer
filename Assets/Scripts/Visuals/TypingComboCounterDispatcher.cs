using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱同一個 GameObject 上 TypingComboTracker 的計數事件，
    /// 轉發給角色頭頂計數器 UI 的 TypingComboCounterView。比照 EnemyHealthBarDispatcher 的作法，
    /// 純粹轉發資料，不參與任何計數／計時邏輯運算。
    /// </summary>
    [RequireComponent(typeof(TypingComboTracker))]
    public sealed class TypingComboCounterDispatcher : MonoBehaviour
    {
        [SerializeField]
        private TypingComboCounterView _counterView;

        private TypingComboTracker _tracker;

        private void Awake()
        {
            _tracker = GetComponent<TypingComboTracker>();
        }

        private void OnEnable()
        {
            _tracker.OnComboIncremented += HandleComboIncremented;
            _tracker.OnComboExpired += HandleComboExpired;
        }

        private void OnDisable()
        {
            _tracker.OnComboIncremented -= HandleComboIncremented;
            _tracker.OnComboExpired -= HandleComboExpired;
        }

        private void HandleComboIncremented(int count)
        {
            if (_counterView == null)
            {
                return;
            }

            _counterView.ShowCount(count);
        }

        private void HandleComboExpired()
        {
            if (_counterView == null)
            {
                return;
            }

            _counterView.Hide();
        }
    }
}
