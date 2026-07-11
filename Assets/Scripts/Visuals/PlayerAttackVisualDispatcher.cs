using UnityEngine;
using DeskSlayer.Combat;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱 TypingEnergySystem 的輕/重攻擊觸發事件，切換角色 Animator 的 Attack 狀態，
    /// 並依「這次觸發的是哪個事件」顯示對應武器子物件。比照 AudioDispatcher/CombatDispatcher 的作法，
    /// 純粹轉發表現效果，不參與任何戰鬥數值運算。
    /// 觸發時會讀取 WeaponDataSO.AttackAnimatorController，把對應的武器 Animator 切換成該武器變體
    /// 自己的揮擊動畫，讓玩家合成/更換的武器外觀在場上實際看得到；武器資料尚未指定 Controller 時
    /// （留空）則保留 Animator 目前已設定的內容，不強制覆蓋，避免尚未建好美術的武器直接播不出動畫。
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
            ApplyWeaponAppearance(_lightWeaponAnimator, weapon);
            ActivateWeapon(_lightWeaponVisual, _heavyWeaponVisual, _lightWeaponAnimator);
            _bodyAnimator.SetTrigger(AttackTriggerHash);
        }

        private void HandleHeavyAttack(HeavyWeaponSO weapon)
        {
            ApplyWeaponAppearance(_heavyWeaponAnimator, weapon);
            ActivateWeapon(_heavyWeaponVisual, _lightWeaponVisual, _heavyWeaponAnimator);
            _bodyAnimator.SetTrigger(AttackTriggerHash);
        }

        /// <summary>
        /// 依裝備的武器切換該武器 Animator 的 RuntimeAnimatorController，讓不同變體/稀有度顯示各自的揮擊外觀。
        /// 武器資料尚未指定 Controller 時保留原本內容，避免尚未建好美術的武器變成完全不播動畫。
        /// </summary>
        private void ApplyWeaponAppearance(Animator weaponAnimator, WeaponDataSO weapon)
        {
            RuntimeAnimatorController controller = weapon.AttackAnimatorController;
            if (controller != null && weaponAnimator.runtimeAnimatorController != controller)
            {
                weaponAnimator.runtimeAnimatorController = controller;
            }
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
