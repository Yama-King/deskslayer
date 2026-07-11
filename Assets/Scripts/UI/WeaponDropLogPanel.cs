using DeskSlayer.Combat;
using DeskSlayer.Juice;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 畫面角落的掉落記錄清單：訂閱 WeaponDropDispatcher 的掉落事件，每次掉落生成一筆
    /// WeaponDropLogEntry。清單上限透過 _listContainer 的子物件數量直接控制，超過上限時
    /// 移除最舊的一筆（最舊＝最後一個子物件，因為新項目固定插入為第一個子物件）。
    /// 項目本身的自動淡出消失由 WeaponDropLogEntry 自行處理，這裡不重複管理計時器。
    /// </summary>
    public sealed class WeaponDropLogPanel : MonoBehaviour
    {
        [SerializeField, Tooltip("要訂閱的武器掉落事件來源")]
        private WeaponDropDispatcher _dropDispatcher;

        [SerializeField]
        private WeaponDropLogEntry _entryPrefab;

        [SerializeField, Tooltip("清單項目的父節點，需搭配 Vertical Layout Group 排列")]
        private Transform _listContainer;

        [SerializeField, Tooltip("背包 UI 共用的稀有度顯示樣式設定（顏色／顯示名稱），避免另外維護一份")]
        private WeaponRarityDisplayConfigSO _displayConfig;

        [SerializeField, Tooltip("各稀有度的掉落視覺回饋強度曲線（與原地跳出提示共用）")]
        private WeaponDropFeedbackConfigSO _feedbackConfig;

        [SerializeField, Min(1), Tooltip("同時最多顯示的紀錄筆數，超過時最舊的先移除")]
        private int _maxEntries = 5;

        private void OnEnable()
        {
            if (_dropDispatcher != null)
            {
                _dropDispatcher.OnWeaponDropped += HandleWeaponDropped;
            }
        }

        private void OnDisable()
        {
            if (_dropDispatcher != null)
            {
                _dropDispatcher.OnWeaponDropped -= HandleWeaponDropped;
            }
        }

        private void HandleWeaponDropped(WeaponDataSO weapon, Vector3 dropPosition, WeaponDropOutcome outcome)
        {
            if (_entryPrefab == null || _listContainer == null || _displayConfig == null || _feedbackConfig == null)
            {
                return;
            }

            WeaponDropLogEntry entry = Instantiate(_entryPrefab, _listContainer);
            entry.transform.SetAsFirstSibling();
            entry.Show(weapon, _displayConfig.GetStyle(weapon.Rarity), _feedbackConfig.GetFeedback(weapon.Rarity), outcome);

            EnforceMaxEntries();
        }

        private void EnforceMaxEntries()
        {
            // Destroy() 只是標記待銷毀，實際從階層移除要等到當前影格結束，
            // 若同一影格內連續觸發多次掉落，childCount 在迴圈中不會即時遞減，會造成無窮迴圈。
            // 因此先用 SetParent(null) 立即把物件移出清單（階層異動是同步的），再標記銷毀。
            while (_listContainer.childCount > _maxEntries)
            {
                Transform oldest = _listContainer.GetChild(_listContainer.childCount - 1);
                oldest.SetParent(null);
                Destroy(oldest.gameObject);
            }
        }
    }
}
