using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.Core.Managers;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Main Menu UI Controller.
    /// Manages the main menu screen with Play and Quit buttons.
    /// Integrates with GameFlowManager for scene transitions.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        // ============================================
        // UI REFERENCES
        // ============================================

        [Header("Button References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        [Header("Optional: Animations & Feedback")]
        [SerializeField] private Animator menuAnimator; // Optional animator for transitions
        [SerializeField] private AudioSource clickSound; // Optional click sound

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Wire button events
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }
            else
            {
                Debug.LogError("[MainMenuUI] Play Button is not assigned!");
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
            else
            {
                Debug.LogError("[MainMenuUI] Quit Button is not assigned!");
            }
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }

        private void Start()
        {
            // Ensure time is running (in case we came from paused game)
            Time.timeScale = 1f;

            if (debugMode)
            {
                Debug.Log("<color=cyan>[MainMenuUI]</color> Main Menu initialized");
            }
        }

        // ============================================
        // BUTTON HANDLERS
        // ============================================

        /// <summary>
        /// Called when Play button is clicked.
        /// Starts the game via GameFlowManager.
        /// </summary>
        private void OnPlayClicked()
        {
            if (debugMode)
            {
                Debug.Log("<color=green>[MainMenuUI]</color> 🎮 Play button clicked!");
            }

            // Play click sound
            PlayClickSound();

            // Trigger animation (optional)
            TriggerTransitionAnimation("PlayTransition");

            // Start game via GameFlowManager
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.StartGame();
            }
            else
            {
                Debug.LogError("[MainMenuUI] GameFlowManager.Instance is null! Cannot start game.");
            }
        }

        /// <summary>
        /// Called when Quit button is clicked.
        /// Quits the application via GameFlowManager.
        /// </summary>
        private void OnQuitClicked()
        {
            if (debugMode)
            {
                Debug.Log("<color=yellow>[MainMenuUI]</color> 🚪 Quit button clicked!");
            }

            // Play click sound
            PlayClickSound();

            // Trigger animation (optional)
            TriggerTransitionAnimation("QuitTransition");

            // Quit game via GameFlowManager
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.QuitGame();
            }
            else
            {
                Debug.LogWarning("[MainMenuUI] GameFlowManager.Instance is null! Quitting directly.");
                QuitGameDirect();
            }
        }

        // ============================================
        // FEEDBACK HELPERS
        // ============================================

        /// <summary>
        /// Plays the click sound if assigned.
        /// </summary>
        private void PlayClickSound()
        {
            if (clickSound != null)
            {
                clickSound.Play();
            }
        }

        /// <summary>
        /// Triggers an animator transition if animator is assigned.
        /// </summary>
        private void TriggerTransitionAnimation(string triggerName)
        {
            if (menuAnimator != null)
            {
                menuAnimator.SetTrigger(triggerName);
            }
        }

        /// <summary>
        /// Direct quit fallback if GameFlowManager is not available.
        /// </summary>
        private void QuitGameDirect()
        {
#if UNITY_EDITOR
            Debug.Log("[MainMenuUI] Quit requested in Editor (would quit in build)");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ============================================
        // PUBLIC API (Optional for external control)
        // ============================================

        /// <summary>
        /// Programmatically trigger Play button.
        /// </summary>
        public void TriggerPlay()
        {
            OnPlayClicked();
        }

        /// <summary>
        /// Programmatically trigger Quit button.
        /// </summary>
        public void TriggerQuit()
        {
            OnQuitClicked();
        }
    }
}
