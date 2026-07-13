using DeskSlayer.Weather;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 天氣圖示的統籌者：訂閱 WeatherService.OnWeatherChanged，把新的天氣圖示轉發給 WeatherIconView 顯示。
    /// 只負責轉發顯示內容，不參與任何天氣查詢或數值判斷邏輯，讓 WeatherService 不需要認識任何 UI 元件，
    /// 比照 WeatherCitySelectionController 的統籌者角色。
    /// </summary>
    public sealed class WeatherIconDispatcher : MonoBehaviour
    {
        [SerializeField]
        private WeatherService _weatherService;

        [SerializeField]
        private WeatherIconView _iconView;

        private void OnEnable()
        {
            if (_weatherService == null)
            {
                Debug.LogWarning("[WeatherIconDispatcher] 尚未指派 WeatherService，天氣圖示無法更新");
                return;
            }

            _weatherService.OnWeatherChanged += HandleWeatherChanged;
        }

        private void OnDisable()
        {
            if (_weatherService != null)
            {
                _weatherService.OnWeatherChanged -= HandleWeatherChanged;
            }
        }

        private void Start()
        {
            // WeatherService.Awake() 已套用快取／預設天氣分類，這裡直接讀取 CurrentModifier 顯示初始圖示，
            // 不等待下一次 OnWeatherChanged（該事件只在分類「改變」時才會發出）。
            if (_weatherService != null)
            {
                _iconView.SetIcon(_weatherService.CurrentModifier.Icon);
            }
        }

        private void HandleWeatherChanged(WeatherModifierData modifier)
        {
            _iconView.SetIcon(modifier.Icon);
        }
    }
}
