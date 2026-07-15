using UnityEngine;

namespace DeskSlayer.Persistence
{
    /// <summary>
    /// 存檔服務與遊戲生命週期的整合點：決定「何時讀」「何時寫」，本身不認識 JSON 或檔案路徑
    /// （那是 SaveService 的職責）。CurrentSaveData 是整個遊戲流程共用的單一實例，讓其他系統
    /// （下一條分支：武器背包、天氣城市、最高分數）直接讀寫這個共用實例的欄位，而不是各自呼叫
    /// SaveService 產生互不相干的獨立副本——否則任何一份「各自快取」的副本在 App 關閉時被寫回磁碟，
    /// 都會蓋掉其他副本同一時間點寫入的異動，變成寫入互相覆蓋的資料競爭。
    ///
    /// CurrentSaveData 刻意設計成靜態、惰性載入（lazy singleton）：第一次被任何系統存取時才觸發
    /// 實際讀檔並快取結果，之後的存取一律直接回傳快取，不重複觸發檔案 I/O。這個設計完全不依賴
    /// 「哪個 GameObject 的 Awake 先執行」——不論是哪個系統、在生命週期的哪個時間點第一次呼叫，
    /// 都保證拿到正確載入完成的資料。之前版本把讀檔放在 Awake、靠 Instance 是否為 null 判斷是否
    /// 已載入，隱含著「Instance 非 null 即代表 Awake 已跑完、CurrentSaveData 已載入」這個未被
    /// 明確表達的耦合前提，一旦有人在未來重構 Awake（例如把讀檔搬去 Start）就可能悄悄破壞這個假設；
    /// 改成 property getter 內部判斷「是否已載入」，直接消除了這個隱性耦合。
    /// 目前只會在 Unity 主執行緒（單執行緒）情境下被存取，因此惰性初始化不需要額外加鎖；
    /// 若未來有背景執行緒需要讀取，需另外補上同步機制。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SaveLifecycleController : MonoBehaviour
    {
        public static SaveLifecycleController Instance { get; private set; }

        private static SaveData _cachedSaveData;

        /// <summary>
        /// 目前遊戲流程共用的存檔資料實例。第一次被存取時觸發讀檔，之後的存取直接回傳快取結果。
        /// 其他系統應直接讀寫這個實例的欄位，再呼叫 RequestSave() 落盤。
        /// </summary>
        public static SaveData CurrentSaveData
        {
            get
            {
                if (_cachedSaveData == null)
                {
                    _cachedSaveData = SaveService.Load();
                }

                return _cachedSaveData;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            SaveService.Save(CurrentSaveData);
        }

        /// <summary>
        /// 供其他系統手動觸發立即寫入（例如未來暫停選單的「保存」按鈕），
        /// 不需要等到 App 關閉才落盤。
        /// </summary>
        public static void RequestSave()
        {
            SaveService.Save(CurrentSaveData);
        }
    }
}
