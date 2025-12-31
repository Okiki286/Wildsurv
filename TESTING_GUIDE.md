# 🧪 Testing Guide - Worker Pooling System

## Quick Start (5 minuti)

### 1️⃣ Setup in Unity Editor

1. **Open Unity Project** (`Wilderness - Copy - Copy`)
2. **Open Main Scene** (la tua scene principale con WorkerSystem)
3. **Verifica che esista il GameObject `WorkerSystem`** nella Hierarchy

### 2️⃣ Aggiungi GC Monitor

1. Create → Empty GameObject
2. Rename to `GCMonitor`
3. Add Component → `GCMonitor` (script in `Assets/_Core/Profiling/`)
4. In Inspector:
   - ✅ Log To Console
   - ✅ Show On Screen GUI
   - Update Interval: `1.0` sec

### 3️⃣ Aggiungi Pooling Tester

1. Create → Empty GameObject
2. Rename to `PoolingTest`
3. Add Component → `PoolingTest` (script in `Assets/_Core/Profiling/`)
4. In Inspector:
   - Spawn Count: `10`
   - Destroy Delay: `2.0` sec

### 4️⃣ Configure WorkerSystem Pooling

1. Select `WorkerSystem` GameObject in Hierarchy
2. In Inspector, trova la sezione **"Pooling Settings"**:
   - **Pool Prewarm Count:** `5` (pre-alloca 5 worker all'avvio)
   - **Pool Max Size:** `100` (massimo worker nel pool)
   - **Worker Pool Container:** (lascia vuoto, si auto-crea)

---

## 🎯 Test Procedure

### Test 1: Verifica Inizializzazione Pool

**Action:** Play the game

**Expected Console Output:**
```
[WorkerSystem] Pre-warmed pool for 'Villager' with 5 workers
[WorkerSystem] Initialized 1 worker pools (capacity: 5, max: 100)
```

**Expected Hierarchy:**
```
WorkerSystem
  └─ _WorkerPools (auto-created)
       ├─ Worker_Villager_0001 (inactive)
       ├─ Worker_Villager_0002 (inactive)
       ├─ Worker_Villager_0003 (inactive)
       ├─ Worker_Villager_0004 (inactive)
       └─ Worker_Villager_0005 (inactive)
```

**Expected GCMonitor (On-Screen GUI):**
```
GC MONITOR
Memory: 150.2 MB
Alloc Rate: 12.3 KB/sec
GC Count: 0
Last GC: 5.2s ago
✓ EXCELLENT
```

**✅ PASS:** Pool initialized, workers pre-warmed, GC rate low
**❌ FAIL:** Errors in console, no _WorkerPools folder, GC rate >100KB/sec

---

### Test 2: Spawn Workers (Pooling Active)

**Action:**
1. Select `PoolingTest` GameObject
2. Right-click on script → `Test Spawn Workers`

**Expected Console Output:**
```
[PoolingTest] Spawning 10 workers...
[WorkerSystem] Spawned Villager at (x,y,z) (pooled)  // x10
[PoolingTest] Spawned 10 workers. Check GCMonitor for allocation rate.
```

**Expected GCMonitor:**
```
Alloc Rate: < 50 KB/sec  // Should be EXCELLENT or GOOD
```

**Expected Hierarchy:**
- 10 new active workers in scene root (NOT under _WorkerPools)
- _WorkerPools now empty (all workers in use)

**✅ PASS:** Workers spawned, labeled "(pooled)", GC <50KB/sec
**❌ FAIL:** "Pool not found" warnings, GC >500KB/sec, workers under _WorkerPools

---

### Test 3: Destroy Workers (Return to Pool)

**Action:**
1. Wait 2 seconds after Test 2
2. OR manually: Right-click script → `Test Destroy Workers`

**Expected Console Output:**
```
[PoolingTest] Destroying 10 workers...
[WorkerSystem] Released WorkerName to pool  // x10
[PoolingTest] Workers destroyed and returned to pool.
```

**Expected GCMonitor:**
```
Alloc Rate: < 10 KB/sec  // Should remain EXCELLENT
```

**Expected Hierarchy:**
- 10 workers moved back under `_WorkerPools` (inactive)
- Scene root clean (no worker clutter)

**✅ PASS:** Workers returned to pool, GC <10KB/sec, no Destroy() calls
**❌ FAIL:** Workers destroyed (removed from Hierarchy), GC >100KB/sec

---

### Test 4: Stress Test (Spawn-Destroy Cycle)

**Action:**
1. Right-click script → `Test Spawn-Destroy Cycle`
2. Repeat 5 times (wait 3 sec between each)

**Expected GCMonitor After 5 Cycles (50 spawns + 50 destroys):**
```
Memory: 150-200 MB (stable, no growth)
Alloc Rate: < 50 KB/sec average
GC Count: 0-1 (should NOT trigger GC)
✓ EXCELLENT or ✓ GOOD
```

**Expected Result:**
- No memory growth (memory returns to baseline after each cycle)
- No GC.Collect events (console should NOT show red GC warnings)
- Smooth 60fps (no frame drops)

**✅ PASS:** Memory stable, no GC, 60fps maintained
**❌ FAIL:** Memory grows each cycle, GC every 30sec, frame drops to 30fps

---

## 📊 Performance Benchmarks

### Before Pooling (Expected Baseline)

| Metric | Value | Status |
|--------|-------|--------|
| Spawn 10 workers | 500KB GC | ❌ POOR |
| Destroy 10 workers | 500KB GC | ❌ POOR |
| Total cycle | 1000KB (1MB) | ❌ POOR |
| GC frequency | Every 30 sec | ❌ POOR |

### After Pooling (Target Results)

| Metric | Value | Status |
|--------|-------|--------|
| Spawn 10 workers | <10KB GC | ✅ EXCELLENT |
| Destroy 10 workers | 0KB GC | ✅ EXCELLENT |
| Total cycle | <10KB | ✅ EXCELLENT |
| GC frequency | Every 5+ min | ✅ EXCELLENT |

**Improvement:** **-99% GC allocation** per spawn/destroy cycle

---

## 🐛 Troubleshooting

### Issue: "WorkerSystem.Instance is null"

**Cause:** WorkerSystem not in scene or disabled
**Fix:**
1. Check WorkerSystem GameObject exists in Hierarchy
2. Ensure it's enabled (checkbox in Inspector)
3. Verify script is attached and not disabled

---

### Issue: "Pool not found for 'WorkerId', falling back to Instantiate"

**Cause:** Worker pool not initialized
**Fix:**
1. Check WorkerSystem has `availableWorkerTypes` configured
2. Ensure WorkerData has valid `WorkerId` field
3. Verify `InitializeWorkerPools()` was called (check console for init logs)

---

### Issue: Workers appear under _WorkerPools when spawned

**Expected Behavior:** Workers are under _WorkerPools only when INACTIVE (pooled)
**Active workers:** Should be under scene root, NOT _WorkerPools

If workers remain under _WorkerPools while active:
1. Check `OnGetWorkerFromPool()` sets `SetActive(true)`
2. Verify caller re-parents worker after `pool.Get()`

---

### Issue: GC still high (>100KB/sec) after pooling

**Possible Causes:**
1. **Other systems allocating** - Check Unity Profiler (CPU → Deep Profile → GC.Alloc)
2. **UI updates** - Phase 1.2 not yet implemented
3. **String allocations** - Debug.Log calls with interpolation
4. **LINQ usage** - Query methods still using `.Where().ToList()`

**Diagnostic:**
- Use `Unity Profiler → CPU Usage → Deep Profile`
- Filter by "GC.Alloc" tag
- Identify top allocation sources

---

## ✅ Acceptance Criteria

Phase 1.1 is **COMPLETE** if all these pass:

- [ ] Pool initialization logs appear on Play
- [ ] _WorkerPools folder created automatically
- [ ] Pre-warmed workers visible in Hierarchy (inactive)
- [ ] Spawn 10 workers: GC <50KB/sec
- [ ] Destroy 10 workers: GC <10KB/sec
- [ ] 5 spawn-destroy cycles: No GC.Collect events
- [ ] Memory stable (no growth after 5 cycles)
- [ ] Console shows "(pooled)" label on spawn
- [ ] Console shows "Released to pool" on destroy
- [ ] 60fps maintained during stress test

**Current Status:** 🟡 **READY FOR TESTING**

---

## 📝 Next Steps After Testing

### If All Tests Pass ✅

1. Mark Phase 1.1 as **VERIFIED**
2. Proceed to **Phase 1.2: UI Element Pooling**
3. Expected combined reduction: **-11.5MB/min GC**

### If Tests Fail ❌

1. Document failing test number
2. Copy Console errors to issue tracker
3. Check Unity version compatibility (requires Unity 2021+)
4. Verify `UnityEngine.Pool` namespace is available

---

## 🎓 Understanding the Results

### What You're Seeing:

**Before Pooling:**
- `Instantiate()` allocates memory on heap
- `Destroy()` marks for GC but doesn't free immediately
- GC runs when heap fills up → 16-33ms freeze

**After Pooling:**
- `pool.Get()` reuses existing GameObject (no allocation)
- `pool.Release()` deactivates and caches (no GC)
- GC rarely runs → smooth 60fps

### Key Metrics:

- **<50KB/sec:** EXCELLENT - Pool working perfectly
- **50-100KB/sec:** GOOD - Pool working, minor leaks elsewhere
- **100-500KB/sec:** MODERATE - Pool working, major leaks elsewhere
- **>500KB/sec:** POOR - Pool NOT working or disabled

---

**Ready to test?** 🚀

Open Unity, follow the steps, and report back the GCMonitor readings!
