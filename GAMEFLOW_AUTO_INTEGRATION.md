# GameFlowManager - Auto-Integration Tool

## 🎯 Quick Start (1-Click Integration)

### Step 1: Run Auto-Integration Tool

1. In Unity Editor, go to menu: **Tools → Wilderness → Integration → Auto-Integrate GameFlowManager**
2. Read the confirmation dialog
3. Click **"Yes, Integrate"**
4. Wait for completion message

That's it! 🎉

---

## 🔧 What the Tool Does Automatically

### ✅ Creates GameObject

Creates `--- GAME FLOW ---` GameObject in the current scene with `GameFlowManager` component attached.

### ✅ Updates GameManager.cs

**Modifies 3 methods** to delegate to GameFlowManager:

1. **`TriggerVictory()`**
   ```csharp
   // ADDED:
   if (GameFlowManager.Instance != null)
   {
       GameFlowManager.Instance.TriggerVictory();
   }
   ```

2. **`TriggerGameOver(string reason)`**
   ```csharp
   // ADDED:
   if (GameFlowManager.Instance != null)
   {
       GameFlowManager.Instance.TriggerGameOver();
   }
   ```

3. **`RestartGame()`**
   ```csharp
   // REPLACED entire method with:
   if (GameFlowManager.Instance != null)
   {
       GameFlowManager.Instance.RestartGame();
   }
   else
   {
       // Fallback to direct scene reload
   }
   ```

### ✅ Updates GameOverUI.cs

**Adds Main Menu button support**:

1. Adds `[SerializeField] private Button mainMenuButton;` field
2. Wires button in `Awake()` and `OnDestroy()`
3. Updates `OnRestartClicked()` to use GameFlowManager first
4. Adds new `OnMainMenuClicked()` method

### ✅ Updates VictoryUI.cs

**Same changes as GameOverUI.cs**:

1. Adds Main Menu button field
2. Wires button listeners
3. Updates restart logic
4. Adds Main Menu handler

### ✅ Creates Backups

All modified files are backed up with `.backup` extension:
- `GameManager.cs.backup`
- `GameOverUI.cs.backup`
- `VictoryUI.cs.backup`

---

## 📋 Post-Integration Checklist

After running the auto-integration tool, complete these steps:

### 1. Save the Scene
- Press **Ctrl+S** to save the current scene
- The `--- GAME FLOW ---` GameObject must be saved

### 2. Configure GameFlowManager
Select the `--- GAME FLOW ---` GameObject in Hierarchy and configure:

```
Main Menu Scene Name: "MainMenu"
Gameplay Scene Name: "Game"
```

Or use indices:
```
Main Menu Scene Index: 0
Gameplay Scene Index: 1
```

### 3. Verify Build Settings
Go to **File → Build Settings → Scenes In Build**:

```
✅ [0] MainMenu
✅ [1] Game
```

If scenes are missing or wrong order:
1. Click **"Add Open Scenes"**
2. Drag to reorder
3. Close Build Settings

### 4. Add Main Menu Buttons to UI

The tool updated the **code**, but you need to add the **UI buttons manually**:

#### GameOverCanvas:
1. Select `GameOverCanvas` in Hierarchy
2. Find the `Container` or button parent
3. Duplicate the `RestartButton`
4. Rename to `MainMenuButton`
5. Change button text to "📋 MAIN MENU"
6. Select `GameOverCanvas` root
7. In Inspector, find `GameOverUI` component
8. Drag `MainMenuButton` into the `Main Menu Button` field

#### VictoryCanvas:
1. Same steps as GameOverCanvas
2. Drag button to `VictoryUI` component's `Main Menu Button` field

### 5. Test Integration

Use the **GameFlowManager Inspector Debug Buttons**:

1. Select `--- GAME FLOW ---` in Hierarchy
2. In Inspector, find the Debug Controls section
3. Test these buttons in Play Mode:
   - 🎮 **Start Game** (should load Game scene)
   - 🔄 **Restart** (should reload current scene)
   - ⏸️ **Pause** / ▶️ **Resume**
   - 🏆 **Victory** / 💀 **Game Over**

---

## 🔄 Rollback (Undo Integration)

If something goes wrong, you can rollback:

### Option 1: Automatic Rollback Tool
1. Go to **Tools → Wilderness → Integration → Rollback GameFlowManager Integration**
2. Confirm rollback
3. All `.backup` files will be restored

### Option 2: Manual Rollback
1. Find `.backup` files in your Assets folders:
   - `Assets/_Core/Managers/GameManager.cs.backup`
   - `Assets/_UI/Scripts/GameOverUI.cs.backup`
   - `Assets/_UI/Scripts/VictoryUI.cs.backup`
2. Remove `.backup` extension
3. Overwrite original files
4. Delete `--- GAME FLOW ---` GameObject manually

---

## 🧪 Verification Tests

