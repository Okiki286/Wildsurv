# GameFlowManager - UI Integration Guide

## 📦 File Creati

### 1. MainMenuUI.cs
**Percorso**: `Assets/_UI/Scripts/MainMenuUI.cs`

Script per il Main Menu con bottoni Play e Quit.

**Features**:
- ✅ `OnPlayClicked()`: Chiama `GameFlowManager.Instance.StartGame()`
- ✅ `OnQuitClicked()`: Chiama `GameFlowManager.Instance.QuitGame()`
- ✅ Supporto opzionale per animazioni (Animator)
- ✅ Supporto opzionale per suoni di click (AudioSource)
- ✅ Fallback se GameFlowManager non esiste

### 2. GameFlowUICoordinator.cs
**Percorso**: `Assets/_UI/Scripts/GameFlowUICoordinator.cs`

Coordinator che ascolta `GameFlowManager.OnStateChanged` e attiva/disattiva i pannelli UI.

**Features**:
- ✅ Ascolta cambio stato Victory → mostra Victory Panel
- ✅ Ascolta cambio stato GameOver → mostra GameOver Panel
- ✅ Ascolta cambio stato Paused → mostra Pause Panel (opzionale)
- ✅ Nasconde tutti i pannelli quando si torna a Gameplay o MainMenu

---

## 🔧 Modifiche ai File Esistenti

### GameFlowManager.cs
**Aggiunto metodo**: `QuitGame()` (linee 419-438)

```csharp
/// <summary>
/// Esce dall'applicazione.
/// In editor: stoppa Play Mode.
/// In build: chiude l'applicazione.
/// </summary>
public void QuitGame()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}
```

### VictoryUI.cs e GameOverUI.cs
**GIÀ CONFIGURATI** (nessuna modifica necessaria):
- ✅ `OnRestartClicked()` chiama `GameFlowManager.Instance.RestartGame()`
- ✅ `OnMainMenuClicked()` chiama `GameFlowManager.Instance.LoadMainMenu()`

---

## 🚀 Setup nella Scena Main Menu

### Step 1: Crea Main Menu Scene
1. **File → New Scene**
2. Salva come: `Assets/Scenes/MainMenu.unity`
3. Aggiungi la scena al Build Settings:
   - **File → Build Settings → Add Open Scenes**
   - Assicurati che sia indice `[0]`

### Step 2: Crea UI Main Menu
1. **GameObject → UI → Canvas** (crea `MainMenuCanvas`)
2. Dentro il Canvas, crea:
   - **Panel** (background del menu)
   - **Button** (rinomina in `PlayButton`)
     - Cambia testo in "▶ PLAY"
   - **Button** (rinomina in `QuitButton`)
     - Cambia testo in "🚪 QUIT"

### Step 3: Aggiungi MainMenuUI Script
1. Seleziona `MainMenuCanvas` in Hierarchy
2. **Add Component → MainMenuUI**
3. Nell'Inspector, assegna:
   - `Play Button`: trascina `PlayButton` qui
   - `Quit Button`: trascina `QuitButton` qui
4. (Opzionale) Assegna:
   - `Menu Animator`: trascina un Animator se hai animazioni
   - `Click Sound`: trascina un AudioSource per i suoni

### Step 4: Verifica GameFlowManager Config
1. Seleziona `--- GAME FLOW ---` GameObject (se esiste in scena)
2. Nell'Inspector, verifica:
   - `Main Menu Scene Name`: `"MainMenu"`
   - `Gameplay Scene Name`: `"Game"` (o il nome della tua scena di gioco)
3. Se non hai il GameObject, verrà auto-creato (vedi `GAMEFLOW_PERSISTENCE_FIX.md`)

---

## 🎮 Setup nella Scena Gameplay

### Step 1: Crea UI Coordinator GameObject
1. Nella scena Gameplay, crea un GameObject vuoto
2. Rinomina in: `--- UI COORDINATOR ---`
3. **Add Component → GameFlowUICoordinator**

### Step 2: Assegna UI Panel References
1. Seleziona `--- UI COORDINATOR ---` in Hierarchy
2. Nell'Inspector del componente `GameFlowUICoordinator`, assegna:
   - `Victory Panel`: trascina il GameObject `VictoryCanvas` (o `VictoryPanel`)
   - `GameOver Panel`: trascina il GameObject `GameOverCanvas` (o `GameOverPanel`)
   - `Pause Panel`: (opzionale) trascina un pannello di pausa se ne hai uno

### Step 3: Verifica VictoryUI e GameOverUI
1. Seleziona `VictoryCanvas` (o il GameObject che contiene VictoryUI)
2. Nell'Inspector del componente `VictoryUI`, verifica che ci siano:
   - `Restart Button`: ✅ Assegnato
   - `Main Menu Button`: ✅ Assegnato
