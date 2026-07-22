using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 目前裝備武器圖示的純顯示元件：只負責把 Sprite 套用到 Image 上，不認識 WeaponSwitcher 或任何
    /// 事件，交由 WeaponIconDispatcher 呼叫，比照 WeatherIconView 的顯示/邏輯分工方式。
    /// </summary>
    public sealed class WeaponIconView : MonoBehaviour
    {
        [SerializeField]
        private Image _iconImage;

        /// <summary>套用指定的武器圖示；武器尚未設定 Icon（null）時隱藏圖示，避免顯示破圖。</summary>
        public void SetIcon(Sprite icon)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }
    }
}
