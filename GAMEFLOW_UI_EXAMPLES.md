# GameFlowManager - UI Integration Examples

Esempi pratici di come aggiornare le UI esistenti per usare il GameFlowManager.

---

## 🔄 GameOverUI.cs - Updated Version

### Before (Using GameManager directly)

```csharp
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
```

### After (Using GameFlowManager)

```csharp
private void OnRestartClicked()
{
    Debug.Log("[GameOverUI] Restart button clicked!");

    // Delegate to GameFlowManager (preferred)
    if (GameFlowManager.Instance != null)
    {
        GameFlowManager.Instance.RestartGame();
    }
    else if (GameManager.Instance != null)
    {
        // Fallback to GameManager if GameFlowManager not available
        GameManager.Instance.RestartGame();
    }
    else
    {
        // Last resort: direct scene reload
        Debug.LogWarning("[GameOverUI] No manager found, reloading scene directly.");
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}
```

### Enhanced Version (With Main Menu Button)

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WildernessSurvival.Core.Managers
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton; // NEW

        private void Awake()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            // NEW: Main Menu button
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

        private void OnRestartClicked()
        {
            Debug.Log("[GameOverUI] Restart button clicked!");

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
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }

        // NEW: Main Menu handler
        private void OnMainMenuClicked()
        {
            Debug.Log("[GameOverUI] Main Menu button clicked!");

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.LoadMainMenu();
            }
            else
            {
                Debug.LogError("[GameOverUI] GameFlowManager not found! Cannot load Main Menu.");
            }
        }
    }
}
```

---

## 🏆 VictoryUI.cs - Updated Version

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WildernessSurvival.Core.Managers
{
    public class VictoryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton; // NEW

        private void Awake()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

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

        public void SetMessage(string message)
        {
            if (subtitleText != null)
            {
                subtitleText.text = message;
            }
        }

        public void Show(string message = "You survived and thrived!")
        {
            SetMessage(message);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

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
```

---

