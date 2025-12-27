# Technical Audit Report
**Project:** Wilderness Survival (Mobile Base-Defense)
**Date:** 2025-12-23
**Auditor:** Lead Unity Architect (Claude)
**Files Analyzed:** 57+ script files across `Assets/_Core`, `Assets/_Gameplay`, `Assets/_UI`

---

## Executive Summary

After analyzing the codebase, the project shows **solid foundational architecture** with good separation of concerns, proper event-driven communication, and mobile-conscious optimizations. However, there are **critical issues** that should be addressed before adding more features.

---

## 1. Critical Issues (Must Fix Immediately)

### 1.1 Singleton Proliferation
**Files:** `GameManager.cs:20`, `DayNightSystem.cs:18`, `WorkerSystem.cs:16`, `WaveManager.cs:19`, `EnemySpawner.cs:15`, `EnemyPooler.cs:19`, `ResourceSystem.cs:66`

**Issue:** 7+ Singleton instances with `public static Instance` pattern.

**Why:**
- Creates hidden dependencies making testing impossible
- Tight coupling between systems (e.g., `WorkerSystem` directly calls `DayNightSystem.Instance.CurrentDayNumber` at `WaveManager.cs:298`)
- Race conditions during initialization if load order changes
- Memory leaks if domain reload is disabled

**Proposed Fix:**
```csharp
// Create a ServiceLocator or use Dependency Injection
public class GameServices : MonoBehaviour
{
    public static GameServices Instance { get; private set; }

    [SerializeField] private DayNightSystem dayNight;
    [SerializeField] private WorkerSystem workers;
    // ... single point of truth for all services

    public IDayNightSystem DayNight => dayNight;
    public IWorkerSystem Workers => workers;
}
```

---

### 1.2 GetComponent Calls in Hot Paths
**File:** `WorkerSystem.cs:433-440`, `WorkerInstance.cs:275-282`, `StructureController.cs:949`

**Issue:** Multiple `GetComponent<WorkerDownedStatus>()` calls per frame inside assignment/update loops.

**Code Example (`WorkerSystem.cs:433`):**
```csharp
public bool AssignWorker(WorkerInstance worker, StructureController structure)
{
    if (worker.PhysicalWorker != null)
    {
        var downedStatus = worker.PhysicalWorker.GetComponent<WorkerDownedStatus>(); // EVERY CALL!
        if (downedStatus != null && downedStatus.IsDowned) { ... }
    }
}
```

**Why:** `GetComponent` allocates and is O(n) complexity. With 20+ workers being evaluated per frame, this causes **GC spikes** on mobile.

**Proposed Fix:**
```csharp
// Cache in WorkerController.Awake()
public class WorkerController : MonoBehaviour
{
    public WorkerDownedStatus DownedStatus { get; private set; }

    private void Awake()
    {
        DownedStatus = GetComponent<WorkerDownedStatus>();
    }
}
```

---

### 1.3 StructureController God Class
**File:** `StructureController.cs` - **1,800+ lines**

**Issue:** This single class handles:
- Health/damage system
- Worker assignment
- Production calculation
- Construction progress & visuals
- Grid placement
- State machine
- VFX spawning

**Why:** Violates Single Responsibility Principle. Any change to one system risks breaking others. Makes code review extremely difficult.

**Proposed Fix:** Extract into composition:
```
StructureController (orchestrator, <200 lines)
├── StructureHealth : IDamageable
├── StructureWorkerSlots
├── StructureProduction
├── StructureConstruction
└── StructureVisuals
```

---

### 1.4 FindAnyObjectByType in Runtime
**File:** `EnemyController.cs:298`, `WorkerSystem.cs:217-227`

**Issue:** `FindAnyObjectByType<>()` called at runtime:
```csharp
// EnemyController.cs:298
var aura = FindAnyObjectByType<WaystoneDebuffAura>();

// WorkerSystem.cs:217 - Called on EVERY Start() if event not assigned!
var allEvents = UnityEngine.Resources.FindObjectsOfTypeAll<GameEvent>();
```

**Why:** `FindObjectsOfType` iterates ALL loaded objects - O(n) complexity. On mobile with 100+ enemies, this causes **frame drops**.

**Proposed Fix:** Use serialized references or ServiceLocator pattern.

---

## 2. Architecture & Patterns (Refactoring Targets)

### 2.1 Event System - GOOD Foundation
**Files:** `GameEvent.cs`, `TypedGameEvents.cs`

