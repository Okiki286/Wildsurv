# GameFlow UI - Code Examples

## 📄 File Principali Creati

### 1. MainMenuUI.cs
**Funzione**: Controller per il Main Menu con bottoni Play e Quit.

**Codice Core**:
```csharp
private void OnPlayClicked()
{
    Debug.Log("[MainMenuUI] Play button clicked!");

    if (GameFlowManager.Instance != null)
    {
        GameFlowManager.Instance.StartGame();
    }
}

private void OnQuitClicked()
{
    Debug.Log("[MainMenuUI] Quit button clicked!");

    if (GameFlowManager.Instance != null)
    {
        GameFlowManager.Instance.QuitGame();
    }
}
```

**Setup in Unity**:
1. Aggiungi `MainMenuUI.cs` al MainMenuCanvas
2. Assegna i bottoni Play e Quit nell'Inspector
3. I listener vengono registrati automaticamente in `Awake()`

---

### 2. GameFlowUICoordinator.cs
**Funzione**: Ascolta `GameFlowManager.OnStateChanged` e mostra/nasconde i pannelli UI.

**Codice Core**:
```csharp
private void OnEnable()
{
    // Subscribe to state changes
    GameFlowManager.OnStateChanged += HandleStateChange;
}

private void OnDisable()
{
    // Unsubscribe to prevent memory leaks
    GameFlowManager.OnStateChanged -= HandleStateChange;
}

private void HandleStateChange(GameFlowManager.GameState newState)
{
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
            HideAllPanels();
            break;
    }
}

private void ShowVictoryPanel()
{
    victoryPanel.SetActive(true);
    gameOverPanel.SetActive(false);
    pausePanel.SetActive(false);
}
```

**Setup in Unity**:
1. Crea un GameObject `--- UI COORDINATOR ---` nella scena Gameplay
2. Aggiungi componente `GameFlowUICoordinator`
3. Assegna i riferimenti ai pannelli Victory, GameOver, Pause nell'Inspector

---

### 3. GameFlowManager.QuitGame() (NUOVO)
**Funzione**: Esce dall'applicazione (o stoppa Play Mode in Editor).

**Codice Aggiunto**:
```csharp
/// <summary>
/// Esce dall'applicazione.
/// In editor: stoppa Play Mode.
/// In build: chiude l'applicazione.
/// </summary>
public void QuitGame()
{
#if UNITY_EDITOR
    Debug.Log("[GameFlowManager] Stopping Play Mode (Editor)");
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Debug.Log("[GameFlowManager] Quitting application");
    Application.Quit();
#endif
}
```

**Uso**:
```csharp
// Da qualsiasi script
GameFlowManager.Instance.QuitGame();
```

---

## 🔄 Modifiche a VictoryUI e GameOverUI

**GIÀ ESISTENTI** - Nessuna modifica necessaria! I button handler già chiamano GameFlowManager.

### VictoryUI.cs - OnRestartClicked()
```csharp
private void OnRestartClicked()
{
    Debug.Log("[VictoryUI] Restart button clicked!");

    if (GameFlowManager.Instance != null)
    {
        GameFlowManager.Instance.RestartGame();
    }
    else if (GameManager.Instance != null)
    {
        GameManager.Instance.RestartGame(); // Fallback
    }
}
```

### VictoryUI.cs - OnMainMenuClicked()
```csharp
private void OnMainMenuClicked()
{
    Debug.Log("[VictoryUI] Main Menu button clicked!");

    if (GameFlowManager.Instance != null)
    {
        GameFlowManager.Instance.LoadMainMenu();
    }
    else
    {
        Debug.LogError("[VictoryUI] GameFlowManager not found!");
    }
}
```

**GameOverUI.cs ha gli stessi handler!**

---

## 🎯 GameFlowManager API Reference

