using DeskSlayer.GameState;
using DeskSlayer.Settings;
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

            // 開機當下就套用玩家上次設定的主音量，不需要等玩家手動打開設定面板才生效。
            SetMasterVolume(GameSettingsPreferenceStore.MasterVolume);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            SubscribeToGameStateMachine();
        }

        private void Start()
        {
            // Unity 只保證所有物件的 Awake 先於任何物件的 Start，不保證 OnEnable 的跨物件順序，
            // 這裡補一次訂閱，確保不論 GameStateMachine 的 Awake 相對順序為何都能訂閱成功。
            SubscribeToGameStateMachine();
        }

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
            {
                GameStateMachine.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
            }
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

        /// <summary>
        /// 暫停時凍結所有正在播放的音源、解除暫停時復播，避免戰鬥音效在暫停中持續播放
        /// 造成違和感。AudioSource.Pause/UnPause 會保留播放位置，恢復時從原本位置接續，
        /// 不會重新播放整段音效。
        /// </summary>
        private void HandleGamePhaseChanged(GamePhase? previous, GamePhase current)
        {
            if (current == GamePhase.Paused)
            {
                foreach (AudioSource source in _sourcePool)
                {
                    if (source.isPlaying)
                    {
                        source.Pause();
                    }
                }
            }
            else
            {
                foreach (AudioSource source in _sourcePool)
                {
                    source.UnPause();
                }
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

        /// <summary>
        /// 設定主音量（0~1，超出範圍會被夾住）。套用到 AudioListener.volume 而非逐一改寫
        /// AudioSource.volume——這是 Unity 標準的全域主音量做法，對「目前正在播放中」與
        /// 「之後才播放」的音源都即時生效，完全不用碰 PlaySound()/GetAvailableSource() 的既有播放邏輯。
        /// 專案目前只有這組 SFX 音效池、沒有獨立的 BGM/Music 系統，因此套用單一全域係數不會誤傷其他音軌。
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
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
