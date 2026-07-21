using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 單一種命中特效的 Object Pool。啟動時依 HitVfxSetSO.PoolCapacity 一次建立完畢並全部停用，
    /// 之後只透過租借/歸還管理生命週期，全程不再呼叫 Instantiate/Destroy，比照 WeaponAfterimagePool 的作法。
    /// 三種特效（輕攻擊粒子／重攻擊粒子／敵人受擊特效）各自使用獨立的 Pool 實例、容量各自可調；
    /// 同一個特效的不同變體共用同一個 Pool——變體由 HitVfxSetSO 在租借當下隨機決定，
    /// 不因為變體數量增加而讓 Pool 數量跟著膨脹。
    /// 池子耗盡時直接跳過本次生成，不強制搶奪既有物件，避免播到一半的特效瞬間跳位造成明顯瑕疵，
    /// 這是打字觸發頻率可能很高的情境下刻意選擇的降級策略。
    /// </summary>
    public sealed class PooledSpriteAnimationPool : MonoBehaviour
    {
        [SerializeField, Tooltip("此 Pool 的資料來源，決定變體清單、播放節奏與 Pool 容量")]
        private HitVfxSetSO _vfxSet;

        [SerializeField, Tooltip("特效物件的 Sorting Layer")]
        private string _sortingLayerName = "Default";

        [SerializeField, Tooltip("特效物件的 Order in Layer")]
        private int _sortingOrder;

        private readonly Queue<PooledSpriteAnimationItem> _availableItems = new Queue<PooledSpriteAnimationItem>();
        private readonly List<PooledSpriteAnimationItem> _borrowedItems = new List<PooledSpriteAnimationItem>();
        private Action<PooledSpriteAnimationItem> _returnToPoolCallback;

        private void Awake()
        {
            // 快取單一委派實例，避免每次租借都重新產生一個新的 method group 委派。
            _returnToPoolCallback = ReturnToPool;

            if (_vfxSet == null)
            {
                Debug.LogWarning($"[PooledSpriteAnimationPool] {name} 未指定 _vfxSet，此 Pool 停用", this);
                return;
            }

            for (int i = 0; i < _vfxSet.PoolCapacity; i++)
            {
                _availableItems.Enqueue(CreatePoolItem(i));
            }
        }

        /// <summary>
        /// 嘗試從變體清單隨機挑一組並在指定錨點底下播放。池子無閒置物件、資料來源未指定、
        /// 錨點為 null、或變體清單為空時皆直接跳過本次生成，不報錯、不噴警告——命中特效允許被丟棄，
        /// 也允許「先建架構、之後補素材」的流程不需要改動任何程式碼。
        /// </summary>
        public void TrySpawn(Transform anchor)
        {
            if (_vfxSet == null || anchor == null || _availableItems.Count == 0)
            {
                return;
            }

            HitVfxVariant variant = _vfxSet.GetRandomVariant();
            if (variant == null || variant.Frames == null || variant.Frames.Length == 0)
            {
                return;
            }

            PooledSpriteAnimationItem item = _availableItems.Dequeue();
            _borrowedItems.Add(item);
            item.Play(variant.Frames, _vfxSet.FrameDuration, _vfxSet.ScaleMultiplier, _vfxSet.Tint, anchor, _returnToPoolCallback);
        }

        /// <summary>
        /// 強制回收目前所有借出中的物件，不等待動畫自然播完。
        /// 供敵人即將被銷毀前呼叫，避免掛在敵人底下的特效物件被 Destroy() 連帶銷毀、從池子中永久流失。
        /// </summary>
        public void ReleaseAllBorrowed()
        {
            // ForceStopAndRelease 會透過 _returnToPoolCallback 呼叫 ReturnToPool，同步把該項目從
            // _borrowedItems 移除，因此從尾端往回遍歷，避免遍歷中列表被同步修改導致跳過項目。
            for (int i = _borrowedItems.Count - 1; i >= 0; i--)
            {
                _borrowedItems[i].ForceStopAndRelease();
            }
        }

        private void ReturnToPool(PooledSpriteAnimationItem item)
        {
            _borrowedItems.Remove(item);
            item.transform.SetParent(transform, worldPositionStays: false);
            _availableItems.Enqueue(item);
        }

        private PooledSpriteAnimationItem CreatePoolItem(int index)
        {
            var itemObject = new GameObject($"{name}_Item_{index}");
            itemObject.transform.SetParent(transform, worldPositionStays: false);

            var spriteRenderer = itemObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = _sortingLayerName;
            spriteRenderer.sortingOrder = _sortingOrder;

            PooledSpriteAnimationItem item = itemObject.AddComponent<PooledSpriteAnimationItem>();
            itemObject.SetActive(false);
            return item;
        }
    }
}
