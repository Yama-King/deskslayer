using System;
using DG.Tweening;
using UnityEngine;
using DeskSlayer.Enemy;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱同一個 GameObject 上 EnemyController 的 OnDeath 事件，先逐格播放完整段死亡
    /// 序列幀，播畢後才透過 DOTween 驅動 MaterialPropertyBlock 的 _DissolveAmount 播放溶解效果，
    /// 動畫播畢通知外部（EnemyRotationManager）並自行銷毀。共用材質不建立 Material Instance。
    /// 死亡序列幀本身是離散的貼圖切換（不是數值插值），因此比照 PooledSpriteAnimationItem 用 Update()
    /// 逐格步進，不是 CLAUDE.md 要求「數值動畫一律用 DOTween」規範的對象——那條規則管的是位移/縮放/
    /// 數值跳動這類需要插值的 Tween，溶解效果本身才是那類數值動畫，因此溶解仍然用 DOTween 驅動。
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyDeathVisualDispatcher : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [SerializeField]
        private Animator _animator;

        [SerializeField, Tooltip("死亡序列幀，死亡當下依序播放完畢後才開始播放溶解效果")]
        private Sprite[] _deathFrames;

        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

        private EnemyController _enemyController;
        private MaterialPropertyBlock _propertyBlock;

        private bool _isPlayingDeathFrames;
        private int _currentDeathFrameIndex;
        private float _deathFrameElapsed;

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

        private void Update()
        {
            if (!_isPlayingDeathFrames)
            {
                return;
            }

            float frameDuration = Mathf.Max(_enemyController.EnemyData.DeathFrameDuration, 0.001f);
            _deathFrameElapsed += Time.deltaTime;

            // 用 while 而非 if 逐格步進，避免低幀率時單次 Update 的 deltaTime 一次跨過多個幀間隔。
            while (_isPlayingDeathFrames && _deathFrameElapsed >= frameDuration)
            {
                _deathFrameElapsed -= frameDuration;
                _currentDeathFrameIndex++;

                if (_currentDeathFrameIndex >= _deathFrames.Length)
                {
                    _isPlayingDeathFrames = false;
                    StartDissolve();
                    break;
                }

                _spriteRenderer.sprite = _deathFrames[_currentDeathFrameIndex];
            }
        }

        private void HandleDeath()
        {
            _animator.enabled = false;

            if (_deathFrames == null || _deathFrames.Length == 0)
            {
                Debug.LogWarning($"[EnemyDeathVisualDispatcher] {name} 未指定 _deathFrames，跳過死亡序列幀直接播放溶解效果", this);
                StartDissolve();
                return;
            }

            _currentDeathFrameIndex = 0;
            _deathFrameElapsed = 0f;
            _isPlayingDeathFrames = true;
            _spriteRenderer.sprite = _deathFrames[0];
        }

        private void StartDissolve()
        {
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