After integration, run these tests:

### Test 1: GameObject Created
- ✅ `--- GAME FLOW ---` exists in Hierarchy
- ✅ Has `GameFlowManager` component
- ✅ Component shows "Current State" in Inspector

### Test 2: Code Changes
- ✅ Open `GameManager.cs` → Search "GameFlowManager.Instance" → Should find 3 occurrences
- ✅ Open `GameOverUI.cs` → Should have `mainMenuButton` field
- ✅ Open `VictoryUI.cs` → Should have `mainMenuButton` field

### Test 3: Backups Created
- ✅ `GameManager.cs.backup` exists
- ✅ `GameOverUI.cs.backup` exists
- ✅ `VictoryUI.cs.backup` exists

### Test 4: Functionality (Play Mode)
- ✅ Press Play
- ✅ Click GameFlowManager "🏆 Victory" button
- ✅ Victory screen should appear
- ✅ Time should freeze (Time.timeScale = 0)
- ✅ Click "🔄 Restart" button
- ✅ Scene should reload

---

## ⚠️ Common Issues

### Issue 1: "File not found" errors
**Cause**: Script paths are hardcoded. If you moved scripts, paths are wrong.

**Fix**:
1. Check these paths exist:
   - `Assets/_Core/Managers/GameManager.cs`
   - `Assets/_UI/Scripts/GameOverUI.cs`
   - `Assets/_UI/Scripts/VictoryUI.cs`
2. If different, update paths in `GameFlowIntegrationTool.cs` (lines 15-17)

### Issue 2: "Pattern not found" warnings
**Cause**: Your code structure is different from expected.

**Fix**:
1. Check Console for which pattern failed
2. Review the backup files
3. Manually apply changes following `GAMEFLOW_INTEGRATION_GUIDE.md`

### Issue 3: Main Menu button doesn't work
**Cause**: Button not assigned in Inspector.

**Fix**:
1. Select GameOverCanvas/VictoryCanvas
2. Find GameOverUI/VictoryUI component
3. Assign `MainMenuButton` to `Main Menu Button` field

### Issue 4: "GameFlowManager.Instance is null"
**Cause**: GameObject not saved in scene or deleted.

**Fix**:
1. Re-run auto-integration tool
2. Save scene (Ctrl+S)

---

## 🎮 Usage After Integration

### From Code:

```csharp
// Load Main Menu
GameFlowManager.Instance.LoadMainMenu();

// Start Game
GameFlowManager.Instance.StartGame();

// Restart Current Scene
GameFlowManager.Instance.RestartGame();

// Pause/Resume
GameFlowManager.Instance.Pause();
GameFlowManager.Instance.Resume();
GameFlowManager.Instance.TogglePause();

// End Game
GameFlowManager.Instance.TriggerVictory();
GameFlowManager.Instance.TriggerGameOver();
```

### From Inspector:

Select `--- GAME FLOW ---` and use Debug Controls buttons.

### Listen to State Changes:

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
            // Do something
            break;
    }
}
```

---

## 📚 Additional Resources

- **Full Integration Guide**: `GAMEFLOW_INTEGRATION_GUIDE.md`
- **UI Examples**: `GAMEFLOW_UI_EXAMPLES.md`
- **Source Code**: `Assets/_Core/Managers/GameFlowManager.cs`
- **Tool Source**: `Assets/_Core/Editor/GameFlowIntegrationTool.cs`

---

## 🚀 Next Steps

1. ✅ Run Auto-Integration Tool
2. ✅ Save Scene
3. ✅ Configure scene names in Inspector
4. ✅ Verify Build Settings
5. ✅ Add Main Menu buttons to UI manually
6. ✅ Test in Play Mode
7. ✅ Create Main Menu scene if needed
8. ✅ Implement Main Menu UI with Play button

---

## 📝 Integration Report Example

After running the tool, you should see:

```
[GameFlowIntegration] ========== STARTING AUTO-INTEGRATION ==========
[GameFlowIntegration] Created GameObject '--- GAME FLOW ---'
[GameFlowIntegration] Backup created: Assets/_Core/Managers/GameManager.cs.backup
[GameFlowIntegration] ✓ TriggerVictory() updated
[GameFlowIntegration] ✓ TriggerGameOver() updated
[GameFlowIntegration] ✓ RestartGame() updated
[GameFlowIntegration] ✅ Step 2/4: GameManager.cs updated
[GameFlowIntegration] ✅ Step 3/4: GameOverUI.cs updated
[GameFlowIntegration] ✅ Step 4/4: VictoryUI.cs updated
[GameFlowIntegration] ========== INTEGRATION COMPLETE ==========
[GameFlowIntegration] Steps completed: 4/4
```

---

**Tool Version**: 1.0
**Created**: 2025-12-31
**Requires**: Unity 2021.3+ with Odin Inspector (optional)
