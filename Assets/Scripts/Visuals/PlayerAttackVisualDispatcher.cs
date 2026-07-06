using UnityEngine;
using DeskSlayer.Combat;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱 TypingEnergySystem 的輕/重攻擊觸發事件，切換角色 Animator 的 Attack 狀態，
    /// 並依「這次觸發的是哪個事件」顯示對應武器子物件。比照 AudioDispatcher/CombatDispatcher 的作法，
    /// 純粹轉發表現效果，不參與任何戰鬥數值運算。
    /// TypingEnergySystem 位於獨立的邏輯物件（GlobalKeyboardHookService）上，因此用序列化欄位參照，
    /// 而非 RequireComponent（兩者不在同一個 GameObject）。
    /// </summary>
    public sealed class PlayerAttackVisualDispatcher : MonoBehaviour
    {
        [SerializeField]
        private TypingEnergySystem _typingEnergySystem;

        [SerializeField]
        private Animator _bodyAnimator;

        [SerializeField]
        private GameObject _lightWeaponVisual;

        [SerializeField]
        private Animator _lightWeaponAnimator;

        [SerializeField]
        private GameObject _heavyWeaponVisual;

        [SerializeField]
        private Animator _heavyWeaponAnimator;

        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private const string WeaponAttackStateName = "Attack";

        private void Awake()
        {
            _lightWeaponVisual.SetActive(false);
            _heavyWeaponVisual.SetActive(false);
        }

        private void OnEnable()
        {
            _typingEnergySystem.OnLightAttackTriggered += HandleLightAttack;
            _typingEnergySystem.OnHeavyAttackTriggered += HandleHeavyAttack;
        }

        private void OnDisable()
        {
            _typingEnergySystem.OnLightAttackTriggered -= HandleLightAttack;
            _typingEnergySystem.OnHeavyAttackTriggered -= HandleHeavyAttack;
        }

        private void HandleLightAttack(LightWeaponSO weapon)
        {
            ActivateWeapon(_lightWeaponVisual, _heavyWeaponVisual, _lightWeaponAnimator);
            _bodyAnimator.SetTrigger(AttackTriggerHash);
        }

        private void HandleHeavyAttack(HeavyWeaponSO weapon)
        {
            ActivateWeapon(_heavyWeaponVisual, _lightWeaponVisual, _heavyWeaponAnimator);
            _bodyAnimator.SetTrigger(AttackTriggerHash);
        }

        private void ActivateWeapon(GameObject toShow, GameObject toHide, Animator weaponAnimator)
        {
            toHide.SetActive(false);
            toShow.SetActive(true);
            // 強制從第 0 幀重播，涵蓋「攻擊尚未播完又再次觸發」的連續打字情境。
            weaponAnimator.Play(WeaponAttackStateName, 0, 0f);
        }

        /// <summary>
        /// 由角色 Attack 動畫片段尾端的 Animation Event 呼叫，回到 Idle 狀態時隱藏兩把武器。
        /// 武器揮動片段與角色 Attack 片段幀數、播放速度相同，因此用同一個結尾事件收尾即可，
        /// 不需要另外判斷目前是哪把武器在播放。
        /// </summary>
        public void OnAttackAnimationEnd()
        {
            _lightWeaponVisual.SetActive(false);
            _heavyWeaponVisual.SetActive(false);
        }
    }
}
