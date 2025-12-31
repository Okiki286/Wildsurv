# 🚀 GameFlowManager - Quick Start (30 Seconds)

## Step 1: Open the Tool (5 seconds)

```
Unity Editor
    └─ Menu Bar
        └─ Tools
            └─ Wilderness
                └─ Setup
                    └─ Complete GameFlow Setup ← CLICK HERE
```

**Result**: Si apre una finestra con il tool di setup.

---

## Step 2: Click Setup (2 seconds)

Nella finestra del tool:

```
┌────────────────────────────────────────────────┐
│     🚀 GameFlow Complete Setup                 │
│     One-Click Automation Tool                  │
├────────────────────────────────────────────────┤
│                                                │
│  Configuration                                 │
│  ┌──────────────────────────────────────────┐  │
│  │ This tool will automatically set up:    │  │
│  │ ✅ Create Main Menu scene with UI       │  │
│  │ ✅ Add UI Coordinator to Gameplay scene │  │
│  │ ✅ Configure Build Settings             │  │
│  │ ✅ Wire all UI components               │  │
│  │                                          │  │
│  │ Total time: ~30 seconds                 │  │
│  └──────────────────────────────────────────┘  │
│                                                │
│  Scene Paths                                   │
│  Main Menu Scene: Assets/Scenes/MainMenu.unity │
│  Gameplay Scene:  Assets/Scenes/Game.unity     │
│                                                │
│  Setup Options                                 │
│  ☑ Create Main Menu Scene                     │
│  ☑ Setup UI Coordinator                       │
│  ☑ Configure Build Settings                   │
│  ☑ Create Sample UI Elements                  │
│                                                │
│         ┌────────────────────────┐             │
│         │ 🚀 RUN COMPLETE SETUP  │ ← CLICK!   │
│         └────────────────────────┘             │
│                                                │
└────────────────────────────────────────────────┘
```

---

## Step 3: Confirm (1 second)

Vedrai un dialogo di conferma:

```
┌────────────────────────────────────────┐
│     Complete GameFlow Setup            │
├────────────────────────────────────────┤
│ This will:                             │
│                                        │
│ 1. Create Main Menu scene with UI     │
│ 2. Add UI Coordinator to Gameplay     │
│    scene                               │
│ 3. Configure Build Settings           │
│ 4. Wire all UI components             │
│                                        │
│ Any existing configurations will be   │
│ preserved.                             │
│                                        │
│ Continue?                              │
│                                        │
│  ┌──────────────┐  ┌─────────┐        │
│  │ Yes, Run Setup│  │ Cancel  │        │
│  └──────────────┘  └─────────┘        │
│       ↑ CLICK                          │
└────────────────────────────────────────┘
```

Clicca **"Yes, Run Setup"**.

---

## Step 4: Wait (~30 seconds)

Vedrai una progress bar:

```
┌────────────────────────────────────────┐
│  GameFlow Setup                        │
├────────────────────────────────────────┤
│                                        │
│  Creating Main Menu scene...           │
│                                        │
│  [=========>                   ] 25%   │
│                                        │
└────────────────────────────────────────┘
```

**Phases**:
1. ⏳ Creating Main Menu scene... (25%)
2. ⏳ Setting up UI Coordinator... (50%)
3. ⏳ Configuring Build Settings... (75%)
4. ⏳ Finalizing setup... (100%)

---

## Step 5: Success! (1 second)

Vedrai il dialogo di successo:

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
│                ↑ CLICK                 │
└────────────────────────────────────────┘
```

Clicca **"OK"**.

---

## Step 6: Save Scenes (2 seconds)

Premi **Ctrl+S** (o **Cmd+S** su Mac) per salvare tutte le scene.

**FATTO!** 🎉

---

## 🧪 Test It Now! (2 minutes)

### Test 1: Main Menu → Gameplay (30 seconds)

1. In Unity, la scena **MainMenu** dovrebbe già essere aperta
2. Premi **Play** (barra spaziatrice o bottone ▶️)
3. Dovresti vedere:
   ```
   ┌────────────────────────────────────┐
   │                                    │
   │    WILDERNESS SURVIVAL             │
   │                                    │
   │      ┌──────────────┐              │
   │      │   ▶ PLAY     │              │
   │      └──────────────┘              │
   │                                    │
   │      ┌──────────────┐              │
   │      │   🚪 QUIT    │              │
   │      └──────────────┘              │
   │                                    │
   └────────────────────────────────────┘
   ```

4. Clicca il bottone **"▶ PLAY"**
5. La scena di gioco dovrebbe caricarsi

**Console Log Expected**:
```
[GameFlowManager] ⚙️ Auto-created before scene load (persistent)
[MainMenuUI] Main Menu initialized
[MainMenuUI] 🎮 Play button clicked!
[GameFlowManager] Starting new game...
[GameFlowManager] State: MainMenu → Gameplay
```

✅ **Success!** Se vedi la scena di gioco, il setup funziona!

---

### Test 2: Victory Flow (30 seconds)

1. Durante il gameplay, trova **GameManager** in Hierarchy
2. Nell'Inspector, clicca il bottone **"🏆 Victory"**
3. Dovresti vedere il pannello Victory apparire
4. Clicca **"Restart"**
5. La scena dovrebbe ricaricarsi

**Console Log Expected**:
```
[GameManager] 🏆 VICTORY! You survived!
[GameFlowManager] State: Gameplay → Victory
[GameFlowUICoordinator] 🏆 Victory panel activated

