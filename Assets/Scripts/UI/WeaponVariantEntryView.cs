using System;
using DeskSlayer.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 變體導覽網格中的單一入口按鈕：只顯示變體編號與該變體 4 個稀有度的收集進度（例如 2/4），
    /// 純粹是導覽用途，不顯示任何武器數值細節——數值留到玩家點進詳情頁後由 WeaponRaritySlotView 呈現。
    /// </summary>
    public sealed class WeaponVariantEntryView : MonoBehaviour
    {
        private static readonly WeaponRarity[] AllRarities =
        {
            WeaponRarity.Common, WeaponRarity.Rare, WeaponRarity.Epic, WeaponRarity.Legendary
        };

        [SerializeField]
        private TextMeshProUGUI _variantLabel;

        [SerializeField]
        private TextMeshProUGUI _progressLabel;

        [SerializeField]
        private Button _button;

        /// <summary>這個變體入口被點擊時發出。</summary>
        public event Action OnClicked;

        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
        }

        /// <summary>綁定顯示內容：變體編號與目前已擁有的稀有度數量。</summary>
        public void Bind(WeaponFamily family, int variant, WeaponDatabaseSO database, WeaponInventoryService inventoryService)
        {
            _variantLabel.text = $"變體 {variant}";

            int ownedCount = 0;
            int totalCount = 0;
            foreach (WeaponRarity rarity in AllRarities)
            {
                WeaponDataSO weapon = database.GetWeapon(family, variant, rarity);
                if (weapon == null)
                {
                    continue;
                }

                totalCount++;
                if (inventoryService.IsOwned(weapon))
                {
                    ownedCount++;
                }
            }

            _progressLabel.text = $"{ownedCount}/{totalCount}";
        }

        private void HandleClicked()
        {
            OnClicked?.Invoke();
        }
    }
}
