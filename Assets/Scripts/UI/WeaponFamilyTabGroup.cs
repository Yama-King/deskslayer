using System;
using DeskSlayer.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 背包面板最上層的家族頁籤（Dagger／Greatsword）。職責單純只有「選取哪個家族」，
    /// 不認識底下的變體列表或稀有度列項要怎麼畫，選取結果透過事件通知 WeaponCollectionNavigator。
    /// </summary>
    public sealed class WeaponFamilyTabGroup : MonoBehaviour
    {
        [SerializeField]
        private Button _daggerTabButton;

        [SerializeField]
        private Button _greatswordTabButton;

        [SerializeField, Tooltip("目前選取頁籤的高亮圖示（可留空，不強制要求視覺區隔）")]
        private Image _daggerTabHighlight;

        [SerializeField]
        private Image _greatswordTabHighlight;

        /// <summary>家族頁籤被選取時發出。</summary>
        public event Action<WeaponFamily> OnFamilySelected;

        private void OnEnable()
        {
            _daggerTabButton.onClick.AddListener(SelectDagger);
            _greatswordTabButton.onClick.AddListener(SelectGreatsword);
        }

        private void OnDisable()
        {
            _daggerTabButton.onClick.RemoveListener(SelectDagger);
            _greatswordTabButton.onClick.RemoveListener(SelectGreatsword);
        }

        /// <summary>面板開啟時預設選取的家族（第一個頁籤）。</summary>
        public void SelectDefault()
        {
            SelectDagger();
        }

        private void SelectDagger()
        {
            Select(WeaponFamily.Dagger);
        }

        private void SelectGreatsword()
        {
            Select(WeaponFamily.Greatsword);
        }

        private void Select(WeaponFamily family)
        {
            UpdateHighlight(family);
            OnFamilySelected?.Invoke(family);
        }

        private void UpdateHighlight(WeaponFamily family)
        {
            if (_daggerTabHighlight != null)
            {
                _daggerTabHighlight.enabled = family == WeaponFamily.Dagger;
            }

            if (_greatswordTabHighlight != null)
            {
                _greatswordTabHighlight.enabled = family == WeaponFamily.Greatsword;
            }
        }
    }
}
