# GameFlowManager - Integration Guide

## 📋 Overview

Il **GameFlowManager** è un Finite State Machine (FSM) persistente che gestisce il flusso dell'applicazione separato dalla logica di gameplay.

### Separazione delle Responsabilità

| Manager | Responsabilità |
|---------|----------------|
| **GameFlowManager** | Scene loading, Stati globali (Boot/Menu/Gameplay/Paused/Victory/GameOver), Transizioni, Persistenza tra scene |
| **GameManager** | Logica di gameplay, Inizializzazione sistemi di gioco, Riferimenti a DayNight/Resources/Workers, Boot sequence in-game |

---

## 🎯 Stati del GameFlowManager

```csharp
public enum GameState
{
    Boot,       // Inizializzazione iniziale
    MainMenu,   // Menu principale
    Gameplay,   // Gioco attivo
    Paused,     // Gioco in pausa
    Victory,    // Vittoria
    GameOver    // Sconfitta
}
```

---

## 🔧 Build Settings Configuration

### 1. Configura le Scene nel Build

Vai a **File → Build Settings → Scenes In Build** e assicurati di avere:

```
[0] MainMenu    (o il nome della tua scena menu)
[1] Game        (o il nome della tua scena gameplay)
```

### 2. Opzioni di Configurazione

Il GameFlowManager può usare **Scene Names** o **Scene Indices**:

#### Opzione A: Scene Names (Consigliato)
Imposta nel GameFlowManager Inspector:
- `Main Menu Scene Name`: `"MainMenu"`
- `Gameplay Scene Name`: `"Game"`

#### Opzione B: Scene Indices (Fallback)
Se i nomi sono vuoti, usa gli indici:
- `Main Menu Scene Index`: `0`
- `Gameplay Scene Index`: `1`

---

## 🚀 Setup Iniziale

### Step 1: Crea il GameObject GameFlowManager

1. Crea un **GameObject vuoto** nella scena di boot (o prima scena caricata)
2. Rinominalo: `"--- GAME FLOW ---"`
3. Aggiungi il componente `GameFlowManager.cs`
4. Configura i nomi delle scene nell'Inspector

### Step 2: Hierarchy Consigliata

```
--- GAME FLOW ---
    └─ GameFlowManager (Script)

--- MANAGERS ---
    ├─ GameManager (Script)
    ├─ DayNightSystem (Script)
    └─ ResourceSystem (Script)
```

**IMPORTANTE**: Il GameFlowManager è **persistente** (DontDestroyOnLoad) e sopravvive al cambio di scena. Il GameManager invece è specifico della scena Gameplay.

---

## 🔗 Integrazione con GameManager

### Modifica 1: Delegare TriggerVictory

**Prima** (GameManager.cs:624):
```csharp
public void TriggerVictory()
{
    if (isGameOver) return;

    Debug.Log("<color=green>[GameManager] 🏆 VICTORY!</color>");

    isGameOver = true;
    isPaused = true;
    Time.timeScale = 0f;
    dayNightSystem?.SetPaused(true);

    if (victoryUI != null)
    {
        victoryUI.SetActive(true);
    }
}
```

