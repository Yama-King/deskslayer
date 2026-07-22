using UnityEngine;
using UnityEngine.UI;
using DeskSlayer.DesktopWindow;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 主選單裡「鎖定桌面世界」的切換鈕：按一下切換 GameWorldDragCoordinator 的鎖定狀態，並依目前
    /// 狀態切換自己的圖示（上鎖／解鎖），比照 PauseToggleButton 用單一 Image 切換兩張圖示的做法，
    /// 不用兩個疊在一起的 Button 各管一種狀態。
    /// </summary>
    public sealed class DesktopLockToggleButton : MonoBehaviour
    {
        [SerializeField]
        private GameWorldDragCoordinator _coordinator;

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private Sprite _lockedIcon;

        [SerializeField]
        private Sprite _unlockedIcon;

        private void Start()
        {
            RefreshIcon();
        }

        /// <summary>切換鎖定狀態；掛在 Button.onClick 上。</summary>
        public void Toggle()
        {
            _coordinator.SetLocked(!_coordinator.IsLocked);
            RefreshIcon();
        }

        private void RefreshIcon()
        {
            _iconImage.sprite = _coordinator.IsLocked ? _lockedIcon : _unlockedIcon;
        }
    }
}
