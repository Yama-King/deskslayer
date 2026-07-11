using System.Collections.Generic;
using System;
using DeskSlayer.Combat;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 選定家族後的變體導覽網格：透過 WeaponDatabaseSO 動態查詢該家族底下實際存在的變體編號
    /// （不寫死變體清單），為每個變體生成一個 WeaponVariantEntryView 入口。
    /// </summary>
    public sealed class WeaponVariantGridView : MonoBehaviour
    {
        [SerializeField]
        private WeaponDatabaseSO _database;

        [SerializeField]
        private WeaponInventoryService _inventoryService;

        [SerializeField]
        private Transform _contentParent;

        [SerializeField]
        private WeaponVariantEntryView _entryPrefab;

        [SerializeField, Tooltip("這個視圖自身的容器物件，Show/Hide 時整個開關")]
        private GameObject _root;

        /// <summary>某個變體入口被選取時發出。</summary>
        public event Action<int> OnVariantSelected;

        private readonly List<WeaponVariantEntryView> _spawnedEntries = new List<WeaponVariantEntryView>();

        /// <summary>顯示指定家族的變體網格。</summary>
        public void Show(WeaponFamily family)
        {
            _root.SetActive(true);
            Populate(family);
        }

        /// <summary>隱藏此視圖。</summary>
        public void Hide()
        {
            _root.SetActive(false);
        }

        private void Populate(WeaponFamily family)
        {
            ClearEntries();

            IReadOnlyList<int> variants = _database.GetVariantNumbers(family);
            foreach (int variant in variants)
            {
                WeaponVariantEntryView entry = Instantiate(_entryPrefab, _contentParent);
                entry.Bind(family, variant, _database, _inventoryService);

                int capturedVariant = variant;
                entry.OnClicked += () => OnVariantSelected?.Invoke(capturedVariant);

                _spawnedEntries.Add(entry);
            }
        }

        private void ClearEntries()
        {
            foreach (WeaponVariantEntryView entry in _spawnedEntries)
            {
                if (entry != null)
                {
                    Destroy(entry.gameObject);
                }
            }

            _spawnedEntries.Clear();
        }
    }
}
