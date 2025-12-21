using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.UI.RewardFeel
{
    /// <summary>
    /// Gestisce SFX per shards guadagnati con rate limiting.
    /// Previene "muro armonico" quando molti nemici muoiono insieme.
    /// </summary>
    public class ShardSfxPlayer : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static ShardSfxPlayer Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Audio")]
        [SerializeField] private AudioClip shardClip;
        [SerializeField] private AudioSource audioSource;

        [TitleGroup("Volume")]
        [SerializeField, Range(0f, 1f)] private float baseVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float volumeVariation = 0.1f;

        [TitleGroup("Pitch")]
        [SerializeField, Range(0.8f, 1.2f)] private float basePitch = 1f;
        [SerializeField, Range(0f, 0.2f)] private float pitchVariation = 0.08f;

        [TitleGroup("Rate Limiting")]
        [Tooltip("Finestra temporale in secondi per raggruppare suoni")]
        [SerializeField] private float rateLimitWindow = 0.1f;
        [Tooltip("Numero massimo di suoni nella finestra")]
        [SerializeField] private int maxSoundsInWindow = 1;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private float lastPlayTime = -1f;
        private int soundsInWindow = 0;
        private int pendingShardsInWindow = 0;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            EnsureAudioSource();
        }

        private void OnEnable()
        {
            ShardRewardEvents.OnShardsGained += HandleShardsGained;
        }

        private void OnDisable()
        {
            ShardRewardEvents.OnShardsGained -= HandleShardsGained;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Configurazione per UI audio
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.loop = false;
        }

        // ============================================
        // EVENT HANDLER
        // ============================================

        private void HandleShardsGained(int amount, Vector3 worldPosition)
        {
            // Ignora posizione - audio 2D per feedback UI
            TryPlaySound(amount);
        }

        // ============================================
        // RATE LIMITING
        // ============================================

        private void TryPlaySound(int amount)
        {
            if (shardClip == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.LogWarning("[ShardSfxPlayer] shardClip is null!");
                }
#endif
                return;
            }

            float currentTime = Time.time;

            // Check se siamo in una nuova finestra
            if (currentTime - lastPlayTime >= rateLimitWindow)
            {
                // Nuova finestra - reset contatori
                soundsInWindow = 0;
                pendingShardsInWindow = 0;
            }

            // Accumula shards per questa finestra
            pendingShardsInWindow += amount;

            // Rate limiting: suona solo se non abbiamo superato il limite
            if (soundsInWindow < maxSoundsInWindow)
            {
                PlaySound();
                soundsInWindow++;
                lastPlayTime = currentTime;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=green>[ShardSfxPlayer]</color> Playing sound ({soundsInWindow}/{maxSoundsInWindow} in window)");
                }
#endif
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=orange>[ShardSfxPlayer]</color> Rate limited - skipped sound (pending: {pendingShardsInWindow} shards)");
                }
#endif
            }
        }

        private void PlaySound()
        {
            if (audioSource == null || shardClip == null) return;

            // Randomize volume e pitch per varietà
            float volume = baseVolume + Random.Range(-volumeVariation, volumeVariation);
            float pitch = basePitch + Random.Range(-pitchVariation, pitchVariation);

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(shardClip, volume);
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Forza riproduzione suono (bypass rate limiting).
        /// Utile per testing o eventi speciali.
        /// </summary>
        [TitleGroup("Debug")]
        [Button("Force Play Sound", ButtonSizes.Medium)]
        public void ForcePlaySound()
        {
            PlaySound();
        }

        /// <summary>
        /// Imposta clip audio a runtime.
        /// </summary>
        public void SetAudioClip(AudioClip clip)
        {
            shardClip = clip;
        }

        /// <summary>
        /// Imposta volume base a runtime.
        /// </summary>
        public void SetVolume(float volume)
        {
            baseVolume = Mathf.Clamp01(volume);
        }

        // ============================================
        // EDITOR VALIDATION
        // ============================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rateLimitWindow <= 0f) rateLimitWindow = 0.1f;
            if (maxSoundsInWindow < 1) maxSoundsInWindow = 1;
        }

        [TitleGroup("Debug")]
        [Button("Stress Test (10 rapid sounds)", ButtonSizes.Medium)]
        private void DebugStressTest()
        {
            for (int i = 0; i < 10; i++)
            {
                TryPlaySound(5);
            }
        }
#endif
    }
}
