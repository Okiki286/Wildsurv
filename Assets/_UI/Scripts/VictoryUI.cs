using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WildernessSurvival.Core.Managers
{
    /// <summary>
    /// Componente UI per la schermata Victory.
    /// Gestisce il display dei giorni sopravvissuti e il bottone restart.
    /// </summary>
    public class VictoryUI : MonoBehaviour
    {
        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Setup restart button
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            // Setup main menu button
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        private void OnEnable()
        {
            Debug.Log("<color=green>[VictoryUI]</color> 🏆 Victory screen activated!");
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Imposta il messaggio con i giorni sopravvissuti.
        /// </summary>
        public void SetMessage(string message)
        {
            if (subtitleText != null)
            {
                subtitleText.text = message;
            }
        }

        /// <summary>
        /// Mostra la schermata con un messaggio specifico.
        /// </summary>
        public void Show(string message = "You survived and thrived!")
        {
            SetMessage(message);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Nasconde la schermata.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ============================================
        // BUTTON HANDLERS
        // ============================================

        private void OnRestartClicked()
        {
            Debug.Log("[VictoryUI] Restart button clicked!");

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.RestartGame();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                Debug.LogWarning("[VictoryUI] No manager found, reloading scene directly.");
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("[VictoryUI] Main Menu button clicked!");

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.LoadMainMenu();
            }
            else
            {
                Debug.LogError("[VictoryUI] GameFlowManager not found! Cannot load Main Menu.");
            }
        }
    }
}
