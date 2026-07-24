using DeskSlayer.DesktopWindow;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 設定選單「畫面縮放」滑桿的顯示值同步：訂閱 GameWorldDragCoordinator.OnWorldScaleChanged，
    /// 不論縮放來源是滾輪（DesktopWorldScrollZoomController）還是滑桿本身拖曳（Inspector 上
    /// 直接掛的 Slider.onValueChanged → GameWorldDragCoordinator.SetWorldScale 持久呼叫），
    /// 滑桿顯示值都跟著更新為目前實際縮放倍率，架構上比照 AnchoredUISyncMover 訂閱同一個事件的既有模式。
    ///
    /// 設定面板開關（SettingsPanelController）只是切換 CanvasGroup 的 alpha/interactable，
    /// 面板 GameObject 本身整個生命週期都維持啟用，不會 SetActive(false)——因此這裡不需要另外監聽
    /// 面板開關事件：訂閱從場景載入後就持續有效，面板不可見時一樣在背景同步，下次打開時自然顯示最新值。
    ///
    /// 用 SetValueWithoutNotify 更新顯示值，刻意不觸發 Slider.onValueChanged，避免形成
    /// 「縮放 → 更新滑桿 → onValueChanged → 又設一次縮放」的多餘重複賦值。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    public sealed class WorldScaleSliderSync : MonoBehaviour
    {
        [SerializeField, Tooltip("縮放數值的單一事實來源")]
        private GameWorldDragCoordinator _coordinator;

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (_coordinator == null)
            {
                return;
            }

            _slider.SetValueWithoutNotify(_coordinator.CurrentWorldScale);
            _coordinator.OnWorldScaleChanged += HandleWorldScaleChanged;
        }

        private void OnDisable()
        {
            if (_coordinator != null)
            {
                _coordinator.OnWorldScaleChanged -= HandleWorldScaleChanged;
            }
        }

        private void HandleWorldScaleChanged(float scale)
        {
            _slider.SetValueWithoutNotify(scale);
        }
    }
}
