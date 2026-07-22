using System;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 「介面大小拉桿」與所有浮動面板（FloatingPanelSize）之間唯一的權責入口：拉桿改變數值時呼叫
    /// SetGlobalScale，訂閱的每個 FloatingPanelSize 收到 OnGlobalScaleChanged 就把這個倍率乘進自己
    /// 原本的尺寸計算裡。比照 GameWorldDragCoordinator 的角色分工——這裡只負責轉發數值，不認識任何
    /// 具體面板，FloatingPanelSize 也不需要知道是誰在改變全域倍率。
    /// </summary>
    public sealed class GlobalPanelScaleBroadcaster : MonoBehaviour
    {
        [SerializeField, Tooltip("全域倍率下限，跟單一面板滾輪縮放的下限維持一致")]
        private float _minScale = 0.6f;

        [SerializeField, Tooltip("全域倍率上限，跟單一面板滾輪縮放的上限維持一致")]
        private float _maxScale = 1.6f;

        private float _currentScale = 1f;

        /// <summary>目前的全域倍率。</summary>
        public float CurrentScale => _currentScale;

        /// <summary>全域倍率改變時廣播，帶出縮放後的絕對倍率（不是差量）。</summary>
        public event Action<float> OnGlobalScaleChanged;

        /// <summary>套用一個新的全域倍率（例如拉桿的 OnValueChanged 呼叫這裡），會被夾在上下限之間。</summary>
        public void SetGlobalScale(float scale)
        {
            _currentScale = Mathf.Clamp(scale, _minScale, _maxScale);
            OnGlobalScaleChanged?.Invoke(_currentScale);
        }
    }
}
