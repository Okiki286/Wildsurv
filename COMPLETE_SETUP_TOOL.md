# GameFlow Complete Setup Tool - One-Click Automation

## 🎯 Overview

**Tool completo di automazione** che configura TUTTO il sistema GameFlowManager + UI in **un solo click**.

**Tempo totale**: ~30 secondi (automatico)

---

## 🚀 Quick Start (1-Click Setup)

### Step 1: Open the Tool
1. In Unity Editor: **Tools → Wilderness → Setup → Complete GameFlow Setup**
2. Si aprirà una finestra con tutte le opzioni

### Step 2: Configure (Optional)
Tutte le opzioni sono già configurate con valori di default. Puoi personalizzare:
- **Main Menu Scene Path**: `Assets/Scenes/MainMenu.unity` (default)
- **Gameplay Scene Path**: `Assets/Scenes/Game.unity` (default)

**Opzioni Setup** (tutte attive di default):
- ✅ Create Main Menu Scene
- ✅ Setup UI Coordinator
- ✅ Configure Build Settings
- ✅ Create Sample UI Elements

### Step 3: Run Setup
1. Clicca il bottone verde: **🚀 RUN COMPLETE SETUP**
2. Conferma il dialogo
3. Aspetta ~30 secondi
4. **FATTO!** 🎉

---

## 🔧 What the Tool Does Automatically

### ✅ Step 1: Creates Main Menu Scene
**File**: `Assets/Scenes/MainMenu.unity`

**Creates**:
- `MainMenuCanvas` con Canvas, CanvasScaler, GraphicRaycaster
- `EventSystem` con StandaloneInputModule
- **Background Panel** (grigio scuro)
- **Title Text**: "WILDERNESS SURVIVAL" (48pt, centrato)
- **Play Button**: "▶ PLAY" (300x60, centrato)
- **Quit Button**: "🚪 QUIT" (300x60, sotto Play)
- **MainMenuUI component** con bottoni già assegnati

**Time**: ~5 secondi

---

### ✅ Step 2: Setup UI Coordinator in Gameplay
**Scene**: `Assets/Scenes/Game.unity`

**Creates**:
- GameObject `--- UI COORDINATOR ---`
- `GameFlowUICoordinator` component
- **Auto-trova e assegna**:
  - `VictoryCanvas` o `VictoryPanel` → Victory Panel reference
  - `GameOverCanvas` or `GameOverPanel` → GameOver Panel reference

**Time**: ~3 secondi

---

### ✅ Step 3: Configure Build Settings
**File → Build Settings → Scenes In Build**

**Adds**:
- `[0] MainMenu` (se non esiste già)
- `[1] Game` (se non esiste già)

**Preserves**: Altre scene già presenti nel build

**Time**: ~2 secondi

---

### ✅ Step 4: Wire GameFlowManager
**Auto-Configuration**:
- Trova GameFlowManager nella scena (o lascia che si auto-crei)
- Configura `mainMenuSceneName` = `"MainMenu"`
- Configura `gameplaySceneName` = `"Game"`
- Salva tutte le modifiche

**Time**: ~2 secondi

---

## 📊 Visual UI Created

### Main Menu Scene Layout

```
MainMenuCanvas
├─ Panel (Background - Dark Gray)
│  ├─ Title (TextMeshProUGUI)
│  │  └─ "WILDERNESS SURVIVAL" (48pt, white, centered)
│  │
│  ├─ PlayButton (Button)
│  │  └─ Text: "▶ PLAY" (24pt, white, centered)
│  │     Size: 300x60
│  │     Position: Center (y: 50%)
│  │
│  └─ QuitButton (Button)
│     └─ Text: "🚪 QUIT" (24pt, white, centered)
│        Size: 300x60
│        Position: Center (y: 35%)
│
└─ MainMenuUI (Script)
   ├─ playButton: → PlayButton (assigned)
   └─ quitButton: → QuitButton (assigned)

EventSystem
└─ StandaloneInputModule
```

---

