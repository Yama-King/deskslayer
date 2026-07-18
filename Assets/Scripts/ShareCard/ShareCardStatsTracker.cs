using System;
using System.Globalization;
using UnityEngine;
using DeskSlayer.Combat;
using DeskSlayer.KeyboardHook;
using DeskSlayer.Persistence;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡系統專用的當日／累積打字量與擊殺數追蹤器：單向訂閱 GlobalKeyboardHookService.OnKeyPressed
    /// 與 WeaponDropDispatcher.OnWeaponDropped 這兩個既有公開事件，兩個系統完全不需要認識這個追蹤器的存在。
    /// 與 AchievementService 使用相同的事件來源，但各自維護獨立的計數變數與存檔欄位
    /// （SaveData.shareCard，非 SaveData.achievements），彼此不參照、不共用。
    ///
    /// 擊殺數目前借用 WeaponDropDispatcher.OnWeaponDropped 作為「一次擊殺」的代理訊號，跟成就系統的
    /// 「初戰告捷」做法一致，同樣隱性依賴 WeaponDropConfigSO 目前 100% 掉落率的假設（見該檔案的風險註解）。
    ///
    /// 跨日判定採惰性機制：不使用常駐計時器輪詢日期，只在事件發生當下、以及外部呼叫
    /// RefreshDailyRolloverIfNeeded() 時（供分享卡生成流程於讀取數據前呼叫）比對日期字串，
    /// 不同就先把當日計數歸零再繼續處理，允許「整天無事件也未生成分享卡」時當日數字暫時不刷新。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShareCardStatsTracker : MonoBehaviour
    {
        private const string DateFormat = "yyyy-MM-dd";

        [SerializeField]
        private GlobalKeyboardHookService _keyboardHookService;

        [SerializeField]
        private WeaponDropDispatcher _weaponDropDispatcher;

        public int DailyTypedCount => SaveLifecycleController.CurrentSaveData.shareCard.dailyTypedCount;

        public int DailyKillCount => SaveLifecycleController.CurrentSaveData.shareCard.dailyKillCount;

        public long TotalTypedCount => SaveLifecycleController.CurrentSaveData.shareCard.totalTypedCount;

        public long TotalKillCount => SaveLifecycleController.CurrentSaveData.shareCard.totalKillCount;

        private void OnEnable()
        {
            if (_keyboardHookService != null)
            {
                _keyboardHookService.OnKeyPressed -= HandleKeyPressed;
                _keyboardHookService.OnKeyPressed += HandleKeyPressed;
            }

            if (_weaponDropDispatcher != null)
            {
                _weaponDropDispatcher.OnWeaponDropped -= HandleWeaponDropped;
                _weaponDropDispatcher.OnWeaponDropped += HandleWeaponDropped;
            }
        }

        private void OnDisable()
        {
            if (_keyboardHookService != null)
            {
                _keyboardHookService.OnKeyPressed -= HandleKeyPressed;
            }

            if (_weaponDropDispatcher != null)
            {
                _weaponDropDispatcher.OnWeaponDropped -= HandleWeaponDropped;
            }
        }

        private void HandleKeyPressed(KeyPressData data)
        {
            RefreshDailyRolloverIfNeeded();

            ShareCardSaveData saveData = SaveLifecycleController.CurrentSaveData.shareCard;
            saveData.dailyTypedCount++;
            saveData.totalTypedCount++;
        }

        private void HandleWeaponDropped(WeaponDataSO weapon, Vector3 dropPosition, WeaponDropOutcome outcome)
        {
            RefreshDailyRolloverIfNeeded();

            ShareCardSaveData saveData = SaveLifecycleController.CurrentSaveData.shareCard;
            saveData.dailyKillCount++;
            saveData.totalKillCount++;
        }

        /// <summary>
        /// 比對「目前日期」與「上次記錄日期」，不同則先歸零當日計數再更新記錄日期。
        /// 供事件處理常式，以及分享卡生成流程在讀取數據前呼叫，確保跨日後第一次讀取就是正確的當日數字。
        /// </summary>
        public void RefreshDailyRolloverIfNeeded()
        {
            ShareCardSaveData saveData = SaveLifecycleController.CurrentSaveData.shareCard;
            string today = DateTime.Today.ToString(DateFormat, CultureInfo.InvariantCulture);

            if (saveData.lastRecordedDate == today)
            {
                return;
            }

            saveData.dailyTypedCount = 0;
            saveData.dailyKillCount = 0;
            saveData.lastRecordedDate = today;
        }
    }
}