**Dopo** (Delegato a GameFlowManager):
```csharp
public void TriggerVictory()
{
    if (isGameOver) return;

    Debug.Log("<color=green>[GameManager] 🏆 VICTORY! Delegating to GameFlowManager...</color>");

    // Set local state
    isGameOver = true;
    isPaused = true;
    dayNightSystem?.SetPaused(true);

    // Show Victory UI
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

### Modifica 2: Delegare TriggerGameOver

**Prima** (GameManager.cs:587):
```csharp
public void TriggerGameOver(string reason = "Base Destroyed")
{
    if (isGameOver) return;

    Debug.Log($"<color=red>[GameManager] GAME OVER: {reason}</color>");

    isGameOver = true;
    isPaused = true;
    Time.timeScale = 0f;
    dayNightSystem?.SetPaused(true);
    onGameOver?.Raise();

    if (gameOverUI != null)
    {
        gameOverUI.SetActive(true);
    }
}
```

**Dopo** (Delegato a GameFlowManager):
```csharp
public void TriggerGameOver(string reason = "Base Destroyed")
{
    if (isGameOver) return;

    Debug.Log($"<color=red>[GameManager] GAME OVER: {reason}. Delegating to GameFlowManager...</color>");

    // Set local state
    isGameOver = true;
    isPaused = true;
    dayNightSystem?.SetPaused(true);
    onGameOver?.Raise();

    // Show Game Over UI
    if (gameOverUI != null)
    {
        gameOverUI.SetActive(true);
    }

    // Delegate to GameFlowManager for global state
    if (GameFlowManager.Instance != null)
    {
        GameFlowManager.Instance.TriggerGameOver();
    }
}
```

### Modifica 3: Delegare RestartGame

**Prima** (GameManager.cs:658):
```csharp
public void RestartGame()
{
    Debug.Log("[GameManager] Riavvio gioco...");

    Time.timeScale = 1f;
    isPaused = false;
    isGameOver = false;
    isInitialized = false;
    bootSequenceComplete = false;

    UnityEngine.SceneManagement.SceneManager.LoadScene(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
    );
}
```

**Dopo** (Delegato a GameFlowManager):
```csharp
public void RestartGame()
{
    Debug.Log("[GameManager] Restart requested, delegating to GameFlowManager...");

    // Reset local state (will be re-initialized on scene load)
    isPaused = false;
    isGameOver = false;
    isInitialized = false;
    bootSequenceComplete = false;

    // Delegate to GameFlowManager
    if (GameFlowManager.Instance != null)
    {
        GameFlowManager.Instance.RestartGame();
    }
    else
    {
        // Fallback if GameFlowManager doesn't exist
        Debug.LogWarning("[GameManager] GameFlowManager not found, using fallback scene reload.");
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}
```

### Modifica 4: Rimuovere Pause Logic da GameManager (Opzionale)

Se vuoi centralizzare la pausa nel GameFlowManager, rimuovi `TogglePause()` da GameManager.Update() e lascia che GameFlowManager gestisca ESC.

**Prima**:
```csharp
private void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
    {
        TogglePause();
    }
}
```

**Dopo** (Rimuovi completamente, GameFlowManager gestisce ESC):
```csharp
// Pause handling now in GameFlowManager
```

---

## 📡 Ascoltare Cambi di Stato (Event Pattern)

### Esempio: UI Manager

```csharp
using UnityEngine;
using WildernessSurvival.Core.Managers;

public class UIManager : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to state changes
        GameFlowManager.OnStateChanged += HandleStateChange;
    }

    private void OnDisable()
    {
        // Unsubscribe
        GameFlowManager.OnStateChanged -= HandleStateChange;
    }

    private void HandleStateChange(GameFlowManager.GameState newState)
    {
        Debug.Log($"[UIManager] State changed to: {newState}");

        switch (newState)
        {
            case GameFlowManager.GameState.MainMenu:
                ShowMainMenuUI();
                break;

            case GameFlowManager.GameState.Gameplay:
                ShowGameplayHUD();
                break;

            case GameFlowManager.GameState.Paused:
                ShowPauseMenu();
                break;

            case GameFlowManager.GameState.Victory:
                ShowVictoryScreen();
                break;

            case GameFlowManager.GameState.GameOver:
                ShowGameOverScreen();
                break;
        }
    }
}
```

### Esempio: Audio Manager

```csharp
private void OnEnable()
{
    GameFlowManager.OnStateChanged += HandleStateChange;
}

private void HandleStateChange(GameFlowManager.GameState newState)
{
    switch (newState)
    {
        case GameFlowManager.GameState.MainMenu:
            PlayMenuMusic();
            break;

        case GameFlowManager.GameState.Gameplay:
            PlayGameplayMusic();
            break;

        case GameFlowManager.GameState.Paused:
            PauseMusic();
            break;

        case GameFlowManager.GameState.Victory:
            PlayVictoryMusic();
            break;

        case GameFlowManager.GameState.GameOver:
            PlayGameOverMusic();
            break;
    }
}
```

---

## 🎮 API Pubblica

### Scene Loading

```csharp
// Torna al Main Menu
GameFlowManager.Instance.LoadMainMenu();

// Avvia nuova partita (carica scena gameplay)
GameFlowManager.Instance.StartGame();

// Ricarica scena corrente
GameFlowManager.Instance.RestartGame();
```

### Pause/Resume

```csharp
// Metti in pausa
GameFlowManager.Instance.Pause();

// Riprendi
GameFlowManager.Instance.Resume();

// Toggle pause
GameFlowManager.Instance.TogglePause();
```

### Game End

```csharp
// Vittoria
GameFlowManager.Instance.TriggerVictory();

// Sconfitta
GameFlowManager.Instance.TriggerGameOver();

// Generico (true = victory, false = game over)
GameFlowManager.Instance.EndGame(victory: true);
```

### State Queries

```csharp
// Stato corrente
GameFlowManager.GameState currentState = GameFlowManager.Instance.CurrentState;

// Stato precedente
GameFlowManager.GameState previousState = GameFlowManager.Instance.PreviousState;

// In pausa?
bool isPaused = GameFlowManager.Instance.IsPaused;

// In gameplay? (Gameplay o Paused)
bool isInGameplay = GameFlowManager.Instance.IsInGameplay;

