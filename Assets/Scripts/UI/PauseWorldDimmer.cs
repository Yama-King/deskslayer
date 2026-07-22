using System.Collections.Generic;
using DeskSlayer.GameState;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 暫停時把 GameWorldRoot 底下「實際有畫面內容」的物件（Tilemap／SpriteRenderer）逐一調暗，
    /// 取代原本一整塊矩形黑幕——每個物件變暗的範圍精準貼合自己的 sprite/tile 形狀，不規則邊緣
    /// 天然不需要另外裁切，因為根本沒有另外疊一層遮罩圖形，只是把「已經在畫的像素」調暗而已。
    /// 只找 UnityEngine.Tilemaps.Tilemap／SpriteRenderer 這兩種 2D 渲染元件，Canvas UI（DesktopMenuBar、
    /// PauseCanvas 自己）用的是 CanvasRenderer，天生不會被這裡的查詢找到，不需要另外排除。
    /// </summary>
    public sealed class PauseWorldDimmer : MonoBehaviour
    {
        [SerializeField]
        private Transform _gameWorldRoot;

        [SerializeField, Range(0f, 1f), Tooltip("暫停時 RGB 各分量乘上的係數，越小越暗；不動 Alpha，維持原本的透明/不透明狀態")]
        private float _dimMultiplier = 0.35f;

        private readonly Dictionary<Tilemap, Color> _originalTilemapColors = new Dictionary<Tilemap, Color>();
        private readonly Dictionary<SpriteRenderer, Color> _originalSpriteColors = new Dictionary<SpriteRenderer, Color>();

        private void OnEnable()
        {
            SubscribeToGameStateMachine();
        }

        private void Start()
        {
            // 比照 PauseToggleButton 的雙重訂閱保護：GameStateMachine.Instance 要等它自己的 Awake()
            // 跑完才會賦值，OnEnable 當下有機率還沒賦值，Start 補訂一次確保不漏訂閱。
            SubscribeToGameStateMachine();
        }

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
            {
                GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            }

            // 元件停用時不可讓世界卡在變暗狀態。
            RestoreColors();
        }

        private void SubscribeToGameStateMachine()
        {
            if (GameStateMachine.Instance == null)
            {
                return;
            }

            GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            GameStateMachine.Instance.OnGamePhaseChanged += HandleGamePhaseChanged;
        }

        private void HandleGamePhaseChanged(GamePhase? previous, GamePhase current)
        {
            if (current == GamePhase.Paused)
            {
                ApplyDim();
            }
            else
            {
                RestoreColors();
            }
        }

        private void ApplyDim()
        {
            // 先還原一次，避免重複進入 Paused（理論上不會發生，但保險起見不疊加調暗）把顏色越乘越暗。
            RestoreColors();

            if (_gameWorldRoot == null)
            {
                return;
            }

            foreach (Tilemap tilemap in _gameWorldRoot.GetComponentsInChildren<Tilemap>(false))
            {
                _originalTilemapColors[tilemap] = tilemap.color;
                tilemap.color = DimColor(tilemap.color);
            }

            foreach (SpriteRenderer spriteRenderer in _gameWorldRoot.GetComponentsInChildren<SpriteRenderer>(false))
            {
                _originalSpriteColors[spriteRenderer] = spriteRenderer.color;
                spriteRenderer.color = DimColor(spriteRenderer.color);
            }
        }

        private void RestoreColors()
        {
            foreach (KeyValuePair<Tilemap, Color> entry in _originalTilemapColors)
            {
                if (entry.Key != null)
                {
                    entry.Key.color = entry.Value;
                }
            }
            _originalTilemapColors.Clear();

            foreach (KeyValuePair<SpriteRenderer, Color> entry in _originalSpriteColors)
            {
                if (entry.Key != null)
                {
                    entry.Key.color = entry.Value;
                }
            }
            _originalSpriteColors.Clear();
        }

        private Color DimColor(Color original)
        {
            return new Color(original.r * _dimMultiplier, original.g * _dimMultiplier, original.b * _dimMultiplier, original.a);
        }
    }
}
