using UnityEngine;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 風格原型判定門檻的可調參數：平衡型帶寬範圍、四象限分界值。這類判定手感需要實際試玩多組
    /// 分數組合才能收斂，因此做成資料資產而非寫死常數，供 Editor 內直接調整。
    /// </summary>
    [CreateAssetMenu(fileName = "StyleArchetypeThresholdConfig", menuName = "DeskSlayer/ShareCard/Style Archetype Threshold Config", order = 3)]
    public sealed class StyleArchetypeThresholdConfigSO : ScriptableObject
    {
        [SerializeField, Tooltip("平衡型帶寬下限，兩軸分數需同時 >= 此值且 <= 上限才判定為平衡型")]
        private float _balancedBandwidthMin = 40f;

        [SerializeField, Tooltip("平衡型帶寬上限")]
        private float _balancedBandwidthMax = 60f;

        [SerializeField, Tooltip("四象限判定的分界值，>= 此值視為高、< 此值視為低")]
        private float _quadrantSplitValue = 50f;

        /// <summary>平衡型帶寬下限。</summary>
        public float BalancedBandwidthMin => _balancedBandwidthMin;

        /// <summary>平衡型帶寬上限。</summary>
        public float BalancedBandwidthMax => _balancedBandwidthMax;

        /// <summary>四象限判定的分界值。</summary>
        public float QuadrantSplitValue => _quadrantSplitValue;
    }
}
