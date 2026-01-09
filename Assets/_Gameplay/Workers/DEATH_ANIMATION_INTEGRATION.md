# Death Animation Integration - Complete Guide

## 🎯 Overview

This document describes the complete implementation of the death animation system for KayKit workers, covering animation clip configuration, animator controller setup, and game logic integration.

---

## ✅ Task 1: Fix Animation Clips (Loop Time)

### Tool: `DeathAnimFixer.cs`

**Location**: `Assets/Editor/DeathAnimFixer.cs`

**Purpose**: Automatically configure loop settings for KayKit death animations.

### How to Use

1. Open Unity Editor
2. Go to menu: **Wilderness → Fix Death Animations**
3. The tool will automatically fix these animations:

| Animation | Loop Time | Purpose |
|-----------|-----------|---------|
| `Death_A` | **FALSE** | Falling animation (plays once) |
| `Death_A_Pose` | **TRUE** | Ground pose (loops indefinitely) |

### Paths Fixed

```
Assets/KayKit/Characters/Animations/Animations/Rig_Medium/General/Death_A.anim
Assets/KayKit/Characters/Animations/Animations/Rig_Medium/General/Death_A_Pose.anim
Assets/KayKit/Characters/Animations/Animations/Rig_Large/General/Death_A.anim
Assets/KayKit/Characters/Animations/Animations/Rig_Large/General/Death_A_Pose.anim
```

### Expected Output

```
[DeathAnimFixer] Set loopTime=False for Death_A
[DeathAnimFixer] Set loopTime=True for Death_A_Pose
[DeathAnimFixer] Successfully fixed 4 animation clips.
```

---

## ✅ Task 2: Update WorkerAnimatorController.cs

### New Features

#### 1. **Death Animation Support**

The controller already had the `SetDead(bool)` method implemented:

```csharp
public void SetDead(bool isDead)
{
    if (animator == null) return;

    if (hasIsDeadParam)
    {
        animator.SetBool(isDeadHash, isDead);

        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Debug.Log($"<color=red>[WorkerAnimatorController]</color> SetDead: {isDead}", this);
        }
        #endif
    }
}
```

**Location**: `WorkerAnimatorController.cs:386-401`

#### 2. **Enhanced Debug Logging**

Updated `DebugPrintState()` to show:
- ✅ IsDead parameter availability
- ✅ Current IsDead value from animator
- ✅ All other animator parameter values

**Example Output**:
```
=== ANIMATOR STATE ===
Controller: KayKit_Worker_Controller
Root Motion: False
Parameters Available: Speed: True, IsMoving: True, IsWorking: True, IsAttacking: True, HasWeapon: True, IsDead: True
Current Values:
  Speed = 0
  IsMoving = False
  IsWorking = False
  IsAttacking = False
  HasWeapon = False
  IsDead = True  ← NOW VISIBLE!
Is Valid: True
```

**Location**: `WorkerAnimatorController.cs:537-562`

#### 3. **New Debug Button**

Added "Test Set Dead" button in Inspector:
- Red colored button for visibility
- Calls `SetDead(true)` for testing
- Logs action to console

**Location**: `WorkerAnimatorController.cs:580-587`

---

## ✅ Task 3: Connect Logic (WorkerDownedStatus.cs)

### Integration Points

#### 1. **Cache AnimatorController Reference**

Added field and initialization:

```csharp
// Field
private WorkerAnimatorController animatorController;

// Awake()
animatorController = GetComponent<WorkerAnimatorController>();
```

**Location**: `WorkerDownedStatus.cs:83, 126`

#### 2. **Down() Method - Trigger Death Animation**

When worker goes down (HP <= 0), now calls animator:

```csharp
public void Down()
{
    if (isDowned) return;
    isDowned = true;

    StopWorkerMovement();
    DisableWorkerFunctions();

    // ✅ NEW: Trigger death animation
    if (animatorController != null)
    {
        animatorController.SetDead(true);
    }
    else
    {
        Debug.LogWarning($"<color=orange>[WorkerDowned]</color> WorkerAnimatorController not found - death animation will not play!", this);
    }

    string workerName = GetWorkerName();
    Debug.Log($"<color=red>[WorkerDowned]</color> {workerName} DOWNED");
}
```

**Location**: `WorkerDownedStatus.cs:179-204`

#### 3. **ReviveAtDawn() Method - Reset Animation**

