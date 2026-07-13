using UnityEngine;
using DeskSlayer.Weather;

namespace DeskSlayer.Combat
{
    /// <summary>
    /// 彙整「基礎數值」與「目前天氣修正」成最終攻擊力/防禦力，供 CombatDispatcher 呼叫
    /// ICombatResolver 之前使用。只負責數值彙整，不管理天氣狀態也不參與戰鬥判定邏輯，
    /// 讓 ICombatResolver 完全不需要知道天氣系統的存在（開放封閉原則：新數值來源用疊加的
    /// 方式加入，不需要修改既有的判定介面）。
    /// 訂閱 WeatherService.OnWeatherChanged 快取目前修正百分比，不逐影格查詢。
    /// </summary>
    public sealed class PlayerCombatStatsProvider
    {
        private readonly WeatherService _weatherService;
        private float _attackModifierPercent;
        private float _defenseModifierPercent;

        public PlayerCombatStatsProvider(WeatherService weatherService)
        {
            _weatherService = weatherService;

            if (_weatherService == null)
            {
                return;
            }

            ApplyModifier(_weatherService.CurrentModifier);
            _weatherService.OnWeatherChanged += ApplyModifier;
        }

        /// <summary>取消訂閱天氣事件，供持有者在銷毀時呼叫，避免事件殘留參考。</summary>
        public void Dispose()
        {
            if (_weatherService != null)
            {
                _weatherService.OnWeatherChanged -= ApplyModifier;
            }
        }

        /// <summary>套用目前天氣攻擊力修正後的最終攻擊力，尚未收到任何天氣資料時視為無修正。</summary>
        public int GetFinalAttackPower(int baseAttackPower)
        {
            return Mathf.RoundToInt(baseAttackPower * (1f + _attackModifierPercent / 100f));
        }

        /// <summary>
        /// 套用目前天氣防禦力修正後的最終防禦力。目前玩家端尚無防禦力數值可套用，
        /// 保留此方法讓未來玩家防禦力上線時可直接重用，不需要重新設計彙整邏輯。
        /// </summary>
        public int GetFinalDefensePower(int baseDefensePower)
        {
            return Mathf.RoundToInt(baseDefensePower * (1f + _defenseModifierPercent / 100f));
        }

        private void ApplyModifier(WeatherModifierData modifier)
        {
            _attackModifierPercent = modifier.AttackModifierPercent;
            _defenseModifierPercent = modifier.DefenseModifierPercent;
        }
    }
}
