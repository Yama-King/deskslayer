using System;
using DeskSlayer.KeyboardHook;
using DeskSlayer.MouseHook;
using DeskSlayer.Settings;
using UnityEngine;

namespace DeskSlayer.AttackInput
{
    /// <summary>
    /// 彙整鍵盤字元輸入（GlobalKeyboardHookService）與滑鼠點擊（GlobalMouseHookService）兩種
    /// 攻擊輸入來源，對外重新發出統一的 OnAttackInputTriggered 事件。TypingEnergySystem、
    /// PlayStyleAnalyzer 只依賴這裡，不直接認識任何具體的 Hook 服務，新增/移除輸入來源
    /// （例如未來的手把）只需要改動這個類別，不影響下游的攻擊判定與統計邏輯（高內聚低耦合）。
    ///
    /// 滑鼠偵測開關（GameSettingsPreferenceStore.MouseAttackInputEnabled）的判定點刻意放在這裡，
    /// 而不是 GlobalMouseHookService：關閉開關時，全域滑鼠 Hook 本身必須持續正常運作（例如供
    /// 點擊穿透以外的未來功能使用），只是「滑鼠點擊是否計入攻擊」這個下游判定被關閉——這正是
    /// 彙整層的職責，不該讓 Hook 服務認識任何遊戲規則層級的開關。
    /// </summary>
    [RequireComponent(typeof(GlobalKeyboardHookService))]
    [RequireComponent(typeof(GlobalMouseHookService))]
    public sealed class AttackInputAggregator : MonoBehaviour
    {
        /// <summary>每當一次攻擊輸入（鍵盤字元或滑鼠點擊）發生時觸發，供 TypingEnergySystem、
        /// PlayStyleAnalyzer 等下游系統訂閱。</summary>
        public event Action<AttackInputData> OnAttackInputTriggered;

        private GlobalKeyboardHookService _keyboardHookService;
        private GlobalMouseHookService _mouseHookService;

        private void Awake()
        {
            _keyboardHookService = GetComponent<GlobalKeyboardHookService>();
            _mouseHookService = GetComponent<GlobalMouseHookService>();
        }

        private void OnEnable()
        {
            _keyboardHookService.OnKeyPressed += HandleKeyPressed;
            _mouseHookService.OnMouseClicked += HandleMouseClicked;
        }

        private void OnDisable()
        {
            _keyboardHookService.OnKeyPressed -= HandleKeyPressed;
            _mouseHookService.OnMouseClicked -= HandleMouseClicked;
        }

        private void HandleKeyPressed(KeyPressData data)
        {
            Dispatch(new AttackInputData(data.TimestampTicks, AttackInputSource.Keyboard));
        }

        private void HandleMouseClicked(MouseClickData data)
        {
            if (!GameSettingsPreferenceStore.MouseAttackInputEnabled)
            {
                // 開關關閉：捨棄這次事件，不轉發也不觸發攻擊/統計，但 Hook 本身完全不受影響。
                return;
            }

            Dispatch(new AttackInputData(data.TimestampTicks, AttackInputSource.Mouse));
        }

        /// <summary>
        /// 逐一呼叫訂閱者並個別隔離例外，比照 GlobalKeyboardHookService.DispatchEvent，
        /// 避免單一訂閱者拋出例外中斷整條事件分派流程。
        /// </summary>
        private void Dispatch(AttackInputData data)
        {
            if (OnAttackInputTriggered == null)
            {
                return;
            }

            foreach (Delegate handler in OnAttackInputTriggered.GetInvocationList())
            {
                try
                {
                    ((Action<AttackInputData>)handler).Invoke(data);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