3. Ripeti per `GameOverUI`

---

## 🧪 Testing Workflow

### Test 1: Main Menu → Gameplay
1. Apri la scena `MainMenu`
2. Premi **Play** in Unity
3. Clicca il bottone "▶ PLAY"
4. **Aspettativa**: La scena Gameplay si carica

**Console Log Aspettato**:
```
[MainMenuUI] 🎮 Play button clicked!
[GameFlowManager] Starting new game...
[GameFlowManager] State: MainMenu → Gameplay
```

### Test 2: Victory Flow
1. Durante il gameplay, triggera Victory (es. da Inspector di GameManager, bottone "🏆 Victory")
2. **Aspettativa**:
   - Il pannello Victory appare
   - Il gioco va in pausa (Time.timeScale = 0)
   - I bottoni "Restart" e "Main Menu" funzionano

**Console Log Aspettato**:
```
[GameManager] 🏆 VICTORY! You survived!
[GameFlowManager] State: Gameplay → Victory
[GameFlowUICoordinator] 🏆 Victory panel activated
```

3. Clicca "Restart":
   - **Aspettativa**: La scena Gameplay si ricarica
   - **Log**: `[GameFlowManager] Restarting current scene...`

4. Triggera di nuovo Victory, poi clicca "Main Menu":
   - **Aspettativa**: Torna alla scena Main Menu
   - **Log**: `[GameFlowManager] Loading Main Menu...`

### Test 3: GameOver Flow
1. Durante il gameplay, triggera GameOver (es. distruggi il Waystone)
2. **Aspettativa**:
   - Il pannello GameOver appare
   - Stesso comportamento di Victory per i bottoni

**Console Log Aspettato**:
```
[GameManager] GAME OVER: Waystone Destroyed
[GameFlowManager] State: Gameplay → GameOver
[GameFlowUICoordinator] 💀 GameOver panel activated
```

### Test 4: Quit from Main Menu
1. Nella scena Main Menu, clicca "🚪 QUIT"
2. **Aspettativa (Editor)**: Play Mode si ferma
3. **Aspettativa (Build)**: L'applicazione si chiude

**Console Log Aspettato**:
```
[MainMenuUI] 🚪 Quit button clicked!
[GameFlowManager] Quitting game...
[GameFlowManager] Stopping Play Mode (Editor)
```

---

## 🔄 Architettura del Flusso

```
┌──────────────────────────────────────────────────────────┐
│                     MAIN MENU SCENE                      │
│                                                          │
│  MainMenuCanvas                                          │
│    └─ MainMenuUI.cs                                      │
│         ├─ OnPlayClicked()                               │
│         │    └─> GameFlowManager.Instance.StartGame()    │
│         └─ OnQuitClicked()                               │
│              └─> GameFlowManager.Instance.QuitGame()     │
│                                                          │
└──────────────────────────────────────────────────────────┘
                          │
                          │ StartGame()
                          ▼
┌──────────────────────────────────────────────────────────┐
│                    GAMEPLAY SCENE                        │
│                                                          │
│  --- UI COORDINATOR ---                                  │
│    └─ GameFlowUICoordinator.cs                           │
│         └─ OnStateChanged(state)                         │
│              ├─ Victory → Show VictoryPanel              │
│              ├─ GameOver → Show GameOverPanel            │
│              └─ Paused → Show PausePanel                 │
│                                                          │
│  VictoryCanvas                                           │
│    └─ VictoryUI.cs                                       │
│         ├─ OnRestartClicked()                            │
│         │    └─> GameFlowManager.Instance.RestartGame()  │
│         └─ OnMainMenuClicked()                           │
│              └─> GameFlowManager.Instance.LoadMainMenu() │
│                                                          │
│  GameOverCanvas                                          │
│    └─ GameOverUI.cs                                      │
│         ├─ OnRestartClicked()                            │
│         │    └─> GameFlowManager.Instance.RestartGame()  │
│         └─ OnMainMenuClicked()                           │
│              └─> GameFlowManager.Instance.LoadMainMenu() │
│                                                          │
└──────────────────────────────────────────────────────────┘
                          │
                          │ LoadMainMenu()
                          ▼
                  Back to Main Menu
```

---

## 🎯 GameFlowManager API Completa

### Scene Loading
```csharp
GameFlowManager.Instance.StartGame();      // MainMenu → Gameplay
GameFlowManager.Instance.LoadMainMenu();   // Gameplay → MainMenu
GameFlowManager.Instance.RestartGame();    // Reload current scene
GameFlowManager.Instance.QuitGame();       // Exit application
```