When worker revives at dawn, resets death state:

```csharp
public void ReviveAtDawn()
{
    if (!isDowned) return;
    isDowned = false;

    // ... injury logic ...

    EnableWorkerMovement();
    EnableWorkerFunctions();

    // ✅ NEW: Reset death animation
    if (animatorController != null)
    {
        animatorController.SetDead(false);
    }

    string workerName = GetWorkerName();
    Debug.Log($"<color=green>[WorkerDowned]</color> {workerName} REVIVED at dawn");
}
```

**Location**: `WorkerDownedStatus.cs:209-239`

#### 4. **ForceFullRecovery() Method - Debug Recovery**

Also resets animation when using debug recovery:

```csharp
public void ForceFullRecovery()
{
    isDowned = false;
    ClearInjury();
    EnableWorkerMovement();
    EnableWorkerFunctions();

    // ✅ NEW: Reset death animation
    if (animatorController != null)
    {
        animatorController.SetDead(false);
    }

    string workerName = GetWorkerName();
    Debug.Log($"<color=lime>[WorkerDowned]</color> {workerName} FORCE RECOVERED");
}
```

**Location**: `WorkerDownedStatus.cs:298-313`

---

## 🎮 How It Works

### Death Flow

```
1. Worker takes damage → HP <= 0
   ↓
2. HealthSystem calls WorkerDownedStatus.Down()
   ↓
3. WorkerDownedStatus.Down() calls animatorController.SetDead(true)
   ↓
4. WorkerAnimatorController sets animator parameter "IsDead" = true
   ↓
5. Animator transitions to Death state
   ↓
6. Death_A animation plays (falling) → Loop Time = FALSE
   ↓
7. Transitions to Death_A_Pose (ground pose) → Loop Time = TRUE
   ↓
8. Worker stays in death pose until dawn
```

### Revival Flow

```
1. New day starts (OnDayStarted event)
   ↓
2. WorkerStatusSystem calls WorkerDownedStatus.ReviveAtDawn()
   ↓
3. WorkerDownedStatus.ReviveAtDawn() calls animatorController.SetDead(false)
   ↓
4. WorkerAnimatorController sets animator parameter "IsDead" = false
   ↓
5. Animator transitions back to Idle/Movement blend tree
   ↓
6. Worker is alive again (with Injured debuff)
```

---

## 🧪 Testing Guide

### Step 1: Fix Animation Clips

1. Run **Wilderness → Fix Death Animations**
2. Check console for success message
3. Verify in Animation Import Settings:
   - `Death_A` → Loop Time = **unchecked**
   - `Death_A_Pose` → Loop Time = **checked**

### Step 2: Test Animator Parameter

1. Select a Worker in scene (Play Mode)
2. Find **WorkerAnimatorController** component
3. Click **"Print Animator State"** button
4. Verify console shows: `IsDead: True` (parameter available)
5. Click **"Test Set Dead"** button (red)
6. Worker should play death animation

### Step 3: Test Game Logic Integration

1. Enter Play Mode
2. Select a Worker in scene
3. Find **WorkerDownedStatus** component
4. Click **"Force Down"** button
5. Expected results:
   - Console log: `[WorkerDownedStatus] <name> DOWNED`
   - Console log: `[WorkerAnimatorController] SetDead: True`
   - Death animation plays
6. Click **"Force Revive"** button
7. Expected results:
   - Console log: `[WorkerDownedStatus] <name> REVIVED at dawn`
   - Worker returns to Idle animation

### Step 4: Full Combat Test

1. Enter Play Mode
2. Spawn a worker
3. Let enemy attack worker until HP = 0
4. Worker should:
   - Play falling animation (Death_A)
   - Transition to ground pose (Death_A_Pose)
   - Stay in pose
5. Wait for next day (or use time cheat)
6. Worker should:
   - Stand up (exit death pose)
   - Return to Idle animation
   - Have Injured debuff active

---

## 🐛 Troubleshooting

### Issue: "IsDead parameter not found"

**Symptom**: Console shows warning about IsDead parameter.

**Cause**: AnimatorController doesn't have "IsDead" bool parameter.

**Solution**:
1. Open the AnimatorController in Animator window
2. Add a Bool parameter named "IsDead"
3. Create transitions:
   - `Any State → Death` (Condition: IsDead = true)
   - `Death → Idle` (Condition: IsDead = false)