### Scene Management
```csharp
// Load scenes
GameFlowManager.Instance.StartGame();      // MainMenu → Gameplay
GameFlowManager.Instance.LoadMainMenu();   // Any → MainMenu
GameFlowManager.Instance.RestartGame();    // Reload current scene
GameFlowManager.Instance.QuitGame();       // Exit app (NEW!)

// Example usage from button
private void OnBackToMenuClicked()
{
    GameFlowManager.Instance.LoadMainMenu();
}
```

### State Management
```csharp
// Trigger end-game states
GameFlowManager.Instance.TriggerVictory();  // State → Victory
GameFlowManager.Instance.TriggerGameOver(); // State → GameOver

// Pause management
GameFlowManager.Instance.Pause();           // State → Paused
GameFlowManager.Instance.Resume();          // Paused → Gameplay
GameFlowManager.Instance.TogglePause();     // Toggle pause on/off

// Example usage
if (Input.GetKeyDown(KeyCode.Escape))
{
    GameFlowManager.Instance.TogglePause();
}
```

### State Query
```csharp
// Check current state
GameFlowManager.GameState currentState = GameFlowManager.Instance.CurrentState;

if (currentState == GameFlowManager.GameState.Victory)
{
    Debug.Log("Player won!");
}

// Convenience properties
bool isPaused = GameFlowManager.Instance.IsPaused;
bool isInGameplay = GameFlowManager.Instance.IsInGameplay;
bool isGameEnded = GameFlowManager.Instance.IsGameEnded;
```

### Event Listening
```csharp
public class MyCustomUI : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to state changes
        GameFlowManager.OnStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks!
        GameFlowManager.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameFlowManager.GameState newState)
    {
        Debug.Log($"Game state changed to: {newState}");

        switch (newState)
        {
            case GameFlowManager.GameState.Gameplay:
                ShowGameplayUI();
                break;

            case GameFlowManager.GameState.Victory:
                PlayVictorySound();
                break;

            case GameFlowManager.GameState.GameOver:
                PlayGameOverSound();
                break;

            case GameFlowManager.GameState.Paused:
                ShowPauseMenu();
                break;
        }
    }
}
```

---

## 🎨 Custom UI Examples

### Example 1: Settings Panel with Return to Menu

```csharp
using UnityEngine;
using UnityEngine.UI;
using WildernessSurvival.Core.Managers;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        backButton.onClick.AddListener(OnBackClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnBackClicked()
    {
        // Resume game if paused
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.Resume();
        }

        gameObject.SetActive(false);
    }

    private void OnMainMenuClicked()
    {
        // Return to main menu
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.LoadMainMenu();
        }
    }
}
```

---

### Example 2: Custom Pause Menu

```csharp
using UnityEngine;
using UnityEngine.UI;
using WildernessSurvival.Core.Managers;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        resumeButton.onClick.AddListener(OnResumeClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnEnable()
    {
        // Listen for ESC key to close pause menu
        GameFlowManager.OnStateChanged += HandleStateChange;
    }

    private void OnDisable()
    {
        GameFlowManager.OnStateChanged -= HandleStateChange;
    }

    private void HandleStateChange(GameFlowManager.GameState newState)
    {
        // Hide pause menu when game resumes
        if (newState == GameFlowManager.GameState.Gameplay)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnResumeClicked()
    {
        GameFlowManager.Instance.Resume();
    }

    private void OnSettingsClicked()
    {
        settingsPanel.SetActive(true);
    }

    private void OnMainMenuClicked()
    {
        // Confirm dialog could be added here
        GameFlowManager.Instance.LoadMainMenu();
    }
}
```

---

### Example 3: In-Game HUD with Pause Button

```csharp
using UnityEngine;
using UnityEngine.UI;
using WildernessSurvival.Core.Managers;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;

    private void Awake()
    {
        pauseButton.onClick.AddListener(OnPauseClicked);
    }

    private void Update()
    {
        // ESC key to pause (optional, GameFlowManager already handles this)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPauseClicked();
        }
    }

    private void OnPauseClicked()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.Pause();
            pausePanel.SetActive(true);
        }
    }
}
```

---

