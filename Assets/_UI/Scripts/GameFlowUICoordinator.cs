using UnityEngine;
using WildernessSurvival.Core.Managers;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Coordinator che collega GameFlowManager agli UI panels.
    /// Ascolta gli stati globali di GameFlowManager e attiva/disattiva i pannelli UI corrispondenti.
    ///
    /// ARCHITETTURA:
    /// - GameFlowManager: Gestisce stati globali (Victory, GameOver, Paused, ecc.)
    /// - GameManager: Gestisce riferimenti diretti a gameOverUI/victoryUI GameObject
    /// - GameFlowUICoordinator: Ascolta GameFlowManager.OnStateChanged e controlla i pannelli UI
    ///
    /// USAGE:
    /// 1. Aggiungi questo script a un GameObject nella scena Gameplay (es. --- UI COORDINATOR ---)
    /// 2. Assegna i riferimenti ai pannelli Victory e GameOver nell'Inspector
    /// 3. Il coordinator si registrerà automaticamente a GameFlowManager.OnStateChanged
    /// </summary>
    public class GameFlowUICoordinator : MonoBehaviour
    {
        // ============================================
        // UI REFERENCES
        // ============================================

        [Header("UI Panel References")]
        [Tooltip("Riferimento al pannello Victory (VictoryCanvas o VictoryPanel)")]
        [SerializeField] private GameObject victoryPanel;

        [Tooltip("Riferimento al pannello GameOver (GameOverCanvas o GameOverPanel)")]
        [SerializeField] private GameObject gameOverPanel;

        [Tooltip("Riferimento al pannello Pause (opzionale)")]
        [SerializeField] private GameObject pausePanel;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void OnEnable()
        {
            // Subscribe to GameFlowManager state changes
            GameFlowManager.OnStateChanged += HandleStateChange;

            if (debugMode)
            {
                Debug.Log("<color=cyan>[GameFlowUICoordinator]</color> ✓ Subscribed to GameFlowManager.OnStateChanged");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            GameFlowManager.OnStateChanged -= HandleStateChange;

            if (debugMode)
            {
                Debug.Log("<color=cyan>[GameFlowUICoordinator]</color> ✗ Unsubscribed from GameFlowManager.OnStateChanged");
            }
        }

        private void Start()
        {
            // Ensure all UI panels are hidden at start
            HideAllPanels();
        }

        // ============================================
        // STATE CHANGE HANDLER
        // ============================================

        /// <summary>
        /// Handles GameFlowManager state changes and activates corresponding UI panels.
        /// </summary>
        private void HandleStateChange(GameFlowManager.GameState newState)
        {
            if (debugMode)
            {
                Debug.Log($"<color=yellow>[GameFlowUICoordinator]</color> State changed to: {newState}");
            }

            switch (newState)
            {
                case GameFlowManager.GameState.Victory:
                    ShowVictoryPanel();
                    break;

                case GameFlowManager.GameState.GameOver:
                    ShowGameOverPanel();
                    break;

                case GameFlowManager.GameState.Paused:
                    ShowPausePanel();
                    break;

                case GameFlowManager.GameState.Gameplay:
                    // When returning to gameplay, hide all end-game panels
                    HideAllPanels();
                    break;

                case GameFlowManager.GameState.MainMenu:
                    // If we're loading main menu, hide all panels (scene will unload anyway)
                    HideAllPanels();
                    break;

                case GameFlowManager.GameState.Boot:
                    // Boot state - ensure all panels are hidden
                    HideAllPanels();
                    break;
            }
        }

        // ============================================
        // PANEL CONTROL
        // ============================================

        /// <summary>
        /// Shows the Victory panel and hides others.
        /// </summary>
        private void ShowVictoryPanel()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                if (debugMode)
                {
                    Debug.Log("<color=green>[GameFlowUICoordinator]</color> 🏆 Victory panel activated");
                }
            }
            else
            {
                Debug.LogWarning("[GameFlowUICoordinator] Victory panel reference is null!");
            }

            // Hide other panels
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        /// <summary>
        /// Shows the GameOver panel and hides others.
        /// </summary>
        private void ShowGameOverPanel()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                if (debugMode)
                {
                    Debug.Log("<color=red>[GameFlowUICoordinator]</color> 💀 GameOver panel activated");
                }
            }
            else
            {
                Debug.LogWarning("[GameFlowUICoordinator] GameOver panel reference is null!");
            }

            // Hide other panels
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        /// <summary>
        /// Shows the Pause panel and hides others.
        /// </summary>
        private void ShowPausePanel()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
                if (debugMode)
                {
                    Debug.Log("<color=yellow>[GameFlowUICoordinator]</color> ⏸️ Pause panel activated");
                }
            }
            else
            {
                // Pause panel is optional, don't warn
                if (debugMode)
                {
                    Debug.Log("[GameFlowUICoordinator] Pause panel not assigned (optional)");
                }
            }

            // Hide end-game panels
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        /// <summary>
        /// Hides all UI panels.
        /// </summary>
        private void HideAllPanels()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);

            if (debugMode)
            {
                Debug.Log("<color=gray>[GameFlowUICoordinator]</color> All panels hidden");
            }
        }

        // ============================================
        // VALIDATION (Editor-only)
        // ============================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Warn if critical references are missing
            if (victoryPanel == null)
            {
                Debug.LogWarning("[GameFlowUICoordinator] Victory Panel is not assigned! Victory state won't show UI.");
            }

            if (gameOverPanel == null)
            {
                Debug.LogWarning("[GameFlowUICoordinator] GameOver Panel is not assigned! GameOver state won't show UI.");
            }
        }
#endif
    }
}
