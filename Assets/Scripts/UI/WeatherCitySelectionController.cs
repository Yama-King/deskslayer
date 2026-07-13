using DeskSlayer.Weather;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 城市選擇清單與天氣服務之間的統籌者：訂閱 WeatherCityListView 發出的選取事件，
    /// 呼叫 WeatherService.SelectCity 觸發切換城市／持久化／立即重新查詢，再把結果反映回
    /// 清單的高亮標示。WeatherCityListView 本身完全不認識 WeatherService，維持顯示層與
    /// 服務層的分工，比照 WeaponCollectionNavigator 的統籌者角色。
    /// </summary>
    public sealed class WeatherCitySelectionController : MonoBehaviour
    {
        [SerializeField]
        private WeatherCityDatabaseSO _cityDatabase;

        [SerializeField]
        private WeatherService _weatherService;

        [SerializeField]
        private WeatherCityListView _listView;

        private void OnEnable()
        {
            _listView.OnCitySelected += HandleCitySelected;
        }

        private void OnDisable()
        {
            _listView.OnCitySelected -= HandleCitySelected;
        }

        private void Start()
        {
            int selectedIndex = _cityDatabase != null ? _cityDatabase.SelectedCityIndex : 0;
            _listView.Populate(selectedIndex);
        }

        private void HandleCitySelected(int cityIndex)
        {
            if (_weatherService == null)
            {
                Debug.LogWarning("[WeatherCitySelectionController] 尚未指派 WeatherService，無法切換城市");
                return;
            }

            _weatherService.SelectCity(cityIndex);
            _listView.SetSelectedIndex(cityIndex);
        }
    }
}
