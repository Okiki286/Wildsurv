using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using Wilderness.Core;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.UI.LoadGame;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI principale per il menu di pausa.
    /// Gestisce pausa del gioco, salvataggio, caricamento, e uscita.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static PauseMenuUI Instance { get; private set; }

        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [TitleGroup("Riferimenti UI")]
        [Required]
        [SerializeField] private GameObject pauseMenuPanel;

        [TitleGroup("Riferimenti UI")]
        [InfoBox("Reference to the Load Game Window. Click 'Find Load Window' button below to auto-populate.", InfoMessageType.Info)]
        [SerializeField] private LoadGameUI loadGameWindow;

        [TitleGroup("Bottoni")]
        [Required]
        [SerializeField] private Button resumeButton;

        [Required]
        [SerializeField] private Button saveButton;

        [Required]
        [SerializeField] private Button loadButton;

        [Required]
        [SerializeField] private Button quitButton;

        [TitleGroup("Testi")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [TitleGroup("Configurazione")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
        [SerializeField] private bool pauseTimeOnOpen = true;

        [TitleGroup("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip buttonClickSound;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME
        // ============================================

        private AudioSource audioSource;
        private bool isPaused = false;

        public bool IsPaused => isPaused;
        public bool IsOpen => pauseMenuPanel != null && pauseMenuPanel.activeSelf;

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

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            SetupButtons();
            Hide();

            if (titleText != null)
            {
                titleText.text = "PAUSA";
            }
        }

        private void Update()
        {
            // Toggle menu con tasto Escape
            if (Input.GetKeyDown(toggleKey))
            {
                TogglePause();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // SETUP
        // ============================================

        private void SetupButtons()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(OnLoadClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        // ============================================
        // SHOW / HIDE
        // ============================================

        [TitleGroup("Azioni")]
        [Button("Show Pause Menu", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        public void Show()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
                isPaused = true;

                if (pauseTimeOnOpen)
                {
                    Time.timeScale = 0f;

                    // Notifica DayNightSystem della pausa
                    if (DayNightSystem.Instance != null)
                    {
                        DayNightSystem.Instance.SetPaused(true);
                    }
                }

                PlaySound(openSound);

                if (debugMode)
                {
                    Debug.Log("<color=green>[PauseMenuUI]</color> Menu di pausa aperto");
                }
            }
        }

        [Button("Hide Pause Menu", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        public void Hide()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
                isPaused = false;

                if (pauseTimeOnOpen)
                {
                    Time.timeScale = 1f;

                    // Notifica DayNightSystem della ripresa
                    if (DayNightSystem.Instance != null)
                    {
                        DayNightSystem.Instance.SetPaused(false);
                    }
                }

                PlaySound(closeSound);
                ClearFeedback();

                if (debugMode)
                {
                    Debug.Log("<color=yellow>[PauseMenuUI]</color> Menu di pausa chiuso");
                }
            }
        }

        public void TogglePause()
        {
            if (IsOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        // ============================================
        // BUTTON CALLBACKS
        // ============================================

        private void OnResumeClicked()
        {
            PlaySound(buttonClickSound);
            Hide();

            if (debugMode)
            {
                Debug.Log("<color=cyan>[PauseMenuUI]</color> Gioco ripreso");
            }
        }

        private void OnSaveClicked()
        {
            PlaySound(buttonClickSound);

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                ShowFeedback("Gioco salvato!", Color.green);

                if (debugMode)
                {
                    Debug.Log("<color=green>[PauseMenuUI]</color> Salvataggio completato");
                }
            }
            else
            {
                ShowFeedback("Errore: SaveManager non trovato", Color.red);
                Debug.LogError("[PauseMenuUI] SaveManager.Instance è null!");
            }
        }

        private void OnLoadClicked()
        {
            PlaySound(buttonClickSound);

            // NEW BEHAVIOR: Open Load Game Window instead of loading directly
            if (loadGameWindow != null)
            {
                loadGameWindow.Show();

                if (debugMode)
                {
                    Debug.Log("<color=cyan>[PauseMenuUI]</color> Opening Load Game Window...");
                }

                // NOTE: Pause Menu stays visible behind Load Game Window
                // This allows the player to see both menus and choose to cancel
            }
            else
            {
                // FALLBACK: If Load Window is not assigned, show error
                ShowFeedback("Errore: Load Game Window non assegnato!", Color.red);
                Debug.LogError("[PauseMenuUI] LoadGameUI reference is null! Please assign it in the Inspector or use 'Find Load Window' button.");
            }
        }

        private void OnQuitClicked()
        {
            PlaySound(buttonClickSound);

            if (debugMode)
            {
                Debug.Log("<color=red>[PauseMenuUI]</color> Uscita dal gioco");
            }

            // In editor, ferma il play mode
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        // ============================================
        // FEEDBACK
        // ============================================

        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                feedbackText.gameObject.SetActive(true);

                // Auto-clear dopo 3 secondi
                CancelInvoke(nameof(ClearFeedback));
                Invoke(nameof(ClearFeedback), 3f);
            }
        }

        private void ClearFeedback()
        {
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }

        // ============================================
        // AUDIO
        // ============================================

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // ============================================
        // ODIN EDITOR UTILITIES
        // ============================================

        #if UNITY_EDITOR
        [TitleGroup("Riferimenti UI")]
        [Button("🔍 Find Load Window", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 1f)]
        [PropertyTooltip("Auto-find LoadGameUI in the scene and assign it")]
        private void FindLoadWindow()
        {
            loadGameWindow = FindFirstObjectByType<LoadGameUI>();

            if (loadGameWindow != null)
            {
                Debug.Log($"<color=green>[PauseMenuUI]</color> ✅ Found LoadGameUI: {loadGameWindow.gameObject.name}");
                UnityEditor.EditorUtility.SetDirty(this);
            }
            else
            {
                Debug.LogWarning("<color=yellow>[PauseMenuUI]</color> ⚠️ LoadGameUI not found in scene. Please generate it using Tools → Wilderness → Load Menu Generator");
            }
        }
        #endif
    }
}
