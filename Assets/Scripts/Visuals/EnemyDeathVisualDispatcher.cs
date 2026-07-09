using System;
using DG.Tweening;
using UnityEngine;
using DeskSlayer.Enemy;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱同一個 GameObject 上 EnemyController 的 OnDeath 事件，
    /// 換上死亡凍結畫面後，透過 DOTween 驅動 MaterialPropertyBlock 的 _DissolveAmount 播放溶解效果，
    /// 動畫播畢通知外部（EnemyRotationManager）並自行銷毀。共用材質不建立 Material Instance。
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyDeathVisualDispatcher : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [SerializeField]
        private Animator _animator;

        [SerializeField, Tooltip("死亡瞬間凍結顯示的靜態畫面（各造型 Death 序列幀的第一格）")]
        private Sprite _deathSprite;

        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

        private EnemyController _enemyController;
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>溶解動畫播放完成、GameObject 即將被銷毀前發出一次，供 EnemyRotationManager 接續生成下一隻敵人。</summary>
        public event Action OnDissolveComplete;

        private void Awake()
        {
            _enemyController = GetComponent<EnemyController>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _enemyController.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _enemyController.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            _animator.enabled = false;
            _spriteRenderer.sprite = _deathSprite;

            float amount = 0f;
            float dissolveDuration = _enemyController.EnemyData.DissolveDuration;
            DOTween.To(() => amount, v => { amount = v; ApplyDissolveAmount(v); }, 1f, dissolveDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    OnDissolveComplete?.Invoke();
                    Destroy(gameObject);
                });
        }

        private void ApplyDissolveAmount(float amount)
        {
            _spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(DissolveAmountId, amount);
            _spriteRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
