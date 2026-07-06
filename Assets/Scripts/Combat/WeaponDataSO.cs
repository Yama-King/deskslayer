using UnityEngine;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 武器數據的抽象基底類別，定義所有武器共通的欄位。
    /// 依 ScriptableObject 架構將數值與邏輯分離，方便日後調整平衡數值時不需重新編譯程式碼。
    /// </summary>
    public abstract class WeaponDataSO : ScriptableObject
    {
        [SerializeField]
        private string _weaponName;

        [SerializeField]
        private int _baseDamage;

        [SerializeField]
        private Sprite _icon;

        /// <summary>武器顯示名稱。</summary>
        public string WeaponName => _weaponName;

        /// <summary>基礎傷害值，實際命中傷害由未來的 ICombatResolver 依此數值運算。</summary>
        public int BaseDamage => _baseDamage;

        /// <summary>武器圖示，供 UI 顯示使用。</summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// 判斷這次按鍵是否應該觸發一次攻擊。由各武器子類別定義自己的觸發規則
        /// （例如輕武器每次按鍵都觸發、重武器需累積按鍵次數達閾值才觸發），
        /// TypingEnergySystem 只需呼叫此方法即可，未來新增武器類型不需要修改 TypingEnergySystem（開放封閉原則）。
        /// </summary>
        public abstract bool TryTriggerAttack();
    }
}