## 📋 PauseMenuUI.cs - Example

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace WildernessSurvival.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            resumeButton?.onClick.AddListener(OnResumeClicked);
            mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);
        }

        private void OnEnable()
        {
            // Subscribe to state changes to auto-show/hide
            GameFlowManager.OnStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            GameFlowManager.OnStateChanged -= HandleStateChange;
        }

        private void OnDestroy()
        {
            resumeButton?.onClick.RemoveListener(OnResumeClicked);
            mainMenuButton?.onClick.RemoveListener(OnMainMenuClicked);
            quitButton?.onClick.RemoveListener(OnQuitClicked);
        }

        private void HandleStateChange(GameFlowManager.GameState newState)
        {
            // Auto show/hide based on state
            if (newState == GameFlowManager.GameState.Paused)
            {
                gameObject.SetActive(true);
            }
            else if (newState == GameFlowManager.GameState.Gameplay)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnResumeClicked()
        {
            Debug.Log("[PauseMenuUI] Resume clicked!");

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.Resume();
            }
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("[PauseMenuUI] Main Menu clicked!");

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.LoadMainMenu();
            }
        }

        private void OnQuitClicked()
        {
            Debug.Log("[PauseMenuUI] Quit clicked!");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
```

---

## 🎮 MainMenuUI.cs - Example

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace WildernessSurvival.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        private void Awake()
        {
            playButton?.onClick.AddListener(OnPlayClicked);
            continueButton?.onClick.AddListener(OnContinueClicked);
            settingsButton?.onClick.AddListener(OnSettingsClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);
        }

        private void Start()
        {
            // Check if save exists for Continue button
            bool hasSave = CheckSaveExists();
            if (continueButton != null)
            {
                continueButton.interactable = hasSave;
            }
        }

        private void OnDestroy()
        {
            playButton?.onClick.RemoveListener(OnPlayClicked);
            continueButton?.onClick.RemoveListener(OnContinueClicked);
            settingsButton?.onClick.RemoveListener(OnSettingsClicked);
            quitButton?.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnPlayClicked()
        {
            Debug.Log("[MainMenuUI] Play (New Game) clicked!");

            // Start new game via GameFlowManager
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.StartGame();
            }
            else
            {
                Debug.LogError("[MainMenuUI] GameFlowManager not found!");
            }
        }

        private void OnContinueClicked()
        {
            Debug.Log("[MainMenuUI] Continue clicked!");

            // Load saved game
            if (GameFlowManager.Instance != null)
            {
                // Start game, SaveManager will auto-load in GameManager.BootSequence
                GameFlowManager.Instance.StartGame();
            }
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuUI] Settings clicked!");

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            }
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenuUI] Quit clicked!");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private bool CheckSaveExists()
        {
            // Check if PlayerPrefs or save file exists
            return PlayerPrefs.HasKey("SaveSlot_0");
        }
    }
}
```

---

## 🎨 UI Manager - Central UI Controller

```csharp
using UnityEngine;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Manager centrale per tutte le UI.
    /// Reagisce ai cambi di stato del GameFlowManager.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Screens")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Subscribe to state changes
            GameFlowManager.OnStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            GameFlowManager.OnStateChanged -= HandleStateChange;
        }

        private void HandleStateChange(GameFlowManager.GameState newState)
        {
            Debug.Log($"[UIManager] State changed to: {newState}");

            // Hide all panels first
            HideAll();

            // Show appropriate panel
            switch (newState)
            {
                case GameFlowManager.GameState.Gameplay:
                    ShowHUD();
                    break;

                case GameFlowManager.GameState.Paused:
                    ShowHUD(); // Keep HUD visible
                    ShowPauseMenu();
                    break;

                case GameFlowManager.GameState.Victory:
                    ShowVictoryScreen();
                    break;

                case GameFlowManager.GameState.GameOver:
                    ShowGameOverScreen();
                    break;

                case GameFlowManager.GameState.MainMenu:
                    // Main Menu has its own UI, hide gameplay UI
                    break;
            }
        }

        private void HideAll()
        {
            hudPanel?.SetActive(false);
            pauseMenuPanel?.SetActive(false);
            gameOverPanel?.SetActive(false);
            victoryPanel?.SetActive(false);
        }

        public void ShowHUD()
        {
            hudPanel?.SetActive(true);
        }

        public void ShowPauseMenu()
        {
            pauseMenuPanel?.SetActive(true);
        }

        public void ShowGameOverScreen()
        {
            gameOverPanel?.SetActive(true);
        }

        public void ShowVictoryScreen()
        {
            victoryPanel?.SetActive(true);
        }
    }
}
```

---

## 🔊 Audio Manager - Example

```csharp
using UnityEngine;

namespace WildernessSurvival.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music Tracks")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField] private AudioClip victoryMusic;
        [SerializeField] private AudioClip gameOverMusic;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // Subscribe to state changes
            GameFlowManager.OnStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            GameFlowManager.OnStateChanged -= HandleStateChange;
        }

        private void HandleStateChange(GameFlowManager.GameState newState)
        {
            Debug.Log($"[AudioManager] State changed to: {newState}");

            switch (newState)
            {
                case GameFlowManager.GameState.MainMenu:
                    PlayMusic(menuMusic);
                    break;

                case GameFlowManager.GameState.Gameplay:
                    PlayMusic(gameplayMusic);
                    break;

                case GameFlowManager.GameState.Paused:
                    PauseMusic();
                    break;

                case GameFlowManager.GameState.Victory:
                    PlayMusic(victoryMusic);
                    break;

                case GameFlowManager.GameState.GameOver:
                    PlayMusic(gameOverMusic);
                    break;
            }
        }

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;

            if (musicSource.clip != clip)
            {
                musicSource.clip = clip;
                musicSource.Play();
            }
            else if (!musicSource.isPlaying)
            {
                musicSource.UnPause();
            }
        }

        private void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }
    }
}
```

---

## 📌 Summary

### Key Patterns

1. **Delegation**: UI buttons call `GameFlowManager.Instance.Method()`
2. **Fallback**: Check GameFlowManager first, then GameManager, then direct action
3. **Event Listening**: Subscribe to `GameFlowManager.OnStateChanged`
4. **Auto-Show/Hide**: UI panels activate/deactivate based on state changes
5. **DontDestroyOnLoad**: AudioManager persists like GameFlowManager

### Benefits

- ✅ Centralized state management
- ✅ Decoupled UI from gameplay logic
- ✅ Event-driven architecture
- ✅ Easy to extend (add new states/listeners)
- ✅ Clean scene transitions

---

**Created**: 2025-12-31
**Version**: 1.0
