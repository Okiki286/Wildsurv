# Auto-Credit Shards on Enemy Death

## A) Diagnosis: Why Log Says "dropping X shards" But Total Doesn't Change

The log at `EnemyInstance.cs:693` shows:
```csharp
Debug.Log($"<color=red>[Enemy]</color> {enemyData.DisplayName} died, dropping {shardDrop} shards");
```

**Root cause**: The code calculates `shardDrop` but **never calls `ResourceSystem.Instance.AddResource()`**. It only logs the value.

### 2-3 Probable Causes (Confirmed):
1. **Missing API Call**: `Die()` calculates shards but has no `ResourceSystem.AddResource("shards", shardDrop)` call.
2. **"TODO" Left Behind**: Line 695 has a comment `// TODO: Drop resources, notify systems, play VFX/SFX` in `EnemyController.Die()` indicating this was never implemented.
3. **Two Die() Implementations**: Both `EnemyInstance.Die()` and `EnemyController.Die()` have this issue.

---

## B) Proposed Changes

### _Gameplay/Enemies

#### [MODIFY] [EnemyInstance.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Gameplay/Enemies/EnemyInstance.cs)

**Changes**:
1. Add an enum and field for drop mode at class level
2. Modify `Die()` to credit shards to player when mode is `AutoCredit`
3. Add protection flag to prevent double-crediting

```csharp
// Add after line 68 (after HasWaystoneDebuff property):
/// <summary>
/// How shards are granted to the player on death
/// </summary>
public enum ShardDropMode
{
    AutoCredit,  // Immediately add to player's total
    WorldPickup  // Spawn pickup in world (future)
}

// Add as new field (around line 83):
[TitleGroup("Rewards")]
[SerializeField] private ShardDropMode shardDropMode = ShardDropMode.AutoCredit;
private bool hasDroppedRewards = false;  // Anti-double-drop guard
```

```csharp
// Replace Die() method (lines 682-697):
protected virtual void Die()
{
    // Guard against double-drop (e.g., if Die() called multiple times)
    if (hasDroppedRewards) return;
    hasDroppedRewards = true;

    // Stop NavMesh
    if (agent != null)
    {
        agent.isStopped = true;
        agent.enabled = false;
    }

    // Calculate rewards
    int shardDrop = Mathf.RoundToInt(enemyData.BaseShardDrop * rewardMultiplier);
    
    // Credit shards based on mode
    if (shardDropMode == ShardDropMode.AutoCredit)
    {
        float shardsBefore = 0f;
        float shardsAfter = 0f;
        
        if (ResourceSystem.Instance != null)
        {
            shardsBefore = ResourceSystem.Instance.GetResourceAmount("shards");
            ResourceSystem.Instance.AddResource("shards", shardDrop);
            shardsAfter = ResourceSystem.Instance.GetResourceAmount("shards");
        }
        
        Debug.Log($"<color=green>[Enemy]</color> {enemyData.DisplayName} died → +{shardDrop} shards " +
            $"(BEFORE: {shardsBefore:F0} → AFTER: {shardsAfter:F0})");
    }
    else
    {
        // WorldPickup mode - just log for now, spawn pickup later
        Debug.Log($"<color=red>[Enemy]</color> {enemyData.DisplayName} died, would drop {shardDrop} shards (WorldPickup mode)");
    }

    // Destroy
    Destroy(gameObject);
}
```

> [!IMPORTANT]
> The `ResourceSystem.Instance.AddResource()` call will trigger the `onResourceChanged` event, which the UI listens to via polling. No additional UI update code is needed.

---

#### [MODIFY] [EnemyController.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Gameplay/Enemies/EnemyController.cs)

Apply same fix to the legacy `EnemyController.Die()` method:

```csharp
// Replace Die() method (around line 395-420):
protected virtual void Die()
{
    // Guard against double-drop
    if (hasDroppedRewards) return;
    hasDroppedRewards = true;

    // Stop movement
    if (navAgent != null && navAgent.isOnNavMesh)
    {
        navAgent.isStopped = true;
    }

    // Calculate rewards
    int shardDrop = 0;
    if (enemyData != null)
    {
        shardDrop = Mathf.RoundToInt(enemyData.BaseShardDrop * rewardMultiplier);
    }

    // Auto-credit shards
    if (ResourceSystem.Instance != null && shardDrop > 0)
    {
        float shardsBefore = ResourceSystem.Instance.GetResourceAmount("shards");
        ResourceSystem.Instance.AddResource("shards", shardDrop);
        float shardsAfter = ResourceSystem.Instance.GetResourceAmount("shards");

        if (debugMode)
        {
            string enemyName = enemyData != null ? enemyData.DisplayName : name;
            Debug.Log($"<color=green>[EnemyController]</color> {enemyName} DIED → +{shardDrop} shards " +
                $"(BEFORE: {shardsBefore:F0} → AFTER: {shardsAfter:F0})");
        }
    }

    Destroy(gameObject);
}
```

Add field at class level:
```csharp
private bool hasDroppedRewards = false;
```

---

## C) Protection Anti-Double-Drop

Implemented via:
1. **`hasDroppedRewards` flag**: Set to `true` at start of `Die()`, checked with early return
2. **`ShardDropMode` enum**: Allows future toggle between `AutoCredit` and `WorldPickup` without code changes
3. **Inspector-configurable**: Can switch mode per-enemy or globally

---

## D) Test Plan

### Test 1: Basic Auto-Credit
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Note current Shards count in HUD | e.g., "💎 10" |
| 2 | Kill one Enemy_Default (5 base shards) | Console shows: `[Enemy] Enemy Default died → +5 shards (BEFORE: 10 → AFTER: 15)` |
| 3 | Check HUD | Shows "💎 15" |

### Test 2: Multiple Enemy Kills
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Start with 0 shards | HUD shows "💎 0" |
| 2 | Kill 3 enemies (5 shards each) | Console shows 3 separate logs with correct BEFORE/AFTER values: 0→5, 5→10, 10→15 |
| 3 | Verify final count | HUD shows "💎 15" |

### Test 3: Double-Die Protection
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Add breakpoint or log in `Die()` after guard check | N/A |
| 2 | Force call `enemy.TakeDamage(999)` twice rapidly | `Die()` only credits shards once |
| 3 | Verify shards only increased once | BEFORE/AFTER log appears exactly once |

### Test 4: WorldPickup Mode (Future-Proofing)
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | In Inspector, set enemy's `shardDropMode = WorldPickup` | N/A |
| 2 | Kill the enemy | Console shows: `[Enemy] ... would drop X shards (WorldPickup mode)` |
| 3 | Verify shards DID NOT increase | HUD unchanged |

### Test 5: ResourceSystem Null Safety
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Temporarily disable ResourceSystem in scene | N/A |
| 2 | Kill enemy | No crash, log still appears |
| 3 | Re-enable ResourceSystem | N/A |

---

## Verification Plan

### Manual Testing (Primary)
Run in Unity Editor:
1. Open scene with enemies and ResourceSystem
2. Press Play
3. Use debug key `R` to reset resources (or start fresh)
4. Kill enemies manually (spawn via WaveSystem or debug button)
5. Observe console logs for BEFORE/AFTER format
6. Observe HUD for real-time update

### Automated (N/A)
No existing unit tests for enemy death rewards. Manual verification is sufficient for this change.

---

## Vincoli Rispettati

| Constraint | Solution |
|------------|----------|
| Mobile-friendly (no alloc, no LINQ) | Uses direct field access, no LINQ, minimal string formatting |
| No breaking future WorldPickup | Enum allows switching modes |
| Naming coerente | Uses existing patterns: `shardDrop`, `rewardMultiplier`, `ResourceSystem.Instance` |
