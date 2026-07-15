using System;
using System.IO;
using UnityEngine;

namespace DeskSlayer.Persistence
{
    /// <summary>
    /// 存檔資料的序列化與檔案 I/O，職責僅限於「一份 SaveData 物件」與「磁碟上的 JSON 檔案」之間的轉換，
    /// 不認識任何遊戲邏輯、不認識何時該讀寫（那是 SaveLifecycleController 的職責）。
    /// 比照 WeatherCacheStore 的既有寫法：用 JsonUtility + Application.persistentDataPath，
    /// 讀寫失敗一律攔截例外、印出 Warning、回傳可用的預設值，不讓存檔問題中斷遊戲啟動或執行。
    /// 刻意設計成無狀態的靜態類別（不快取讀取結果、每次呼叫都直接觸碰磁碟）：
    /// 存檔檔案很小、讀寫次數在一次遊戲流程中屈指可數，用「每次都读最新的磁碟內容」換取
    /// 「不必擔心呼叫端跟 Unity 生命週期執行順序有任何相依」的簡單性，划算。
    /// </summary>
    public static class SaveService
    {
        /// <summary>目前的存檔版本號。之後新增/異動欄位時，讀檔端可比對這個常數判斷是否需要遷移邏輯。</summary>
        public const int CurrentSaveVersion = 1;

        private const string SaveFileName = "savedata.json";

        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        /// <summary>
        /// 讀取存檔。檔案不存在（例如玩家第一次啟動遊戲）或內容無法解析（損毀/格式不符）時，
        /// 一律回傳一份帶有預設值的 SaveData，並印出可辨識的 Warning／Log，絕不拋出例外。
        /// </summary>
        public static SaveData Load()
        {
            string path = SaveFilePath;

            if (!File.Exists(path))
            {
                Debug.Log("[SaveService] 找不到存檔檔案，視為第一次啟動，使用預設存檔資料");
                return new SaveData();
            }

            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data == null || data.saveVersion <= 0)
                {
                    Debug.LogWarning("[SaveService] 存檔內容格式不符預期（無法辨識版本號），改用預設存檔資料");
                    return new SaveData();
                }

                if (data.saveVersion != CurrentSaveVersion)
                {
                    Debug.LogWarning($"[SaveService] 存檔版本（{data.saveVersion}）與目前版本（{CurrentSaveVersion}）不符，暫時直接沿用讀到的內容");
                }

                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveService] 讀取存檔失敗，改用預設存檔資料：{exception.Message}");
                return new SaveData();
            }
        }

        /// <summary>
        /// 將指定的存檔資料寫入磁碟。寫入過程發生例外（磁碟空間不足、檔案被鎖定等）時只印出 Warning，
        /// 不讓存檔失敗導致遊戲當掉。
        /// </summary>
        public static void Save(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveService] 寫入存檔失敗：{exception.Message}");
            }
        }
    }
}
