using DeskSlayer.Persistence;
using UnityEngine;

namespace DeskSlayer.Settings
{
    /// <summary>
    /// 玩家可調整設定（滑鼠偵測開關、主音量）的輕量存取外觀，比照 Weather.WeatherCityPreferenceStore：
    /// 對外只曝露屬性 get/set，內部讀寫 SaveLifecycleController 共用的 SaveData.gameSettings 欄位，
    /// set 時立即呼叫 RequestSave() 落盤，不等待其他存檔時機。
    /// </summary>
    public static class GameSettingsPreferenceStore
    {
        /// <summary>是否將滑鼠點擊視為攻擊輸入。關閉時全域滑鼠 Hook 仍正常運作，只是 AttackInputAggregator
        /// 不會把滑鼠事件轉發為攻擊觸發（見該類別註解），這是下游消費開關，不是 Hook 生命週期開關。</summary>
        public static bool MouseAttackInputEnabled
        {
            get => SaveLifecycleController.CurrentSaveData.gameSettings.mouseAttackInputEnabled;
            set
            {
                SaveLifecycleController.CurrentSaveData.gameSettings.mouseAttackInputEnabled = value;
                SaveLifecycleController.RequestSave();
            }
        }

        /// <summary>主音量（0~1），對應 AudioManager.SetMasterVolume 實際套用的係數。</summary>
        public static float MasterVolume
        {
            get => SaveLifecycleController.CurrentSaveData.gameSettings.masterVolume;
            set
            {
                float clamped = Mathf.Clamp01(value);
                SaveLifecycleController.CurrentSaveData.gameSettings.masterVolume = clamped;
                SaveLifecycleController.RequestSave();
            }
        }
    }
}