// Gioco terminato? (Victory o GameOver)
bool isEnded = GameFlowManager.Instance.IsGameEnded;
```

---

## 🔍 Debug in Editor (Odin Inspector)

Il GameFlowManager espone bottoni nell'Inspector per testare transizioni:

### Scene Controls
- **📋 Main Menu**: Carica Main Menu
- **🎮 Start Game**: Carica Gameplay
- **🔄 Restart**: Ricarica scena corrente

### State Controls
- **⏸️ Pause**: Metti in pausa
- **▶️ Resume**: Riprendi

### End Game Controls
- **🏆 Victory**: Trigger vittoria
- **💀 Game Over**: Trigger game over

---

## 🧪 Testing Checklist

### ✅ Scene Transitions
- [ ] Main Menu → Gameplay funziona
- [ ] Gameplay → Main Menu funziona
- [ ] Restart in Gameplay ricarica correttamente
- [ ] Restart in Main Menu ricarica correttamente

### ✅ State Management
- [ ] Pause blocca Time.timeScale = 0
- [ ] Resume ripristina Time.timeScale = 1
- [ ] Victory imposta stato e blocca input
- [ ] GameOver imposta stato e blocca input

### ✅ Event System
- [ ] OnStateChanged viene invocato ad ogni cambio
- [ ] UI reagisce ai cambi di stato
- [ ] Audio reagisce ai cambi di stato

### ✅ Persistenza
- [ ] GameFlowManager sopravvive a LoadScene
- [ ] Singleton non si duplica tra scene

---

## 🚨 Common Issues

### Issue 1: "GameFlowManager.Instance is null"
**Causa**: GameFlowManager non esiste nella scena iniziale.
**Fix**: Aggiungi un GameObject con GameFlowManager nella scena di boot.

### Issue 2: "Scene not found in Build Settings"
**Causa**: Nome scena errato o scena non aggiunta al Build.
**Fix**:
1. Vai a File → Build Settings
2. Aggiungi le scene necessarie
3. Verifica i nomi nel GameFlowManager Inspector

### Issue 3: "Time.timeScale non si ripristina dopo pause"
**Causa**: Conflitto tra GameManager e GameFlowManager che gestiscono entrambi timeScale.
**Fix**: Delega **tutta** la gestione di timeScale al GameFlowManager.

### Issue 4: "OnStateChanged non viene invocato"
**Causa**: Subscribe fatto troppo tardi o Unsubscribe mancante.
**Fix**:
```csharp
private void OnEnable()
{
    GameFlowManager.OnStateChanged += YourHandler;
}

private void OnDisable()
{
    GameFlowManager.OnStateChanged -= YourHandler;
}
```

---

## 📚 Example: Main Menu Button Integration

### MainMenuUI.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using WildernessSurvival.Core.Managers;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnPlayClicked()
    {
        // Delegate to GameFlowManager
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartGame();
        }
    }

    private void OnQuitClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
```

### GameOverUI.cs / VictoryUI.cs

```csharp
private void OnRestartClicked()
{
    Debug.Log("[VictoryUI] Restart button clicked!");

    // Delegate to GameFlowManager
    if (GameFlowManager.Instance != null)
    {
        GameFlowManager.Instance.RestartGame();
    }
    else
    {
        // Fallback
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
}
```

---

## 🎯 Migration Summary

### GameManager Delegation Pattern

```
OLD FLOW:
GameManager.TriggerVictory()
    → Set local state
    → Set Time.timeScale = 0
    → Show UI

NEW FLOW:
GameManager.TriggerVictory()
    → Set local gameplay state
    → Show gameplay UI
    → GameFlowManager.Instance.TriggerVictory()
        → Set global app state
        → Set Time.timeScale = 0
        → Notify all listeners
```

### Benefits

1. **Separation of Concerns**: GameManager = gameplay, GameFlowManager = app flow
2. **Scene Independence**: GameFlowManager persists, GameManager doesn't
3. **Centralized State**: One source of truth for app state
4. **Event-Driven**: UI/Audio can react to state changes without tight coupling
5. **Testability**: Easier to test scene transitions and state logic

---

## 📝 Next Steps

1. ✅ Crea GameObject con GameFlowManager
2. ✅ Configura scene names/indices
3. ✅ Modifica GameManager secondo le istruzioni sopra
4. ✅ Aggiorna GameOverUI/VictoryUI per usare GameFlowManager
5. ✅ Testa tutte le transizioni di scena
6. ✅ Implementa listener per UI/Audio
7. ✅ Rimuovi logica duplicata da GameManager

---

**Created by**: Unity Senior Architect
**Date**: 2025-12-31
**Version**: 1.0
