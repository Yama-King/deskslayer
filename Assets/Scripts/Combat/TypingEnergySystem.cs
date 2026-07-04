using System;
using UnityEngine;
using DeskSlayer.KeyboardHook;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 訂閱 GlobalKeyboardHookService 的按鍵事件，將打字輸入轉換為輕／重攻擊的「觸發事件」。
    /// 職責僅限於按鍵計數與事件分派，不做傷害運算或敵人互動（交由下一階段的 ICombatResolver 等系統處理）。
    /// </summary>
    [RequireComponent(typeof(GlobalKeyboardHookService))]
    public sealed class TypingEnergySystem : MonoBehaviour
    {
        [SerializeField]
        private LightWeaponSO _lightWeapon;

        [SerializeField]
        private HeavyWeaponSO _heavyWeapon;

        /// <summary>每次輕攻擊觸發時發出，帶入對應的輕武器數據。</summary>
        public event Action<LightWeaponSO> OnLightAttackTriggered;

        /// <summary>累積按鍵次數達到重攻擊閾值時發出，帶入對應的重武器數據。</summary>
        public event Action<HeavyWeaponSO> OnHeavyAttackTriggered;

        private GlobalKeyboardHookService _hookService;
        private int _keyPressCount;

        private void Awake()
        {
            _hookService = GetComponent<GlobalKeyboardHookService>();
        }

        private void OnEnable()
        {
            _hookService.OnKeyPressed += HandleKeyPressed;
        }

        private void OnDisable()
        {
            _hookService.OnKeyPressed -= HandleKeyPressed;
        }

        private void HandleKeyPressed(KeyPressData data)
        {
            TriggerLightAttack();
            AccumulateHeavyAttack();
        }

        private void TriggerLightAttack()
        {
            if (_lightWeapon == null)
            {
                return;
            }

            Debug.Log($"[TypingEnergySystem] 輕攻擊觸發，武器={_lightWeapon.WeaponName}，傷害={_lightWeapon.BaseDamage}");
            OnLightAttackTriggered?.Invoke(_lightWeapon);
        }

        private void AccumulateHeavyAttack()
        {
            if (_heavyWeapon == null)
            {
                return;
            }

            _keyPressCount++;
            if (_keyPressCount < _heavyWeapon.KeyPressThreshold)
            {
                return;
            }

            Debug.Log($"[TypingEnergySystem] 重攻擊觸發，武器={_heavyWeapon.WeaponName}，傷害={_heavyWeapon.BaseDamage}");
            OnHeavyAttackTriggered?.Invoke(_heavyWeapon);
            _keyPressCount = 0;
        }
    }
}
