# Load Game Menu - Setup Guide

## 📋 Overview

This guide will walk you through creating a fully-styled Load Game Menu that displays all save files with the ability to load or delete them.

**Art Style**: Rustic/Survival (Cream Background #F0E6D2, Dark Brown Borders #3C280D)

---

## 🎨 Visual Reference

```
┌─────────────────────────────────────────┐
│         📂 LOAD GAME                  X │ ← Window Title
├─────────────────────────────────────────┤
│ ┌─────────────────────────────────────┐ │
│ │ ╔═══════════════════════════════╗   │ │
│ │ ║ AutoSave 0     Day 5          ║   │ │ ← Slot Item
│ │ ║ 2025-01-15 14:32   [📥] [🗑️]  ║   │ │
│ │ ╚═══════════════════════════════╝   │ │
│ │ ╔═══════════════════════════════╗   │ │
│ │ ║ AutoSave 1     Day 4          ║   │ │
│ │ ║ 2025-01-15 12:15   [📥] [🗑️]  ║   │ │
│ │ ╚═══════════════════════════════╝   │ │
│ │ ╔═══════════════════════════════╗   │ │
│ │ ║ Manual Save    Day 3          ║   │ │
│ │ ║ 2025-01-14 22:45   [📥] [🗑️]  ║   │ │
│ │ ╚═══════════════════════════════╝   │ │
│ └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

---

## 🛠️ Step-by-Step Setup

### **Step 1: Create the Window Panel**

1. **Right-click in Hierarchy** → `UI` → `Canvas` (if you don't have one)
2. **Right-click Canvas** → `UI` → `Panel`
3. **Rename** to `LoadGameWindow`
4. **Configure RectTransform**:
   - **Anchors**: Center (0.5, 0.5, 0.5, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Position**: (0, 0, 0)
   - **Width**: 600
   - **Height**: 700

5. **Style the Panel**:
   - **Image Component**:
     - **Color**: `#F0E6D2` (Cream)
   - **Add Component** → `Outline`:
     - **Effect Color**: `#3C280D` (Dark Brown)
     - **Effect Distance**: (4, -4)

---

### **Step 2: Create the Title Bar**

1. **Right-click LoadGameWindow** → `UI` → `Panel`
2. **Rename** to `TitleBar`
3. **Configure RectTransform**:
   - **Anchors**: Top Stretch (0, 1, 1, 1)
   - **Pivot**: (0.5, 1)
   - **Position Y**: 0
   - **Height**: 60
   - **Left/Right**: 0

4. **Style**:
   - **Image Color**: `#D6C8B0` (Slightly darker cream)

5. **Add Title Text**:
   - **Right-click TitleBar** → `UI` → `Text - TextMeshPro`
   - **Rename** to `TitleText`
   - **Text**: "📂 LOAD GAME"
   - **Font Size**: 32
   - **Alignment**: Center/Middle
   - **Color**: `#3C280D` (Dark Brown)
   - **RectTransform**: Stretch/Stretch

6. **Add Close Button**:
   - **Right-click TitleBar** → `UI` → `Button - TextMeshPro`
   - **Rename** to `Btn_Close`
   - **Position**: Top-Right corner
   - **Width**: 50, **Height**: 50
   - **Text**: "X"
   - **Font Size**: 28
   - **Button Colors**:
     - Normal: `#E6D8C0`
     - Highlighted: `#F0E6D2`
     - Pressed: `#C8B8A0`

---

### **Step 3: Create the Scroll View**

1. **Right-click LoadGameWindow** → `UI` → `Scroll View`
2. **Rename** to `SaveListScrollView`
3. **Configure RectTransform**:
   - **Anchors**: Stretch/Stretch
   - **Top**: -70 (below title bar)
   - **Bottom**: 20
   - **Left**: 20
   - **Right**: -20

4. **Delete** the `Scrollbar Horizontal` (we only need vertical)

5. **Configure Scroll Rect Component**:
   - **Horizontal**: ✗ OFF
   - **Vertical**: ✓ ON
   - **Movement Type**: Elastic
   - **Scroll Sensitivity**: 20

6. **Find `Viewport`** → **Find `Content`**:
   - **Add Component** → `Vertical Layout Group`:
     - **Child Alignment**: Upper Center
     - **Spacing**: 10
     - **Child Force Expand**: Width ✓, Height ✗
   - **Add Component** → `Content Size Fitter`:
     - **Vertical Fit**: Preferred Size

7. **Style Viewport Background** (optional):
   - **Viewport Image Color**: `#E6D8C0` (Lighter cream)

---

### **Step 4: Create the Slot Prefab**

1. **Right-click Hierarchy** → `Create Empty`
2. **Rename** to `SaveSlotItem_Prefab` (temporary, we'll make it a prefab)
3. **Add Component** → `RectTransform`
4. **Configure RectTransform**:
   - **Width**: 520
   - **Height**: 80

5. **Add Component** → `Image`:
   - **Color**: `#E6D8C0` (Darker Cream)

6. **Add Component** → `Outline`:
   - **Effect Color**: `#3C280D`
   - **Effect Distance**: (2, -2)

7. **Add Component** → `SaveSlotItem` (the script we created)

---

#### **Step 4a: Add Filename Text**

1. **Right-click SaveSlotItem_Prefab** → `UI` → `Text - TextMeshPro`
2. **Rename** to `Text_Filename`
3. **Configure**:
   - **Anchors**: Left/Middle
   - **Position**: (120, 15, 0)
   - **Width**: 150, **Height**: 30
   - **Text**: "AutoSave 0"
   - **Font Size**: 20
   - **Alignment**: Left/Middle
   - **Color**: `#3C280D`

---

#### **Step 4b: Add Day Text**

1. **Right-click SaveSlotItem_Prefab** → `UI` → `Text - TextMeshPro`
2. **Rename** to `Text_Day`
3. **Configure**:
   - **Anchors**: Left/Middle
   - **Position**: (120, -15, 0)
   - **Width**: 100, **Height**: 25
   - **Text**: "Day 5"
   - **Font Size**: 16
   - **Alignment**: Left/Middle
   - **Color**: `#5A4A3A` (Slightly lighter brown)

---

#### **Step 4c: Add Timestamp Text**

1. **Right-click SaveSlotItem_Prefab** → `UI` → `Text - TextMeshPro`
2. **Rename** to `Text_Timestamp`
3. **Configure**:
   - **Anchors**: Right/Middle
   - **Position**: (-160, 0, 0)
   - **Width**: 150, **Height**: 25
   - **Text**: "2025-01-15 14:32"
   - **Font Size**: 14
   - **Alignment**: Right/Middle
   - **Color**: `#5A4A3A`

---

#### **Step 4d: Add Load Button**

1. **Right-click SaveSlotItem_Prefab** → `UI` → `Button - TextMeshPro`
2. **Rename** to `Btn_Load`
3. **Configure**:
   - **Anchors**: Right/Middle
   - **Position**: (-90, 0, 0)
   - **Width**: 60, **Height**: 50
   - **Text**: "📥"
   - **Font Size**: 24
   - **Button Colors**:
     - Normal: `#A8D8A8` (Light Green)
     - Highlighted: `#C8F8C8`
     - Pressed: `#88B888`

---

#### **Step 4e: Add Delete Button**

1. **Right-click SaveSlotItem_Prefab** → `UI` → `Button - TextMeshPro`
2. **Rename** to `Btn_Delete`
3. **Configure**:
   - **Anchors**: Right/Middle
   - **Position**: (-30, 0, 0)
   - **Width**: 50, **Height**: 50
   - **Text**: "🗑️"
   - **Font Size**: 22
   - **Button Colors**:
     - Normal: `#D8A8A8` (Light Red)
     - Highlighted: `#F8C8C8`
     - Pressed: `#B88888`

---

#### **Step 4f: Wire References in SaveSlotItem Script**

1. **Select `SaveSlotItem_Prefab`** in Hierarchy
2. **In Inspector**, find the `SaveSlotItem` component
3. **Drag references**:
   - **Filename Text** → `Text_Filename`
   - **Timestamp Text** → `Text_Timestamp`
   - **Day Text** → `Text_Day`
   - **Load Button** → `Btn_Load`
   - **Delete Button** → `Btn_Delete`

---

#### **Step 4g: Create Prefab**

1. **Create folder**: `Assets/_UI/Prefabs/LoadGame/`
2. **Drag `SaveSlotItem_Prefab`** from Hierarchy → into the folder
3. **Delete** `SaveSlotItem_Prefab` from Hierarchy (we only need the prefab)

---

### **Step 5: Create Empty State Panel**

1. **Right-click LoadGameWindow** → `UI` → `Panel`
2. **Rename** to `EmptyStatePanel`
3. **Configure RectTransform**: Same as ScrollView (Stretch/Stretch)
4. **Style**: Same as ScrollView background

5. **Add Text**:
   - **Right-click EmptyStatePanel** → `UI` → `Text - TextMeshPro`
   - **Rename** to `Text_EmptyState`
   - **Text**: "No save files found.\n\nStart a new game to create a save."
   - **Font Size**: 24
   - **Alignment**: Center/Middle
   - **Color**: `#5A4A3A`

6. **Initially Hide**: Uncheck `EmptyStatePanel` in Inspector

---

### **Step 6: Create Confirmation Dialog**

1. **Right-click LoadGameWindow** → `UI` → `Panel`
2. **Rename** to `ConfirmDeletePanel`
3. **Configure RectTransform**:
   - **Anchors**: Center (0.5, 0.5, 0.5, 0.5)
   - **Width**: 400, **Height**: 200

4. **Style**:
   - **Image Color**: `#F0E6D2`
   - **Outline**: `#3C280D`, Distance (4, -4)

5. **Add Confirmation Text**:
   - **Right-click ConfirmDeletePanel** → `UI` → `Text - TextMeshPro`
   - **Rename** to `Text_ConfirmDelete`
   - **Position**: (0, 30, 0)
   - **Width**: 350, **Height**: 100
   - **Text**: "Delete this save file?\n\nThis cannot be undone."
   - **Font Size**: 18
   - **Alignment**: Center/Middle
   - **Color**: `#3C280D`

6. **Add Yes Button**:
   - **Right-click ConfirmDeletePanel** → `UI` → `Button - TextMeshPro`
   - **Rename** to `Btn_ConfirmYes`
   - **Position**: (-60, -50, 0)
   - **Width**: 100, **Height**: 40
   - **Text**: "YES"
   - **Button Colors**: Red theme (Normal: `#D8A8A8`)

7. **Add No Button**:
   - **Right-click ConfirmDeletePanel** → `UI` → `Button - TextMeshPro`
   - **Rename** to `Btn_ConfirmNo`
   - **Position**: (60, -50, 0)
   - **Width**: 100, **Height**: 40
   - **Text**: "NO"
   - **Button Colors**: Green theme (Normal: `#A8D8A8`)

8. **Initially Hide**: Uncheck `ConfirmDeletePanel` in Inspector

---

### **Step 7: Wire LoadGameUI Script**

1. **Select `LoadGameWindow`** in Hierarchy
2. **Add Component** → `LoadGameUI`
3. **Drag references**:
   - **Window Panel** → `LoadGameWindow`
   - **Close Button** → `Btn_Close`
   - **Slot Container** → `SaveListScrollView/Viewport/Content`
   - **Slot Prefab** → Drag from `Assets/_UI/Prefabs/LoadGame/SaveSlotItem_Prefab`
   - **Empty State Panel** → `EmptyStatePanel`
   - **Empty State Text** → `Text_EmptyState`
   - **Confirm Delete Panel** → `ConfirmDeletePanel`
   - **Confirm Delete Text** → `Text_ConfirmDelete`
   - **Confirm Delete Yes Button** → `Btn_ConfirmYes`
   - **Confirm Delete No Button** → `Btn_ConfirmNo`
   - **Debug Mode** → ✓ ON (for testing)

---

### **Step 8: Test in Play Mode**

1. **Enter Play Mode**
2. **Find `LoadGameWindow`** in Hierarchy
3. **Enable it** (check the GameObject)
4. **Verify**:
   - List populates with save files
   - Newest saves appear at the top
   - Load button works
   - Delete button shows confirmation
   - Empty state shows when no saves exist

---

## 🎮 Usage from Code

### **Open Load Game Window:**
```csharp
using WildernessSurvival.UI.LoadGame;

// Find the LoadGameUI component (on Canvas or wherever you placed it)
LoadGameUI loadGameUI = FindObjectOfType<LoadGameUI>();

// Show the window
loadGameUI.Show();
```

### **From Pause Menu:**
Add a "Load Game" button to your PauseMenuUI:

```csharp
[SerializeField] private LoadGameUI loadGameUI;

public void OnLoadGameButtonClicked()
{
    // Hide pause menu
    Hide();

    // Show load game window
    loadGameUI.Show();
}
```

---

## 🧪 Testing Scenarios

### **Test 1: Empty State**
1. Delete all `.json` files from `Application.persistentDataPath`
2. Open Load Game window
3. **Expected**: "No save files found" message

### **Test 2: Multiple Saves**
1. Play the game for multiple days (auto-saves create files)
2. Make a manual save via Pause Menu
3. Open Load Game window
4. **Expected**: All saves listed, sorted by date (newest first)

### **Test 3: Load Functionality**
1. Click "📥" button on a save
2. **Expected**: Game loads from that save, window closes

### **Test 4: Delete Functionality**
1. Click "🗑️" button on a save
2. **Expected**: Confirmation dialog appears
3. Click "YES"
4. **Expected**: Save deleted, list refreshes

### **Test 5: Corrupt File**
1. Manually create a corrupt `.json` file (invalid JSON)
2. **Expected**: Slot shows "Corrupt" with red text, but doesn't crash

---

## 📁 File Structure

```
Assets/
  _UI/
    Scripts/
      LoadGame/
        ├── LoadGameUI.cs          (Window Manager)
        ├── SaveSlotItem.cs        (List Element)
        └── SETUP_GUIDE.md         (This file)
    Prefabs/
      LoadGame/
        └── SaveSlotItem_Prefab.prefab
```

---

## 🎨 Color Reference

| Element | Color Code | RGB | Description |
|---------|-----------|-----|-------------|
| Window Background | `#F0E6D2` | (240, 230, 210) | Cream |
| Slot Background | `#E6D8C0` | (230, 216, 192) | Darker Cream |
| Border/Outline | `#3C280D` | (60, 40, 13) | Dark Brown |
| Text Primary | `#3C280D` | (60, 40, 13) | Dark Brown |
| Text Secondary | `#5A4A3A` | (90, 74, 58) | Lighter Brown |
| Load Button | `#A8D8A8` | (168, 216, 168) | Light Green |
| Delete Button | `#D8A8A8` | (216, 168, 168) | Light Red |

---

## ✅ Checklist

- [ ] LoadGameUI.cs script created
- [ ] SaveSlotItem.cs script created
- [ ] Window Panel created and styled
- [ ] Title Bar created with close button
- [ ] Scroll View created with Vertical Layout Group
- [ ] Slot Prefab created with all UI elements
- [ ] Slot Prefab references wired
- [ ] Empty State Panel created
- [ ] Confirmation Dialog created
- [ ] LoadGameUI component added and wired
- [ ] Tested in Play Mode
- [ ] Integrated with Pause Menu (optional)

---

## 🔧 Troubleshooting

### **Problem: List doesn't populate**
- **Solution**: Check that `slotContainer` is assigned to `Content` (not `Viewport`)
- **Solution**: Verify save files exist in `Application.persistentDataPath`

### **Problem: Load button doesn't work**
- **Solution**: Ensure `SaveSlotItem` script has button references wired
- **Solution**: Check that `LoadGameUI` parent reference is set (auto-set in Initialize)

### **Problem: Slots overlap in ScrollView**
- **Solution**: Add `Content Size Fitter` to Content object
- **Solution**: Check `Vertical Layout Group` spacing

### **Problem: Can't see scroll bar**
- **Solution**: Ensure Scrollbar Vertical is assigned in Scroll Rect component
- **Solution**: Check that Content height > Viewport height

---

## 🚀 Optional Enhancements

1. **Add Icons**: Replace emoji with actual UI sprites
2. **Add Animations**: Fade in/out for window show/hide
3. **Add Sound Effects**: Click sounds for buttons
4. **Add Sorting Options**: Sort by Day, Name, or Date
5. **Add Search Filter**: Search by filename or day number
6. **Add Preview**: Show screenshot/thumbnail of save

---

That's it! Your Load Game Menu is now fully functional. 🎉