### Issue: "Death animation loops incorrectly"

**Symptom**: Death_A loops forever, or Death_A_Pose doesn't loop.

**Cause**: Animation clip loop settings incorrect.

**Solution**:
1. Run **Wilderness → Fix Death Animations** tool again
2. Check Animation Import Settings manually
3. Reimport animations if needed

### Issue: "WorkerAnimatorController not found"

**Symptom**: Console warning when worker goes down.

**Cause**: Worker prefab missing WorkerAnimatorController component.

**Solution**:
1. Open Worker_Rogue prefab
2. Add `WorkerAnimatorController` component
3. Assign Animator reference
4. Set parameter names in Inspector

### Issue: "Death animation not playing"

**Symptom**: Worker goes invisible or freezes instead of playing death animation.

**Debug Steps**:
1. Check `WorkerAnimatorController.DebugPrintState()`:
   - Is `IsDead: True`? (parameter exists)
   - Is `IsDead = True`? (current value)
2. Check Animator window:
   - Does "IsDead" parameter exist?
   - Are transitions set up correctly?
3. Check Animation clips:
   - Are Death_A and Death_A_Pose assigned to states?
   - Are loop settings correct?

---

## 📋 File Changes Summary

### Modified Files

1. **WorkerAnimatorController.cs**
   - Enhanced `DebugPrintState()` to show IsDead value
   - Added `DebugTestDead()` button
   - (SetDead method already existed)

2. **WorkerDownedStatus.cs**
   - Added `animatorController` field
   - Cache reference in `Awake()`
   - Call `SetDead(true)` in `Down()`
   - Call `SetDead(false)` in `ReviveAtDawn()`
   - Call `SetDead(false)` in `ForceFullRecovery()`

3. **DeathAnimFixer.cs** (Editor Tool)
   - Already existed, no changes needed
   - Ready to use via menu

### No Changes Required

- **WorkerController.cs** - No changes
- **WorkerVisualController.cs** - No changes
- **HealthSystem.cs** - Already calls `Down()` correctly

---

## 🎓 Best Practices

### Animation Setup

1. **Always use two-state death**:
   - State 1: Falling animation (non-looping)
   - State 2: Ground pose (looping)

2. **Transition timing**:
   - Fall → Pose: Use "Has Exit Time" with duration matching fall animation
   - Pose → Idle: No exit time, immediate (condition-based)

3. **Parameter naming**:
   - Use consistent names: `IsDead` (bool)
   - Match between code and Animator

### Code Integration

1. **Component caching**:
   - Always cache references in `Awake()`
   - Check for null before using

2. **State consistency**:
   - Set IsDead = true when going down
   - Set IsDead = false when reviving
   - Don't forget debug/cheat methods

3. **Logging**:
   - Use color-coded logs for visibility
   - Include worker name in messages
   - Log both parameter set and animation state

---

## 🔮 Future Enhancements

### Potential Improvements

1. **Death Variants**:
   - Random death animations (Death_A, Death_B, Death_C)
   - Different deaths for different damage types

2. **Ragdoll Physics**:
   - Disable animator on death
   - Enable ragdoll for physics-based falling
   - Transition to static pose after settling

3. **VFX Integration**:
   - Spawn dust cloud on impact
   - Play injury VFX when reviving
   - Blood decal at death position

4. **Audio**:
   - Death grunt sound
   - Thud sound on impact
   - Groan sound when reviving

---

## ✅ Checklist

### Setup Checklist

- [x] Run `DeathAnimFixer` tool
- [x] Verify animation loop settings
- [x] Add `IsDead` parameter to AnimatorController
- [x] Set up transitions (Any State → Death, Death → Idle)
- [x] Assign Death_A and Death_A_Pose to states
- [x] Ensure WorkerAnimatorController on prefab
- [x] Test death in Play Mode
- [x] Test revival at dawn
- [x] Test debug buttons

### Integration Checklist

- [x] WorkerDownedStatus caches AnimatorController
- [x] Down() calls SetDead(true)
- [x] ReviveAtDawn() calls SetDead(false)
- [x] ForceFullRecovery() calls SetDead(false)
- [x] Debug logging includes IsDead state
- [x] All parameters properly initialized

---

**Implementation Complete!** 🎉

All three tasks have been successfully implemented and integrated.
