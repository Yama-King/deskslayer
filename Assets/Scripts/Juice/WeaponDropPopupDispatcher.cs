using UnityEngine;
using DeskSlayer.Combat;
using DeskSlayer.UI;

namespace DeskSlayer.Juice
{
    /// <summary>
    /// 表現層橋接元件：訂閱同一個 GameObject 上 WeaponDropDispatcher 的 OnWeaponDropped 事件，
    /// 在敵人死亡位置生成一個 WeaponDropPopup 顯示本次掉落的武器。比照 DamagePopupDispatcher
    /// 的作法，純粹轉發表現效果，不參與任何掉落判定邏輯。
    /// </summary>
    [RequireComponent(typeof(WeaponDropDispatcher))]
    public sealed class WeaponDropPopupDispatcher : MonoBehaviour
    {
        [SerializeField]
        private WeaponDropPopup _popupPrefab;

        [SerializeField, Tooltip("背包 UI 共用的稀有度顯示樣式設定（顏色／顯示名稱），避免另外維護一份")]
        private WeaponRarityDisplayConfigSO _displayConfig;

        [SerializeField, Tooltip("相對於死亡位置的生成偏移量（世界座標），預設往上偏移到頭頂位置")]
        private Vector3 _spawnOffset = new Vector3(0f, 1.6f, 0f);

        private WeaponDropDispatcher _dropDispatcher;

        private void Awake()
        {
            _dropDispatcher = GetComponent<WeaponDropDispatcher>();
        }

        private void OnEnable()
        {
            _dropDispatcher.OnWeaponDropped += HandleWeaponDropped;
        }

        private void OnDisable()
        {
            _dropDispatcher.OnWeaponDropped -= HandleWeaponDropped;
        }

        private void HandleWeaponDropped(WeaponDataSO weapon, Vector3 dropPosition)
        {
            if (_popupPrefab == null || _displayConfig == null)
            {
                return;
            }

            WeaponDropPopup popup = Instantiate(_popupPrefab, dropPosition + _spawnOffset, Quaternion.identity);
            popup.Show(weapon, _displayConfig.GetStyle(weapon.Rarity));
        }
    }
}