## 🧪 Post-Setup Testing

### Test 1: Main Menu Scene
1. Il tool apre automaticamente la Main Menu scene alla fine
2. Premi **Play** in Unity
3. Dovresti vedere:
   - Background grigio scuro
   - Titolo "WILDERNESS SURVIVAL"
   - Bottone "▶ PLAY"
   - Bottone "🚪 QUIT"

**Expected Console Log**:
```
[GameFlowManager] ⚙️ Auto-created before scene load (persistent)
[MainMenuUI] Main Menu initialized
```

### Test 2: Play Button
1. Nella Main Menu scene, premi Play in Unity
2. Clicca il bottone "▶ PLAY"
3. **Expected**: La scena `Game.unity` si carica

**Expected Console Log**:
```
[MainMenuUI] 🎮 Play button clicked!
[GameFlowManager] Starting new game...
[GameFlowManager] State: MainMenu → Gameplay
```

### Test 3: Victory Flow
1. Durante il gameplay, apri Inspector di GameManager
2. Clicca bottone "🏆 Victory"
3. **Expected**:
   - Victory panel appare
   - Time.timeScale = 0

**Expected Console Log**:
```
[GameManager] 🏆 VICTORY!
[GameFlowManager] State: Gameplay → Victory
[GameFlowUICoordinator] 🏆 Victory panel activated
```

### Test 4: Restart Button
1. Nel Victory panel, clicca "Restart"
2. **Expected**: La scena Game ricarica
3. GameFlowManager persiste (no "Auto-created" nei log)

### Test 5: Main Menu Button
1. Triggera Victory di nuovo
2. Clicca "Main Menu"
3. **Expected**: Torna alla Main Menu scene

**Expected Console Log**:
```
[VictoryUI] Main Menu button clicked!
[GameFlowManager] Loading Main Menu...
[GameFlowManager] State: Victory → MainMenu
```

---

## 🔄 Rollback (Undo Setup)

Se vuoi rimuovere TUTTO:

### Option 1: Use the Tool
1. **Tools → Wilderness → Setup → Complete GameFlow Setup**
2. Clicca bottone rosso: **🔄 Rollback All**
3. Conferma il dialogo
4. **FATTO!** Tutto rimosso

### What Rollback Does:
- ✅ Deletes `MainMenu.unity`
- ✅ Removes `--- UI COORDINATOR ---` from Gameplay scene
- ✅ Restores backup files (`.backup` → original)
- ✅ Cleans up Build Settings (opzionale)

### Option 2: Manual Rollback
1. Delete `Assets/Scenes/MainMenu.unity`
2. Open `Game.unity` → Delete `--- UI COORDINATOR ---`
3. Restore `.backup` files:
   - `GameManager.cs.backup` → `GameManager.cs`
   - `VictoryUI.cs.backup` → `VictoryUI.cs`
   - `GameOverUI.cs.backup` → `GameOverUI.cs`

---

## ⚙️ Configuration Options

### Scene Paths
**Default**:
- Main Menu: `Assets/Scenes/MainMenu.unity`
- Gameplay: `Assets/Scenes/Game.unity`

**Customize**: Se le tue scene hanno nomi diversi, cambia i path nella finestra del tool prima di cliccare "RUN SETUP".

### Setup Options

#### Create Main Menu Scene
- **ON** (default): Crea una nuova Main Menu scene con UI completo
- **OFF**: Salta la creazione (se hai già una Main Menu scene)

#### Setup UI Coordinator
- **ON** (default): Aggiunge GameFlowUICoordinator alla scena Gameplay
- **OFF**: Salta l'aggiunta (se l'hai già aggiunto manualmente)

#### Configure Build Settings
- **ON** (default): Aggiunge scene al Build Settings automaticamente
- **OFF**: Salta la configurazione (se vuoi configurare manualmente)

#### Create Sample UI Elements
- **ON** (default): Crea UI completo (titolo, bottoni, styling)
- **OFF**: Crea solo struttura base (Canvas + EventSystem)

