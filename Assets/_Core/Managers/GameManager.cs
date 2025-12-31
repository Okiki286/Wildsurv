using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using WildernessSurvival.Core.Events;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Core.StateMachines;
using WildernessSurvival.Core.Utils;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.Gameplay.Core;
using Wilderness.Core;

namespace WildernessSurvival.Core.Managers
{
    /// <summary>
    /// Manager principale del gioco.
    /// Coordina tutti i sistemi, gestisce stati globali e inizializzazione.
    ///
    /// CRITICAL FIX: Boot sequence now uses coroutine to ensure scene objects
    /// (like Waystone) complete their Start() before SaveManager.LoadGame() runs.
    /// This prevents duplicate structure spawning.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static GameManager Instance { get; private set; }

        /// <summary>
        /// Reset static quando disabilitato domain reload (Enter Play Mode Options)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        // ============================================
        // RIFERIMENTI SISTEMI
        // ============================================

        [TitleGroup("Riferimenti Sistemi")]
        [BoxGroup("Riferimenti Sistemi/Core")]
        [Required("DayNightSystem è richiesto!")]
        [SerializeField] private DayNightSystem dayNightSystem;

        [BoxGroup("Riferimenti Sistemi/Core")]
        [Required("ResourceSystem è richiesto!")]
        [SerializeField] private ResourceSystem resourceSystem;

        // [BoxGroup("Riferimenti Sistemi/Gameplay")]
        // [SerializeField] private WaveSystem waveSystem;
        // [BoxGroup("Riferimenti Sistemi/Gameplay")]
        // [SerializeField] private WorkerSystem workerSystem;

        [TitleGroup("Performance")]
        [BoxGroup("Performance/Settings")]
        [HorizontalGroup("Performance/Settings/Row1")]
        [LabelWidth(120)]
        [Tooltip("Target frame rate per mobile")]
        [SerializeField] private int targetFrameRate = 60;

        [HorizontalGroup("Performance/Settings/Row1")]
        [ToggleLeft]
        [Tooltip("Disabilita vsync per mobile")]
        [SerializeField] private bool disableVSync = true;

        [TitleGroup("Eventi Globali")]
        [FoldoutGroup("Eventi Globali/Game Flow")]
        [SerializeField] private GameEvent onGameInitialized;
        [FoldoutGroup("Eventi Globali/Game Flow")]
        [SerializeField] private GameEvent onGamePaused;
        [FoldoutGroup("Eventi Globali/Game Flow")]
        [SerializeField] private GameEvent onGameResumed;
        [FoldoutGroup("Eventi Globali/Game Flow")]
        [SerializeField] private GameEvent onGameOver;

        [TitleGroup("UI References")]
        [Tooltip("Riferimento al Canvas/Panel Game Over. Se null, verrà solo loggato.")]
        [SerializeField] private GameObject gameOverUI;

        [Tooltip("Riferimento al Canvas/Panel Victory. Se null, verrà solo loggato.")]
        [SerializeField] private GameObject victoryUI;

        [TitleGroup("Debug")]
        [HorizontalGroup("Debug/Row1")]
        [ToggleLeft]
        [SerializeField] private bool debugMode = true;

        [HorizontalGroup("Debug/Row1")]
        [ToggleLeft]
        [SerializeField] private bool autoStartGame = true;

        [HorizontalGroup("Debug/Row1")]
        [ToggleLeft]
        [Tooltip("Enable detailed boot sequence logging (frame-by-frame trace)")]
        [SerializeField] private bool debugBootSequence = true;

        // ============================================
        // STATO
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        [HorizontalGroup("Runtime Status/Row1")]
        private bool isInitialized = false;

        [ShowInInspector]
        [ReadOnly]
        [HorizontalGroup("Runtime Status/Row1")]
        private bool isPaused = false;

        [ShowInInspector]
        [ReadOnly]
        [HorizontalGroup("Runtime Status/Row1")]
        private bool isGameOver = false;

        [ShowInInspector]
        [ReadOnly]
        [HorizontalGroup("Runtime Status/Row2")]
        [Tooltip("Boot sequence completion status")]
        private bool bootSequenceComplete = false;

        // Waystone reference for event subscription
        private WaystoneBeaconController waystoneBeacon;

        public bool IsBooting => !bootSequenceComplete;

        // ============================================
        // PROPERTIES
        // ============================================

