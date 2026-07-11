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

        [SerializeField, Min(0f), Tooltip("命中瞬間的頓幀（Hit-stop）持續時間，秒。輕/重武器各自設定，不共用同一數值。")]
        private float _hitStopDuration = 0.03f;

        [SerializeField, Tooltip("所屬武器家族（外觀分類），供收集/合成系統依家族分組查詢")]
        private WeaponFamily _family;

        [SerializeField, Range(1, 5), Tooltip("同家族底下的外觀變體編號（1~5）")]
        private int _variant = 1;

        [SerializeField, Tooltip("稀有度，影響掉落權重與合成消耗數量")]
        private WeaponRarity _rarity;

        [SerializeField, Tooltip("是否為永久保底武器：遊戲啟動時一定會發放，且不會因為任何操作而從背包移除，但仍可正常合成升級")]
        private bool _isPermanentStarter;

        [SerializeField, Min(1), Tooltip("合成可提升到的最大等級，達到此等級後無法再消耗重複品繼續合成")]
        private int _maxUpgradeLevel = 10;

        [SerializeField, Range(0f, 2f), Tooltip("合成加成封頂比例，例如 0.5 代表滿級時傷害最多比基礎值提升 50%")]
        private float _upgradeBonusCapRatio = 0.5f;

        /// <summary>武器顯示名稱。</summary>
        public string WeaponName => _weaponName;

        /// <summary>基礎傷害值，實際命中傷害由未來的 ICombatResolver 依此數值運算。</summary>
        public int BaseDamage => _baseDamage;

        /// <summary>武器圖示，供 UI 顯示使用。</summary>
        public Sprite Icon => _icon;

        /// <summary>命中瞬間觸發的頓幀持續時間（秒），由 HitStopController 讀取套用。</summary>
        public float HitStopDuration => _hitStopDuration;

        /// <summary>所屬武器家族，供 WeaponDatabaseSO／掉落判定依家族分組查詢使用。</summary>
        public WeaponFamily Family => _family;

        /// <summary>同家族底下的外觀變體編號（1~5）。</summary>
        public int Variant => _variant;

        /// <summary>稀有度，供掉落權重與合成消耗數量查詢使用。</summary>
        public WeaponRarity Rarity => _rarity;

        /// <summary>是否為永久保底武器，WeaponInventoryService 於遊戲啟動時會確保這類武器一定持有。</summary>
        public bool IsPermanentStarter => _isPermanentStarter;

        /// <summary>合成可提升到的最大等級。</summary>
        public int MaxUpgradeLevel => _maxUpgradeLevel;

        /// <summary>合成加成封頂比例（滿級時的傷害加成上限）。</summary>
        public float UpgradeBonusCapRatio => _upgradeBonusCapRatio;

        /// <summary>
        /// 判斷這次按鍵是否應該觸發一次攻擊。由各武器子類別定義自己的觸發規則
        /// （例如輕武器每次按鍵都觸發、重武器需累積按鍵次數達閾值才觸發），
        /// TypingEnergySystem 只需呼叫此方法即可，未來新增武器類型不需要修改 TypingEnergySystem（開放封閉原則）。
        /// </summary>
        public abstract bool TryTriggerAttack();

        /// <summary>
        /// 依合成等級計算傷害加成倍率。採遞減收益曲線（平方根），等級越接近封頂單級加成越小，
        /// 但加成比例永遠不會超過 UpgradeBonusCapRatio；0 級（尚未合成）回傳 1 倍，不加成。
        /// </summary>
        public float GetUpgradeDamageMultiplier(int upgradeLevel)
        {
            if (upgradeLevel <= 0 || _maxUpgradeLevel <= 0)
            {
                return 1f;
            }

            float progress = Mathf.Clamp01((float)upgradeLevel / _maxUpgradeLevel);
            float bonus = _upgradeBonusCapRatio * Mathf.Sqrt(progress);
            return 1f + bonus;
        }
    }
}
