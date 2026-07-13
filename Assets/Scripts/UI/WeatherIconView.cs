using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 天氣圖示的純顯示元件：只負責把 Sprite 套用到 Image 上，不認識 WeatherService 或任何事件，
    /// 交由 WeatherIconDispatcher 呼叫，比照 WeatherCityEntryView 的顯示/邏輯分工方式。
    /// </summary>
    public sealed class WeatherIconView : MonoBehaviour
    {
        [SerializeField]
        private Image _iconImage;

        /// <summary>套用指定的天氣圖示；資料庫尚未設定 Icon（null）時隱藏圖示，避免顯示破圖。</summary>
        public void SetIcon(Sprite icon)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }
    }
}