---

## 🐛 Troubleshooting

### Issue 1: "Gameplay scene not found"
**Causa**: Il path `Assets/Scenes/Game.unity` non esiste.

**Fix**:
1. Verifica il nome esatto della tua scena di gioco
2. Nella finestra del tool, cambia "Gameplay Scene" al path corretto
3. Esempio: `Assets/MyScenes/GameplayScene.unity`

### Issue 2: "Victory/GameOver panel not found"
**Causa**: I GameObject Victory/GameOver hanno nomi diversi.

**Fix**:
1. Dopo il setup, apri `Game.unity`
2. Seleziona `--- UI COORDINATOR ---`
3. Nell'Inspector, assegna manualmente:
   - `Victory Panel` → trascina il tuo GameObject Victory
   - `GameOver Panel` → trascina il tuo GameObject GameOver

### Issue 3: "Main Menu already exists"
**Causa**: Hai già una scena Main Menu.

**Fix**:
- **Option A**: Disabilita "Create Main Menu Scene" nella finestra del tool
- **Option B**: Rinomina la tua Main Menu scene esistente prima di eseguire il setup
- **Option C**: Elimina la Main Menu esistente se vuoi usare quella generata

### Issue 4: UI looks broken
**Causa**: TextMeshPro non è installato.

**Fix**:
1. **Window → TextMeshPro → Import TMP Essential Resources**
2. Riapri la Main Menu scene
3. I testi dovrebbero apparire correttamente

### Issue 5: Buttons don't work
**Causa**: MainMenuUI non ha i riferimenti assegnati.

**Fix**:
1. Seleziona `MainMenuCanvas` in Hierarchy
2. Nell'Inspector del componente `MainMenuUI`, assegna:
   - `Play Button` → trascina `PlayButton`
   - `Quit Button` → trascina `QuitButton`

---

## 📋 Complete Checklist

### Before Running the Tool
- [ ] Unity project aperto
- [ ] TextMeshPro importato (Window → TextMeshPro → Import TMP Essential Resources)
- [ ] Scena Gameplay esiste in `Assets/Scenes/Game.unity` (o path personalizzato)
- [ ] Hai fatto backup del progetto (opzionale, ma consigliato)

### During Setup
- [ ] Apri **Tools → Wilderness → Setup → Complete GameFlow Setup**
- [ ] Verifica i path delle scene
- [ ] Clicca **🚀 RUN COMPLETE SETUP**
- [ ] Aspetta il completamento (~30 secondi)

### After Setup
- [ ] Premi **Ctrl+S** per salvare tutte le scene
- [ ] Test 1: Play in Main Menu → Clicca Play button
- [ ] Test 2: Durante gameplay → Triggera Victory
- [ ] Test 3: Nel Victory panel → Clicca Restart
- [ ] Test 4: Triggera Victory di nuovo → Clicca Main Menu
- [ ] Test 5: Nel Main Menu → Clicca Quit (dovrebbe fermare Play Mode)

### Customization (Optional)
- [ ] Customizza colori UI nel Main Menu
- [ ] Aggiungi logo/immagini
- [ ] Aggiungi animazioni (Animator)
- [ ] Aggiungi suoni di click (AudioSource)

---

## 🎨 Customization After Setup

### Change Button Colors
1. Seleziona `PlayButton` in Hierarchy
2. Inspector → Button component → Colors:
   - Normal Color: Cambia colore base
   - Highlighted Color: Cambia colore hover
   - Pressed Color: Cambia colore click

### Change Title Text
1. Seleziona `Title` in Hierarchy
2. Inspector → TextMeshProUGUI:
   - Text: Cambia testo
   - Font Size: Cambia dimensione
   - Color: Cambia colore

### Add Background Image
1. Seleziona `Panel` in Hierarchy
2. Inspector → Image component:
   - Source Image: Trascina un'immagine
   - Image Type: Scegli "Sliced" per 9-slice
   - Color: Tinta immagine

