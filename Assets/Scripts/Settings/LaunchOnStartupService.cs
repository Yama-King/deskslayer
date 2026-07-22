using System;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace DeskSlayer.Settings
{
    /// <summary>
    /// 開機自動啟動的實際 OS 層級開關：寫入/移除 Windows 登錄檔 HKCU Run 機碼裡的一筆值，
    /// 讓 Windows 在使用者登入時直接啟動這個桌面陪伴程式，不需要玩家每次手動開啟。
    ///
    /// 直接用 P/Invoke 呼叫 advapi32.dll 的登錄檔 API，不使用 Microsoft.Win32.Registry 這個
    /// managed 包裝類別——Player Settings 的 Api Compatibility Level 是 .NET Standard 2.0，
    /// Standalone Build 底下 Microsoft.Win32.Registry/RegistryKey 屬於另一個獨立組件
    /// （Microsoft.Win32.Registry.dll），預設沒有被引用，Editor 裡編譯得過、正式 Build 時卻會
    /// 噴 CS1069/CS0103 編譯錯誤——這是實測 Build and Run 抓到的真實錯誤，不是假設性風險。
    /// 比照專案裡 Global Keyboard Hook 手刻 Win32 P/Invoke（而非套件/managed wrapper）的既有慣例，
    /// 這裡同樣直接呼叫 Win32 API，不依賴任何額外組件引用。
    ///
    /// 只在正式 Standalone Windows Build 生效（#if UNITY_STANDALONE_WIN && !UNITY_EDITOR）：
    /// Editor 裡執行的是 Unity Editor.exe 本身，不是打包後的遊戲執行檔，寫入登錄檔既沒有意義，
    /// 也會讓開發機的登入機碼被塞進一筆指向 Editor 的無效項目，因此 Editor 環境下只記錄 Log，
    /// 不實際觸碰登錄檔。
    ///
    /// HKCU（HKEY_CURRENT_USER）不需要系統管理員權限即可寫入，寫入失敗時只記錄警告、不丟例外
    /// 中斷設定流程——這是使用者可有可無的偏好設定，不該讓一次登錄檔寫入失敗連帶讓其他設定跟著壞掉。
    /// </summary>
    public static class LaunchOnStartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "DeskSlayer";

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int HKEY_CURRENT_USER = unchecked((int)0x80000001);
        private const int KEY_SET_VALUE = 0x0002;
        private const int REG_SZ = 1;
        private const int ERROR_SUCCESS = 0;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, int options, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegSetValueEx(IntPtr hKey, string valueName, int reserved, int dwType, string data, int cbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegDeleteValue(IntPtr hKey, string valueName);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(IntPtr hKey);
#endif

        public static void SetEnabled(bool enabled)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            IntPtr hKey;
            int openResult = RegOpenKeyEx(new IntPtr(HKEY_CURRENT_USER), RunKeyPath, 0, KEY_SET_VALUE, out hKey);
            if (openResult != ERROR_SUCCESS)
            {
                Debug.LogWarning("[LaunchOnStartupService] 找不到登錄檔 Run 機碼，無法設定開機自動啟動（error=" + openResult + "）");
                return;
            }

            try
            {
                int result;
                if (enabled)
                {
                    string exeFullPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    string quotedPath = "\"" + exeFullPath + "\"";
                    // Win32 REG_SZ 字串需要包含結尾的 Null Terminator，長度要多算一個字元。
                    result = RegSetValueEx(hKey, ValueName, 0, REG_SZ, quotedPath, (quotedPath.Length + 1) * 2);
                }
                else
                {
                    result = RegDeleteValue(hKey, ValueName);
                    // 值原本就不存在（ERROR_FILE_NOT_FOUND=2）不算失敗，關閉狀態本來就該是「沒有這個值」。
                    if (result == 2)
                    {
                        result = ERROR_SUCCESS;
                    }
                }

                if (result != ERROR_SUCCESS)
                {
                    Debug.LogWarning("[LaunchOnStartupService] 設定開機自動啟動失敗（error=" + result + "）");
                }
            }
            finally
            {
                RegCloseKey(hKey);
            }
#else
            Debug.Log("[LaunchOnStartupService] 目前不是正式 Windows Build，略過登錄檔寫入（enabled=" + enabled + "）");
#endif
        }
    }
}
