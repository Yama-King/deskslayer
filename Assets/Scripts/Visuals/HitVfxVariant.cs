using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 單一命中特效動畫變體的幀序列。同一個 HitVfxSetSO 底下可以有多組變體，
    /// 每次觸發隨機挑一組播放，避免同一種特效每次看起來都一模一樣。
    /// </summary>
    [System.Serializable]
    public sealed class HitVfxVariant
    {
        [SerializeField, Tooltip("勾選時才會被 HitVfxSetSO.GetRandomVariant() 抽中；取消勾選可以暫時停用某一組變體，" +
            "不用刪除素材或搬動陣列順序，方便看過實際演出效果後自行取捨")]
        private bool _enabled = true;

        [SerializeField, Tooltip("依序播放的幀序列，順序需與素材切割後的幀序一致（由 HitVfxSpriteSlicer 切割產出）")]
        private Sprite[] _frames;

        /// <summary>是否啟用此變體，停用的變體不會被隨機抽中。</summary>
        public bool IsEnabled => _enabled;

        /// <summary>依序播放的幀序列。</summary>
        public Sprite[] Frames => _frames;
    }
}