### Example 4: Victory Screen with Stats

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.Core.Managers;

public class VictoryStatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI daysText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button mainMenuButton;

    private void OnEnable()
    {
        // Display stats when panel activates
        DisplayStats();

        nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void DisplayStats()
    {
        // Get stats from GameManager or other source
        if (GameManager.Instance != null)
        {
            int days = GameManager.Instance.DayNight.GetCurrentDay();
            int gold = GameManager.Instance.Resources.GetResource(ResourceType.Gold);

            daysText.text = $"Survived: {days} days";
            goldText.text = $"Gold Collected: {gold}";
        }
    }

    private void OnNextLevelClicked()
    {
        // Load next level (would need scene index management)
        GameFlowManager.Instance.StartGame();
    }

    private void OnMainMenuClicked()
    {
        GameFlowManager.Instance.LoadMainMenu();
    }
}
```

---

### Example 5: Confirmation Dialog for Quit

```csharp
using UnityEngine;
using UnityEngine.UI;
using WildernessSurvival.Core.Managers;

public class QuitConfirmationDialog : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnConfirmClicked()
    {
        // Actually quit the game
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.QuitGame();
        }
    }

    private void OnCancelClicked()
    {
        // Just close the dialog
        gameObject.SetActive(false);
    }

    // Call this from your main menu or pause menu
    public static void Show(GameObject dialogPrefab)
    {
        GameObject dialog = Instantiate(dialogPrefab);
        dialog.SetActive(true);
    }
}
```

---

## 🔧 Integration with Existing GameManager

Se vuoi che il GameManager triggheri automaticamente Victory/GameOver tramite GameFlowManager:

### GameManager.cs - TriggerVictory() Integration

**ALREADY IMPLEMENTED** (lines 624-656):
```csharp
public void TriggerVictory()
{
    if (isGameOver) return;

    Debug.Log("[GameManager] 🏆 VICTORY!");

    // Set local state
    isGameOver = true;
    isPaused = true;
    Time.timeScale = 0f;

    // Show local UI
    if (victoryUI != null)
    {
        victoryUI.SetActive(true);
    }

    // Delegate to GameFlowManager for global state
    if (GameFlowManager.Instance != null)
    {
        GameFlowManager.Instance.TriggerVictory();
    }
}
```

**Questo significa**: Quando chiami `GameManager.Instance.TriggerVictory()`, automaticamente:
1. GameManager mostra il pannello Victory locale
2. GameFlowManager cambia stato a `Victory`
3. `GameFlowUICoordinator` ascolta il cambio di stato e può fare azioni aggiuntive

---

## 📊 State Transition Diagram

```
┌─────────────┐
│    Boot     │
└──────┬──────┘
       │
       ▼
┌─────────────┐     StartGame()      ┌─────────────┐
│  MainMenu   │─────────────────────>│  Gameplay   │
└──────┬──────┘                      └──────┬──────┘
       ▲                                     │
       │                                     │ Pause()
       │ LoadMainMenu()                      ▼
       │                              ┌─────────────┐
       │                              │   Paused    │
       │                              └──────┬──────┘
       │                                     │
       │                                     │ Resume()
       │                                     ▼
       │                              ┌─────────────┐
       │<─────────────────────────────│  Gameplay   │
       │                              └──────┬──────┘
       │                                     │
       │                                     │ TriggerVictory()
       │                                     ▼
       │                              ┌─────────────┐
       └──────────────────────────────│   Victory   │
       │                              └─────────────┘
       │
       │                              ┌─────────────┐
       └──────────────────────────────│  GameOver   │
                                      └─────────────┘
                                            ▲
                                            │
                                            │ TriggerGameOver()
                                      ┌─────────────┐
                                      │  Gameplay   │
                                      └─────────────┘
```

---

**Code Examples Complete**: 2025-12-31
**Total Examples**: 8 (MainMenuUI, GameFlowUICoordinator, 5 Custom UIs, GameManager Integration)
**Estado**: ✅ Ready to Use
