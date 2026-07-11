using System;
using System.Collections.Generic;
using DeskSlayer.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 單一變體的詳情頁：同一頁內把 4 個稀有度由高到低（Legendary→Common）降冪列出，
    /// 讓玩家一次看到這個變體從高階到低階的完整收集進度，不再切成獨立子頁籤。
    /// 找不到對應稀有度資產時安全跳過該列（例如尚未設計該稀有度的美術/資料）。
    /// </summary>
    public sealed class WeaponVariantDetailView : MonoBehaviour
    {
        private static readonly WeaponRarity[] DescendingRarities =
        {
            WeaponRarity.Legendary, WeaponRarity.Epic, WeaponRarity.Rare, WeaponRarity.Common
        };

        [SerializeField]
        private WeaponDatabaseSO _database;

        [SerializeField]
        private WeaponInventoryService _inventoryService;

        [SerializeField]
        private WeaponDropConfigSO _dropConfig;

        [SerializeField]
        private WeaponSwitcher _weaponSwitcher;

        [SerializeField]
        private WeaponRarityDisplayConfigSO _rarityDisplayConfig;

        [SerializeField]
        private Transform _contentParent;

        [SerializeField]
        private WeaponRaritySlotView _slotPrefab;

        [SerializeField, Tooltip("這個視圖自身的容器物件，Show/Hide 時整個開關")]
        private GameObject _root;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private TextMeshProUGUI _variantTitleLabel;

        /// <summary>按下返回按鈕時發出。</summary>
        public event Action OnBackRequested;

        private readonly List<WeaponRaritySlotView> _spawnedSlots = new List<WeaponRaritySlotView>();

        private void OnEnable()
        {
            _backButton.onClick.AddListener(HandleBackClicked);
        }

        private void OnDisable()
        {
            _backButton.onClick.RemoveListener(HandleBackClicked);
        }

        /// <summary>顯示指定家族+變體的稀有度收集詳情。</summary>
        public void Show(WeaponFamily family, int variant)
        {
            _root.SetActive(true);

            if (_variantTitleLabel != null)
            {
                _variantTitleLabel.text = $"變體 {variant}";
            }

            Populate(family, variant);
        }

        /// <summary>隱藏此視圖。</summary>
        public void Hide()
        {
            _root.SetActive(false);
            ClearSlots();
        }

        private void Populate(WeaponFamily family, int variant)
        {
            ClearSlots();

            foreach (WeaponRarity rarity in DescendingRarities)
            {
                WeaponDataSO weapon = _database.GetWeapon(family, variant, rarity);
                if (weapon == null)
                {
                    continue;
                }

                WeaponRaritySlotView slot = Instantiate(_slotPrefab, _contentParent);
                slot.Bind(weapon, _inventoryService, _dropConfig, _weaponSwitcher, _rarityDisplayConfig);
                _spawnedSlots.Add(slot);
            }
        }

        private void ClearSlots()
        {
            foreach (WeaponRaritySlotView slot in _spawnedSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _spawnedSlots.Clear();
        }

        private void HandleBackClicked()
        {
            OnBackRequested?.Invoke();
        }
    }
}
