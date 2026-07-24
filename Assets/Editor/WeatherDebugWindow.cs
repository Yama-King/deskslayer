using UnityEditor;
using UnityEngine;
using DeskSlayer.Weather;

namespace DeskSlayer.EditorTools
{
    /// <summary>
    /// 開發用天氣切換工具：Play Mode 下用按鈕個別切到指定天氣分類，方便直接盯著 Game View 檢視
    /// 角色/敵人/GameWorldRoot 染色與 Rain/Snow/Thunderstorm Tilemap 顯示切換的實際視覺效果。
    /// 跟 AchievementWeatherTestMenu 一次把五種循環過一遍不同，這裡是逐一點選、方便單獨盯著某一種
    /// 天氣看視覺結果。呼叫的一樣是 WeatherService 既有的 DebugForceCategory，跟正式判定走同一套
    /// ApplyCategory 邏輯，不繞過既有的天氣資料轉換層。只在 Play Mode 下可用，因為 WeatherService
    /// 是場景中的執行期物件。放在 Editor 資料夾底下，Unity 會自動排除在正式版本之外，不會被打包進
    /// itch.io 的下載版 exe。
    /// </summary>
    public sealed class WeatherDebugWindow : EditorWindow
    {
        [MenuItem("DeskSlayer/Debug/Weather Switch Tool")]
        private static void Open()
        {
            GetWindow<WeatherDebugWindow>("天氣切換工具");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("需要進入 Play Mode 才能切換天氣，WeatherService 是執行期物件。", MessageType.Info);
                return;
            }

            WeatherService weatherService = Object.FindFirstObjectByType<WeatherService>();
            if (weatherService == null)
            {
                EditorGUILayout.HelpBox("場景中找不到執行中的 WeatherService 實例。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("目前天氣分類", weatherService.CurrentCategory.ToString());

            GUI.enabled = false;
            EditorGUILayout.ColorField("目前色調", weatherService.CurrentModifier.TintColor);
            GUI.enabled = true;

            EditorGUILayout.Space();

            foreach (WeatherCategory category in (WeatherCategory[])System.Enum.GetValues(typeof(WeatherCategory)))
            {
                GUI.enabled = category != weatherService.CurrentCategory;

                if (GUILayout.Button(category.ToString(), GUILayout.Height(28)))
                {
                    weatherService.DebugForceCategory(category);
                }
            }

            GUI.enabled = true;
        }

        private void OnInspectorUpdate()
        {
            // 天氣分類是事件觸發才改變的資料，不是每影格都會變，但視窗本身只有滑鼠互動時才會重繪，
            // 這裡強制定時重繪，讓「目前天氣分類」標籤不需要滑鼠移動到視窗上就能即時反映最新狀態。
            Repaint();
        }
    }
}
