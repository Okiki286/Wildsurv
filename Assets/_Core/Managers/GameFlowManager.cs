using UnityEngine;
using UnityEngine.SceneManagement;
using System;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace WildernessSurvival.Core.Managers
{
    /// <summary>
    /// Finite State Machine persistente per il flusso dell'applicazione.
    /// Gestisce transizioni di scena, stati globali e persiste tra scene.
    ///
    /// ARCHITETTURA:
    /// - Singleton persistente (DontDestroyOnLoad)
    /// - Separato da GameManager (che gestisce gameplay)
    /// - Responsabile di: Scene loading, Stati globali, Pause/Resume
    ///
    /// USAGE:
    /// - GameFlowManager.Instance.StartGame() -> Carica scena gameplay
    /// - GameFlowManager.Instance.LoadMainMenu() -> Torna al menu
    /// - GameFlowManager.OnStateChanged += HandleStateChange -> Listener
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        // ============================================
        // ENUMS
        // ============================================

        public enum GameState
        {
            Boot,       // Inizializzazione iniziale
            MainMenu,   // Menu principale
            Gameplay,   // Gioco attivo
            Paused,     // Gioco in pausa
            Victory,    // Vittoria
            GameOver    // Sconfitta
        }

        // ============================================
        // SINGLETON
        // ============================================

        public static GameFlowManager Instance { get; private set; }

        /// <summary>
        /// Reset statico per domain reload disabled (Enter Play Mode Options)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            Instance = null;
            OnStateChanged = null;
        }

        // ============================================
        // EVENTS
        // ============================================

        /// <summary>
        /// Evento globale invocato quando lo stato cambia.
        /// Subscribe: GameFlowManager.OnStateChanged += YourHandler;
        /// </summary>
        public static event Action<GameState> OnStateChanged;

        // ============================================
        // CONFIGURATION
        // ============================================

        #if UNITY_EDITOR
        [TitleGroup("Scene Configuration")]
        [InfoBox("Configure scene names or indices for scene transitions.\n" +
                 "Build Settings > Scenes In Build must contain:\n" +
                 "  [0] MainMenu\n" +
                 "  [1] Gameplay (Game Scene)")]
        #endif
        [Header("Scene Names (Alternative to indices)")]
        [Tooltip("Nome della scena Main Menu. Se vuoto, usa mainMenuSceneIndex.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Tooltip("Nome della scena Gameplay. Se vuoto, usa gameplaySceneIndex.")]
        [SerializeField] private string gameplaySceneName = "Game";

        [Header("Scene Indices (Fallback if names empty)")]
        [Tooltip("Build index della scena Main Menu (tipicamente 0)")]
        [SerializeField] private int mainMenuSceneIndex = 0;

        [Tooltip("Build index della scena Gameplay (tipicamente 1)")]
        [SerializeField] private int gameplaySceneIndex = 1;

        #if UNITY_EDITOR
        [TitleGroup("Debug")]
        [ToggleLeft]
        #endif
        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // STATE
        // ============================================

        #if UNITY_EDITOR
        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Current State")]
        [GUIColor("GetStateColor")]
        #endif
        private GameState currentState = GameState.Boot;

        #if UNITY_EDITOR
        [ShowInInspector]
        [ReadOnly]
        #endif
        private GameState previousState = GameState.Boot;

        private bool isTransitioning = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public GameState CurrentState => currentState;
        public GameState PreviousState => previousState;
        public bool IsTransitioning => isTransitioning;
        public bool IsPaused => currentState == GameState.Paused;
        public bool IsInGameplay => currentState == GameState.Gameplay || currentState == GameState.Paused;
        public bool IsGameEnded => currentState == GameState.Victory || currentState == GameState.GameOver;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[GameFlowManager] Duplicate instance destroyed! Keeping existing: {Instance.name}");
                }
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (debugMode)
            {
                Debug.Log("<color=cyan>[GameFlowManager]</color> ✓ Singleton initialized with DontDestroyOnLoad");
            }
        }

        private void Start()
        {
            // Initialize from Boot state
            ChangeState(GameState.Boot);

            // Auto-detect current scene and sync state
            DetectInitialState();
        }

        private void Update()
        {
            // Global pause input (ESC)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsInGameplay && !IsGameEnded)
                {
                    TogglePause();
                }
            }
        }

        // ============================================
        // STATE MANAGEMENT
        // ============================================

        /// <summary>
        /// Cambia lo stato corrente e notifica i listener.
        /// </summary>
        private void ChangeState(GameState newState)
        {
            if (currentState == newState)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[GameFlowManager] Already in state {newState}, ignoring.");
                }
                return;
            }

            previousState = currentState;
            currentState = newState;

            if (debugMode)
            {
                Debug.Log($"<color=yellow>[GameFlowManager]</color> State: {previousState} → {currentState}");
            }

            // Notify listeners
            OnStateChanged?.Invoke(currentState);

            // State-specific setup
            OnStateEnter(currentState);
        }

        /// <summary>
        /// Chiamato quando si entra in un nuovo stato.
        /// </summary>
        private void OnStateEnter(GameState state)
        {
            switch (state)
            {
                case GameState.Boot:
                    // Boot logic can be extended here
                    break;

                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;

                case GameState.Gameplay:
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                case GameState.Victory:
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
            }
        }

        /// <summary>
        /// Rileva lo stato iniziale basandosi sulla scena attiva.
        /// Chiamato all'avvio per sincronizzare lo stato con la scena caricata.
        /// </summary>
        private void DetectInitialState()
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Check if we're in Main Menu
            bool isMainMenu = (!string.IsNullOrEmpty(mainMenuSceneName) && activeSceneName == mainMenuSceneName) ||
                              (activeSceneIndex == mainMenuSceneIndex);

            // Check if we're in Gameplay
            bool isGameplay = (!string.IsNullOrEmpty(gameplaySceneName) && activeSceneName == gameplaySceneName) ||
                              (activeSceneIndex == gameplaySceneIndex);

            if (isMainMenu)
            {
                ChangeState(GameState.MainMenu);
            }
            else if (isGameplay)
            {
                ChangeState(GameState.Gameplay);
            }
            else
            {
                // Unknown scene, stay in Boot
                if (debugMode)
                {
                    Debug.LogWarning($"[GameFlowManager] Unknown scene: {activeSceneName}. Staying in Boot state.");
                }
            }
        }

        // ============================================
        // SCENE LOADING
        // ============================================

        /// <summary>
        /// Carica la scena Main Menu.
        /// </summary>
        public void LoadMainMenu()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[GameFlowManager] Scene transition already in progress!");
                return;
            }

            if (debugMode)
            {
                Debug.Log("[GameFlowManager] Loading Main Menu...");
            }

            isTransitioning = true;

            // Reset time scale before transition
            Time.timeScale = 1f;

            // Load scene
            if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                SceneManager.LoadScene(mainMenuSceneIndex);
            }

            // Change state
            ChangeState(GameState.MainMenu);

            isTransitioning = false;
        }

        /// <summary>
        /// Avvia una nuova partita caricando la scena Gameplay.
        /// </summary>
        public void StartGame()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[GameFlowManager] Scene transition already in progress!");
                return;
            }

            if (debugMode)
            {
                Debug.Log("[GameFlowManager] Starting new game...");
            }

            isTransitioning = true;

            // Reset time scale before transition
            Time.timeScale = 1f;

            // Load scene
            if (!string.IsNullOrEmpty(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                SceneManager.LoadScene(gameplaySceneIndex);
            }

            // Change state
            ChangeState(GameState.Gameplay);

            isTransitioning = false;
        }

        /// <summary>
        /// Ricarica la scena corrente (per restart).
        /// </summary>
        public void RestartGame()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[GameFlowManager] Scene transition already in progress!");
                return;
            }

            if (debugMode)
            {
                Debug.Log("[GameFlowManager] Restarting current scene...");
            }

            isTransitioning = true;

            // Reset time scale
            Time.timeScale = 1f;

            // Reload current scene
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);

            // Restore appropriate state based on scene
            if (activeScene.buildIndex == gameplaySceneIndex ||
                (!string.IsNullOrEmpty(gameplaySceneName) && activeScene.name == gameplaySceneName))
            {
                ChangeState(GameState.Gameplay);
            }
            else if (activeScene.buildIndex == mainMenuSceneIndex ||
                     (!string.IsNullOrEmpty(mainMenuSceneName) && activeScene.name == mainMenuSceneName))
            {
                ChangeState(GameState.MainMenu);
            }

            isTransitioning = false;
        }

        // ============================================
        // PAUSE LOGIC
        // ============================================

        /// <summary>
        /// Mette in pausa il gioco (solo se in Gameplay).
        /// </summary>
        public void Pause()
        {
            if (currentState != GameState.Gameplay)
            {
                Debug.LogWarning($"[GameFlowManager] Cannot pause from state {currentState}");
                return;
            }

            ChangeState(GameState.Paused);
        }

        /// <summary>
        /// Riprende il gioco dalla pausa.
        /// </summary>
        public void Resume()
        {
            if (currentState != GameState.Paused)
            {
                Debug.LogWarning($"[GameFlowManager] Cannot resume from state {currentState}");
                return;
            }

            ChangeState(GameState.Gameplay);
        }

        /// <summary>
        /// Toggle pause (alterna tra Gameplay e Paused).
        /// </summary>
        public void TogglePause()
        {
            if (currentState == GameState.Gameplay)
            {
                Pause();
            }
            else if (currentState == GameState.Paused)
            {
                Resume();
            }
        }

        // ============================================
        // GAME END STATES
        // ============================================

        /// <summary>
        /// Termina il gioco con vittoria o sconfitta.
        /// </summary>
        /// <param name="victory">True per Victory, False per GameOver</param>
        public void EndGame(bool victory)
        {
            if (IsGameEnded)
            {
                Debug.LogWarning("[GameFlowManager] Game already ended!");
                return;
            }

            if (victory)
            {
                ChangeState(GameState.Victory);
            }
            else
            {
                ChangeState(GameState.GameOver);
            }
        }

        /// <summary>
        /// Termina il gioco con sconfitta.
        /// </summary>
        public void TriggerGameOver()
        {
            EndGame(victory: false);
        }

        /// <summary>
        /// Termina il gioco con vittoria.
        /// </summary>
        public void TriggerVictory()
        {
            EndGame(victory: true);
        }

        // ============================================
        // DEBUG (ODIN BUTTONS)
        // ============================================

        #if UNITY_EDITOR
        [TitleGroup("Debug Controls")]
        [ButtonGroup("Debug Controls/Scenes")]
        [Button("📋 Main Menu", ButtonSizes.Medium)]
        [GUIColor(0.7f, 0.7f, 1f)]
        private void DebugLoadMainMenu()
        {
            LoadMainMenu();
        }

        [ButtonGroup("Debug Controls/Scenes")]
        [Button("🎮 Start Game", ButtonSizes.Medium)]
        [GUIColor(0.6f, 1f, 0.6f)]
        private void DebugStartGame()
        {
            StartGame();
        }

        [ButtonGroup("Debug Controls/Scenes")]
        [Button("🔄 Restart", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.5f)]
        private void DebugRestartGame()
        {
            RestartGame();
        }

        [ButtonGroup("Debug Controls/States")]
        [Button("⏸️ Pause", ButtonSizes.Medium)]
        [GUIColor(1f, 1f, 0.6f)]
        private void DebugPause()
        {
            Pause();
        }

        [ButtonGroup("Debug Controls/States")]
        [Button("▶️ Resume", ButtonSizes.Medium)]
        [GUIColor(0.6f, 1f, 0.6f)]
        private void DebugResume()
        {
            Resume();
        }

        [ButtonGroup("Debug Controls/End")]
        [Button("🏆 Victory", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.9f, 0.4f)]
        private void DebugTriggerVictory()
        {
            TriggerVictory();
        }

        [ButtonGroup("Debug Controls/End")]
        [Button("💀 Game Over", ButtonSizes.Medium)]
        [GUIColor(0.9f, 0.4f, 0.4f)]
        private void DebugTriggerGameOver()
        {
            TriggerGameOver();
        }

        // Odin color helper
        private Color GetStateColor()
        {
            return currentState switch
            {
                GameState.Boot => Color.gray,
                GameState.MainMenu => new Color(0.7f, 0.7f, 1f),
                GameState.Gameplay => new Color(0.6f, 1f, 0.6f),
                GameState.Paused => new Color(1f, 1f, 0.6f),
                GameState.Victory => new Color(0.4f, 0.9f, 0.4f),
                GameState.GameOver => new Color(0.9f, 0.4f, 0.4f),
                _ => Color.white
            };
        }
        #endif
    }
}
