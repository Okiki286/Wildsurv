# Performance Patch Report - GC Elimination

**Data:** 2025-12-19
**Obiettivo:** Eliminare GC spikes e costi inutili per performance mobile

---

## File Modificati

| File | Path Completo |
| :--- | :--- |
| StructureSystem.cs | `Assets/_Gameplay/Structures/StructureSystem.cs` |
| StructureController.cs | `Assets/_Gameplay/Structures/StructureController.cs` |
| EnemyInstance.cs | `Assets/_Gameplay/Enemies/EnemyInstance.cs` |

---

## Obiettivo A — Structure Tracking senza LINQ

### Cambiamenti Applicati

#### 1. Aggiunto evento `OnStructuresChanged` (StructureSystem.cs)
```diff
+        // ============================================
+        // EVENT: Notifica cambiamenti strutture (no polling)
+        // ============================================
+        public event System.Action OnStructuresChanged;
```

#### 2. Rimosso LINQ da `UpdateCounts()` (StructureSystem.cs)
```diff
         private void UpdateCounts()
         {
-            totalStructureCount = allStructures.Count;
-            operationalCount = allStructures.Count(s => s.IsOperational);
-            buildingCount = allStructures.Count(s => s.State == StructureState.Building);
+            // FIX: I contatori sono ora gestiti incrementalmente in Register/Unregister.
+            // Questo metodo non fa più nulla ma è mantenuto per backward compatibility.
         }
```

#### 3. Contatori incrementali in `RegisterStructure()` (StructureSystem.cs)
```diff
             if (!allStructures.Contains(structure))
             {
                 allStructures.Add(structure);
+                totalStructureCount++;
+
+                // Incrementa contatore stato
+                if (structure.State == StructureState.Building)
+                    buildingCount++;
+                else if (structure.IsOperational)
+                    operationalCount++;
+
+                OnStructuresChanged?.Invoke();
             }
```

#### 4. Contatori incrementali in `UnregisterStructure()` (StructureSystem.cs)
```diff
-            allStructures.Remove(structure);
+            if (allStructures.Remove(structure))
+            {
+                totalStructureCount = Mathf.Max(0, totalStructureCount - 1);
+
+                // Decrementa contatore stato
+                if (structure.State == StructureState.Building)
+                    buildingCount = Mathf.Max(0, buildingCount - 1);
+                else if (structure.IsOperational)
+                    operationalCount = Mathf.Max(0, operationalCount - 1);
+
+                OnStructuresChanged?.Invoke();
+            }
```

#### 5. Nuovo metodo `NotifyStructureStateChanged()` (StructureSystem.cs)
```diff
+        public void NotifyStructureStateChanged(StructureState oldState, StructureState newState)
+        {
+            if (oldState == StructureState.Building)
+                buildingCount = Mathf.Max(0, buildingCount - 1);
+            else if (oldState == StructureState.Operating)
+                operationalCount = Mathf.Max(0, operationalCount - 1);
+
+            if (newState == StructureState.Building)
+                buildingCount++;
+            else if (newState == StructureState.Operating)
+                operationalCount++;
+
+            OnStructuresChanged?.Invoke();
+        }
```

#### 6. Integrazione in `ChangeState()` (StructureController.cs)
```diff
             currentState = newState;
+
+            // Notifica StructureSystem del cambio stato per aggiornare i contatori
+            StructureSystem.Instance?.NotifyStructureStateChanged(previousState, newState);
+
             OnStateChanged(newState, previousState);
```

---

## Obiettivo B — Enemy AI Scan Throttled

> [!NOTE]
> Il codice esistente **già usa** `Physics.OverlapSphereNonAlloc` con un buffer statico preallocato (`scanBuffer`). Le modifiche aggiungono solo lo staggered delay e la pulizia buffer.

### Cambiamenti Applicati

#### 1. Staggered scan delay (EnemyInstance.cs)
```diff
         private float targetScanTimer;
         private const float TARGET_SCAN_INTERVAL = 0.5f;
+        // Staggered scan: ogni nemico inizia con delay random per distribuire il carico CPU
+        private bool hasInitializedScanDelay = false;
```

```diff
             EnsureOnNavMesh();

-            // Initial target scan
-            targetScanTimer = 0f; // Force immediate scan
+            // Staggered scan: ogni nemico inizia con delay random per distribuire il carico CPU
+            if (!hasInitializedScanDelay)
+            {
+                targetScanTimer = UnityEngine.Random.Range(0f, TARGET_SCAN_INTERVAL);
+                hasInitializedScanDelay = true;
+            }
             isInitialized = true;
```

#### 2. Pulizia buffer dopo scan (EnemyInstance.cs)
```diff
             }

+            // Pulizia buffer per evitare riferimenti stale (safe per GC)
+            for (int i = 0; i < hitCount; i++)
+            {
+                scanBuffer[i] = null;
+            }
+
             // Se trovato un target migliore, aggiorna
```

---

## Perché è Sicuro

| Cambiamento | Motivo Sicurezza |
| :--- | :--- |
| Contatori incrementali | `Mathf.Max(0, ...)` previene conteggi negativi. Evento invocato dopo la modifica. |
| NotifyStructureStateChanged | Chiamato solo quando lo stato effettivamente cambia (guard `if newState == currentState return`). |
| Staggered delay | Non influenza il targeting - il fallback a Waystone è immediato in `Initialize()`. |
| Pulizia buffer | Buffer è statico condiviso, la pulizia evita riferimenti stale se oggetti vengono distrutti tra scan. |

---

## Checklist di Test Manuale

- [ ] **Spawn 50+ nemici:** Verifica che non ci siano GC spikes evidenti nel Profiler Unity.
- [ ] **Targeting corretto:** I nemici devono ancora trovare e attaccare worker/strutture correttamente.
- [ ] **Conteggi strutture Building:** Piazza 3 strutture in costruzione, verifica `buildingCount = 3`.
- [ ] **Conteggi strutture Operating:** Completa costruzione, verifica `operationalCount` incrementa e `buildingCount` decrementa.
- [ ] **Distruzione struttura:** Distruggi una struttura, verifica conteggi decrementano (no negativi).
- [ ] **Upgrade (se implementato):** Se esiste logica di upgrade che cambia StructureType, verifica conteggi coerenti.

---

## Elementi Riusati (Non Duplicati)

- **`scanBuffer` statico:** Già esistente in `EnemyInstance.cs` linea 99 (`private static readonly Collider[] scanBuffer = new Collider[32]`).
- **`OverlapSphereNonAlloc`:** Già usato in `ScanForTarget()` linea 428.
- **`structuresByCategory` Dictionary:** Già esistente per lookup per categoria.
