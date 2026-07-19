using UnityEngine;
using DeskSlayer.Combat;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 表現層橋接元件：訂閱重攻擊觸發事件，在觸發當下擷取武器 SpriteRenderer 的貼圖與世界座標
    /// Transform 快照，交給 WeaponAfterimagePool 生成一張殘影。純粹轉發表現效果，不參與任何戰鬥
    /// 數值運算，與蓄力發光/抖動（HeavyChargeVisualDispatcher）是各自獨立的視覺回饋模組，只共用
    /// 「訂閱同一個 TypingEnergySystem.OnHeavyAttackTriggered 事件」這件事，不共用元件或狀態。
    /// 不需要比照 HeavyChargeVisualDispatcher 追蹤「目前是哪一個 HeavyWeaponSO 實例」——
    /// OnHeavyAttackTriggered 只會在目前裝備重武器時觸發，殘影只需要當下這一刻武器 SpriteRenderer
    /// 實際顯示的畫面快照，與是哪個稀有度變體無關。
    /// </summary>
    public sealed class WeaponAfterimageDispatcher : MonoBehaviour
    {
        [SerializeField]
        private TypingEnergySystem _typingEnergySystem;

        [SerializeField]
        private WeaponAfterimagePool _afterimagePool;

        [SerializeField]
        private SpriteRenderer _heavyWeaponSpriteRenderer;

        [SerializeField, Min(0.01f), Tooltip("單一殘影從生成到完全消失的秒數")]
        private float _afterimageLifetime = 0.35f;

        [SerializeField, Tooltip("存活期間 Alpha 衰減節奏：橫軸為存活進度 0（剛生成）~1（存活時間結束），" +
            "縱軸為 Alpha 倍率（實際 Alpha = 這裡取樣的值 x 初始 Alpha）。預設前段清晰、後段快速消散")]
        private AnimationCurve _alphaDecayCurve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.4f, 0.85f), new Keyframe(1f, 0f));

        [SerializeField, Range(0f, 1f), Tooltip("殘影生成當下的起始透明度")]
        private float _initialAlpha = 0.6f;

        private void OnEnable()
        {
            _typingEnergySystem.OnHeavyAttackTriggered += HandleHeavyAttackTriggered;
        }

        private void OnDisable()
        {
            _typingEnergySystem.OnHeavyAttackTriggered -= HandleHeavyAttackTriggered;
        }

        private void HandleHeavyAttackTriggered(HeavyWeaponSO weapon)
        {
            Transform weaponTransform = _heavyWeaponSpriteRenderer.transform;
            _afterimagePool.TrySpawn(
                _heavyWeaponSpriteRenderer.sprite,
                weaponTransform.position,
                weaponTransform.rotation,
                weaponTransform.lossyScale,
                _afterimageLifetime,
                _alphaDecayCurve,
                _initialAlpha);
        }
    }
}
