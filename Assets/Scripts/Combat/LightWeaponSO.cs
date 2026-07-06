using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 輕武器數據。W1 階段每次有效按鍵都會觸發一次輕攻擊，
    /// _triggerOnEveryKeyPress 欄位為 W2 時間窗口式蓄力（依打字節奏調整觸發頻率）預留的擴充點。
    /// </summary>
    [CreateAssetMenu(fileName = "NewLightWeapon", menuName = "DeskSlayer/Combat/Light Weapon", order = 0)]
    public sealed class LightWeaponSO : WeaponDataSO
    {
        [SerializeField, Tooltip("是否每次按鍵都觸發輕攻擊，W1 階段固定為 true")]
        private bool _triggerOnEveryKeyPress = true;

        /// <summary>是否每次按鍵都觸發輕攻擊。</summary>
        public bool TriggerOnEveryKeyPress => _triggerOnEveryKeyPress;

        /// <summary>輕武器的觸發規則：每次呼叫都立即回傳 true。</summary>
        public override bool TryTriggerAttack() => true;
    }
}
