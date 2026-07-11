using DeskSlayer.Combat;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 家族 → 變體網格 → 變體詳情的畫面轉場狀態機。刻意獨立於 WeaponInventoryPanelController
    /// （面板開關）與各個 View（單純顯示 + 發事件）之外，扮演唯一知道「目前該顯示哪個畫面」的
    /// 統籌者，View 之間不互相依賴，避免職責混雜成一個 God Class。
    /// </summary>
    public sealed class WeaponCollectionNavigator : MonoBehaviour
    {
        [SerializeField]
        private WeaponFamilyTabGroup _familyTabGroup;

        [SerializeField]
        private WeaponVariantGridView _variantGridView;

        [SerializeField]
        private WeaponVariantDetailView _variantDetailView;

        private WeaponFamily _currentFamily;

        private void OnEnable()
        {
            _familyTabGroup.OnFamilySelected += HandleFamilySelected;
            _variantGridView.OnVariantSelected += HandleVariantSelected;
            _variantDetailView.OnBackRequested += HandleBackRequested;
        }

        private void OnDisable()
        {
            _familyTabGroup.OnFamilySelected -= HandleFamilySelected;
            _variantGridView.OnVariantSelected -= HandleVariantSelected;
            _variantDetailView.OnBackRequested -= HandleBackRequested;
        }

        private void Start()
        {
            _variantDetailView.Hide();
            _familyTabGroup.SelectDefault();
        }

        private void HandleFamilySelected(WeaponFamily family)
        {
            _currentFamily = family;
            _variantDetailView.Hide();
            _variantGridView.Show(family);
        }

        private void HandleVariantSelected(int variant)
        {
            _variantGridView.Hide();
            _variantDetailView.Show(_currentFamily, variant);
        }

        private void HandleBackRequested()
        {
            _variantDetailView.Hide();
            _variantGridView.Show(_currentFamily);
        }
    }
}