### Add Logo
1. Create → UI → Image (child of Panel)
2. Posiziona sopra il titolo
3. Assegna sprite del logo
4. Regola dimensioni

### Add Animations
1. Create → Animator Controller → `MainMenuAnimator`
2. Aggiungi trigger `PlayTransition` e `QuitTransition`
3. Crea animazioni (es. fade out, scale up)
4. Seleziona `MainMenuCanvas` → Aggiungi Animator component
5. Assegna `MainMenuAnimator` al controller
6. Seleziona `MainMenuCanvas` → MainMenuUI component
7. Assegna Animator al campo `Menu Animator`

### Add Click Sound
1. Create → GameObject → `AudioSource`
2. Assegna un AudioClip (es. "click.wav")
3. Disabilita "Play On Awake"
4. Seleziona `MainMenuCanvas` → MainMenuUI component
5. Assegna AudioSource al campo `Click Sound`

---

## 📊 Tool Execution Flow

```
User clicks "RUN COMPLETE SETUP"
         │
         ▼
┌─────────────────────────────────────┐
│  Step 1: Create Main Menu Scene    │
│  ├─ New scene                       │
│  ├─ Create Canvas + EventSystem     │
│  ├─ Create Panel + Title + Buttons  │
│  ├─ Add MainMenuUI component        │
│  └─ Save scene                      │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Step 2: Setup UI Coordinator      │
│  ├─ Open Gameplay scene             │
│  ├─ Create "--- UI COORDINATOR ---" │
│  ├─ Add GameFlowUICoordinator       │
│  ├─ Find Victory/GameOver panels    │
│  ├─ Assign references               │
│  └─ Save scene                      │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Step 3: Configure Build Settings  │
│  ├─ Get current scenes in build     │
│  ├─ Add MainMenu (index 0)          │
│  ├─ Add Game (index 1)              │
│  └─ Save Build Settings             │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Step 4: Wire GameFlowManager      │
│  ├─ Find GameFlowManager            │
│  ├─ Set mainMenuSceneName = "MainMenu" │
│  ├─ Set gameplaySceneName = "Game"  │
│  └─ Save all scenes                 │
└──────────────┬──────────────────────┘
               │
               ▼
         ✅ COMPLETE!
   Show success dialog
```

---

## 🚦 Progress Bar During Execution

Durante l'esecuzione, vedrai una progress bar:

```
[========================================] 100%
GameFlow Setup: Finalizing setup...
```

**Steps**:
1. Creating Main Menu scene... (25%)
2. Setting up UI Coordinator... (50%)
3. Configuring Build Settings... (75%)
4. Finalizing setup... (100%)

---

## 📝 Success Dialog

Alla fine dell'esecuzione, vedrai:

```
┌────────────────────────────────────────┐
│         Setup Complete! ✅             │
├────────────────────────────────────────┤
│ GameFlow setup completed successfully! │
│                                        │
│ ✅ Main Menu scene created             │
│ ✅ UI Coordinator added to Gameplay    │
│ ✅ Build Settings configured           │
│ ✅ All UI components wired             │
│                                        │
│ Next steps:                            │
│ 1. Save all scenes (Ctrl+S)           │
│ 2. Test the flow by pressing Play     │
│ 3. See GAMEFLOW_IMPLEMENTATION_        │
│    SUMMARY.md for details              │
│                                        │
│              [ OK ]                    │
└────────────────────────────────────────┘
```

---

## 🎓 Additional Resources

- **GAMEFLOW_IMPLEMENTATION_SUMMARY.md** - Complete implementation overview
- **GAMEFLOW_UI_INTEGRATION.md** - Manual UI integration guide
- **UI_CODE_EXAMPLES.md** - Code examples for custom UI
- **GAMEFLOW_INTEGRATION_GUIDE.md** - Complete architecture guide

---

**Tool Version**: 1.0
**Created**: 2025-12-31
**Execution Time**: ~30 seconds
**Requires**: Unity 2021.3+, TextMeshPro
**Status**: ✅ Production Ready
