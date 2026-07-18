using UnityEditor;
using UnityEngine;
using DeskSlayer.Weather;

namespace DeskSlayer.EditorTools
{
    /// <summary>
    /// 開發用天氣循環工具：依序觸發五種天氣分類各一次的 OnWeatherChanged，用來驗證「風雨無阻」
    /// 成就等訂閱端的集合判定邏輯，不需要真的等待/操作 API 天氣輪詢。呼叫 WeatherService 既有的
    /// DebugForceCategory（僅 Editor 下才存在），測試路徑與正式路徑走同一套判定邏輯，不繞過既有的
    /// 天氣資料轉換層。只在 Play Mode 下可用，因為 WeatherService 是場景中的執行期物件。放在 Editor
    /// 資料夾底下，Unity 會自動排除在正式版本之外，不會被打包進 itch.io 的下載版 exe，玩家在實際
    /// 遊玩路徑完全接觸不到這個入口。
    /// </summary>
    public static class AchievementWeatherTestMenu
    {
        [MenuItem("DeskSlayer/Debug/Cycle All Weather Categories (Achievement Test)", true)]
        private static bool ValidateCycleAllWeatherCategories()
        {
            return Application.isPlaying;
        }

        [MenuItem("DeskSlayer/Debug/Cycle All Weather Categories (Achievement Test)")]
        private static void CycleAllWeatherCategories()
        {
            WeatherService weatherService = Object.FindFirstObjectByType<WeatherService>();
            if (weatherService == null)
            {
                Debug.LogWarning("[AchievementWeatherTestMenu] 場景中找不到執行中的 WeatherService 實例，請先進入 Play Mode。");
                return;
            }

            var categories = (WeatherCategory[])System.Enum.GetValues(typeof(WeatherCategory));
            if (categories.Length == 0)
            {
                return;
            }

            // 暖身：先切到列舉最後一個分類，確保迴圈跑到第一個分類時，目前分類已知不同，
            // ApplyCategory 才會判定為「有變化」而真正觸發事件——避免剛好目前天氣本來就等於
            // 迴圈第一個分類，導致那一次被誤判為沒有變化而漏觸發。
            weatherService.DebugForceCategory(categories[categories.Length - 1]);

            foreach (WeatherCategory category in categories)
            {
                weatherService.DebugForceCategory(category);
                Debug.Log($"[AchievementWeatherTestMenu] 已觸發天氣分類：{category}");
            }
        }
    }
}
