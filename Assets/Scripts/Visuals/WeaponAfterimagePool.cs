using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 重攻擊揮擊殘影的 Object Pool。啟動時一次預先建立 _poolSize 個殘影物件並全部停用，
    /// 之後整個生命週期只透過 SetActive 切換使用中/閒置狀態，不再呼叫 Instantiate/Destroy，
    /// 避免遊玩過程中第一次觸發時才臨時建立造成的效能尖峰。
    ///
    /// 池子耗盡（無閒置物件可租借）時的降級策略選擇「直接跳過本次生成」，而非「強制回收池中
    /// 最早生成的殘影」：強制回收會讓一個已經淡出到一半的殘影瞬間跳到新的位置並跳回全透明，
    /// 視覺上是明顯的「彈跳」瑕疵；而池子耗盡代表玩家正在極高速連續觸發重攻擊，畫面上已有
    /// 大量殘影疊加，少一張不會被注意到，直接跳過對觀感的傷害遠比讓既有殘影瞬移還小。
    /// </summary>
    public sealed class WeaponAfterimagePool : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("池子初始大小，啟動時一次建立完畢，之後不再動態擴增")]
        private int _poolSize = 12;

        [SerializeField, Tooltip("殘影物件共用的 Sprite Material，需搭配手寫的 WeaponAfterimage Shader，" +
            "透過 MaterialPropertyBlock 控制個別 Alpha 衰減，所有殘影物件共用同一份，不建立 Material Instance")]
        private Material _afterimageMaterial;

        [SerializeField, Tooltip("殘影 SpriteRenderer 的 Sorting Layer，建議與武器本體相同")]
        private string _sortingLayerName = "Default";

        [SerializeField, Tooltip("殘影 SpriteRenderer 的 Order in Layer，建議略低於武器本體，讓殘影顯示在武器後方")]
        private int _sortingOrder;

        private readonly Queue<WeaponAfterimageItem> _availableItems = new Queue<WeaponAfterimageItem>();
        private Action<WeaponAfterimageItem> _returnToPoolCallback;

        private void Awake()
        {
            // 快取單一委派實例，避免每次租借（TrySpawn）都重新產生一個新的 method group 委派。
            _returnToPoolCallback = ReturnToPool;

            for (int i = 0; i < _poolSize; i++)
            {
                _availableItems.Enqueue(CreatePoolItem(i));
            }
        }

        /// <summary>
        /// 嘗試租借一個閒置殘影物件並立即開始播放。池子目前無閒置物件時（降級情境）直接跳過本次生成。
        /// </summary>
        public void TrySpawn(Sprite sprite, Vector3 worldPosition, Quaternion worldRotation, Vector3 worldScale,
            float lifetime, AnimationCurve alphaDecayCurve, float initialAlpha)
        {
            if (sprite == null || _availableItems.Count == 0)
            {
                return;
            }

            WeaponAfterimageItem item = _availableItems.Dequeue();
            item.Activate(sprite, worldPosition, worldRotation, worldScale, lifetime, alphaDecayCurve, initialAlpha, _returnToPoolCallback);
        }

        private void ReturnToPool(WeaponAfterimageItem item)
        {
            item.Deactivate();
            _availableItems.Enqueue(item);
        }

        private WeaponAfterimageItem CreatePoolItem(int index)
        {
            var itemObject = new GameObject($"WeaponAfterimage_{index}");
            itemObject.transform.SetParent(transform, worldPositionStays: false);

            var spriteRenderer = itemObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sharedMaterial = _afterimageMaterial;
            spriteRenderer.sortingLayerName = _sortingLayerName;
            spriteRenderer.sortingOrder = _sortingOrder;

            WeaponAfterimageItem item = itemObject.AddComponent<WeaponAfterimageItem>();
            itemObject.SetActive(false);
            return item;
        }
    }
}
