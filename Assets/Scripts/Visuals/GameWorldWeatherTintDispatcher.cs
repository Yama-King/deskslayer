using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;
using DeskSlayer.Weather;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 把 SpriteWeatherTintDispatcher 對 Player/Enemy 使用的染色手法（訂閱 WeatherService.OnWeatherChanged，
    /// 直接把 WeatherModifierData.TintColor 指派給渲染元件的顏色，DOTween 過渡）擴大套用到整個
    /// GameWorldRoot 底下的環境渲染物件，不額外疊一張半透明遮罩 Sprite。範圍查找比照 PauseWorldDimmer
    /// 用 GetComponentsInChildren 找出 Tilemap／SpriteRenderer，差別是這裡用 DOTween 平滑過渡而不是
    /// 瞬間切換。GameWorldRoot 底下不包含 Player/Enemy（各自的 Sprite 由既有的 SpriteWeatherTintDispatcher
    /// 負責），天然不會重複染色；UI 用 CanvasRenderer，這裡的查詢也天生找不到。
    /// </summary>
    public sealed class GameWorldWeatherTintDispatcher : MonoBehaviour
    {
        [SerializeField]
        private WeatherService _weatherService;

        [SerializeField]
        private Transform _gameWorldRoot;

        [SerializeField, Min(0f), Tooltip("色調切換的過渡時長（秒），需與 SpriteWeatherTintDispatcher 的設定值一致，確保跟角色/敵人染色同步變化")]
        private float _tintTransitionDuration = 0.6f;

        private readonly Dictionary<Tilemap, float> _tilemapOriginalAlphas = new Dictionary<Tilemap, float>();
        private readonly Dictionary<SpriteRenderer, float> _spriteOriginalAlphas = new Dictionary<SpriteRenderer, float>();
        private readonly List<Tween> _activeTweens = new List<Tween>();

        private void Start()
        {
            if (_gameWorldRoot == null || _weatherService == null)
            {
                Debug.LogWarning("[GameWorldWeatherTintDispatcher] 尚未指派 GameWorldRoot 或 WeatherService，無法套用天氣染色");
                return;
            }

            // 只覆蓋 RGB、保留每個物件原本的 Alpha——RainTilemap/SnowTilemap/ThunderstormTilemap
            // 這類天氣特效 Tile 本身就刻意畫成半透明（例如 0.471），直接整個蓋成 TintColor 的
            // Alpha=1 會讓半透明效果消失，不是預期行為。
            foreach (Tilemap tilemap in _gameWorldRoot.GetComponentsInChildren<Tilemap>(true))
            {
                _tilemapOriginalAlphas[tilemap] = tilemap.color.a;
            }

            foreach (SpriteRenderer spriteRenderer in _gameWorldRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                _spriteOriginalAlphas[spriteRenderer] = spriteRenderer.color.a;
            }

            ApplyTint(_weatherService.CurrentModifier.TintColor, immediate: true);
            _weatherService.OnWeatherChanged += HandleWeatherChanged;
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
            KillActiveTweens();
        }

        private void HandleWeatherChanged(WeatherModifierData modifier)
        {
            ApplyTint(modifier.TintColor, immediate: false);
        }

        private void ApplyTint(Color tintColor, bool immediate)
        {
            KillActiveTweens();

            foreach (KeyValuePair<Tilemap, float> entry in _tilemapOriginalAlphas)
            {
                Tilemap tilemap = entry.Key;
                if (tilemap == null)
                {
                    continue;
                }

                Color target = new Color(tintColor.r, tintColor.g, tintColor.b, entry.Value);

                if (immediate)
                {
                    tilemap.color = target;
                }
                else
                {
                    _activeTweens.Add(DOTween.To(() => tilemap.color, c => tilemap.color = c, target, _tintTransitionDuration));
                }
            }

            foreach (KeyValuePair<SpriteRenderer, float> entry in _spriteOriginalAlphas)
            {
                SpriteRenderer spriteRenderer = entry.Key;
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color target = new Color(tintColor.r, tintColor.g, tintColor.b, entry.Value);

                if (immediate)
                {
                    spriteRenderer.color = target;
                }
                else
                {
                    _activeTweens.Add(spriteRenderer.DOColor(target, _tintTransitionDuration));
                }
            }
        }

        private void KillActiveTweens()
        {
            foreach (Tween tween in _activeTweens)
            {
                tween?.Kill();
            }
            _activeTweens.Clear();
        }
    }
}
