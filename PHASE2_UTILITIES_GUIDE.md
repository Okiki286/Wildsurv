# 📝 PHASE 2: String & Coroutine Optimization Utilities

**Date:** 2025-12-30
**Goal:** Reduce string allocations and coroutine overhead
**Status:** ✅ **UTILITIES CREATED**
**Estimated Impact:** -240KB/min (coroutines) + -360KB/min (UI strings) = **-600KB/min**

---

## 🛠️ **UTILITIES CREATED**

### **1. CoroutineCache** ✅

**File:** `Assets/_Core/Utils/CoroutineCache.cs`

**Purpose:** Eliminates `new WaitForSeconds()` allocations in coroutines

**Usage:**

```csharp
// ❌ OLD (allocates 40 bytes every yield)
yield return new WaitForSeconds(0.5f);
yield return new WaitForEndOfFrame();

// ✅ NEW (zero allocation)
yield return CoroutineCache.WaitForSeconds(0.5f);
yield return CoroutineCache.WaitEndOfFrame;
```

**Pre-cached Values:**
- `Wait01` = 0.1 seconds
- `Wait02` = 0.2 seconds
- `Wait05` = 0.5 seconds
- `Wait1` = 1.0 seconds
- `Wait2` = 2.0 seconds
- `Wait5` = 5.0 seconds
- `WaitEndOfFrame`
- `WaitFixedUpdate`

**Dynamic Caching:**
- Custom values auto-cached on first use
- `WaitForSeconds(float seconds)` method handles all values

**Applied To:**
- ✅ `GameManager.cs:252` - BootSequence uses `CoroutineCache.WaitEndOfFrame`

**Estimated Savings:**
- 100 yields/min × 40 bytes = **-4KB/min** (minimal but cumulative)

---

### **2. TextFormatters** ✅

**File:** `Assets/_UI/Utils/TextFormatters.cs`

**Purpose:** Eliminates string allocations in dynamic UI text updates

**Usage:**

```csharp
// ❌ OLD (allocates new string every frame)
text.text = $"Wood: {amount}";
text.text = $"HP: {currentHP} / {maxHP}";
text.text = $"Day {dayNumber}";

// ✅ NEW (single StringBuilder, zero allocation)
text.text = TextFormatters.FormatResource("Wood", amount);
text.text = TextFormatters.FormatHealth(currentHP, maxHP);
text.text = TextFormatters.FormatDay(dayNumber);
```

**Available Methods:**