The ScriptableObject-based event system is **well implemented**:
- Reverse iteration prevents modification issues
- Supports both UnityEvent (Inspector) and Action callbacks
- Zero-allocation iteration

**However:** Event assets must be manually wired in Inspector for every system, leading to "invisible connections" that are hard to debug.

---

### 2.2 State Management - Inconsistent

| System | Pattern | Issue |
|--------|---------|-------|
| `DayNightSystem` | Enum-based FSM | Clean, but `GameState` enum at `GameState.cs` couples it to game logic |
| `WorkerController` | `MovementState` enum + boolean flags | Mixed approach creates state explosion |
| `StructureController` | `StructureState` enum | Good, but state transitions buried in 1800-line class |

**WorkerController has 6 boolean flags:**
```csharp
private bool isPatrollingWorksite = false;
private bool isForcedIdle = false;
private bool isMoving = false;
private bool isGoingToShelter = false;
private bool isInShelter = false;
private bool isRetreatingToWaystone = false;
```

**Proposed Fix:** Convert to proper State Machine pattern:
```csharp
public abstract class WorkerState { }
public class IdleState : WorkerState { }
public class MovingState : WorkerState { }
public class WorkingState : WorkerState { }
public class RetreatState : WorkerState { }
```

---

### 2.3 Data-Driven Design - EXCELLENT

ScriptableObjects are used correctly for:
- `EnemyData`, `WaveData` - Enemy configuration
- `WorkerData`, `WorkerJobData` - Worker types and jobs
- `StructureData`, `ResourceData` - Buildings and resources

This is **best practice** for mobile - keeps data separate from behavior.

---

### 2.4 Object Pooling - IMPLEMENTED
**File:** `EnemyPooler.cs`

The enemy pooling system is **well implemented**:
- Multi-prefab support via dictionary
- Pre-warming capability
- Proper NavMeshAgent reset on reactivation
- Automatic expansion with limits

**However:** `GetPoolSizeForPrefab()` at line 285 iterates `activeEnemies` dictionary - O(n) per call during spawn.

---

## 3. Performance Concerns

### 3.1 WorkerSystem Update Loop - Per-Frame Iteration
**File:** `WorkerSystem.cs:230-286`

```csharp
private void Update()
{
    // Structure Tick Loop - iterates ALL structures
    for (int i = activeStructures.Count - 1; i >= 0; i--) { ... }

    // Worker Tick Loop - iterates ALL workers
    for (int i = physicalWorkers.Count - 1; i >= 0; i--) { ... }

    // Auto-Assignment check every frame
    if (Time.time >= _nextAutoAssignTime) { ... }
}
```

**Impact:** With 50 structures + 30 workers = 80 iterations per frame. The reverse-iteration is good (handles removal), but could be optimized with dirty flags.

---

### 3.2 List Allocations in Public APIs
**Files:** Multiple

```csharp
// WorkerSystem.cs:416
public List<WorkerInstance> GetAvailableWorkers() => new List<WorkerInstance>(availableWorkers);

// StructureController.cs:1490
public List<WorkerInstance> GetAssignedWorkerInstances() => new List<WorkerInstance>(assignedWorkerInstances);
```

**Why:** Creates garbage every call. UI polling this will cause GC spikes.

**Proposed Fix:** Return `IReadOnlyList<T>` or cache the list.

---

### 3.3 LINQ Usage Avoided - GOOD
The codebase correctly avoids LINQ in Update loops. `cachedWorkersToAssign` and `cachedBuildingStructures` show awareness of allocation costs.

---

## 4. Quick Wins (Low Effort / High Reward)

### 4.1 Cache GetComponent Results
**Effort:** 30 minutes | **Impact:** High (GC reduction)

Add cached references to `WorkerController`:
```csharp
// Already partially done, complete it:
public WorkerDownedStatus DownedStatus => downedStatus;
```

### 4.2 Remove FindObjectByType from EnemyController
**Effort:** 15 minutes | **Impact:** High

Inject via `EnemySpawner.Initialize()`:
```csharp
public void Initialize(EnemyData data, Transform target, ...) // Add target parameter
```

### 4.3 Add Object Pool for Workers
**Effort:** 2 hours | **Impact:** Medium

Workers are spawned via `Instantiate` at `WorkerSystem.cs:367`. Apply same pooling pattern as `EnemyPooler`.

### 4.4 Extract WorkerStatusSystem to Handle Downed/Shelter Logic
**File:** `WorkerStatusSystem.cs` exists but appears underutilized

**Effort:** 1 hour | **Impact:** Medium (code clarity)

