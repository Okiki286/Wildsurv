using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Events;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Core.StateMachines;
using WildernessSurvival.Gameplay.Resources;

namespace WildernessSurvival.Core.Managers
{
    /// <summary>
    /// Manager principale del gioco.
    /// Coordina tutti i sistemi, gestisce stati globali e inizializzazione.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================
        
        public static GameManager Instance { get; private set; }

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

        [TitleGroup("Debug")]
        [HorizontalGroup("Debug/Row1")]
        [ToggleLeft]
        [SerializeField] private bool debugMode = true;
        
        [HorizontalGroup("Debug/Row1")]
        [ToggleLeft]
        [SerializeField] private bool autoStartGame = true;

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

        // ============================================
        // PROPERTIES
        // ============================================

        public bool IsInitialized => isInitialized;
        public bool IsPaused => isPaused;
        public DayNightSystem DayNight => dayNightSystem;
        public ResourceSystem Resources => resourceSystem;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] Istanza duplicata distrutta");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Configurazione performance
            SetupPerformanceSettings();
        }

        private void Start()
        {
            if (autoStartGame)
            {
                InitializeGame();
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

        [TitleGroup("Inizializzazione")]
        [Button("Initialize Game", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        public void InitializeGame()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[GameManager] Gioco già inizializzato!");
                return;
            }

            Debug.Log("<color=green>[GameManager] === INIZIALIZZAZIONE GIOCO ===</color>");

            // Verifica sistemi
            ValidateSystems();

            // Inizializza nell'ordine corretto
            // 1. Resources (deve esistere prima degli altri)
            // 2. DayNight (controlla il flusso)
            // 3. Altri sistemi...

            isInitialized = true;
            onGameInitialized?.Raise();

            Debug.Log("<color=green>[GameManager] === GIOCO INIZIALIZZATO ===</color>");
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
            Debug.Log($"<color=red>[GameManager] GAME OVER: {reason}</color>");
            
            Time.timeScale = 0f;
            onGameOver?.Raise();
            
            // TODO: Mostra schermata game over
        }

        [ButtonGroup("Game End/Row1")]
        [Button("🏆 Victory", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.9f, 0.4f)]
        public void TriggerVictory()
        {
            Debug.Log("<color=green>[GameManager] VITTORIA!</color>");
            
            // TODO: Mostra schermata vittoria
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
            isInitialized = false;
            
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
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            // Salvataggio automatico prima di uscire
            Debug.Log("[GameManager] Applicazione in chiusura...");
            // TODO: SaveSystem.Save();
        }
    }
}
