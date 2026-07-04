using UnityEngine;

namespace DeskSlayer.Audio
{
    /// <summary>
    /// 純播放端：只認識 SoundDataSO 與 AudioSource，不認識任何遊戲邏輯或觸發來源。
    /// 內部維護一小組可重複借用的 AudioSource，避免逐次 Instantiate 造成 GC 壓力，
    /// 呼應本專案「背景常駐、CPU/記憶體佔用率極低」的核心要求。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        private const int SourcePoolSize = 6;

        /// <summary>簡易靜態存取入口，供各 Dispatcher 呼叫 PlaySound。</summary>
        public static AudioManager Instance { get; private set; }

        private AudioSource[] _sourcePool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSourcePool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void BuildSourcePool()
        {
            _sourcePool = new AudioSource[SourcePoolSize];
            for (int i = 0; i < SourcePoolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sourcePool[i] = source;
            }
        }

        /// <summary>
        /// 播放一個音效事件。clips 為空時安靜跳過（不報錯、不噴警告），
        /// 讓「先建架構、之後補檔案」的流程不需要改動任何程式碼。
        /// </summary>
        public void PlaySound(SoundDataSO data)
        {
            if (data == null)
            {
                return;
            }

            AudioClip clip = data.GetRandomClip();
            if (clip == null)
            {
                return;
            }

            AudioSource source = GetAvailableSource();
            if (source == null)
            {
                // 借用池全數忙碌，直接放棄這次播放；SFX 允許被丟棄，不需要排隊等待。
                return;
            }

            source.clip = clip;
            source.volume = data.GetRandomVolume();
            source.pitch = data.GetRandomPitch();
            source.Play();
        }

        private AudioSource GetAvailableSource()
        {
            foreach (AudioSource source in _sourcePool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            return null;
        }
    }
}