Move scattered `IsDowned` checks to a centralized status query.

### 4.5 Replace Magic Numbers with Constants
**File:** `DayNightSystem.cs:257`, `WorkerController.cs:136-137`

```csharp
// DayNightSystem.cs:257
if (!dayEndingNotified && TimeRemaining <= 30f) // Magic number!

// WorkerController.cs:136-137
private const float MIN_MOVEMENT_DELTA_SQR = 0.01f; // Good! Already has constants
```

---

## 5. Architectural Strengths

| Pattern | Implementation | Rating |
|---------|---------------|--------|
| ScriptableObject Events | `GameEvent.cs` | Excellent |
| Object Pooling | `EnemyPooler.cs` | Excellent |
| Data-Driven Design | `*Data.cs` files | Excellent |
| Event-Driven Build Speed | `OnWorkerArrivedAtSite()` | Good |
| Namespace Organization | `WildernessSurvival.*` | Good |
| Odin Inspector Integration | Consistent across codebase | Good |
| Manual Update Loop | `WorkerController.ManualUpdate()` | Good (avoids Unity overhead) |

---

## 6. Dependency Graph (Core Systems)

```
GameManager
    ├── DayNightSystem (ref)
    ├── ResourceSystem (ref)
    └── Raises: onGameInitialized, onGamePaused, onGameOver

DayNightSystem
    ├── Raises: onDayStarted, onNightStarted, onDayEnding, onNightEnding
    └── Listens: GameManager.SetPaused()

WaveManager
    ├── EnemySpawner (ref)
    ├── EnemySpawnPointProvider (ref)
    └── Listens: onNightStarted, onDayStarted

WorkerSystem
    ├── WorkerController[] (registered)
    ├── StructureController[] (registered)
    └── Listens: onDayStarted (RefreshIdleBuildersOnDayStart)

EnemySpawner <--> EnemyPooler (bidirectional)

StructureController
    ├── WorkerSystem.Instance (assignment)
    ├── ResourceSystem.Instance (production)
    └── BaseCenterSystem.Instance (Waystone)
```

---

## 7. Recommended Priority Order

### Immediate (Before Next Feature)
- [ ] Cache `GetComponent<WorkerDownedStatus>()` results
- [ ] Remove `FindObjectByType` from runtime paths
- [ ] Add `IReadOnlyList` return types to public APIs

### Short-Term (This Sprint)
- [ ] Split `StructureController` into components
- [ ] Implement Worker object pooling
- [ ] Create ServiceLocator to replace Singleton access

### Medium-Term (Next Milestone)
- [ ] Refactor WorkerController state machine
- [ ] Centralize all status queries in WorkerStatusSystem
- [ ] Add automated integration tests using existing `WorkerIntegrationTest.cs` framework

---

## 8. Files Analyzed

### Core Systems
- `Assets/_Core/Managers/GameManager.cs`
- `Assets/_Core/Systems/DayNightSystem.cs`
- `Assets/_Core/Systems/BaseCenterSystem.cs`
- `Assets/_Core/Systems/PopulationSystem.cs`
- `Assets/_Core/Systems/EconomyFeedbackSystem.cs`
- `Assets/_Core/Events/GameEvent.cs`
- `Assets/_Core/Events/TypedGameEvents.cs`

### Gameplay Systems
- `Assets/_Gameplay/Workers/WorkerSystem.cs`
- `Assets/_Gameplay/Workers/WorkerController.cs`
- `Assets/_Gameplay/Workers/WorkerInstance.cs`
- `Assets/_Gameplay/Workers/WorkerStatusSystem.cs`
- `Assets/_Gameplay/Enemies/WaveManager.cs`
- `Assets/_Gameplay/Enemies/EnemyController.cs`
- `Assets/_Gameplay/Enemies/EnemySpawner.cs`
- `Assets/_Gameplay/Enemies/EnemyPooler.cs`
- `Assets/_Gameplay/Structures/StructureController.cs`
- `Assets/_Gameplay/Structures/StructureSystem.cs`
- `Assets/_Gameplay/Resources/ResourceSystem.cs`

---

## Summary

The codebase is **production-viable** with solid mobile-conscious patterns (pooling, manual updates, cached lists). The main technical debt is:

1. **Singleton abuse** creating hidden coupling
2. **StructureController god class** at 1800 lines
3. **Uncached GetComponent calls** in hot paths

Addressing these before MVP will significantly reduce bug surface area and make future feature development faster.

---

*Report generated by Technical Audit System*