[VictoryUI] Restart button clicked!
[GameFlowManager] Restarting current scene...
```

✅ **Success!** Se la scena ricarica, il sistema funziona!

---

### Test 3: Main Menu Return (30 seconds)

1. Triggera Victory di nuovo (bottone "🏆 Victory" in GameManager)
2. Nel pannello Victory, clicca **"Main Menu"** (o "📋 MAIN MENU")
3. Dovresti tornare alla scena Main Menu

**Console Log Expected**:
```
[VictoryUI] Main Menu button clicked!
[GameFlowManager] Loading Main Menu...
[GameFlowManager] State: Victory → MainMenu
```

✅ **Success!** Se torni al Main Menu, tutto funziona perfettamente!

---

### Test 4: Quit (10 seconds)

1. Nel Main Menu, clicca **"🚪 QUIT"**
2. Play Mode dovrebbe fermarsi (in Editor)

**Console Log Expected**:
```
[MainMenuUI] 🚪 Quit button clicked!
[GameFlowManager] Quitting game...
[GameFlowManager] Stopping Play Mode (Editor)
```

✅ **Success!** Se Play Mode si ferma, il bottone Quit funziona!

---

## 🎉 Congratulations!

Hai completato il setup e il testing del **GameFlowManager**!

### What You Have Now:

✅ **Main Menu** con bottoni Play e Quit funzionanti
✅ **Gameplay Scene** con UI Coordinator integrato
✅ **Victory/GameOver** con bottoni Restart e Main Menu
✅ **Build Settings** configurato correttamente
✅ **GameFlowManager** persistente che sopravvive a scene reload

---

## 🎨 Next Steps (Optional)

### Customize UI (5-10 minutes)

**Change Colors**:
1. Seleziona `PlayButton` in Hierarchy
2. Inspector → Button → Colors → Normal Color → Scegli colore

**Add Logo**:
1. Create → UI → Image
2. Trascina sopra il titolo
3. Assegna sprite del logo

**Change Title**:
1. Seleziona `Title` in Hierarchy
2. Inspector → Text → Cambia "WILDERNESS SURVIVAL" in quello che vuoi

**Full Customization Guide**: See [COMPLETE_SETUP_TOOL.md](COMPLETE_SETUP_TOOL.md#customization-after-setup)

---

### Add Animations (10-15 minutes)

1. Create → Animator Controller → `MainMenuAnimator`
2. Aggiungi trigger `PlayTransition`
3. Crea animazione (es. fade out)
4. Assegna Animator al MainMenuCanvas
5. Assegna Animator al campo `Menu Animator` in MainMenuUI

---

### Add Sounds (5 minutes)

1. Importa un suono di click (es. "click.wav")
2. Create → GameObject → `ClickSound`
3. Aggiungi AudioSource
4. Assegna AudioClip
5. Disabilita "Play On Awake"
6. Assegna AudioSource al campo `Click Sound` in MainMenuUI

---

## 📚 Learn More

**Core Documentation**:
- [GAMEFLOW_README.md](GAMEFLOW_README.md) - Complete overview
- [COMPLETE_SETUP_TOOL.md](COMPLETE_SETUP_TOOL.md) - Tool documentation
- [UI_CODE_EXAMPLES.md](UI_CODE_EXAMPLES.md) - Code examples for custom UI

**Technical Details**:
- [GAMEFLOW_IMPLEMENTATION_SUMMARY.md](GAMEFLOW_IMPLEMENTATION_SUMMARY.md) - Complete summary
- [GAMEFLOW_PERSISTENCE_FIX.md](GAMEFLOW_PERSISTENCE_FIX.md) - How auto-creation works

---

## 🐛 Something Went Wrong?

### UI Looks Broken
**Solution**: Import TextMeshPro
```
Unity Menu → Window → TextMeshPro → Import TMP Essential Resources
```

### "Gameplay scene not found"
**Solution**: Il tool cerca `Assets/Scenes/Game.unity`.

Se la tua scena di gioco ha un nome diverso:
1. Apri di nuovo il tool
2. Cambia "Gameplay Scene" al path corretto
3. Clicca "RUN COMPLETE SETUP" di nuovo

### Buttons Don't Work
**Solution**: Assegna i bottoni manualmente
1. Seleziona `MainMenuCanvas` in Hierarchy
2. Inspector → MainMenuUI component
3. Assegna `PlayButton` e `QuitButton`

**Full Troubleshooting**: See [COMPLETE_SETUP_TOOL.md](COMPLETE_SETUP_TOOL.md#troubleshooting)

---

## 🔄 Need to Start Over?

### Rollback Everything

1. **Tools → Wilderness → Setup → Complete GameFlow Setup**
2. Clicca **🔄 Rollback All**
3. Conferma
4. **DONE!** Tutto rimosso

Then you can run the setup again if needed.

---

## ✅ Checklist

- [x] Tool opened
- [x] Setup run successfully
- [x] Scenes saved (Ctrl+S)
- [x] Test 1: Main Menu → Gameplay ✅
- [x] Test 2: Victory → Restart ✅
- [x] Test 3: Victory → Main Menu ✅
- [x] Test 4: Quit button ✅

**All tests passed?** You're ready to go! 🎉

---

**Total Time**: ~5 minutes (setup + testing)
**Difficulty**: ⭐ Easy (1-click)
**Status**: ✅ Complete

Happy coding! 🎮
