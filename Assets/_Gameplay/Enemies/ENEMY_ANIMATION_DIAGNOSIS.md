# Enemy Animation System - Diagnosis and Fix Guide

## 🔍 PROBLEM ANALYSIS

### Current Situation
- **Enemy Prefab**: `W_Skeleton_Minion` (KayKit asset)
- **Data Asset**: `enemy_default` (ScriptableObject)
- **Symptom**: Skeleton spawns and moves toward player but stays in **T-Pose** or **Idle** (no walk animation)

---

## ✅ WHAT I FOUND

### 1. **EnemyController.cs Analysis**

**Location**: `Assets/_Gameplay/Enemies/EnemyController.cs`

**Critical Finding**: ❌ **NO ANIMATION CODE EXISTS**

The EnemyController handles:
- ✅ NavMeshAgent movement
- ✅ Health system
- ✅ Targeting (Base/Waystone)
- ✅ Debuff system
- ❌ **MISSING**: Animator parameter updates

**Code Review**:
```csharp
// Line 103-107: Cached references
private NavMeshAgent navAgent;
private Transform targetTransform;
// ... but NO Animator reference!

// Line 159-176: Update loop
private void Update()
{
    // Updates destination, moves NavMeshAgent
    // ... but NEVER updates animator parameters!
}
```

**Conclusion**: The enemy moves via NavMeshAgent, but the Animator never receives movement speed/state updates.

---

### 2. **W_Skeleton_Minion.prefab Analysis**

**Structure**:
```
W_Skeleton_Minion (ROOT GameObject)
├─ Transform (position/rotation)
├─ Animator ✅ (Component exists!)
│  ├─ Avatar: Skeleton avatar
│  ├─ Controller: {guid: 39aa1412544a0644284f02765d54b1d8} ← IMPORTANT
│  ├─ ApplyRootMotion: FALSE (correct for NavMesh)
├─ NavMeshAgent ✅
├─ EnemyController ✅ (MonoBehaviour)
├─ CapsuleCollider ✅
└─ [Child GameObjects: Skeleton mesh parts]
```

**✅ Good News**: The Animator component IS present on the root GameObject.

**❌ Problem**: The Animator has a RuntimeAnimatorController assigned, but **EnemyController.cs never communicates with it**.

---

### 3. **Worker System Comparison (Reference)**

