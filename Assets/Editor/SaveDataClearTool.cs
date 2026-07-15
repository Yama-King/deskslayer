using System;
using System.IO;
using DeskSlayer.Persistence;
using UnityEditor;
using UnityEngine;

namespace DeskSlayer.EditorTools
{
    /// <summary>
    /// 開發用存檔清空工具：測試「從全新狀態開始」時，不必手動到 persistentDataPath 底下找檔案刪除。
    /// 刻意禁止在 Play Mode 執行——SaveLifecycleController.CurrentSaveData 是惰性載入後常駐在記憶體的
    /// 快取，Play Mode 中刪檔案並不會清掉這份記憶體快取，OnApplicationQuit 反而會把記憶體裡的舊資料
    /// 重新寫回磁碟、蓋掉這次清空的結果，造成「明明清過了怎麼還在」的誤解。放在 Editor 資料夾底下，
    /// Unity 會自動排除在正式版本之外，不會被打包進 itch.io 的下載版 exe。
    /// </summary>
    public static class SaveDataClearTool
    {
        [MenuItem("DeskSlayer/Debug/Clear Save Data", true)]
        private static bool ValidateClearSaveData()
        {
            return !Application.isPlaying;
        }

        [MenuItem("DeskSlayer/Debug/Clear Save Data")]
        private static void ClearSaveData()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[SaveDataClearTool] Play Mode 執行中無法清空存檔，請先停止 Play Mode 再執行。");
                return;
            }

            string path = SaveService.SaveFilePath;

            if (!File.Exists(path))
            {
                Debug.Log("[SaveDataClearTool] 目前本來就沒有存檔檔案，無需清空。");
                return;
            }

            try
            {
                File.Delete(path);
                Debug.Log($"[SaveDataClearTool] 存檔已清空：{path}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveDataClearTool] 清空存檔失敗：{exception.Message}");
            }
        }
    }
}