        public bool IsInitialized => isInitialized;
        public bool IsPaused => isPaused;
        public bool IsGameOver => isGameOver;
        public DayNightSystem DayNight => dayNightSystem;
        public ResourceSystem Resources => resourceSystem;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (debugBootSequence)
            {
                Debug.Log($"<color=cyan>[GameManager]</color> [BOOT] Awake called on '{name}' | Frame={Time.frameCount} | SceneObj ID={GetInstanceID()}");
            }

            // Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[GameManager] Istanza duplicata distrutta! Existing Instance id={Instance.GetInstanceID()}, this id={GetInstanceID()}");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // DontDestroyOnLoad only works for root objects
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
                if (debugBootSequence)
                {
                    Debug.Log($"<color=cyan>[GameManager]</color> [BOOT] DontDestroyOnLoad applied");
                }
            }

            // Configurazione performance
            SetupPerformanceSettings();
        }

        /// <summary>
        /// CRITICAL FIX: Start() now launches a coroutine instead of calling InitializeGame() directly.
        /// This ensures all scene objects (Waystone, etc.) complete their Start() methods
        /// before SaveManager.LoadGame() executes, preventing duplicate structure spawning.
        /// </summary>
        private void Start()
        {
            if (debugBootSequence)
            {
                Debug.Log($"<color=cyan>[GameManager]</color> [BOOT] Start called | Frame={Time.frameCount}");
            }

            if (autoStartGame)
            {
                // Launch boot sequence coroutine instead of immediate initialization
                StartCoroutine(BootSequence());
            }
        }

        private void Update()
        {
            // Input pausa (Escape o P)
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }

            #if UNITY_EDITOR
            // Debug shortcuts
            HandleDebugInput();
            #endif
        }

        // ============================================
        // BOOT SEQUENCE (CRITICAL FIX)
        // ============================================

        /// <summary>
        /// CRITICAL FIX: Coroutine-based boot sequence to solve race condition.
        ///
        /// PROBLEM: Previously, GameManager.Start() called InitializeGame() → LoadGame() immediately.
        /// This happened BEFORE scene objects (Waystone, etc.) ran their Start() methods to register
        /// with StructureSystem, causing SaveManager to spawn duplicates.
        ///
        /// SOLUTION: Yield WaitForEndOfFrame to ensure ALL Start() methods complete first.
        ///
        /// EXECUTION ORDER:
        /// Frame N:   GameManager.Awake() → Waystone.Awake() → Other.Awake()
        /// Frame N:   GameManager.Start() → Waystone.Start() → Other.Start()
        /// Frame N:   [WaitForEndOfFrame - ALL Start() methods complete]
        /// Frame N+1: InitializeGame() → SaveManager.LoadGame()
        ///
        /// Result: Waystone is already registered before LoadGame() runs, preventing duplicates.
        /// </summary>
        private IEnumerator BootSequence()
        {
            if (debugBootSequence)
            {
                Debug.Log($"<color=lime>[GameManager]</color> [BOOT] BootSequence started | Frame={Time.frameCount}");
            }

            // PHASE 1: System Validation
            if (debugBootSequence)
            {
                Debug.Log($"<color=lime>[GameManager]</color> [BOOT] Phase 1: System Validation | Frame={Time.frameCount}");
            }
            ValidateSystems();

            // PHASE 2: Core Initialization (Systems init, no loading yet)
            if (debugBootSequence)
            {
                Debug.Log($"<color=lime>[GameManager]</color> [BOOT] Phase 2: Core Systems Init | Frame={Time.frameCount}");
            }
            InitializeCoreSystemsOnly();

            // Ensure DayNightSystem is paused during boot to prevent "Day 1" race conditions
            if (dayNightSystem != null)
            {
                dayNightSystem.SetPaused(true);
            }

            // CRITICAL: Wait for End of Frame
            // This ensures ALL scene objects (Waystone, Workers, Structures) have completed
            // their Start() methods and registered themselves with their respective systems.
            if (debugBootSequence)
            {
                Debug.Log($"<color=yellow>[GameManager]</color> [BOOT] Phase 3: Yielding WaitForEndOfFrame... | Frame={Time.frameCount}");
            }

            // ✅ OPTIMIZATION: Use cached WaitForEndOfFrame (zero allocation)
            yield return CoroutineCache.WaitEndOfFrame;

            // PHASE 3.5: Wire Waystone OnDestroyed Event
            // Now that all Start() methods have completed, we can safely access the Waystone
            if (debugBootSequence)
            {
                Debug.Log($"<color=lime>[GameManager]</color> [BOOT] Phase 3.5: Wiring Waystone Event | Frame={Time.frameCount}");
            }
            WireWaystoneEvents();

            // PHASE 4: BRANCHING LOGIC - Load Game OR Initialize New Game
            // CRITICAL FIX: This prevents ghost workers by checking for save file BEFORE spawning.
            // Branch A: Save file exists → Load saved workers
            // Branch B: No save file → Spawn starting workers
            if (debugBootSequence)
            {
                Debug.Log($"<color=lime>[GameManager]</color> [BOOT] Phase 4: Save/New Game Branch | Frame={Time.frameCount}");
            }

            if (SaveManager.Instance != null)
            {
                bool hasSaveFile = SaveManager.Instance.HasSaveFile();

                if (hasSaveFile)
                {
                    // BRANCH A: Load Game (restore workers from save)
                    if (debugBootSequence)
                    {
                        Debug.Log($"<color=cyan>[GameManager]</color> [BOOT] Branch A: Loading from save file...");
                    }
                    SaveManager.Instance.LoadGame();
                }
                else
                {
                    // BRANCH B: New Game (spawn starting workers)
                    if (debugBootSequence)
                    {
                        Debug.Log($"<color=yellow>[GameManager]</color> [BOOT] Branch B: No save file - Initializing new game...");
                    }

                    // Spawn starting workers via WorkerSystem
                    if (WildernessSurvival.Gameplay.Workers.WorkerSystem.Instance != null)
                    {
                        WildernessSurvival.Gameplay.Workers.WorkerSystem.Instance.InitializeNewGame();
                        Debug.Log($"<color=green>[GameManager]</color> [BOOT] Starting workers spawned for new game");
                    }
                    else
                    {
                        Debug.LogWarning("[GameManager] [BOOT] WorkerSystem.Instance is null! Cannot spawn starting workers.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] [BOOT] SaveManager.Instance is null! Skipping load/spawn.");
            }

            // PHASE 4: Finalize
            bootSequenceComplete = true;

            // Unpause DayNightSystem after boot is complete
            if (dayNightSystem != null)
            {
                dayNightSystem.SetPaused(false);
            }

            if (debugBootSequence)
            {
                Debug.Log($"<color=green>[GameManager]</color> [BOOT] ✅ Boot Sequence COMPLETE | Frame={Time.frameCount}");
            }
        }

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        private void SetupPerformanceSettings()
        {
            // Target framerate
            Application.targetFrameRate = targetFrameRate;

            // VSync
            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }

            // Mobile optimizations
            #if UNITY_ANDROID || UNITY_IOS
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            #endif

            if (debugMode)
            {
                Debug.Log($"[GameManager] Performance: {targetFrameRate} FPS, VSync: {!disableVSync}");
            }
        }

        /// <summary>
        /// REFACTORED: Core system initialization WITHOUT loading save data.
        /// Save loading now happens AFTER WaitForEndOfFrame in BootSequence().
        /// </summary>
        private void InitializeCoreSystemsOnly()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[GameManager] Gioco già inizializzato!");
                return;
            }

            Debug.Log("<color=green>[GameManager] === CORE SYSTEMS INITIALIZATION ===</color>");

            // Inizializza nell'ordine corretto
            // 1. Resources (deve esistere prima degli altri)
            // 2. DayNight (controlla il flusso)
            // 3. Altri sistemi...
            // NOTE: Systems self-initialize in their Awake/Start, we just validate here

            isInitialized = true;
            onGameInitialized?.Raise();

            Debug.Log("<color=green>[GameManager] === CORE SYSTEMS READY (Load pending) ===</color>");
        }

        /// <summary>
        /// DEPRECATED: Use BootSequence() coroutine instead.
        /// Kept for Odin button backward compatibility.
        /// </summary>
        [TitleGroup("Inizializzazione")]
        [Button("Initialize Game (Legacy)", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        public void InitializeGame()
        {
            Debug.LogWarning("[GameManager] InitializeGame() called directly (legacy). Use BootSequence() coroutine for proper initialization.");

            if (isInitialized)
            {
                Debug.LogWarning("[GameManager] Gioco già inizializzato!");
                return;
            }

            Debug.Log("<color=green>[GameManager] === INIZIALIZZAZIONE GIOCO (LEGACY) ===</color>");

            // Verifica sistemi
            ValidateSystems();

            isInitialized = true;
            onGameInitialized?.Raise();

            // Carica salvataggio se disponibile
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
            }

            Debug.Log("<color=green>[GameManager] === GIOCO INIZIALIZZATO (LEGACY) ===</color>");
        }

        [Button("Auto-Find Systems", ButtonSizes.Medium)]
        private void ValidateSystems()
        {
            // Auto-find se non assegnati
            if (dayNightSystem == null)
            {
                dayNightSystem = FindFirstObjectByType<DayNightSystem>();
                if (dayNightSystem == null)
                {
                    Debug.LogError("[GameManager] DayNightSystem non trovato!");
                }
                else
                {
                    Debug.Log("[GameManager] DayNightSystem trovato automaticamente");
                }
            }

            if (resourceSystem == null)
            {
                resourceSystem = FindFirstObjectByType<ResourceSystem>();
                if (resourceSystem == null)
                {
                    Debug.LogError("[GameManager] ResourceSystem non trovato!");
                }
                else
                {
                    Debug.Log("[GameManager] ResourceSystem trovato automaticamente");
                }
            }
        }

        // ============================================
        // WAYSTONE EVENT WIRING
        // ============================================

        /// <summary>
        /// Wire Waystone OnDestroyed event to GameManager's TriggerGameOver
        /// </summary>
        private void WireWaystoneEvents()
        {
            // Get Waystone reference from CoreTargetProvider
            waystoneBeacon = CoreTargetProvider.GetCoreController();

            if (waystoneBeacon != null)
            {
                // Subscribe to OnDestroyed event
                waystoneBeacon.OnDestroyed += OnWaystoneDestroyed;

                if (debugMode)
                {
                    Debug.Log($"<color=green>[GameManager]</color> Waystone event wired: {waystoneBeacon.name}");
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] Waystone not found at boot! Will attempt late binding when Waystone is built.");
            }
        }

        /// <summary>
        /// Public method to allow late binding when Waystone is constructed at runtime
        /// </summary>
        public void TryWireWaystone(WaystoneBeaconController waystone)
        {
            if (waystone == null) return;

            // Unwire previous if any
            UnwireWaystoneEvents();

            // Wire new waystone
            waystoneBeacon = waystone;
            waystoneBeacon.OnDestroyed += OnWaystoneDestroyed;

            if (debugMode)
            {
                Debug.Log($"<color=green>[GameManager]</color> Late-bound Waystone event: {waystoneBeacon.name}");
            }
        }

        /// <summary>
        /// Unwire Waystone events (cleanup)
        /// </summary>
        private void UnwireWaystoneEvents()
        {
            if (waystoneBeacon != null)
            {
                waystoneBeacon.OnDestroyed -= OnWaystoneDestroyed;

                if (debugMode)
                {
                    Debug.Log("<color=gray>[GameManager]</color> Waystone event unwired.");
                }
            }
        }

        /// <summary>
        /// Event handler for Waystone destruction
        /// </summary>
        private void OnWaystoneDestroyed()
        {
            TriggerGameOver("Waystone Destroyed");
        }

        // ============================================
        // PAUSA
        // ============================================

        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        [TitleGroup("Game Control")]
        [ButtonGroup("Game Control/Row1")]
        [Button("⏸️ Pause", ButtonSizes.Large)]
        [GUIColor(1f, 0.8f, 0.4f)]
        [DisableIf("isPaused")]
        public void PauseGame()
        {
            if (isPaused) return;

            isPaused = true;
            Time.timeScale = 0f;

            dayNightSystem?.SetPaused(true);
            onGamePaused?.Raise();

            if (debugMode)
            {
                Debug.Log("<color=yellow>[GameManager]</color> ⏸️ GIOCO IN PAUSA");
            }
        }

        [ButtonGroup("Game Control/Row1")]
        [Button("▶️ Resume", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        [EnableIf("isPaused")]
        public void ResumeGame()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = 1f;

            dayNightSystem?.SetPaused(false);
            onGameResumed?.Raise();

            if (debugMode)
            {
                Debug.Log("<color=green>[GameManager]</color> ▶️ GIOCO RIPRESO");
            }
        }

        // ============================================
        // GAME OVER / VITTORIA
        // ============================================

        [TitleGroup("Game End")]
        [ButtonGroup("Game End/Row1")]
        [Button("💀 Game Over", ButtonSizes.Medium)]
        [GUIColor(0.9f, 0.3f, 0.3f)]
        public void TriggerGameOver(string reason = "Base Destroyed")
        {
            if (isGameOver) return; // Prevent multiple triggers

            Debug.Log($"<color=red>[GameManager] GAME OVER: {reason}</color>");

            // Set game over state
            isGameOver = true;
            isPaused = true;

            // Pause the game
            Time.timeScale = 0f;

            // Pause DayNightSystem
            dayNightSystem?.SetPaused(true);

            // Raise the GameEvent for any subscribers
            onGameOver?.Raise();

            // Show Game Over UI
            if (gameOverUI != null)
            {
                gameOverUI.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[GameManager] gameOverUI is not assigned! Cannot show Game Over screen.");
            }
        }

        [ButtonGroup("Game End/Row1")]
        [Button("🏆 Victory", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.9f, 0.4f)]
        public void TriggerVictory()
        {
            if (isGameOver) return; // Prevent multiple triggers

            Debug.Log("<color=green>[GameManager] 🏆 VICTORY! You survived!</color>");

            // Set game over state (victory is also a game end state)
            isGameOver = true;
            isPaused = true;

            // Pause the game
            Time.timeScale = 0f;

            // Pause DayNightSystem
            dayNightSystem?.SetPaused(true);

            // Show Victory UI
            if (victoryUI != null)
            {
                victoryUI.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[GameManager] victoryUI is not assigned! Cannot show Victory screen.");
            }
        

            // Delegate to GameFlowManager for global state
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.TriggerVictory();
            }
}

        // ============================================
        // RESET / RESTART
        // ============================================

        [TitleGroup("Game Control")]
        [Button("🔄 Restart Game", ButtonSizes.Medium)]
        [GUIColor(0.6f, 0.6f, 1f)]
        public void RestartGame()
        {
            Debug.Log("[GameManager] Riavvio gioco...");

            Time.timeScale = 1f;
            isPaused = false;
            isGameOver = false;
            isInitialized = false;
            bootSequenceComplete = false;

            // Ricarica scena
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }

        // ============================================
        // DEBUG
        // ============================================

        #if UNITY_EDITOR
        private void HandleDebugInput()
        {
            // N = Skip to Night
            if (Input.GetKeyDown(KeyCode.N))
            {
                dayNightSystem?.SkipToNight();
            }

            // D = Skip to Day
            if (Input.GetKeyDown(KeyCode.D))
            {
                dayNightSystem?.SkipToDay();
            }

            // R = Add resources
            if (Input.GetKeyDown(KeyCode.R))
            {
                resourceSystem?.AddResource("warmwood", 50);
                resourceSystem?.AddResource("food", 25);
                resourceSystem?.AddResource("shards", 10);
                Debug.Log("[Debug] Risorse aggiunte!");
            }

            // G = Game Over test
            if (Input.GetKeyDown(KeyCode.G))
            {
                TriggerGameOver("Debug Test");
            }
        }
        #endif

        [TitleGroup("Debug Actions")]
        [Button("📊 Print Game State", ButtonSizes.Medium)]
        private void DebugPrintState()
        {
            Debug.Log($"=== GAME STATE ===\n" +
                $"Initialized: {isInitialized}\n" +
                $"Boot Complete: {bootSequenceComplete}\n" +
                $"Paused: {isPaused}\n" +
                $"Day/Night State: {dayNightSystem?.CurrentState}\n" +
                $"Current Day: {dayNightSystem?.CurrentDayNumber}");
        }

        [Button("🎁 Add Debug Resources", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.6f)]
        private void DebugAddResources()
        {
            resourceSystem?.AddResource("warmwood", 50);
            resourceSystem?.AddResource("food", 25);
            resourceSystem?.AddResource("shards", 10);
        }

        // ============================================
        // CLEANUP
        // ============================================

        private void OnDestroy()
        {
            // Cleanup event subscriptions
            UnwireWaystoneEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            // Auto-save on quit DISABLED (user preference)
            Debug.Log("[GameManager] Applicazione in chiusura... (auto-save disabilitato)");

            // DISABLED: Auto-save on application quit
            // if (SaveManager.Instance != null)
            // {
            //     SaveManager.Instance.SaveGame();
            // }
        }
    }
}
