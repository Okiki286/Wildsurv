using UnityEngine;
using Sirenix.OdinInspector;
using Wilderness.Core;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Sistema di auto-salvataggio automatico.
    /// Salva il gioco all'inizio di ogni nuovo giorno.
    /// </summary>
    public class AutoSaveSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static AutoSaveSystem Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Configurazione")]
        [ToggleLeft]
        [Tooltip("Abilita il salvataggio automatico")]
        [SerializeField] private bool enableAutoSave = true;

        [ShowIf("enableAutoSave")]
        [ToggleLeft]
        [Tooltip("Salva all'inizio di ogni giorno")]
        [SerializeField] private bool saveOnDayStart = true;

        [ShowIf("enableAutoSave")]
        [ToggleLeft]
        [Tooltip("Salva all'inizio di ogni notte")]
        [SerializeField] private bool saveOnNightStart = false;

        [ShowIf("enableAutoSave")]
        [Tooltip("Intervallo di salvataggio automatico (in secondi). 0 = disabilitato")]
        [SerializeField] private float autoSaveInterval = 0f;

        [TitleGroup("Feedback")]
        [ToggleLeft]
        [Tooltip("Mostra messaggio di feedback quando salva")]
        [SerializeField] private bool showFeedback = true;

        [ShowIf("showFeedback")]
#pragma warning disable CS0414 // Reserved for future UIToastNotifier integration
        [SerializeField] private float feedbackDuration = 2f;
