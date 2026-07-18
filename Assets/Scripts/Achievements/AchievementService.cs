using System;
using System.Collections.Generic;
using UnityEngine;
using DeskSlayer.Combat;
using DeskSlayer.KeyboardHook;
using DeskSlayer.Persistence;
using DeskSlayer.Weather;

namespace DeskSlayer.Achievements
{
    /// <summary>
    /// 成就系統的協調者：單向訂閱敵人／武器／天氣／打字能量系統既有的公開事件，判定各類成就
    /// 是否解鎖，解鎖時發出 OnAchievementUnlocked 供 Toast 與成就選單訂閱。這五個被訂閱的系統
    /// 完全不需要認識成就系統的存在，也不需要修改任何一行既有程式碼（比照 CombatDispatcher／
    /// WeaponDropDispatcher 的 Mediator 模式）。
    ///
    /// 存檔串接：直接讀寫 SaveLifecycleController.CurrentSaveData.achievements 這個共用實例的欄位，
    /// 不另外維護一份會跟存檔內容脫節的記憶體快取——已解鎖清單、累積數值、已見天氣集合全部
    /// 以存檔物件本身為唯一真相來源，讀寫永遠一致，落盤時機沿用既有的 OnApplicationQuit。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AchievementService : MonoBehaviour
    {
        [SerializeField]
        private AchievementDatabaseSO _database;

        [SerializeField]
        private GlobalKeyboardHookService _keyboardHookService;

        [SerializeField]
        private TypingEnergySystem _typingEnergySystem;

        [SerializeField]
        private WeatherService _weatherService;

        [SerializeField]
        private WeaponDropDispatcher _weaponDropDispatcher;

        [SerializeField, Tooltip("初戰告捷：WeaponDropDispatcher 第一次發出掉落結果時解鎖（代表恰好發生過一次擊殺）")]
        private SimpleAchievementSO _firstKillAchievement;

        [SerializeField, Tooltip("軍火商：WeaponInventoryService 第一次發出「取得新武器」時解鎖（不含遊戲啟動時的保底武器發放）")]
        private SimpleAchievementSO _firstWeaponAchievement;

        [SerializeField, Tooltip("風雨無阻：五種天氣分類全部體驗過一次後解鎖")]
        private WeatherSetAchievementSO _weatherSetAchievement;

        /// <summary>成就解鎖時發出，供 Toast 與成就選單訂閱。</summary>
        public event Action<AchievementDefinitionSO> OnAchievementUnlocked;

        /// <summary>查詢指定成就 Id 是否已解鎖。</summary>
        public bool IsUnlocked(string id)
        {
            return !string.IsNullOrEmpty(id)
                && SaveLifecycleController.CurrentSaveData.achievements.unlockedAchievementIds.Contains(id);
        }

        /// <summary>目前已解鎖的成就 Id 清單，供成就選單查詢使用。</summary>
        public IReadOnlyList<string> UnlockedIds => SaveLifecycleController.CurrentSaveData.achievements.unlockedAchievementIds;

        private void OnEnable()
        {
            SubscribeToSerializedFieldEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromSerializedFieldEvents();

            if (WeaponInventoryService.Instance != null)
            {
                WeaponInventoryService.Instance.OnWeaponObtained -= HandleWeaponObtained;
            }
        }

        /// <summary>
        /// WeaponInventoryService.Instance 是靜態單例、於其自身 Awake() 賦值，跨物件的 Awake 執行順序
        /// 不保證，若在 OnEnable() 訂閱可能拿到尚未賦值的 null。更重要的是，WeaponInventoryService.Awake()
        /// 內的保底武器發放（GrantPermanentStarterWeapons）會在遊戲啟動當下就發出 OnWeaponObtained——
        /// 只在 Start() 訂閱，確保拿到的一定是玩家之後實際打怪掉落／碎片兌換取得的武器，
        /// 不會讓「軍火商」在玩家還沒做任何事之前就被保底武器誤觸發解鎖。
        /// </summary>
        private void Start()
        {
            if (WeaponInventoryService.Instance != null)
            {
                WeaponInventoryService.Instance.OnWeaponObtained -= HandleWeaponObtained;
                WeaponInventoryService.Instance.OnWeaponObtained += HandleWeaponObtained;
            }

            // 存檔中已累積的數值可能因為新增門檻階段資產而已經跨過門檻，遊戲啟動時先補跑一次判定，
            // 不需要等到下一次事件才觸發解鎖。
            AchievementSaveData saveData = SaveLifecycleController.CurrentSaveData.achievements;
            CheckThresholds(AchievementValueSource.TypedCharacterCount, saveData.typedCharacterCount);
            CheckThresholds(AchievementValueSource.AttackTriggerCount, saveData.attackTriggerCount);
            CheckWeatherSetCompletion(saveData.seenWeatherCategories);
        }

        private void SubscribeToSerializedFieldEvents()
        {
            if (_keyboardHookService != null)
            {
                _keyboardHookService.OnKeyPressed -= HandleKeyPressed;
                _keyboardHookService.OnKeyPressed += HandleKeyPressed;
            }

            if (_typingEnergySystem != null)
            {
                _typingEnergySystem.OnLightAttackTriggered -= HandleLightAttackTriggered;
                _typingEnergySystem.OnLightAttackTriggered += HandleLightAttackTriggered;
                _typingEnergySystem.OnHeavyAttackTriggered -= HandleHeavyAttackTriggered;
                _typingEnergySystem.OnHeavyAttackTriggered += HandleHeavyAttackTriggered;
            }

            if (_weatherService != null)
            {
                _weatherService.OnWeatherChanged -= HandleWeatherChanged;
                _weatherService.OnWeatherChanged += HandleWeatherChanged;
            }

            if (_weaponDropDispatcher != null)
            {
                _weaponDropDispatcher.OnWeaponDropped -= HandleWeaponDropped;
                _weaponDropDispatcher.OnWeaponDropped += HandleWeaponDropped;
            }
        }

        private void UnsubscribeFromSerializedFieldEvents()
        {
            if (_keyboardHookService != null)
            {
                _keyboardHookService.OnKeyPressed -= HandleKeyPressed;
            }

            if (_typingEnergySystem != null)
            {
                _typingEnergySystem.OnLightAttackTriggered -= HandleLightAttackTriggered;
                _typingEnergySystem.OnHeavyAttackTriggered -= HandleHeavyAttackTriggered;
            }

            if (_weatherService != null)
            {
                _weatherService.OnWeatherChanged -= HandleWeatherChanged;
            }

            if (_weaponDropDispatcher != null)
            {
                _weaponDropDispatcher.OnWeaponDropped -= HandleWeaponDropped;
            }
        }

        /// <summary>
        /// 隱性依賴 WeaponDropConfigSO 目前為 100% 掉落率（DefaultWeaponDropResolver 只有在該家族
        /// 完全沒有建置任何武器資產時才會回傳 null、不觸發這個事件，不是機率制的「這次沒掉東西」）。
        /// 若未來把掉落改成真正的機率制（例如某個稀有度或家族可能「這次沒掉落」），這個成就的觸發
        /// 語意會偏離「首次擊殺」的原始定義（擊殺了但沒掉落，就不會解鎖），屆時需要重新評估是否
        /// 改回直接訂閱 EnemyController.OnDeath（會需要在 EnemyRotationManager 新增一行退目標呼叫）。
        /// </summary>
        private void HandleWeaponDropped(WeaponDataSO weapon, Vector3 dropPosition, WeaponDropOutcome outcome)
        {
            Unlock(_firstKillAchievement);
        }

        private void HandleWeaponObtained(WeaponInstance instance)
        {
            Unlock(_firstWeaponAchievement);
        }

        private void HandleKeyPressed(KeyPressData data)
        {
            AchievementSaveData saveData = SaveLifecycleController.CurrentSaveData.achievements;
            saveData.typedCharacterCount++;
            CheckThresholds(AchievementValueSource.TypedCharacterCount, saveData.typedCharacterCount);
        }

        private void HandleLightAttackTriggered(LightWeaponSO weapon)
        {
            HandleAttackTriggered();
        }

        private void HandleHeavyAttackTriggered(HeavyWeaponSO weapon)
        {
            HandleAttackTriggered();
        }

        private void HandleAttackTriggered()
        {
            AchievementSaveData saveData = SaveLifecycleController.CurrentSaveData.achievements;
            saveData.attackTriggerCount++;
            CheckThresholds(AchievementValueSource.AttackTriggerCount, saveData.attackTriggerCount);
        }

        private void HandleWeatherChanged(WeatherModifierData data)
        {
            List<WeatherCategory> seenCategories = SaveLifecycleController.CurrentSaveData.achievements.seenWeatherCategories;
            if (!seenCategories.Contains(data.Category))
            {
                seenCategories.Add(data.Category);
            }

            CheckWeatherSetCompletion(seenCategories);
        }

        /// <summary>
        /// 遍歷資料庫中所有 ThresholdAchievementSO，找出符合來源且累積值已跨過門檻、尚未解鎖的項目逐一解鎖。
        /// 同一次呼叫可能同時跨過多個門檻階段（例如讀檔還原後一次補上好幾個階段），彼此獨立判定。
        /// </summary>
        private void CheckThresholds(AchievementValueSource source, long currentValue)
        {
            if (_database == null || _database.AllAchievements == null)
            {
                return;
            }

            foreach (AchievementDefinitionSO definition in _database.AllAchievements)
            {
                if (definition is ThresholdAchievementSO threshold
                    && threshold.SourceType == source
                    && currentValue >= threshold.Threshold)
                {
                    Unlock(threshold);
                }
            }
        }

        private void CheckWeatherSetCompletion(List<WeatherCategory> seenCategories)
        {
            if (_weatherSetAchievement != null
                && seenCategories.Count >= Enum.GetValues(typeof(WeatherCategory)).Length)
            {
                Unlock(_weatherSetAchievement);
            }
        }

        /// <summary>
        /// 解鎖指定成就：已解鎖或資產未指派時安全地不做任何事，成功解鎖時寫入存檔清單並發出事件。
        /// 各成就的判定各自獨立呼叫這個方法，單一項目的判定邏輯出錯不會影響其他項目。
        /// </summary>
        private void Unlock(AchievementDefinitionSO definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id) || IsUnlocked(definition.Id))
            {
                return;
            }

            SaveLifecycleController.CurrentSaveData.achievements.unlockedAchievementIds.Add(definition.Id);
            Debug.Log($"[AchievementService] 成就解鎖：{definition.DisplayName}（{definition.Id}）");
            OnAchievementUnlocked?.Invoke(definition);
        }
    }
}
