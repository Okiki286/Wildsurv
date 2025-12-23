# Reskin UI with Modular Game UI Kit

Reskin the `WorkerAssignmentUI` and `Top HUD` to transition from "Programmer Art" to a polished, professional look using the "Modular Game UI Kit" assets.

## UI Theme: Rustic Survival (Artistic Override)
- **Main Panel**: `#2D241E` (Dark Leather Brown) @ 94% opacity
- **Buttons**: `#5D4037` (Dark Wood)
- **HUD Background**: `#3E2723` (Deep Earth) @ 95% opacity
- **Accent/Gold**: `#E6B85C` (Muted Gold)
- **Primary Font**: `Inter-SemiBold SDF`
- **Secondary Font**: `Inter-Regular SDF`

---

## Implementation

### Editor Tool Created
**Path:** `Assets/_UI/Scripts/Editor/RusticSurvivalThemeApplicator.cs`

**How to Use:**
1. Open the **SampleScene** in Unity.
2. Go to menu: `Tools → UI Kit → Apply Rustic Survival Theme`.
3. The tool will automatically apply:
   - Dark Leather background to the main WorkerAssignmentUI panel
   - Dark Wood styling to all buttons with proper hover/pressed states
   - Deep Earth background behind the resource HUD
   - Inter fonts to all text elements
   - Gold accent outlines on icons
4. **Save the scene** to keep changes.

---

## Target Assets (From Kit)
| Element | Sprite | Color Code |
|---------|--------|------------|
| Main Panel | `Background.png` | `#2D241E` |
| Buttons | `Rectangle.png` | `#5D4037` |
| HUD Background | `Rectangle.png` | `#3E2723` |
| Accent | - | `#E6B85C` |

## Verification Checklist
- [ ] Verify brown tones contrast well with green grass background
- [ ] Ensure 9-slice borders are not distorted
- [ ] Check button hover/pressed states are visible
- [ ] Confirm all text uses Inter font