**WorkerAnimatorController.cs** (Workers have this, Enemies don't!)

```csharp
// Workers have a dedicated animation controller script
public class WorkerAnimatorController : MonoBehaviour
{
    private Animator animator;

    // Parameters cached as hashes
    private int speedHash;
    private int isMovingHash;

    public void SetSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(speedHash, speed);
        }
    }

    public void SetMoving(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool(isMovingHash, isMoving);
        }
    }
}
```

**Enemies NEED this**, but it doesn't exist!

---

## 🎯 ROOT CAUSE ANALYSIS

### Why Animation Isn't Working

1. **NavMeshAgent is moving** the enemy ✅
2. **Animator exists** on the prefab ✅
3. **AnimatorController is assigned** ✅
4. **BUT**: No code sets Animator parameters like:
   - `Speed` (float) - for blending walk/run
   - `IsMoving` (bool) - for idle/walk transition
   - `Velocity` (float) - common parameter for locomotion

**The Animator is waiting for input that never comes.**

---

## 🔧 SOLUTION OPTIONS

### ⭐ **Option 1: Create EnemyAnimatorController.cs (RECOMMENDED)**

**Similar to Worker system, create a dedicated animation controller.**

**File**: `Assets/_Gameplay/Enemies/EnemyAnimatorController.cs`

```csharp
using UnityEngine;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Gestisce animazioni per nemici.
    /// Aggiorna parametri Animator basato su stato del nemico.
    /// </summary>
    public class EnemyAnimatorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Parameter Names")]
        [SerializeField] private string speedParamName = "Speed";
        [SerializeField] private string isMovingParamName = "IsMoving";
        [SerializeField] private string isDeadParamName = "IsDead";
        [SerializeField] private string attackTriggerName = "Attack";

        // Cached hashes (performance)
        private int speedHash;
        private int isMovingHash;
        private int isDeadHash;
        private int attackHash;

        // Flags per parametri esistenti
        private bool hasSpeedParam;
        private bool hasIsMovingParam;
        private bool hasIsDeadParam;
        private bool hasAttackParam;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator != null)
            {
                CacheParameters();
            }
        }

        private void CacheParameters()
        {
            // Cache hash IDs
            speedHash = Animator.StringToHash(speedParamName);
            isMovingHash = Animator.StringToHash(isMovingParamName);
            isDeadHash = Animator.StringToHash(isDeadParamName);
            attackHash = Animator.StringToHash(attackTriggerName);

            // Check se parametri esistono
            if (animator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.nameHash == speedHash) hasSpeedParam = true;
                    if (param.nameHash == isMovingHash) hasIsMovingParam = true;
                    if (param.nameHash == isDeadHash) hasIsDeadParam = true;
                    if (param.nameHash == attackHash) hasAttackParam = true;
                }
            }
        }

        /// <summary>
        /// Aggiorna velocità per blend tree locomotion
        /// </summary>
        public void SetSpeed(float speed)
        {
            if (animator == null || !hasSpeedParam) return;
            animator.SetFloat(speedHash, speed);
        }

        /// <summary>
        /// Setta stato movimento (Idle/Walk)
        /// </summary>
        public void SetMoving(bool isMoving)
        {
            if (animator == null || !hasIsMovingParam) return;
            animator.SetBool(isMovingHash, isMoving);
        }

        /// <summary>
        /// Trigger animazione attacco
        /// </summary>
        public void TriggerAttack()
        {
            if (animator == null || !hasAttackParam) return;
            animator.SetTrigger(attackHash);
        }

        /// <summary>
        /// Setta stato morto
        /// </summary>
        public void SetDead(bool isDead)
        {
            if (animator == null || !hasIsDeadParam) return;
            animator.SetBool(isDeadHash, isDead);
        }
    }
}
```

---

### Step 2: Update EnemyController.cs

**Add Animator Integration**:

```csharp
// Line 103: Add cached reference
private NavMeshAgent navAgent;
private EnemyAnimatorController animatorController; // NEW

// Line 123-135: Awake()
private void Awake()
{
    navAgent = GetComponent<NavMeshAgent>();
    animatorController = GetComponent<EnemyAnimatorController>(); // NEW

    // ... rest of Awake
}

// Line 159-176: Update() - ADD ANIMATION UPDATES
private void Update()
{
    if (!isInitialized) return;

    // Update destination periodically
    destinationUpdateTimer += Time.deltaTime;
    if (destinationUpdateTimer >= destinationUpdateInterval)
    {
        UpdateDestination();
        destinationUpdateTimer = 0f;
    }

    // ============ NEW: UPDATE ANIMATOR ============
    if (animatorController != null && navAgent != null)
    {
        // Check se il nemico si sta muovendo
        bool isMoving = navAgent.velocity.sqrMagnitude > 0.01f;
        float currentSpeed = navAgent.velocity.magnitude;

        animatorController.SetMoving(isMoving);
        animatorController.SetSpeed(currentSpeed);
    }
    // =============================================

    // If not using NavMesh, do manual movement
    if (!useNavMesh || navAgent == null || !navAgent.isOnNavMesh)
    {
        MoveTowardsTarget();
    }
}

// Line 441-474: Die() - ADD DEATH ANIMATION
protected virtual void Die()
{
    // Stop movement
    if (navAgent != null && navAgent.isOnNavMesh)
    {
        navAgent.isStopped = true;
    }

    // NEW: Trigger death animation
    if (animatorController != null)
    {
        animatorController.SetDead(true);
    }

    // ... rest of Die()
}
```

---

### Step 3: Update W_Skeleton_Minion.prefab

**Add EnemyAnimatorController Component**:

1. Open `W_Skeleton_Minion.prefab` in Unity Editor
2. **Add Component** → `EnemyAnimatorController`
3. **Assign References**:
   - `Animator`: Drag the Animator component from same GameObject
4. **Configure Parameter Names** (Inspector):
   - Speed Param Name: `Speed`
   - Is Moving Param Name: `IsMoving`
   - Is Dead Param Name: `IsDead`
   - Attack Trigger Name: `Attack`
5. **Save Prefab**

---

## 📋 ANIMATOR CONTROLLER REQUIREMENTS (KayKit_Enemy_Controller)

The AnimatorController `KayKit_Enemy_Controller` has these parameters:

| Parameter Name | Type | Purpose | Default Value | Used By Enemies? |
|---------------|------|---------|---------------|------------------|
| `Speed` | **Float** | Blend walk/run speed (0=Idle, 1.5=Walk, 3.5=Run) | 0.0 | ✅ YES |
| `IsMoving` | **Bool** | Idle ↔ Walk transition | false | ✅ YES |
| `IsWorking` | **Bool** | Work animation (for workers) | false | ❌ NO (workers only) |
| `IsAttacking` | **Bool** | Attack state | false | ✅ YES |
| `HasWeapon` | **Bool** | Weapon equipped flag | false | ⚠️ Optional |
| `IsDead` | **Bool** | Death state | false | ✅ YES |
| `OnHit` | **Trigger** | Hit reaction animation | - | ✅ YES |

---

## 🎮 ANIMATOR CONTROLLER SETUP GUIDE

### If Controller Doesn't Have Parameters

**Option A: Modify Existing Controller**

1. Find the AnimatorController: guid `39aa1412544a0644284f02765d54b1d8`
2. Open in Animator window
3. **Add Parameters** (Parameters tab):
   - Click `+` → Float → Name: `Speed`
   - Click `+` → Bool → Name: `IsMoving`
   - Click `+` → Bool → Name: `IsDead`
   - Click `+` → Trigger → Name: `Attack`

4. **Create States**:
   - Base Layer:
     - `Idle` (default state)
     - `Walk`
     - `Death`
     - `Attack`

5. **Create Transitions**:
   ```
   Idle → Walk
   Condition: IsMoving == true

   Walk → Idle
   Condition: IsMoving == false

   Any State → Death
   Condition: IsDead == true
   ```

---

### Option B: Use KayKit Generator Tool

**File**: `Assets/Editor/KayKitAnimatorGenerator.cs` exists!

This tool might auto-generate KayKit animator controllers.

**Check if it can create skeleton controller**:
1. Unity menu → Tools/KayKit → Generate Animator
2. Select skeleton FBX model
3. Generate controller with standard parameters

---

## 🐛 DEBUGGING CHECKLIST

### Test 1: Verify Animator Parameters Exist

1. **Select** W_Skeleton_Minion prefab
2. **Animator window** → Controller should show:
   - ✅ Speed (Float)
   - ✅ IsMoving (Bool)
   - ✅ IsDead (Bool)

### Test 2: Verify Script Communication

**Add Debug Logs**:

```csharp
// In EnemyAnimatorController.SetSpeed()
public void SetSpeed(float speed)
{
    if (animator == null || !hasSpeedParam) return;
    animator.SetFloat(speedHash, speed);
    Debug.Log($"[EnemyAnim] Speed set to {speed}"); // DEBUG
}
```

**Expected Console Output**:
```
[EnemyAnim] Speed set to 3.5
[EnemyAnim] Speed set to 3.2
[EnemyAnim] Speed set to 0.0 (when stopped)
```

### Test 3: Runtime Inspector Check

**In Play Mode**:
1. Select spawned skeleton enemy
2. **Animator component** → Parameters section should show:
   - Speed = (changing value, e.g. 3.5)
   - IsMoving = true (when walking)

---

## 🚀 QUICK FIX ALTERNATIVE (Minimal Code)

**If you don't want a separate script**, add this directly to EnemyController.cs:

```csharp
// Line 103: Add field
private Animator animator;

// Line 123: Awake()
private void Awake()
{
    navAgent = GetComponent<NavMeshAgent>();
    animator = GetComponent<Animator>(); // NEW
}

// Line 159-176: Update()
private void Update()
{
    // ... existing code ...

    // QUICK FIX: Update animator directly
    if (animator != null && navAgent != null)
    {
        float speed = navAgent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsMoving", speed > 0.1f);
    }
}
```

**Pros**:
- ✅ Minimal code changes
- ✅ Quick to test

**Cons**:
- ❌ No parameter validation (will spam warnings if params don't exist)
- ❌ Less modular than dedicated script
- ❌ No performance optimization (string lookups every frame)

---

## 📊 EXPECTED BEHAVIOR AFTER FIX

### ✅ Correct Animation Flow

```
Enemy Spawns
    ↓
Idle animation playing (Speed = 0, IsMoving = false)
    ↓
NavMeshAgent starts moving toward Waystone
    ↓
Update() detects velocity > 0
    ↓
SetSpeed(3.5), SetMoving(true)
    ↓
Walk animation plays (blended at Speed=3.5)
    ↓
Enemy reaches target / stops
    ↓
SetSpeed(0), SetMoving(false)
    ↓
Idle animation returns
```

---

## 🎯 STEP-BY-STEP FIX GUIDE

### Phase 1: Create Animation Controller Script

1. ✅ Create `EnemyAnimatorController.cs` (code above)
2. ✅ Add to `Assets/_Gameplay/Enemies/`
3. ✅ Wait for Unity compile

### Phase 2: Update EnemyController.cs

1. ✅ Add `animatorController` field
2. ✅ Cache reference in `Awake()`
3. ✅ Add animation updates in `Update()`
4. ✅ Add death animation in `Die()`

### Phase 3: Update Prefab

1. ✅ Open `W_Skeleton_Minion.prefab`
2. ✅ Add `EnemyAnimatorController` component
3. ✅ Assign Animator reference
4. ✅ Save prefab

### Phase 4: Configure Animator Controller

1. ✅ Open AnimatorController in Animator window
2. ✅ Add required parameters (Speed, IsMoving, IsDead, Attack)
3. ✅ Create Idle/Walk/Death states
4. ✅ Create transitions with conditions
5. ✅ Assign animation clips to states

### Phase 5: Test

1. ✅ Enter Play Mode
2. ✅ Spawn enemy (via spawner or Data)
3. ✅ Check Console for animation logs
4. ✅ Verify walk animation plays when moving
5. ✅ Verify idle animation when stopped

---

## 🔍 COMMON ISSUES & SOLUTIONS

### Issue 1: "Parameter 'Speed' does not exist"

**Cause**: AnimatorController doesn't have Speed parameter.

**Fix**: Add Float parameter named `Speed` in Animator window.

---

### Issue 2: Animation plays but wrong clip

**Cause**: State transitions incorrect or animation clip not assigned.

**Fix**:
1. Check state has correct animation clip assigned
2. Verify transition conditions (IsMoving == true for Walk)

---

### Issue 3: "EnemyAnimatorController component not found"

**Cause**: Forgot to add component to prefab.

**Fix**: Add `EnemyAnimatorController` to W_Skeleton_Minion root GameObject.

---

### Issue 4: Animation stutters/loops incorrectly

**Cause**: Walk animation has Loop Time = false.

**Fix**:
1. Select walk animation asset
2. Inspector → Loop Time = ✅ CHECKED
3. Apply

---

## ✅ SUCCESS CRITERIA

Your fix is working when:

✅ Enemy spawns in **Idle** animation
✅ Enemy **walks** smoothly when moving
✅ Animation **speed matches** NavMeshAgent velocity
✅ Enemy returns to **Idle** when stopped
✅ Death animation plays when killed
✅ **No console errors** about missing parameters

---

## 📚 SUMMARY

**Problem**: EnemyController moves enemies via NavMeshAgent but never updates Animator parameters.

**Root Cause**: No code connects NavMeshAgent.velocity → Animator.SetFloat("Speed")

**Solution**: Create EnemyAnimatorController to bridge movement and animation systems.

**Files to Modify**:
1. ✅ NEW: `EnemyAnimatorController.cs`
2. ✅ EDIT: `EnemyController.cs` (add animator integration)
3. ✅ EDIT: `W_Skeleton_Minion.prefab` (add component)
4. ✅ EDIT: AnimatorController (add parameters if missing)

---

**Implementation Complete!** 🎉

After following this guide, your skeleton enemies will walk with proper animations instead of T-posing.
