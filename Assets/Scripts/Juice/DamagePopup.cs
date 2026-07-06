using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DeskSlayer.Juice
{
    /// <summary>
    /// 傷害飄字：顯示一次傷害數字，用 DOTween 做「往上飄移＋淡出」，動畫結束後透過 OnComplete
    /// 回呼自行銷毀，不額外寫計時器邏輯。兩個 Tween 都設定 SetUpdate(true)（忽略 Time.timeScale），
    /// 確保 Hit-stop 期間飄字仍能正常繼續播放，不會被頓幀凍結。
    /// 淡出改用 DOTween.To 直接補間 alpha，而非 TMP_Text 的 DOFade 擴充方法，
    /// 避免依賴 DOTween Setup 精靈裡「TextMeshPro」選用模組是否有被勾選。
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class DamagePopup : MonoBehaviour
    {
        [SerializeField, Tooltip("往上飄移的總距離（世界座標單位）")]
        private float _floatDistance = 1.2f;

        [SerializeField, Tooltip("飄移＋淡出動畫的總時長（秒）")]
        private float _duration = 0.8f;

        private TextMeshPro _label;

        private void Awake()
        {
            _label = GetComponent<TextMeshPro>();
        }

        /// <summary>顯示本次傷害數字並開始飄移淡出動畫。</summary>
        public void Show(int damage)
        {
            _label.text = damage.ToString();

            Color startColor = _label.color;
            startColor.a = 1f;
            _label.color = startColor;

            transform.DOMoveY(transform.position.y + _floatDistance, _duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

            DOTween.To(() => _label.color.a, SetAlpha, 0f, _duration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() => Destroy(gameObject));
        }

        private void SetAlpha(float alpha)
        {
            Color color = _label.color;
            color.a = alpha;
            _label.color = color;
        }
    }
}
