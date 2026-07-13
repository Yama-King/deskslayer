using DG.Tweening;
using UnityEngine;
using DeskSlayer.Weather;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱 WeatherService.OnWeatherChanged，透過 DOTween 把 SpriteRenderer.color
    /// 平滑過渡到目前天氣分類對應的色調。只套用在角色/敵人自己的 Sprite 上，刻意不使用全螢幕後製
    /// 效果，避免連同色鍵去背的背景區域一起被染色（本專案桌面透明視窗採二元色鍵去背，全螢幕濾鏡
    /// 會破壞去背效果）。
    /// 靜態場景物件（例如 Player）可直接在 Inspector 指派 _weatherService，由 Start() 自行初始化；
    /// 動態生成物件（例如 Enemy，透過 EnemyRotationManager Instantiate 生成）在 Awake/OnEnable 執行
    /// 當下外部依賴尚未注入，因此改由外部在 Instantiate 後呼叫 Initialize() 顯式注入，
    /// 比照 CombatDispatcher.SetTarget() 的依賴注入方式。
    /// </summary>
    public sealed class SpriteWeatherTintDispatcher : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [SerializeField, Tooltip("靜態場景物件可直接指派；動態生成物件請留空，改由外部呼叫 Initialize() 注入")]
        private WeatherService _weatherService;

        [SerializeField, Min(0f), Tooltip("色調切換的過渡時長（秒）")]
        private float _tintTransitionDuration = 0.6f;

        private void Start()
        {
            if (_weatherService != null)
            {
                Initialize(_weatherService);
            }
        }

        private void OnDisable()
        {
            if (_weatherService != null)
            {
                _weatherService.OnWeatherChanged -= HandleWeatherChanged;
            }
        }

        private void OnDestroy()
        {
            _spriteRenderer.DOKill();
        }

        /// <summary>
        /// 指派天氣服務來源並訂閱事件，立即套用目前天氣色調（不等待下一次事件），
        /// 供 EnemyRotationManager 在 Instantiate 動態敵人後呼叫。
        /// </summary>
        public void Initialize(WeatherService weatherService)
        {
            if (_weatherService != null)
            {
                _weatherService.OnWeatherChanged -= HandleWeatherChanged;
            }

            _weatherService = weatherService;

            if (_weatherService == null)
            {
                return;
            }

            ApplyTint(_weatherService.CurrentModifier.TintColor, immediate: true);
            _weatherService.OnWeatherChanged += HandleWeatherChanged;
        }

        private void HandleWeatherChanged(WeatherModifierData modifier)
        {
            ApplyTint(modifier.TintColor, immediate: false);
        }

        private void ApplyTint(Color tintColor, bool immediate)
        {
            _spriteRenderer.DOKill();

            if (immediate)
            {
                _spriteRenderer.color = tintColor;
            }
            else
            {
                _spriteRenderer.DOColor(tintColor, _tintTransitionDuration);
            }
        }
    }
}
