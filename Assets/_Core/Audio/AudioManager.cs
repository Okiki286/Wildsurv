using UnityEngine;

namespace WildernessSurvival.Core
{
    /// <summary>
    /// Central audio manager handling UI, Gameplay SFX, and Music.
    /// Uses a singleton pattern and persists across scene loads.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create audio sources if not assigned
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }
        #endregion

        #region Audio Sources
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        #endregion

        #region UI Sounds
        [Header("UI Sounds")]
        [Tooltip("Sound for button clicks")]
        public AudioClip uiClickClip;
        [Tooltip("Sound for popups appearing")]
        public AudioClip uiPopupClip;
        #endregion

        #region Worker Sounds
        [Header("Worker Sounds")]
        [Tooltip("Array of chopping sounds for variety")]
        public AudioClip[] chopSounds;
        [Tooltip("Array of building/hammering sounds for variety")]
        public AudioClip[] buildSounds;
        #endregion

        #region Combat Sounds
        [Header("Combat Sounds")]
        [Tooltip("Array of arrow impact sounds")]
        public AudioClip[] arrowHitSounds;
        [Tooltip("Array of sword/melee impact sounds")]
        public AudioClip[] swordHitSounds;
        #endregion

        #region Volume Settings
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        #endregion

        #region Pitch Variance Settings
        [Header("Pitch Variance")]
        [Tooltip("Minimum pitch for randomized SFX")]
        [SerializeField] private float minPitch = 0.9f;
        [Tooltip("Maximum pitch for randomized SFX")]
        [SerializeField] private float maxPitch = 1.1f;
        #endregion

        #region Core Playback Methods

        /// <summary>
        /// Plays a random clip from the provided array with pitch variance.
        /// </summary>
        /// <param name="clips">Array of audio clips to pick from.</param>
        public void PlayRandomSFX(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning("[AudioManager] PlayRandomSFX called with null or empty clip array.");
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] Selected clip is null.");
                return;
            }

            // Randomize pitch for variety
            sfxSource.pitch = Random.Range(minPitch, maxPitch);
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
            
            // Reset pitch after playing (for non-randomized sounds)
            // Note: PlayOneShot uses the current pitch, so this is safe.
        }

        /// <summary>
        /// Plays a single SFX clip without pitch variance.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlaySFX called with null clip.");
                return;
            }
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }

        /// <summary>
        /// Plays background music (loops).
        /// </summary>
        /// <param name="musicClip">The music clip to play.</param>
        public void PlayMusic(AudioClip musicClip)
        {
            if (musicClip == null)
            {
                Debug.LogWarning("[AudioManager] PlayMusic called with null clip.");
                return;
            }
            musicSource.clip = musicClip;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }

        /// <summary>
        /// Stops the currently playing music.
        /// </summary>
        public void StopMusic()
        {
            musicSource.Stop();
        }

        #endregion

        #region Integration Helper Methods

        /// <summary>
        /// Plays a UI click sound. Call this from button OnClick events.
        /// </summary>
        public void PlayUIClick()
        {
            PlaySFX(uiClickClip);
        }

        /// <summary>
        /// Plays a UI popup/appear sound.
        /// </summary>
        public void PlayUIPopup()
        {
            PlaySFX(uiPopupClip);
        }

        /// <summary>
        /// Plays a random chopping sound with pitch variance.
        /// Call this when workers are chopping wood.
        /// </summary>
        public void PlayChopSound()
        {
            PlayRandomSFX(chopSounds);
        }

        /// <summary>
        /// Plays a random building/hammering sound with pitch variance.
        /// Call this when workers are constructing structures.
        /// </summary>
        public void PlayBuildSound()
        {
            PlayRandomSFX(buildSounds);
        }

        /// <summary>
        /// Plays a random arrow hit sound with pitch variance.
        /// Call this when an arrow impacts a target.
        /// </summary>
        public void PlayArrowHit()
        {
            PlayRandomSFX(arrowHitSounds);
        }

        /// <summary>
        /// Plays a random sword/melee hit sound with pitch variance.
        /// Call this when a melee attack lands.
        /// </summary>
        public void PlaySwordHit()
        {
            PlayRandomSFX(swordHitSounds);
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Auto-Load
        /// <summary>
        /// Editor-only method to auto-load audio assets from known paths.
        /// Use context menu: Right-click component > AutoLoadAssets
        /// </summary>
        [ContextMenu("AutoLoadAssets")]
        private void AutoLoadAssets()
        {
            // UI Sounds
            uiClickClip = LoadAsset<AudioClip>("Assets/Universal Sound FX/USER_INTERFACES/Beeps/UI_Beep_Single_Clean_stereo.wav");
            uiPopupClip = LoadAsset<AudioClip>("Assets/Universal Sound FX/USER_INTERFACES/Appear_Disappear/UI_3_Clicks_01_Appear_mono.wav");

            // Chop Sounds
            chopSounds = new AudioClip[]
            {
                LoadAsset<AudioClip>("Assets/Universal Sound FX/TOOLS/Axe/AXE_Chop_Tree_01_RR1_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/TOOLS/Axe/AXE_Chop_Tree_01_RR2_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/TOOLS/Axe/AXE_Chop_Tree_01_RR3_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/TOOLS/Axe/AXE_Chop_Wood_01_RR1_mono.wav"),
            };

            // Build Sounds
            buildSounds = new AudioClip[]
            {
                LoadAsset<AudioClip>("Assets/Universal Sound FX/TOOLS/Various/TOOL_Hammer_Nail_RR1_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/TOOLS/Various/TOOL_Hammer_Nail_RR2_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/TOOLS/Various/TOOL_Sledge_Hammer_Metal_RR1_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/TOOLS/Various/TOOL_Sledge_Hammer_Metal_RR2_mono.wav"),
            };

            // Arrow Hit Sounds
            arrowHitSounds = new AudioClip[]
            {
                LoadAsset<AudioClip>("Assets/Universal Sound FX/WEAPONS/Bow_Arrow/ARROW_Hit_Body_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/WEAPONS/Bow_Arrow/ARROW_Hit_Wood_Shield_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/WEAPONS/Bow_Arrow/ARROW_Hit_Metal_01_mono.wav"),
            };

            // Sword Hit Sounds
            swordHitSounds = new AudioClip[]
            {
                LoadAsset<AudioClip>("Assets/Universal Sound FX/WEAPONS/Melee/Swords/SWORD_Hit_Sword_Cling_01_RR1_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/WEAPONS/Melee/Swords/SWORD_Hit_Sword_Cling_01_RR2_mono.wav"),
                LoadAsset<AudioClip>("Assets/Universal Sound FX/WEAPONS/Melee/Swords/SWORD_Hit_Sword_Cling_01_RR3_mono.wav"),
            };

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[AudioManager] Auto-loaded audio assets from Universal Sound FX pack.");
        }

        private T LoadAsset<T>(string path) where T : Object
        {
            T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[AudioManager] Could not load asset at: {path}");
            }
            return asset;
        }
        #endregion
#endif
    }
}
