using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Events;
using WildernessSurvival.Core.StateMachines;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Sistema che gestisce il ciclo giorno/notte.
    /// Controlla durata fasi, transizioni, e notifica altri sistemi.
    /// </summary>
    public class DayNightSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static DayNightSystem Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE TEMPI
        // ============================================

        [TitleGroup("Durata Fasi")]
        [BoxGroup("Durata Fasi/Tempi")]
        [HorizontalGroup("Durata Fasi/Tempi/Row1")]
        [LabelWidth(120)]
        [SuffixLabel("sec", Overlay = true)]
        [Tooltip("Durata della fase diurna in secondi reali")]
        [SerializeField] private float dayDuration = 120f;

        [HorizontalGroup("Durata Fasi/Tempi/Row1")]
        [LabelWidth(120)]
        [SuffixLabel("sec", Overlay = true)]
        [Tooltip("Durata della fase notturna in secondi reali")]
        [SerializeField] private float nightDuration = 90f;

        [BoxGroup("Durata Fasi/Tempi")]
        [LabelWidth(120)]
        [SuffixLabel("sec", Overlay = true)]
        [Tooltip("Durata transizione giornoâ†’notte")]
        [SerializeField] private float transitionDuration = 5f;

        [TitleGroup("Stato Iniziale")]
        [HorizontalGroup("Stato Iniziale/Row1")]
        [ToggleLeft]
        [Tooltip("Inizia dal giorno?")]
        [SerializeField] private bool startWithDay = true;

        [HorizontalGroup("Stato Iniziale/Row1")]
        [LabelWidth(100)]
        [Tooltip("Giorno corrente (1 = primo giorno)")]
        [SerializeField] private int currentDayNumber = 1;

        [TitleGroup("Eventi")]
        [FoldoutGroup("Eventi/Day Events")]
        [SerializeField] private GameEvent onDayStarted;
        [FoldoutGroup("Eventi/Day Events")]
        [SerializeField] private GameEvent onDayEnding;

        [FoldoutGroup("Eventi/Night Events")]
        [SerializeField] private GameEvent onNightStarted;
        [FoldoutGroup("Eventi/Night Events")]
        [SerializeField] private GameEvent onNightEnding;

        [FoldoutGroup("Eventi/Progress Events")]
        [SerializeField] private IntEvent onDayNumberChanged;
        [FoldoutGroup("Eventi/Progress Events")]
        [SerializeField] private FloatEvent onTimeProgressChanged;

        [TitleGroup("Debug")]
        [HorizontalGroup("Debug/Row1")]
        [ToggleLeft]
        [SerializeField] private bool debugMode = true;

        [HorizontalGroup("Debug/Row1")]
        [ToggleLeft]
        [SerializeField] private bool pauseTime = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        [EnumToggleButtons]
        private GameState currentState = GameState.Initializing;

        [ShowInInspector]
        [ReadOnly]
        [HorizontalGroup("Runtime Status/Row1")]
        [LabelWidth(100)]
        private DayPhase currentDayPhase = DayPhase.Dawn;

        [ShowInInspector]
        [ReadOnly]
        [HorizontalGroup("Runtime Status/Row1")]
        [LabelWidth(100)]
        [ProgressBar(0, 1, ColorGetter = "GetProgressBarColor")]
        private float phaseProgress = 0f;

        private float phaseTimer = 0f;
        private float currentPhaseDuration = 0f;
        private bool dayEndingNotified = false;
        private bool nightEndingNotified = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public GameState CurrentState => currentState;
        public DayPhase CurrentDayPhase => currentDayPhase;
        public int CurrentDayNumber => currentDayNumber;
        public bool IsDay => currentState == GameState.Day;
        public bool IsNight => currentState == GameState.Night;
        public bool IsTransitioning => currentState == GameState.DayToNight || currentState == GameState.NightToDay;

        /// <summary>Tempo rimanente nella fase corrente</summary>
        public float TimeRemaining => Mathf.Max(0, currentPhaseDuration - phaseTimer);

        /// <summary>Progresso 0-1 della fase corrente</summary>
        public float PhaseProgress => currentPhaseDuration > 0 ? phaseTimer / currentPhaseDuration : 0f;

        /// <summary>Tempo corrente in formato 0-24 ore (per GameHUD)</summary>
        public float CurrentTime
        {
            get
            {
                // Calcola ora del giorno basata su fase e progresso
                switch (currentState)
                {
                    case GameState.Day:
                        // Giorno: 6:00 -> 18:00 (12 ore)
                        return 6f + (PhaseProgress * 12f);

                    case GameState.Night:
                        // Notte: 18:00 -> 6:00 (12 ore)
                        float nightTime = 18f + (PhaseProgress * 12f);
                        return nightTime >= 24f ? nightTime - 24f : nightTime;

                    case GameState.DayToNight:
                        // Transizione verso notte (18:00)
                        return 18f;

                    case GameState.NightToDay:
                        // Transizione verso giorno (6:00)
                        return 6f;

                    default:
                        return 12f; // Default mezzogiorno
                }
            }
        }

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
        }

        private void Start()
        {
            InitializeCycle();
        }

        private void Update()
        {
            if (pauseTime) return;
            if (currentState == GameState.Paused || currentState == GameState.GameOver) return;

            UpdateCycle(Time.deltaTime);

            // Aggiorna per Odin display
            phaseProgress = PhaseProgress;
        }

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        private void InitializeCycle()
        {
            if (startWithDay)
            {
                StartDay();
            }
            else
            {
                StartNight();
            }

            Debug.Log($"<color=yellow>[DayNight]</color> Ciclo inizializzato - Giorno {currentDayNumber}");
        }

        // ============================================
        // UPDATE CICLO
        // ============================================

        private void UpdateCycle(float deltaTime)
        {
            phaseTimer += deltaTime;

            // Notifica progresso per UI
            onTimeProgressChanged?.Raise(PhaseProgress);

            // Check transizioni e notifiche
            switch (currentState)
            {
                case GameState.Day:
                    UpdateDayPhase();
                    break;

                case GameState.Night:
                    UpdateNightPhase();
                    break;

                case GameState.DayToNight:
                case GameState.NightToDay:
                    UpdateTransition();
                    break;
            }
        }

        private void UpdateDayPhase()
        {
            // Notifica "giorno sta finendo" 30 sec prima
            if (!dayEndingNotified && TimeRemaining <= 30f)
            {
                dayEndingNotified = true;
                onDayEnding?.Raise();

                if (debugMode)
                {
                    Debug.Log("<color=orange>[DayNight]</color> Giorno sta terminando! 30 secondi rimanenti.");
                }
            }

            // Aggiorna sottofase
            float progress = PhaseProgress;
            if (progress < 0.15f)
            {
                currentDayPhase = DayPhase.Dawn;
            }
            else if (progress < 0.85f)
            {
                currentDayPhase = DayPhase.Working;
            }
            else
            {
                currentDayPhase = DayPhase.Dusk;
            }

            // Fine giorno
            if (phaseTimer >= currentPhaseDuration)
            {
                StartTransition(GameState.DayToNight);
            }
        }

        private void UpdateNightPhase()
        {
            // Notifica "notte sta finendo" 30 sec prima
            if (!nightEndingNotified && TimeRemaining <= 30f)
            {
                nightEndingNotified = true;
                onNightEnding?.Raise();

                if (debugMode)
                {
                    Debug.Log("<color=cyan>[DayNight]</color> Notte sta terminando! 30 secondi rimanenti.");
                }
            }

            // Fine notte
            if (phaseTimer >= currentPhaseDuration)
            {
                StartTransition(GameState.NightToDay);
            }
        }

        private void UpdateTransition()
        {
            if (phaseTimer >= currentPhaseDuration)
            {
                if (currentState == GameState.DayToNight)
                {
                    StartNight();
                }
                else
                {
                    // Nuovo giorno
                    currentDayNumber++;
                    onDayNumberChanged?.Raise(currentDayNumber);
                    StartDay();
                }
            }
        }

        // ============================================
        // CAMBIO STATO
        // ============================================

        private void StartDay()
        {
            currentState = GameState.Day;
            currentDayPhase = DayPhase.Dawn;
            phaseTimer = 0f;
            currentPhaseDuration = dayDuration;
            dayEndingNotified = false;

            onDayStarted?.Raise();

            if (debugMode)
            {
                Debug.Log($"<color=yellow>[DayNight]</color> â˜€ï¸ GIORNO {currentDayNumber} iniziato! Durata: {dayDuration}s");
            }
        }

        private void StartNight()
        {
            currentState = GameState.Night;
            phaseTimer = 0f;
            currentPhaseDuration = nightDuration;
            nightEndingNotified = false;

            onNightStarted?.Raise();

            if (debugMode)
            {
                Debug.Log($"<color=blue>[DayNight]</color> ðŸŒ™ NOTTE {currentDayNumber} iniziata! Durata: {nightDuration}s");
            }
        }

        private void StartTransition(GameState transitionState)
        {
            currentState = transitionState;
            phaseTimer = 0f;
            currentPhaseDuration = transitionDuration;

            if (debugMode)
            {
                string transitionName = transitionState == GameState.DayToNight ? "Tramonto" : "Alba";
                Debug.Log($"<color=magenta>[DayNight]</color> ðŸŒ… {transitionName} in corso...");
            }
        }

        // ============================================
        // API PUBBLICA
        // ============================================

        /// <summary>
        /// Salta direttamente alla notte (per testing/eventi)
        /// </summary>
        public void SkipToNight()
        {
            if (currentState != GameState.Night)
            {
                StartTransition(GameState.DayToNight);
                phaseTimer = currentPhaseDuration; // Skip immediato
            }
        }

        /// <summary>
        /// Salta direttamente al giorno (per testing/eventi)
        /// </summary>
        public void SkipToDay()
        {
            if (currentState != GameState.Day)
            {
                StartTransition(GameState.NightToDay);
                phaseTimer = currentPhaseDuration; // Skip immediato
            }
        }

        /// <summary>
        /// Estende la durata della fase corrente
        /// </summary>
        public void ExtendCurrentPhase(float extraSeconds)
        {
            currentPhaseDuration += extraSeconds;

            if (debugMode)
            {
                Debug.Log($"[DayNight] Fase estesa di {extraSeconds}s");
            }
        }

        /// <summary>
        /// Mette in pausa/riprende il ciclo
        /// </summary>
        public void SetPaused(bool paused)
        {
            pauseTime = paused;

            if (paused)
            {
                currentState = GameState.Paused;
            }
            else
            {
                // Ripristina stato precedente (semplificato: torna a Day)
                currentState = IsNight ? GameState.Night : GameState.Day;
            }
        }

        /// <summary>
        /// Formatta il tempo rimanente per UI (MM:SS)
        /// </summary>
        public string GetFormattedTimeRemaining()
        {
            float remaining = TimeRemaining;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        // ============================================
        // ODIN HELPERS
        // ============================================

        private Color GetProgressBarColor()
        {
            if (currentState == GameState.Day) return new Color(1f, 0.9f, 0.3f);
            if (currentState == GameState.Night) return new Color(0.3f, 0.4f, 0.8f);
            return Color.magenta;
        }

        // ============================================
        // DEBUG BUTTONS
        // ============================================

        [TitleGroup("Quick Actions")]
        [ButtonGroup("Quick Actions/Row1")]
        [Button("â˜€ï¸ Skip to Day", ButtonSizes.Large)]
        [GUIColor(1f, 0.9f, 0.4f)]
        private void DebugSkipToDay() => SkipToDay();

        [ButtonGroup("Quick Actions/Row1")]
        [Button("ðŸŒ™ Skip to Night", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.5f, 0.9f)]
        private void DebugSkipToNight() => SkipToNight();

        [ButtonGroup("Quick Actions/Row2")]
        [Button("â±ï¸ Add 30 sec", ButtonSizes.Medium)]
        private void DebugAddTime() => ExtendCurrentPhase(30f);

        [ButtonGroup("Quick Actions/Row2")]
        [Button("ðŸ“Š Print Status", ButtonSizes.Medium)]
        private void DebugPrintStatus()
        {
            Debug.Log($"=== DAY/NIGHT STATUS ===\n" +
                $"State: {currentState}\n" +
                $"Day #: {currentDayNumber}\n" +
                $"Phase Progress: {PhaseProgress:P0}\n" +
                $"Time Remaining: {GetFormattedTimeRemaining()}");
        }
    }
}