using UnityEngine;

namespace DeskSlayer.Audio
{
    /// <summary>
    /// 單一音效事件的資料容器。clips 支援多個變化版本，播放時隨機挑選一個，
    /// 搭配音量／音高隨機範圍避免重複播放時聲音過於單調。
    /// 之後補音效檔案時，直接把 AudioClip 拖進 clips 陣列即可生效，不需改程式碼。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSound", menuName = "DeskSlayer/Audio/Sound Data", order = 0)]
    public sealed class SoundDataSO : ScriptableObject
    {
        [SerializeField, Tooltip("候選音效片段，播放時隨機挑選一個；留空則安靜跳過")]
        private AudioClip[] _clips;

        [SerializeField, Range(0f, 1f)]
        private float _minVolume = 0.8f;

        [SerializeField, Range(0f, 1f)]
        private float _maxVolume = 1f;

        [SerializeField]
        private float _minPitch = 0.95f;

        [SerializeField]
        private float _maxPitch = 1.05f;

        /// <summary>從 clips 陣列隨機挑一個片段；陣列為空時回傳 null。</summary>
        public AudioClip GetRandomClip()
        {
            if (_clips == null || _clips.Length == 0)
            {
                return null;
            }

            return _clips[Random.Range(0, _clips.Length)];
        }

        /// <summary>在設定範圍內隨機取一個音量值。</summary>
        public float GetRandomVolume()
        {
            return Random.Range(_minVolume, _maxVolume);
        }

        /// <summary>在設定範圍內隨機取一個音高值。</summary>
        public float GetRandomPitch()
        {
            return Random.Range(_minPitch, _maxPitch);
        }
    }
}
