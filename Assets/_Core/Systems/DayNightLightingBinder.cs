using UnityEngine;
using WildernessSurvival.Core.Events;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Collega automaticamente gli eventi Day/Night al DayNightLightingManager.
    /// Usa il sistema Action di GameEvent per sottoscrizioni runtime.
    /// </summary>
    public class DayNightLightingBinder : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("References")]
        [Tooltip("Riferimento al DayNightLightingManager. Se vuoto, cerca automaticamente.")]
        [SerializeField] private DayNightLightingManager lightingManager;

        [TitleGroup("Game Events")]
        [Tooltip("Evento sollevato quando inizia il giorno")]
        [SerializeField] private GameEvent onDayStarted;

        [Tooltip("Evento sollevato 30s prima della fine del giorno (sunset)")]
        [SerializeField] private GameEvent onDayEnding;

        [Tooltip("Evento sollevato quando inizia la notte")]
        [SerializeField] private GameEvent onNightStarted;

        [Tooltip("Evento sollevato 30s prima della fine della notte (pre-alba)")]
        [SerializeField] private GameEvent onNightEnding;

        [TitleGroup("Settings")]
        [Tooltip("Chiama TransitionToSunset() anche su NightEnding per effetto pre-alba")]
        [SerializeField] private bool transitionOnNightEnding = false;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // STATE
        // ============================================

        private bool isInitialized = false;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Auto-find lighting manager se non assegnato
            if (lightingManager == null)
            {
                lightingManager = FindFirstObjectByType<DayNightLightingManager>();
            }

            // Validate setup
            if (!ValidateSetup())
            {
                return;
            }

            isInitialized = true;
        }

        private void OnEnable()
        {
            if (!isInitialized) return;

            // Subscribe to events using Action pattern
            if (onDayStarted != null)
            {
                onDayStarted.AddListener(OnDayStarted);
            }

            if (onDayEnding != null)
            {
                onDayEnding.AddListener(OnDayEnding);
            }

            if (onNightStarted != null)
            {
                onNightStarted.AddListener(OnNightStarted);
            }

            if (onNightEnding != null)
            {
                onNightEnding.AddListener(OnNightEnding);
            }

            if (debugMode)
            {
                Debug.Log("<color=cyan>[LightingBinder]</color> Event listeners registered");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (onDayStarted != null)
            {
                onDayStarted.RemoveListener(OnDayStarted);
            }

            if (onDayEnding != null)
            {
                onDayEnding.RemoveListener(OnDayEnding);
            }

            if (onNightStarted != null)
            {
                onNightStarted.RemoveListener(OnNightStarted);
            }

            if (onNightEnding != null)
            {
                onNightEnding.RemoveListener(OnNightEnding);
            }

            if (debugMode && isInitialized)
            {
                Debug.Log("<color=cyan>[LightingBinder]</color> Event listeners unregistered");
            }
        }

        // ============================================
        // VALIDATION
        // ============================================

        private bool ValidateSetup()
        {
            if (lightingManager == null)
            {
                Debug.LogError("[DayNightLightingBinder] LightingManager not found! Binder disabled.");
                enabled = false;
                return false;
            }

            // At least one event should be assigned
            if (onDayStarted == null && onDayEnding == null &&
                onNightStarted == null && onNightEnding == null)
            {
                Debug.LogError("[DayNightLightingBinder] No GameEvents assigned! Binder disabled.");
                enabled = false;
                return false;
            }

            return true;
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnDayStarted()
        {
            if (lightingManager == null) return;

            lightingManager.TransitionToDay();

            if (debugMode)
            {
                Debug.Log("<color=yellow>[LightingBinder]</color> DayStarted -> TransitionToDay()");
            }
        }

        private void OnDayEnding()
        {
            if (lightingManager == null) return;

            lightingManager.TransitionToSunset();

            if (debugMode)
            {
                Debug.Log("<color=orange>[LightingBinder]</color> DayEnding -> TransitionToSunset()");
            }
        }

        private void OnNightStarted()
        {
            if (lightingManager == null) return;

            lightingManager.TransitionToNight();

            if (debugMode)
            {
                Debug.Log("<color=blue>[LightingBinder]</color> NightStarted -> TransitionToNight()");
            }
        }

        private void OnNightEnding()
        {
            if (lightingManager == null) return;

            if (transitionOnNightEnding)
            {
                lightingManager.TransitionToSunset();

                if (debugMode)
                {
                    Debug.Log("<color=cyan>[LightingBinder]</color> NightEnding -> TransitionToSunset() (pre-dawn)");
                }
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Quick Test")]
        [ButtonGroup("Quick Test/Row")]
        [Button("Day", ButtonSizes.Medium)]
        [GUIColor(1f, 0.9f, 0.4f)]
        private void DebugTransitionToDay()
        {
            if (lightingManager != null) lightingManager.TransitionToDay();
        }

        [ButtonGroup("Quick Test/Row")]
        [Button("Sunset", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void DebugTransitionToSunset()
        {
            if (lightingManager != null) lightingManager.TransitionToSunset();
        }

        [ButtonGroup("Quick Test/Row")]
        [Button("Night", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.5f, 0.9f)]
        private void DebugTransitionToNight()
        {
            if (lightingManager != null) lightingManager.TransitionToNight();
        }
#endif
    }
}
