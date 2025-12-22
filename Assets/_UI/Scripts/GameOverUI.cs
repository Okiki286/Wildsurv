using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WildernessSurvival.Core.Managers
{
    /// <summary>
    /// Componente UI per la schermata Game Over.
    /// Gestisce il display del motivo e il bottone restart.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button restartButton;

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
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }
        }

        private void OnEnable()
        {
            // Quando viene attivato, assicuriamoci che il gioco sia in pausa
            // (dovrebbe già esserlo, ma per sicurezza)
            Debug.Log("<color=red>[GameOverUI]</color> 💀 Game Over screen activated!");
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Imposta il messaggio del motivo del game over.
        /// </summary>
        public void SetReason(string reason)
        {
            if (subtitleText != null)
            {
                subtitleText.text = reason;
            }
        }

        /// <summary>
        /// Mostra la schermata con un motivo specifico.
        /// </summary>
        public void Show(string reason = "The Waystone has been destroyed...")
        {
            SetReason(reason);
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
            Debug.Log("[GameOverUI] Restart button clicked!");
            
            // Usa GameManager per riavviare
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                // Fallback: ricarica scena direttamente
                Debug.LogWarning("[GameOverUI] GameManager.Instance is null, reloading scene directly.");
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }
    }
}
