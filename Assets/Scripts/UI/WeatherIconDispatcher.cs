using DeskSlayer.Weather;
using TMPro;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 天氣狀態欄的統籌者：訂閱 WeatherService.OnWeatherChanged／OnCityChanged，把新的天氣圖示轉發給
    /// WeatherIconView、把目前選擇城市名稱轉發給城市文字。只負責轉發顯示內容，不參與任何天氣查詢或
    /// 數值判斷邏輯，讓 WeatherService 不需要認識任何 UI 元件，比照 WeatherCitySelectionController
    /// 的統籌者角色。城市名稱是可留空欄位：沒有指派就單純不顯示，不強制每個天氣狀態欄都要有城市文字。
    /// </summary>
    public sealed class WeatherIconDispatcher : MonoBehaviour
    {
        [SerializeField]
        private WeatherService _weatherService;

        [SerializeField]
        private WeatherIconView _iconView;

        [SerializeField, Tooltip("顯示目前選擇城市名稱的文字（可留空，不強制要求）")]
        private TextMeshProUGUI _cityNameLabel;

        private void OnEnable()
        {
            if (_weatherService == null)
            {
                Debug.LogWarning("[WeatherIconDispatcher] 尚未指派 WeatherService，天氣圖示無法更新");
                return;
            }

            _weatherService.OnWeatherChanged += HandleWeatherChanged;
            _weatherService.OnCityChanged += HandleCityChanged;
        }

        private void OnDisable()
        {
            if (_weatherService != null)
            {
                _weatherService.OnWeatherChanged -= HandleWeatherChanged;
                _weatherService.OnCityChanged -= HandleCityChanged;
            }
        }

        private void Start()
        {
            // WeatherService.Awake() 已套用快取／預設天氣分類，這裡直接讀取 CurrentModifier 顯示初始圖示，
            // 不等待下一次 OnWeatherChanged（該事件只在分類「改變」時才會發出）。城市名稱同理直接讀取
            // CurrentCityDisplayName，不等待 OnCityChanged（該事件只在玩家「切換」城市時才會發出）。
            if (_weatherService != null)
            {
                _iconView.SetIcon(_weatherService.CurrentModifier.Icon);
                HandleCityChanged(_weatherService.CurrentCityDisplayName);
            }
        }

        private void HandleWeatherChanged(WeatherModifierData modifier)
        {
            _iconView.SetIcon(modifier.Icon);
        }

        private void HandleCityChanged(string cityDisplayName)
        {
            if (_cityNameLabel != null)
            {
                _cityNameLabel.text = cityDisplayName;
            }
        }
    }
}
