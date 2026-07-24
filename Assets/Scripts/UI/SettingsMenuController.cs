using DeskSlayer.Audio;
using DeskSlayer.DesktopWindow;
using DeskSlayer.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 設定面板的內容邏輯：滑鼠偵測開關、主音量滑桿、視窗置頂開關等的讀取/寫入，跟 SettingsPanelController
    /// （只負責面板開關動畫）職責分開，比照 WeaponInventoryPanelController（開關殼）與
    /// WeaponCollectionNavigator 等內容元件之間的分工方式。
    ///
    /// 開關只作用在 GameSettingsPreferenceStore.MouseAttackInputEnabled 這個下游判定點
    /// （由 AttackInputAggregator 讀取），不會停止或影響 GlobalMouseHookService 本身的監聽；
    /// 音量滑桿變更會同時寫回存檔與呼叫 AudioManager.SetMasterVolume() 即時套用；
    /// 視窗置頂開關變更會同時寫回存檔與呼叫 DesktopWindowTopmostController.SetTopmost() 即時套用。
    /// </summary>
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [SerializeField]
        private Toggle _mouseInputToggle;

        [SerializeField]
        private Slider _masterVolumeSlider;

        [SerializeField]
        private Toggle _launchOnStartupToggle;

        [SerializeField]
        private Toggle _windowTopmostToggle;

        private void Start()
        {
            // 用 SetIsOnWithoutNotify/SetValueWithoutNotify 同步初始值，避免初始化過程觸發
            // onValueChanged 回呼、把讀到的值又立刻寫回存檔（多此一舉，但不算錯誤，這裡單純避免浪費）。
            _mouseInputToggle.SetIsOnWithoutNotify(GameSettingsPreferenceStore.MouseAttackInputEnabled);
            _masterVolumeSlider.SetValueWithoutNotify(GameSettingsPreferenceStore.MasterVolume);
            _launchOnStartupToggle.SetIsOnWithoutNotify(GameSettingsPreferenceStore.LaunchOnStartupEnabled);
            _windowTopmostToggle.SetIsOnWithoutNotify(GameSettingsPreferenceStore.WindowTopmostEnabled);
        }

        private void OnEnable()
        {
            _mouseInputToggle.onValueChanged.AddListener(HandleMouseInputToggleChanged);
            _masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
            _launchOnStartupToggle.onValueChanged.AddListener(HandleLaunchOnStartupToggleChanged);
            _windowTopmostToggle.onValueChanged.AddListener(HandleWindowTopmostToggleChanged);
        }

        private void OnDisable()
        {
            _mouseInputToggle.onValueChanged.RemoveListener(HandleMouseInputToggleChanged);
            _masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            _launchOnStartupToggle.onValueChanged.RemoveListener(HandleLaunchOnStartupToggleChanged);
            _windowTopmostToggle.onValueChanged.RemoveListener(HandleWindowTopmostToggleChanged);
        }

        private void HandleMouseInputToggleChanged(bool isOn)
        {
            GameSettingsPreferenceStore.MouseAttackInputEnabled = isOn;
        }

        private void HandleMasterVolumeChanged(float value)
        {
            GameSettingsPreferenceStore.MasterVolume = value;
            AudioManager.Instance?.SetMasterVolume(value);
        }

        private void HandleLaunchOnStartupToggleChanged(bool isOn)
        {
            GameSettingsPreferenceStore.LaunchOnStartupEnabled = isOn;
            LaunchOnStartupService.SetEnabled(isOn);
        }

        private void HandleWindowTopmostToggleChanged(bool isOn)
        {
            GameSettingsPreferenceStore.WindowTopmostEnabled = isOn;
            DesktopWindowTopmostController.Instance?.SetTopmost(isOn);
        }
    }
}