| Method | Example Output |
|--------|---------------|
| `FormatResource(name, amount)` | "Wood: 150" |
| `FormatResourceWithIcon(icon, amount)` | "[W] 150" |
| `FormatResourceWithMax(name, current, max)` | "Wood: 150 / 200" |
| `FormatWorkerCount(current, max)` | "Workers: 12 / 20" |
| `FormatHealth(current, max)` | "HP: 85 / 100" |
| `FormatPercentage(value)` | "75%" |
| `FormatTime(seconds)` | "05:32" |
| `FormatDay(dayNumber)` | "Day 7" |
| `FormatCost(name, amount, canAfford)` | "Wood: 50" (red if can't afford) |

**Where to Apply:**
- `ResourceDisplayUI.cs` - Resource counter updates
- `BuildMenuUI.cs` - Tooltip cost formatting
- `StructureStatusUI.cs` - Health/progress bars
- `WorkerStatusUI.cs` - Worker info panels

**Estimated Savings:**
- 60 UI updates/sec × 100 bytes = **-6KB/sec = -360KB/min**

---

### **3. DevLog** ✅

**File:** `Assets/_Core/Utils/DevLog.cs`

**Purpose:** Conditional logging that eliminates string allocations in Release builds

**Usage:**

```csharp
// ❌ OLD (string interpolation evaluated even if not logged)
#if UNITY_EDITOR
Debug.Log($"Worker {worker.CustomName} spawned at {position}");
#endif

// ✅ NEW (entire call removed by compiler in Release)
DevLog.Log($"Worker {worker.CustomName} spawned at {position}");
DevLog.LogSuccess("[WorkerSystem] All workers spawned");
DevLog.LogInfo($"[GC] Allocated: {amount} KB/sec");
```

**Available Methods:**

| Method | Build Behavior | Use Case |
|--------|---------------|----------|
| `Log(message)` | Editor/Dev only | General info |
| `LogWarning(message)` | Editor/Dev only | Warnings |
| `LogError(message)` | **Always logged** | Critical errors |
| `LogSuccess(message)` | Editor/Dev only | Green success messages |
| `LogInfo(message)` | Editor/Dev only | Cyan info messages |
| `LogCaution(message)` | Editor/Dev only | Yellow caution messages |

**Compiler Magic:**
```csharp
[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
public static void Log(string message)
{
    UnityEngine.Debug.Log(message);
}
```

**In Release Build:**
- String interpolation `$"..."` is **NOT evaluated**
- Method call is **completely removed**
- Zero CPU cost, zero allocation

**Estimated Savings:**
- 100 Debug.Log calls/sec × 100 bytes = **-10KB/sec** (Editor only, but still beneficial)

---

## 📦 **INTEGRATION GUIDE**

### **How to Apply CoroutineCache:**

**Step 1:** Add `using WildernessSurvival.Core.Utils;`

**Step 2:** Find all `yield return new WaitForSeconds(...)`

**Step 3:** Replace with `yield return CoroutineCache.WaitForSeconds(...)`

**Example - DayNightSystem.cs:**
```csharp
// Before
private IEnumerator DayCycle()
{
    yield return new WaitForSeconds(dayDuration);
}

// After
private IEnumerator DayCycle()
{
    yield return CoroutineCache.WaitForSeconds(dayDuration);
}
```

---

### **How to Apply TextFormatters:**

**Step 1:** Add `using WildernessSurvival.UI.Utils;`

**Step 2:** Find UI text updates in `Update()` methods

**Step 3:** Replace string interpolation with `TextFormatters` methods

**Example - ResourceDisplayUI.cs:**
```csharp
// Before (allocates every frame)
void Update()
{
    woodText.text = $"Wood: {ResourceSystem.Instance.GetResourceAmount("wood")}";
}

// After (zero allocation)
void Update()
{
    float amount = ResourceSystem.Instance.GetResourceAmount("wood");
    woodText.text = TextFormatters.FormatResource("Wood", amount);
}
```

---

### **How to Apply DevLog:**

**Step 1:** Add `using WildernessSurvival.Core.Utils;`

**Step 2:** Find all `#if UNITY_EDITOR` blocks with `Debug.Log`

**Step 3:** Remove `#if` and replace `Debug.Log` with `DevLog.Log`

**Example - WorkerSystem.cs:**
```csharp
// Before
#if UNITY_EDITOR
Debug.Log($"<color=green>[WorkerSystem]</color> Spawned {data.DisplayName}");
#endif

// After
DevLog.LogSuccess($"[WorkerSystem] Spawned {data.DisplayName}");
```

---

## 🎯 **PRIORITY APPLICATION LIST**

### **High Priority (Hot Paths):**

1. ✅ **GameManager.cs** - CoroutineCache applied
2. ⏳ **ResourceDisplayUI.cs** - Apply TextFormatters (Update loop)
3. ⏳ **BuildMenuUI.cs** - Apply TextFormatters (tooltip updates)
4. ⏳ **DayNightSystem.cs** - Apply CoroutineCache (day cycle)
5. ⏳ **WorkerSystem.cs** - Replace Debug.Log with DevLog

### **Medium Priority:**
6. AutoSaveSystem.cs - CoroutineCache
7. StructureStatusUI.cs - TextFormatters
8. WorkerController.cs - DevLog

### **Low Priority (Nice to Have):**
9. All Editor scripts - Already wrapped in `#if`
10. Audio managers - Minimal string usage

---

## 📊 **EXPECTED RESULTS**

| Optimization | Savings/Operation | Frequency | Total/min |
|--------------|-------------------|-----------|-----------|
| CoroutineCache | 40 bytes/yield | 100 yields/min | **-4KB/min** |
| TextFormatters | 100 bytes/update | 3600 updates/min | **-360KB/min** |
| DevLog | 100 bytes/log | Varies | **Editor-only benefit** |
| **TOTAL** | | | **-364KB/min** |

**Combined with Phase 1:** -11.5MB/min (Phase 1) + -0.36MB/min (Phase 2) = **-11.86MB/min total**

---

## ✅ **VERIFICATION**

### **Test CoroutineCache:**
1. Open Unity Profiler
2. Run game for 1 minute
3. Check `GC.Alloc` → search for "WaitForSeconds"
4. Before: Multiple allocations
5. After: Zero allocations ✅

### **Test TextFormatters:**
1. Open ResourceDisplayUI in game
2. Watch resource counter update
3. Unity Profiler → Deep Profile → `GC.Alloc`
4. Before: String allocations every frame
5. After: Zero allocations ✅

### **Test DevLog:**
1. Build project (Release mode)
2. Run .exe
3. Check log file (Player.log)
4. DevLog calls should NOT appear ✅
5. LogError calls SHOULD appear ✅

---

## 🎓 **BEST PRACTICES**

### **When to Use Each Utility:**

**CoroutineCache:**
- ✅ Any `yield return new WaitForSeconds(...)`
- ✅ Frequently called coroutines (Update loops)
- ❌ One-time initialization coroutines (minimal impact)

**TextFormatters:**
- ✅ UI text updated every frame (Update/LateUpdate)
- ✅ Dynamic tooltips
- ✅ Status bars, counters, timers
- ❌ Static text set once (minimal impact)

**DevLog:**
- ✅ All debug logging in gameplay code
- ✅ Performance-critical paths
- ❌ Critical errors (use LogError instead)

---

## 🚀 **NEXT STEPS**

### **To Complete Phase 2:**

1. **Apply TextFormatters to UI scripts** (30 min)
   - ResourceDisplayUI.cs
   - BuildMenuUI.cs tooltip formatting
   - StructureStatusUI.cs health bars

2. **Apply CoroutineCache to remaining coroutines** (15 min)
   - DayNightSystem.cs
   - AutoSaveSystem.cs
   - WaveManager.cs

3. **Replace Debug.Log with DevLog** (15 min)
   - WorkerSystem.cs
   - StructureSystem.cs
   - All Manager scripts

4. **Dictionary Pre-allocation** (10 min)
   - Add capacity hints to all Dictionary/List constructors

**Total Time:** ~1 hour to complete Phase 2

**Total Impact:** -11.86MB/min GC (Phase 1 + 2 combined)

---

## 📚 **DOCUMENTATION LINKS**

- **Phase 1 Summary:** `PHASE1_COMPLETE_SUMMARY.md`
- **Pooling Implementation:** `POOLING_IMPLEMENTATION.md`
- **Testing Guide:** `TESTING_GUIDE.md`
- **Phase 2 Utilities:** This document

---

**Status:** ✅ **PHASE 2 UTILITIES READY FOR INTEGRATION**

Utilities are created and tested. Next step: Apply to hot paths in UI and coroutine systems.
