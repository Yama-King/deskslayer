using System;
using UnityEngine;
using DeskSlayer.AttackInput;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 訂閱 AttackInputAggregator 彙整後的攻擊輸入事件（鍵盤字元輸入與滑鼠點擊完全對等），
    /// 向 WeaponSwitcher 查詢目前裝備的武器，呼叫該武器的 TryTriggerAttack() 決定是否觸發攻擊。
    /// 職責僅限於輸入轉發與事件分派，不做傷害運算或敵人互動（交由 CombatDispatcher 等下游模組處理），
    /// 也不認識任何具體武器的觸發規則，更不認識輸入實際來自鍵盤還是滑鼠。
    /// </summary>
    [RequireComponent(typeof(AttackInputAggregator))]
    [RequireComponent(typeof(WeaponSwitcher))]
    public sealed class TypingEnergySystem : MonoBehaviour
    {
        /// <summary>裝備輕武器時觸發攻擊，帶入對應的輕武器數據。</summary>
        public event Action<LightWeaponSO> OnLightAttackTriggered;

        /// <summary>裝備重武器且累積按鍵次數達到閾值時觸發攻擊，帶入對應的重武器數據。</summary>
        public event Action<HeavyWeaponSO> OnHeavyAttackTriggered;

        private AttackInputAggregator _attackInputAggregator;
        private WeaponSwitcher _weaponSwitcher;

        private void Awake()
        {
            _attackInputAggregator = GetComponent<AttackInputAggregator>();
            _weaponSwitcher = GetComponent<WeaponSwitcher>();
        }

        private void OnEnable()
        {
            _attackInputAggregator.OnAttackInputTriggered += HandleAttackInput;
        }

        private void OnDisable()
        {
            _attackInputAggregator.OnAttackInputTriggered -= HandleAttackInput;
        }

        private void HandleAttackInput(AttackInputData data)
        {
            WeaponDataSO weapon = _weaponSwitcher.CurrentWeapon;
            if (weapon == null || !weapon.TryTriggerAttack())
            {
                return;
            }

            NotifyAttackTriggered(weapon);
        }

        /// <summary>
        /// 依武器實際型別分派對應的攻擊事件。輕/重攻擊是目前戰鬥系統既有的兩種「攻擊類別」，
        /// CombatDispatcher、AudioDispatcher、PlayStyleAnalyzer 都已經依這兩個類別設計；
        /// 之後若新增全新的攻擊類別（而非只是新增武器），才需要在這裡新增對應的分支與事件。
        /// </summary>
        private void NotifyAttackTriggered(WeaponDataSO weapon)
        {
            switch (weapon)
            {
                case LightWeaponSO lightWeapon:
                    Debug.Log($"[TypingEnergySystem] 輕攻擊觸發，武器={lightWeapon.WeaponName}，傷害={lightWeapon.BaseDamage}");
                    OnLightAttackTriggered?.Invoke(lightWeapon);
                    break;

                case HeavyWeaponSO heavyWeapon:
                    Debug.Log($"[TypingEnergySystem] 重攻擊觸發，武器={heavyWeapon.WeaponName}，傷害={heavyWeapon.BaseDamage}");
                    OnHeavyAttackTriggered?.Invoke(heavyWeapon);
                    break;

                default:
                    Debug.LogWarning($"[TypingEnergySystem] 未知的武器型別 {weapon.GetType().Name}，無法分派攻擊事件");
                    break;
            }
        }
    }
}
