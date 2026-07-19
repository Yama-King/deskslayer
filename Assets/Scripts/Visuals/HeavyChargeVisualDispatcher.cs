using DG.Tweening;
using UnityEngine;
using DeskSlayer.Combat;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱目前裝備武器的重攻擊蓄力進度，驅動角色的蓄力發光（HLSL Shader + MaterialPropertyBlock）
    /// 與抖動（每次有效按鍵觸發一次脈衝式位移，隨蓄力進度增強）兩種視覺回饋。
    /// 重武器有多種稀有度變體（各自是獨立的 HeavyWeaponSO 資產、各自持有獨立的蓄力進度），因此不寫死
    /// 追蹤單一武器資產，改為訂閱 WeaponSwitcher.OnWeaponEquipped 動態切換要監聽哪一個 HeavyWeaponSO。
    /// 抖動位移只寫入 _visualOffsetRoot（Player 的父物件）的 localPosition，不寫入 Player 自己的 Transform，
    /// 避免與拖曳系統（操作 GameWorldRoot）、未來可能新增的受擊 hit-stop/punch 效果共用同一個欄位互相覆蓋。
    /// </summary>
    public sealed class HeavyChargeVisualDispatcher : MonoBehaviour
    {
        [SerializeField]
        private TypingEnergySystem _typingEnergySystem;

        [SerializeField]
        private WeaponSwitcher _weaponSwitcher;

        [SerializeField]
        private SpriteRenderer _bodySpriteRenderer;

        [SerializeField]
        private SpriteRenderer _lightWeaponSpriteRenderer;

        [SerializeField]
        private SpriteRenderer _heavyWeaponSpriteRenderer;

        [SerializeField, Tooltip("只承載本元件程序化視覺位移的專職子物件（Player 的父物件），不可與其他系統共用同一個 Transform 欄位")]
        private Transform _visualOffsetRoot;

        [SerializeField, Tooltip("蓄力進度 0~1 對應的發光強度（0~1），預設起點平緩、收尾陡峭，可依美術需求調整")]
        private AnimationCurve _glowCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.5f, 0.2f), new Keyframe(0.8f, 0.5f), new Keyframe(1f, 1f));

        [SerializeField, Tooltip("蓄力進度 0~1 對應的單次按鍵抖動幅度比例（0~1），數值越高代表當下這一下按鍵震得越大力，預設前 70% 幾乎不抖、收尾快速攀升，可依美術需求調整")]
        private AnimationCurve _jitterCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.7f, 0.1f), new Keyframe(0.9f, 0.4f), new Keyframe(1f, 1f));

        [SerializeField, Min(0f), Tooltip("抖動幅度比例為 1.0 時對應的最大位移量（Transform local 單位）")]
        private float _jitterMaxDistance = 0.15f;

        [SerializeField, Tooltip("Perlin Noise 取樣頻率，只決定單次抖動震動的紋理/方向變化速度，數值越大抖動質感越生硬")]
        private float _noiseFrequency = 18f;

        [SerializeField, Min(0.01f), Tooltip("每次按鍵觸發的抖動脈衝，從當下峰值衰減回 0 所花的時間（秒）。下一次按鍵會直接打斷衰減、重新頂到新的峰值")]
        private float _jitterImpulseDecayDuration = 0.15f;

        [SerializeField, Min(0f), Tooltip("重攻擊觸發或中途停止蓄力時，發光強度平滑回到 0 所花的時間（秒）")]
        private float _glowSettleDuration = 0.15f;

        [SerializeField, Min(0f), Tooltip("重攻擊觸發或中途停止蓄力時，抖動偏移平滑回到原點所花的時間（秒）")]
        private float _jitterSettleDuration = 0.225f;

        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");

        private MaterialPropertyBlock _propertyBlock;
        private HeavyWeaponSO _trackedHeavyWeapon;
        private Vector3 _restPosition;
        private float _noiseSeed;
        private float _currentProgress;
        private float _lastGlowIntensity;
        private float _jitterImpulsePeak;
        private float _jitterImpulseElapsed;
        private bool _isActive;
        private Tween _glowTween;
        private Tween _jitterTween;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _restPosition = _visualOffsetRoot.localPosition;
            _noiseSeed = Random.Range(0f, 1000f);
        }

        private void OnEnable()
        {
            _weaponSwitcher.OnWeaponEquipped += HandleWeaponEquipped;
            _typingEnergySystem.OnHeavyAttackTriggered += HandleHeavyAttackTriggered;

            // OnDisable 會連同 _trackedHeavyWeapon 的訂閱一併清掉，重新啟用時（非僅止於第一次啟動）
            // 必須在這裡立刻補回目前裝備的追蹤，否則要等到下一次實際切換武器才會恢復追蹤。
            TrackWeapon(_weaponSwitcher.CurrentWeapon);
        }

        private void Start()
        {
            // WeaponSwitcher 的預設武器在自己的 Awake 指定，跨物件不保證 OnEnable 執行順序，
            // 這裡等所有物件的 Awake 都跑完後再同步一次目前裝備，確保能抓到場景一開始就裝備的重武器
            // （第一次啟動時 OnEnable 當下 WeaponSwitcher 可能還沒 Awake，CurrentWeapon 會是 null）。
            TrackWeapon(_weaponSwitcher.CurrentWeapon);
        }

        private void OnDisable()
        {
            _weaponSwitcher.OnWeaponEquipped -= HandleWeaponEquipped;
            _typingEnergySystem.OnHeavyAttackTriggered -= HandleHeavyAttackTriggered;

            if (_trackedHeavyWeapon != null)
            {
                _trackedHeavyWeapon.OnChargeProgressChanged -= HandleChargeProgressChanged;
                _trackedHeavyWeapon = null;
            }

            // 元件停用/物件銷毀前必須立即歸零，不可讓角色卡在偏移後的位置或殘留發光。
            KillTweens();
            _isActive = false;
            ApplyGlow(0f);
            _visualOffsetRoot.localPosition = _restPosition;
        }

        private void Update()
        {
            // 蓄力進度為 0 且未在收尾動畫中時直接跳過，收尾動畫由 DOTween 自己的更新迴圈驅動，不經過這裡。
            if (!_isActive)
            {
                return;
            }

            ApplyGlow(_glowCurve.Evaluate(_currentProgress));

            // 抖動幅度是「按一下頂到峰值、之後隨時間衰減回 0」的脈衝，不是隨蓄力進度連續跟隨；
            // 方向/紋理仍用連續 Perlin Noise 取樣，避免單次脈衝看起來像單純的直線彈回，維持顫動感。
            _jitterImpulseElapsed += Time.deltaTime;
            float decayT = Mathf.Clamp01(_jitterImpulseElapsed / _jitterImpulseDecayDuration);
            float amplitude = Mathf.Lerp(_jitterImpulsePeak, 0f, decayT);

            float noiseX = Mathf.PerlinNoise(_noiseSeed, Time.time * _noiseFrequency) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(_noiseSeed + 100f, Time.time * _noiseFrequency) * 2f - 1f;
            _visualOffsetRoot.localPosition = _restPosition + new Vector3(noiseX, noiseY, 0f) * amplitude;
        }

        private void HandleWeaponEquipped(WeaponDataSO weapon)
        {
            TrackWeapon(weapon);

            // 換裝當下，換掉的舊武器蓄力視覺已跟目前裝備無關（不論換到另一把重武器變體或輕武器），一律先歸零。
            BeginSettle();
        }

        /// <summary>
        /// 切換目前訂閱蓄力事件的武器。重武器有多種稀有度變體，各自是獨立的 HeavyWeaponSO 資產、
        /// 各自持有獨立的蓄力進度，因此每次裝備變更都要重新解析並訂閱正確的實例。
        /// </summary>
        private void TrackWeapon(WeaponDataSO weapon)
        {
            if (_trackedHeavyWeapon != null)
            {
                _trackedHeavyWeapon.OnChargeProgressChanged -= HandleChargeProgressChanged;
            }

            _trackedHeavyWeapon = weapon as HeavyWeaponSO;

            if (_trackedHeavyWeapon != null)
            {
                _trackedHeavyWeapon.OnChargeProgressChanged += HandleChargeProgressChanged;
            }
        }

        private void HandleChargeProgressChanged(float progress01)
        {
            _currentProgress = progress01;

            // 每次有效按鍵都重新頂一次抖動脈衝峰值（用當下最新進度換算，蓄力越久峰值越大），
            // 並歸零衰減計時器讓這一下重新從峰值開始衰減，達成「按一下抖一下、越按越大力」的效果。
            _jitterImpulsePeak = _jitterCurve.Evaluate(progress01) * _jitterMaxDistance;
            _jitterImpulseElapsed = 0f;

            if (_isActive)
            {
                return;
            }

            // 蓄力從 0 開始增加：發光與抖動同步啟動，若上一輪收尾動畫還在跑就立即中斷改為即時跟隨。
            _isActive = true;
            KillTweens();
        }

        private void HandleHeavyAttackTriggered(HeavyWeaponSO weapon)
        {
            if (weapon != _trackedHeavyWeapon)
            {
                return;
            }

            BeginSettle();
        }

        private void BeginSettle()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            KillTweens();

            float glowValue = _lastGlowIntensity;
            _glowTween = DOTween.To(() => glowValue, v =>
                {
                    glowValue = v;
                    ApplyGlow(v);
                }, 0f, _glowSettleDuration).SetEase(Ease.OutQuad);

            Vector3 offsetStart = _visualOffsetRoot.localPosition;
            float t = 0f;
            _jitterTween = DOTween.To(() => t, v =>
                {
                    t = v;
                    _visualOffsetRoot.localPosition = Vector3.LerpUnclamped(offsetStart, _restPosition, v);
                }, 1f, _jitterSettleDuration).SetEase(Ease.OutQuad);
        }

        private void KillTweens()
        {
            _glowTween?.Kill();
            _jitterTween?.Kill();
            _glowTween = null;
            _jitterTween = null;
        }

        private void ApplyGlow(float intensity)
        {
            _lastGlowIntensity = intensity;
            ApplyGlowToRenderer(_bodySpriteRenderer, intensity);
            ApplyGlowToRenderer(_lightWeaponSpriteRenderer, intensity);
            ApplyGlowToRenderer(_heavyWeaponSpriteRenderer, intensity);
        }

        private void ApplyGlowToRenderer(SpriteRenderer spriteRenderer, float intensity)
        {
            spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(GlowIntensityId, intensity);
            spriteRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
