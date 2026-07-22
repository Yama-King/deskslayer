using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 敵人血條的純視覺元件：不認識 EnemyController，只透過公開方法 UpdateHealth 接收血量資料，
    /// 資料如何來完全交給 EnemyHealthBarDispatcher 負責轉發。作為敵人 Prefab 的子物件，
    /// 位置跟隨與滿血重置皆隨父物件的生成/銷毀自然達成，這裡不寫任何追蹤或重置邏輯。
    /// 血條預設不可見，每次收到血量變化就淡入顯示、停留一段時間後淡出；停留期間若又收到
    /// 新的血量變化，則直接 Kill 掉進行中的淡入淡出序列並重新開始，讓畫面立即回到完全可見。
    ///
    /// 血條美術來源是 HealthBar1/2/3（滿血）／HealthBar5/6/7（空血）這組左端蓋/可平鋪中段/
    /// 右端蓋 3 張拆件圖，但套用方式改回標準的 Image.fillAmount 做法：左右端蓋 + 中段預先在
    /// Editor 外用 Python 依 8px/8px/184px 的比例合成成一張 200x14 的單一貼圖，Fill/Background
    /// 各自只是一張套用該合成貼圖的 Image，不是子物件組合，填充才能直接用 Unity 內建
    /// Image.Type.Filled（Horizontal）搭配 DOFillAmount 補間，屬於業界最常見的血條做法。
    /// </summary>
    public sealed class EnemyHealthBarView : MonoBehaviour
    {
        [SerializeField]
        private EnemyHealthBarConfigSO _config;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private Image _fillImage;

        [SerializeField]
        private Image _backgroundImage;

        private Sequence _visibilitySequence;
        private Tween _fillTween;

        private void Awake()
        {
            // 敵人翻面朝向是透過父物件 Y 軸旋轉 180 度達成（非 Sprite Flip），
            // 這裡固定使用世界座標朝向，讓血條視覺永遠正面朝向鏡頭、不會跟著鏡像。
            transform.rotation = Quaternion.identity;

            _canvasGroup.alpha = 0f;

            if (_fillImage != null)
            {
                _fillImage.fillAmount = 1f;
            }

            if (_config != null)
            {
                if (_fillImage != null)
                {
                    _fillImage.color = _config.FillColor;
                }

                if (_backgroundImage != null)
                {
                    _backgroundImage.color = _config.BackgroundColor;
                }
            }
        }

        /// <summary>套用一次血量變化：填充值平滑補間到新比例，並淡入顯示血條、重新排定淡出時間。</summary>
        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            float ratio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

            _fillTween?.Kill();
            _fillTween = _fillImage.DOFillAmount(ratio, _config.FillTweenDuration).SetEase(Ease.OutQuad);

            _visibilitySequence?.Kill();
            _visibilitySequence = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1f, _config.FadeInDuration))
                .AppendInterval(_config.HoldDuration)
                .Append(_canvasGroup.DOFade(0f, _config.FadeOutDuration));
        }
    }
}