### Game States
```csharp
GameFlowManager.Instance.TriggerVictory();  // State → Victory
GameFlowManager.Instance.TriggerGameOver(); // State → GameOver
GameFlowManager.Instance.Pause();           // State → Paused
GameFlowManager.Instance.Resume();          // Paused → Gameplay
GameFlowManager.Instance.TogglePause();     // Toggle Pause/Resume
```

### State Listening
```csharp
private void OnEnable()
{
    GameFlowManager.OnStateChanged += HandleStateChange;
}

private void OnDisable()
{
    GameFlowManager.OnStateChanged -= HandleStateChange;
}

private void HandleStateChange(GameFlowManager.GameState newState)
{
    switch (newState)
    {
        case GameFlowManager.GameState.Victory:
            // Handle victory
            break;
        case GameFlowManager.GameState.GameOver:
            // Handle game over
            break;
    }
}
```

---

## 🐛 Troubleshooting

### Issue 1: "GameFlowManager.Instance is null"
**Causa**: GameFlowManager non esiste nella scena o non si è auto-creato.

**Fix**:
1. Verifica che `GameFlowManager.cs` abbia il metodo `AutoCreate()` (linee 60-80)
2. Se hai un GameObject manuale `--- GAME FLOW ---`, verifica che abbia il componente `GameFlowManager`
3. Controlla la console per errori di compilazione

### Issue 2: Victory/GameOver Panel non appare
**Causa**: GameFlowUICoordinator non è configurato o non ha i riferimenti.

**Fix**:
1. Verifica che `--- UI COORDINATOR ---` esista nella scena Gameplay
2. Verifica che `Victory Panel` e `GameOver Panel` siano assegnati nell'Inspector
3. Verifica che il coordinator sia attivo (checkbox ON)

### Issue 3: Main Menu button non funziona
**Causa**: Scene non configurata nel Build Settings.

**Fix**:
1. **File → Build Settings**
2. Aggiungi `MainMenu` alla lista (deve essere indice 0)
3. Aggiungi `Game` (o la tua scena gameplay) alla lista (indice 1)

### Issue 4: Restart ricarica, ma il GameFlowManager sparisce
**Causa**: Problema risolto con `AutoCreate()`, ma verifica che la fix sia stata applicata.

**Fix**:
1. Leggi `GAMEFLOW_PERSISTENCE_FIX.md`
2. Verifica che `GameFlowManager.cs` abbia il metodo `AutoCreate()` (linee 60-80)

---

## 📝 Checklist di Integrazione Completa

### Main Menu Scene
- [ ] Creata scena `MainMenu.unity`
- [ ] Aggiunta a Build Settings (indice 0)
- [ ] Creato `MainMenuCanvas` con UI
- [ ] Aggiunto `MainMenuUI.cs` al Canvas
- [ ] Assegnati bottoni Play e Quit nell'Inspector
- [ ] Testato: Play button carica la scena Gameplay
- [ ] Testato: Quit button ferma Play Mode (Editor)

### Gameplay Scene
- [ ] Creato GameObject `--- UI COORDINATOR ---`
- [ ] Aggiunto componente `GameFlowUICoordinator`
- [ ] Assegnati Victory Panel e GameOver Panel nell'Inspector
- [ ] Verificato che VictoryUI abbia bottoni Restart e Main Menu
- [ ] Verificato che GameOverUI abbia bottoni Restart e Main Menu
- [ ] Testato: Victory triggera il pannello Victory
- [ ] Testato: GameOver triggera il pannello GameOver
- [ ] Testato: Restart button ricarica la scena
- [ ] Testato: Main Menu button torna al menu principale

### GameFlowManager
- [ ] Verificato che si auto-crei (check console log "Auto-created")
- [ ] Verificato che persista tra scene reload
- [ ] Configurati scene names: `MainMenu` e `Game`
- [ ] Testato: Sopravvive a Restart
- [ ] Testato: Sopravvive a LoadMainMenu

---

## 🎨 Opzionale: Visual Feedback

### Animazioni (Animator)
Se vuoi aggiungere animazioni al Main Menu:

1. Crea un `Animator Controller` per il Canvas
2. Aggiungi trigger `PlayTransition` e `QuitTransition`
3. Assegna l'Animator al campo `Menu Animator` in MainMenuUI
4. Le animazioni si triggheranno automaticamente al click

### Suoni (AudioSource)
Se vuoi aggiungere suoni di click:

1. Crea un GameObject con un `AudioSource` nel Canvas
2. Assegna un AudioClip (es. "click.wav")
3. Assegna l'AudioSource al campo `Click Sound` in MainMenuUI
4. Il suono si riprodurrà automaticamente al click

---

**Integrazione Completata**: 2025-12-31
**Files Creati**: 2 (MainMenuUI.cs, GameFlowUICoordinator.cs)
**Files Modificati**: 1 (GameFlowManager.cs - aggiunto QuitGame())
**Stato**: ✅ Pronto per Testing
