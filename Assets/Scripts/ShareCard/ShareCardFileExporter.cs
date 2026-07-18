#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡 PNG 輸出與檔案總管定位的無狀態工具類別，比照 SaveService 的既有慣例
    /// （沿用 Application.persistentDataPath 為基底目錄，單一職責只處理檔案 I/O）。
    ///
    /// DeskSlayer 的桌面視窗預設置頂顯示，Windows 對「搶焦點」有內建限制，新開的檔案總管視窗
    /// 預設可能被擋在置頂視窗後面、玩家看起來像什麼都沒發生。這裡呼叫 AllowSetForegroundWindow
    /// 讓即將開啟的檔案總管視窗有權把自己帶到最前面，是這類情境下的標準 Win32 對策，
    /// 但 Windows 的搶焦點限制本來就不保證每個環境都 100% 生效。
    /// </summary>
    public static class ShareCardFileExporter
    {
        private const int ASFW_ANY = -1;

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);

        private const string SubFolderName = "ShareCards";

        // 只有這段才是 DateTime 自訂格式字串；"sharecard_" 前綴另外用字串串接，
        // 不能直接寫進格式字串裡——裡面的字母（例如 s/h/a/c/d）會被當成格式代碼解析，
        // 曾經因此產生過類似「3010arecar18_...」的錯亂檔名。
        private const string TimestampFormat = "yyyyMMdd_HHmmssfff";

        /// <summary>
        /// 把 PNG 位元組寫入獨立子資料夾，檔名含產生時間避免覆蓋，完成後自動開啟檔案總管定位到該資料夾。
        /// 任一步驟失敗都只記錄警告、回傳 null，不拋出例外中斷呼叫端流程。
        /// </summary>
        public static string SaveAndReveal(byte[] pngBytes)
        {
            if (pngBytes == null || pngBytes.Length == 0)
            {
                Debug.LogWarning("[ShareCardFileExporter] PNG 資料為空，未寫入檔案。");
                return null;
            }

            try
            {
                string folderPath = Path.Combine(Application.persistentDataPath, SubFolderName);
                Directory.CreateDirectory(folderPath);

                string fileName = "sharecard_" + DateTime.Now.ToString(TimestampFormat, CultureInfo.InvariantCulture) + ".png";
                string fullPath = Path.Combine(folderPath, fileName);

                File.WriteAllBytes(fullPath, pngBytes);
                Debug.Log($"[ShareCardFileExporter] 分享卡已輸出：{fullPath}");

                RevealFolderInExplorer(folderPath);
                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// 直接開啟資料夾本身（不用 /select 定位單一檔案）。/select 在部分環境下不夠可靠——
        /// 曾經實測遇過只把既有檔案總管視窗帶到前面、卻沒真正切換到目標資料夾的情況，
        /// 直接開資料夾路徑是保證一定會落在正確位置的做法。
        ///
        /// Application.persistentDataPath 一律回傳正斜線路徑（即使在 Windows 上），
        /// 跟 Path.Combine 補上的反斜線混在一起會變成正反斜線夾雜的路徑字串。explorer.exe
        /// 的命令列參數解析對這種混合路徑不夠穩定，曾經實測遇過解析失敗、直接退回開啟預設的
        /// 「文件」資料夾而非目標路徑——這裡統一換成 Windows 慣用的反斜線再傳給 explorer.exe。
        /// </summary>
        private static void RevealFolderInExplorer(string folderPath)
        {
            try
            {
                string windowsStylePath = folderPath.Replace('/', '\\');
                AllowSetForegroundWindow(ASFW_ANY);
                Process.Start("explorer.exe", $"\"{windowsStylePath}\"");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
#endif
