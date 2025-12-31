using UnityEngine;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Core.Managers;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace WildernessSurvival.Gameplay.Core
{
    /// <summary>
    /// Survival System - Gestisce la Win Condition basata sulla sopravvivenza.
    /// Il giocatore vince sopravvivendo per N giorni consecutivi.
    /// </summary>
    public class SurvivalSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static SurvivalSystem Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        // ============================================
        // CONFIGURATION
        // ============================================

        #if UNITY_EDITOR
        [TitleGroup("Win Condition")]
        [BoxGroup("Win Condition/Settings")]
        #endif
        [Header("Victory Settings")]
        [Tooltip("Numero di giorni da sopravvivere per vincere")]
        [SerializeField] private int daysToWin = 5;

        #if UNITY_EDITOR
        [BoxGroup("Win Condition/Settings")]
        [ToggleLeft]
        #endif
        [Tooltip("Abilita debug logs")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        #if UNITY_EDITOR
        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        [ProgressBar(0, "@daysToWin", ColorGetter = "GetProgressColor")]
        #endif
        private int daysSurvived = 0;

        #if UNITY_EDITOR
        [ShowInInspector]
        [ReadOnly]
        #endif
        private bool victoryTriggered = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public int DaysSurvived => daysSurvived;
        public int DaysToWin => daysToWin;
        public bool VictoryTriggered => victoryTriggered;
        public float ProgressPercent => daysToWin > 0 ? (float)daysSurvived / daysToWin : 0f;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[SurvivalSystem] Duplicate instance destroyed! Existing: {Instance.GetInstanceID()}, This: {GetInstanceID()}");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (debugMode)
            {
                Debug.Log($"<color=green>[SurvivalSystem]</color> Initialized. Victory at {daysToWin} days.");
            }
        }

        private void Start()
        {
            // Subscribe to DayNightSystem's day change event
            SubscribeToDayNightEvents();
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            UnsubscribeFromDayNightEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // EVENT SUBSCRIPTION
        // ============================================

        private void SubscribeToDayNightEvents()
        {
            if (DayNightSystem.Instance != null)
            {
                // Subscribe to day started event using the IntEvent
                // Note: DayNightSystem raises onDayStarted (GameEvent) and onDayNumberChanged (IntEvent)
                // We need the day number, so we subscribe to the IntEvent if available
                // However, from the code we read, onDayNumberChanged is a SerializeField IntEvent
                // We cannot subscribe directly. We need to use onDayStarted GameEvent instead
                // and manually track days.

                // ALTERNATIVE: Since DayNightSystem has a public CurrentDayNumber property,
                // we can subscribe to onDayStarted and check the current day number.
                // But GameEvent doesn't expose AddListener publicly in the code we've seen.

                // BEST APPROACH: Subscribe directly in Update or use a coroutine to poll,
                // OR implement a proper listener pattern.
                // For MVP, we'll check in a coroutine or Update.

                if (debugMode)
                {
                    Debug.Log("<color=cyan>[SurvivalSystem]</color> Monitoring DayNightSystem for day changes...");
                }
            }
            else
            {
                Debug.LogWarning("[SurvivalSystem] DayNightSystem.Instance is null! Cannot subscribe to day events.");
            }
        }

        private void UnsubscribeFromDayNightEvents()
        {
            // If we were using event subscription, we would unsubscribe here
            // For Update-based approach, nothing to do
        }

        // ============================================
        // UPDATE LOGIC
        // ============================================

        private int lastCheckedDay = 0;

        private void Update()
        {
            // Poll DayNightSystem for day changes
            if (DayNightSystem.Instance != null && !victoryTriggered)
            {
                int currentDay = DayNightSystem.Instance.CurrentDayNumber;

                // Check if a new day has started
                if (currentDay > lastCheckedDay)
                {
                    OnNewDayStarted(currentDay);
                    lastCheckedDay = currentDay;
                }
            }
        }

        // ============================================
        // DAY TRACKING
        // ============================================

        /// <summary>
        /// Called when a new day starts
        /// </summary>
        private void OnNewDayStarted(int dayNumber)
        {
            daysSurvived = dayNumber;

            if (debugMode)
            {
                Debug.Log($"<color=yellow>[SurvivalSystem]</color> Day {daysSurvived}/{daysToWin} - Progress: {ProgressPercent:P0}");
            }

            // Check victory condition
            if (daysSurvived >= daysToWin)
            {
                TriggerVictory();
            }
        }

        // ============================================
        // VICTORY
        // ============================================

        /// <summary>
        /// Triggers the victory condition
        /// </summary>
        private void TriggerVictory()
        {
            if (victoryTriggered) return;

            victoryTriggered = true;

            Debug.Log($"<color=green>[SurvivalSystem]</color> 🏆 VICTORY! Survived {daysSurvived} days!");

            // Call GameManager's TriggerVictory
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerVictory();
            }
            else
            {
                Debug.LogError("[SurvivalSystem] GameManager.Instance is null! Cannot trigger victory.");
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Resets the survival progress (for new game)
        /// </summary>
        public void ResetProgress()
        {
            daysSurvived = 0;
            lastCheckedDay = 0;
            victoryTriggered = false;

            if (debugMode)
            {
                Debug.Log("<color=cyan>[SurvivalSystem]</color> Progress reset.");
            }
        }

        /// <summary>
        /// Sets the current progress (for save/load)
        /// </summary>
        public void SetProgress(int days)
        {
            daysSurvived = days;
            lastCheckedDay = days;

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[SurvivalSystem]</color> Progress set to {daysSurvived}/{daysToWin} days");
            }
        }

        // ============================================
        // DEBUG HELPERS (Odin)
        // ============================================

        #if UNITY_EDITOR
        private Color GetProgressColor()
        {
            float percent = ProgressPercent;
            if (percent >= 1f) return Color.green;
            if (percent >= 0.75f) return new Color(0.5f, 1f, 0.5f);
            if (percent >= 0.5f) return Color.yellow;
            if (percent >= 0.25f) return new Color(1f, 0.7f, 0.3f);
            return new Color(1f, 0.3f, 0.3f);
        }

        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("🏆 Force Victory", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.9f, 0.4f)]
        [DisableIf("victoryTriggered")]
        private void DebugForceVictory()
        {
            daysSurvived = daysToWin;
            TriggerVictory();
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("➕ Add Day", ButtonSizes.Medium)]
        [GUIColor(0.6f, 0.8f, 1f)]
        private void DebugAddDay()
        {
            daysSurvived++;
            lastCheckedDay = daysSurvived;
            Debug.Log($"[SurvivalSystem] DEBUG: Days survived set to {daysSurvived}/{daysToWin}");

            if (daysSurvived >= daysToWin && !victoryTriggered)
            {
                TriggerVictory();
            }
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("🔄 Reset Progress", ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.4f)]
        private void DebugResetProgress()
        {
            ResetProgress();
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("📊 Print Status", ButtonSizes.Medium)]
        private void DebugPrintStatus()
        {
            Debug.Log($"=== SURVIVAL SYSTEM STATUS ===\n" +
                $"Days Survived: {daysSurvived}/{daysToWin}\n" +
                $"Progress: {ProgressPercent:P1}\n" +
                $"Victory Triggered: {victoryTriggered}\n" +
                $"Current Day (DayNightSystem): {DayNightSystem.Instance?.CurrentDayNumber ?? 0}");
        }
        #endif
    }
}
