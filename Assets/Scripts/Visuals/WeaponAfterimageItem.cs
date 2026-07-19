using System;
using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 單一殘影物件的存活計時與視覺呈現。存活期間每幀依 Alpha 衰減曲線更新 MaterialPropertyBlock，
    /// 停用狀態下 Update 直接 return，不做任何運算。不認識池子的內部資料結構，衰減完成時只透過
    /// Activate 當下傳入的回呼通知「可以回收了」，由呼叫方（WeaponAfterimagePool）決定如何處理。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WeaponAfterimageItem : MonoBehaviour
    {
        private static readonly int AfterimageAlphaId = Shader.PropertyToID("_AfterimageAlpha");

        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _propertyBlock;

        private Action<WeaponAfterimageItem> _onLifetimeEnded;
        private AnimationCurve _alphaDecayCurve;
        private float _initialAlpha;
        private float _lifetime;
        private float _elapsed;
        private bool _isActive;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// 從池子租借時呼叫：重置貼圖、Transform 與衰減進度後立即啟用。position/rotation/worldScale
        /// 皆為世界座標下的快照值，本物件會自行換算成目前父階層下對應的 localScale。
        /// </summary>
        public void Activate(Sprite sprite, Vector3 worldPosition, Quaternion worldRotation, Vector3 worldScale,
            float lifetime, AnimationCurve alphaDecayCurve, float initialAlpha, Action<WeaponAfterimageItem> onLifetimeEnded)
        {
            _spriteRenderer.sprite = sprite;
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            transform.localScale = ToLocalScale(worldScale);

            _lifetime = Mathf.Max(lifetime, 0.01f);
            _alphaDecayCurve = alphaDecayCurve;
            _initialAlpha = initialAlpha;
            _onLifetimeEnded = onLifetimeEnded;
            _elapsed = 0f;
            _isActive = true;

            ApplyAlpha(EvaluateAlpha(0f));
            gameObject.SetActive(true);
        }

        /// <summary>歸還池子前呼叫：停止計時並停用，不執行 Destroy。</summary>
        public void Deactivate()
        {
            _isActive = false;
            _onLifetimeEnded = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            // 停用狀態（池中閒置）直接跳過，不做任何每幀運算。
            if (!_isActive)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifetime);
            ApplyAlpha(EvaluateAlpha(t));

            if (t >= 1f)
            {
                _isActive = false;
                _onLifetimeEnded?.Invoke(this);
            }
        }

        private float EvaluateAlpha(float t)
        {
            return _alphaDecayCurve.Evaluate(t) * _initialAlpha;
        }

        private void ApplyAlpha(float alpha)
        {
            _spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(AfterimageAlphaId, alpha);
            _spriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// 將世界座標縮放換算成本物件在目前父階層下應設定的 localScale，避免池子容器本身若非
        /// 單位縮放（非 1,1,1）導致殘影大小跟武器實際尺寸不一致。
        /// </summary>
        private Vector3 ToLocalScale(Vector3 worldScale)
        {
            Vector3 parentLossyScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            return new Vector3(
                parentLossyScale.x != 0f ? worldScale.x / parentLossyScale.x : worldScale.x,
                parentLossyScale.y != 0f ? worldScale.y / parentLossyScale.y : worldScale.y,
                parentLossyScale.z != 0f ? worldScale.z / parentLossyScale.z : worldScale.z);
        }
    }
}