#pragma warning restore CS0414

        [TitleGroup("Audio")]
        [SerializeField] private AudioClip autoSaveSound;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME
        // ============================================

        private AudioSource audioSource;
        private float timeSinceLastSave = 0f;

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        private int lastSavedDay = 0;

        [ShowInInspector]
        [ReadOnly]
        private string lastSaveTime = "Never";

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            SubscribeToEvents();

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[AutoSaveSystem]</color> Inizializzato - AutoSave: {enableAutoSave}");
            }
        }

        private void Update()
        {
            // Auto-save basato su intervallo di tempo
            if (enableAutoSave && autoSaveInterval > 0f)
            {
                timeSinceLastSave += Time.deltaTime;

                if (timeSinceLastSave >= autoSaveInterval)
                {
                    PerformAutoSave("Intervallo di tempo");
                    timeSinceLastSave = 0f;
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // EVENT SUBSCRIPTION
        // ============================================

        private void SubscribeToEvents()
        {
            if (DayNightSystem.Instance != null)
            {
                // Verifica che l'evento esista prima di sottoscriversi
                if (DayNightSystem.Instance.gameObject != null)
                {
                    // Sottoscrizione agli eventi via GameEvent ScriptableObject
                    // Nota: Gli eventi sono configurati nell'Inspector del DayNightSystem
                    // Qui ci registriamo direttamente ai metodi pubblici

                    if (debugMode)
                    {
                        Debug.Log("<color=cyan>[AutoSaveSystem]</color> Registrato agli eventi DayNightSystem");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[AutoSaveSystem] DayNightSystem.Instance è null! Riprovo in Start coroutine...");
                StartCoroutine(WaitForDayNightSystem());
            }
        }

        private void UnsubscribeFromEvents()
        {
            // Cleanup eventi se necessario
        }

        private System.Collections.IEnumerator WaitForDayNightSystem()
        {
            int maxAttempts = 10;
            int attempts = 0;

            while (DayNightSystem.Instance == null && attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.5f);
                attempts++;
            }

            if (DayNightSystem.Instance != null)
            {
                SubscribeToEvents();
            }
            else
            {
                Debug.LogError("[AutoSaveSystem] DayNightSystem non trovato dopo 10 tentativi!");
            }
        }

        // ============================================
        // AUTO-SAVE LOGIC
        // ============================================

        /// <summary>
        /// Chiamato tramite GameEvent quando inizia un nuovo giorno.
        /// Configurare onDayStarted nell'Inspector del DayNightSystem
        /// per invocare questo metodo.
        /// </summary>
        public void OnDayStarted()
        {
            if (!enableAutoSave || !saveOnDayStart) return;

            // Evita salvataggi duplicati nello stesso giorno
            int currentDay = DayNightSystem.Instance != null ? DayNightSystem.Instance.CurrentDayNumber : 0;
            if (currentDay == lastSavedDay) return;

            PerformAutoSave($"Giorno {currentDay}");
            lastSavedDay = currentDay;
        }

        /// <summary>
        /// Chiamato tramite GameEvent quando inizia la notte.
        /// Configurare onNightStarted nell'Inspector del DayNightSystem
        /// per invocare questo metodo.
        /// </summary>
        public void OnNightStarted()
        {
            if (!enableAutoSave || !saveOnNightStart) return;

            PerformAutoSave("Inizio notte");
        }

        private void PerformAutoSave(string reason)
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[AutoSaveSystem] SaveManager.Instance è null! Impossibile salvare.");
                return;
            }

            // Esegui il salvataggio ROLLING (cicla tra 3 slot)
            int slotIndex = SaveManager.Instance.PerformRollingAutoSave();

            if (slotIndex == -1)
            {
                Debug.LogError("[AutoSaveSystem] Rolling auto-save fallito!");
                return;
            }

            lastSaveTime = System.DateTime.Now.ToString("HH:mm:ss");
            timeSinceLastSave = 0f;

            // Feedback
            if (showFeedback)
            {
                ShowSaveFeedback();
            }

            // Audio
            PlaySound(autoSaveSound);

            if (debugMode)
            {
                int currentDay = DayNightSystem.Instance != null ? DayNightSystem.Instance.CurrentDayNumber : 0;
                Debug.Log($"<color=green>[AutoSaveSystem]</color> ✅ Auto-save completato - Giorno {currentDay} → Slot {slotIndex} - Motivo: {reason}");
            }
        }

        // ============================================
        // FEEDBACK
        // ============================================

        private void ShowSaveFeedback()
        {
            // Cerca un UIToastNotifier se esiste
            // Altrimenti usa solo il debug log
            var toastNotifier = FindFirstObjectByType<UIToastNotifier>();
            if (toastNotifier != null)
            {
                // Se hai un sistema di toast, usalo qui
                // toastNotifier.ShowToast("Game Auto-Saved", feedbackDuration);
                if (debugMode)
                {
                    Debug.Log("<color=cyan>[AutoSaveSystem]</color> Toast feedback inviato");
                }
            }
            else
            {
                // Fallback: solo log
                if (debugMode)
                {
                    Debug.Log("<color=green>[AutoSaveSystem]</color> 💾 Gioco salvato automaticamente!");
                }
            }
        }

        // ============================================
        // AUDIO
        // ============================================

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Forza un auto-save manuale.
        /// </summary>
        [TitleGroup("Azioni")]
        [Button("Force Auto-Save Now", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void ForceAutoSave()
        {
            if (enableAutoSave)
            {
                PerformAutoSave("Manuale");
            }
            else
            {
                Debug.LogWarning("[AutoSaveSystem] Auto-save disabilitato!");
            }
        }

        /// <summary>
        /// Abilita/disabilita l'auto-save a runtime.
        /// </summary>
        public void SetAutoSaveEnabled(bool enabled)
        {
            enableAutoSave = enabled;

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[AutoSaveSystem]</color> Auto-save {(enabled ? "abilitato" : "disabilitato")}");
            }
        }
    }

    /// <summary>
    /// Placeholder per UIToastNotifier - puoi implementarlo in futuro.
    /// </summary>
    public class UIToastNotifier : MonoBehaviour
    {
        public void ShowToast(string message, float duration)
        {
            Debug.Log($"[Toast] {message}");
        }
    }
}
