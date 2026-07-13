using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 城市清單中的單一項目：顯示城市名稱、被選取時顯示明顯標示，純粹是顯示 + 收集點擊，
    /// 不知道自己代表清單中的第幾筆、也不認識 WeatherService，交由 WeatherCityListView 綁定與轉發。
    /// </summary>
    public sealed class WeatherCityEntryView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        [SerializeField]
        private Button _button;

        [SerializeField, Tooltip("目前選定城市的視覺標示（例如高亮背景或勾選圖示），Bind 時依 isSelected 開關")]
        private GameObject _selectedIndicator;

        /// <summary>這個城市項目被點擊時發出。</summary>
        public event Action OnClicked;

        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
        }

        /// <summary>綁定顯示內容：城市名稱與目前是否為選定城市。</summary>
        public void Bind(string displayName, bool isSelected)
        {
            _nameLabel.text = displayName;
            SetSelected(isSelected);
        }

        /// <summary>只更新選取標示，不重新設定名稱，供清單切換選取項目時使用。</summary>
        public void SetSelected(bool isSelected)
        {
            if (_selectedIndicator != null)
            {
                _selectedIndicator.SetActive(isSelected);
            }
        }

        private void HandleClicked()
        {
            OnClicked?.Invoke();
        }
    }
}
