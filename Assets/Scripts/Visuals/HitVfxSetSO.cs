using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 單一種命中特效（輕攻擊粒子／重攻擊粒子／敵人受擊特效）的資料容器。
    /// 三種特效在行為上完全相同、只有素材與參數不同，因此共用同一份 ScriptableObject 定義，
    /// 各自建立獨立的資產實例即可，不需要為每種特效各寫一份重複的資料類別。
    /// 之後補動畫素材或調整播放節奏，直接在 Inspector 上調整此資產即可生效，不需要改程式碼。
    /// </summary>
    [CreateAssetMenu(fileName = "NewHitVfxSet", menuName = "DeskSlayer/Visuals/Hit Vfx Set", order = 0)]
    public sealed class HitVfxSetSO : ScriptableObject
    {
        [SerializeField, Tooltip("候選動畫變體清單，每次觸發隨機挑一組播放；建議 2 組以上，避免同一種特效每次視覺重複")]
        private HitVfxVariant[] _variants;

        [SerializeField, Min(0.001f), Tooltip("單幀播放時間（秒）。實際總播放時長 = 該次挑中的變體幀數 x 此值，" +
            "各變體幀數不同時各自依自己的幀數計算，不用固定計時器猜測")]
        private float _frameDuration = 0.05f;

        [SerializeField, Min(1), Tooltip("此特效類型的 Object Pool 初始容量，啟動時一次建立完畢，之後不再動態擴增")]
        private int _poolCapacity = 8;

        [SerializeField, Min(0.01f), Tooltip("播放時額外套用的縮放係數，1 代表使用素材原始大小")]
        private float _scaleMultiplier = 1f;

        [SerializeField, Tooltip("套用在 SpriteRenderer 上的顏色 Tint，預設白色代表不套用額外染色")]
        private Color _tint = Color.white;

        /// <summary>單幀播放時間（秒）。</summary>
        public float FrameDuration => _frameDuration;

        /// <summary>此特效類型的 Object Pool 初始容量。</summary>
        public int PoolCapacity => _poolCapacity;

        /// <summary>播放時額外套用的縮放係數。</summary>
        public float ScaleMultiplier => _scaleMultiplier;

        /// <summary>套用在 SpriteRenderer 上的顏色 Tint。</summary>
        public Color Tint => _tint;

        /// <summary>
        /// 從變體清單中，已勾選 Enabled 的變體裡隨機挑一組；清單為空或全數停用時回傳 null，
        /// 呼叫端需自行判斷是否要跳過本次播放。輕攻擊每次按鍵都可能觸發，這裡刻意用兩輪陣列掃描
        /// 取代 LINQ 篩選，避免每次挑選都配置暫存陣列造成不必要的 GC 壓力。
        /// </summary>
        public HitVfxVariant GetRandomVariant()
        {
            if (_variants == null || _variants.Length == 0)
            {
                return null;
            }

            int enabledCount = 0;
            for (int i = 0; i < _variants.Length; i++)
            {
                if (_variants[i] != null && _variants[i].IsEnabled)
                {
                    enabledCount++;
                }
            }

            if (enabledCount == 0)
            {
                return null;
            }

            int targetIndex = Random.Range(0, enabledCount);
            int seen = 0;
            for (int i = 0; i < _variants.Length; i++)
            {
                if (_variants[i] == null || !_variants[i].IsEnabled)
                {
                    continue;
                }

                if (seen == targetIndex)
                {
                    return _variants[i];
                }

                seen++;
            }

            return null;
        }
    }
}
