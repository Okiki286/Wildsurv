# Enemy "Sliding" Bug - SOLUZIONE COMPLETA 🎯

## ✅ STATO ATTUALE

Dalla tua ultima verifica, so che:

✅ **EnemyAnimatorController** component ESISTE sul prefab W_Skeleton_Minion
✅ **AnimatorController** (KayKit_Enemy_Controller) è ASSEGNATO all'Animator
✅ **Parametri** Speed, IsMoving, IsDead, IsAttacking esistono e sono rilevati
✅ **Animator reference** è correttamente assegnato in EnemyAnimatorController

❌ **MA**: Speed = 0 e IsMoving = False anche quando il nemico si muove!

---

## 🔍 DIAGNOSI: Perché Speed resta 0?

Il codice in `EnemyController.Update()` (linee 174-178) **dovrebbe** aggiornare l'animator:

```csharp
if (animatorController != null && navAgent != null && navAgent.isOnNavMesh)
{
    float speed = navAgent.velocity.magnitude;
    animatorController.SetSpeed(speed);
    animatorController.SetMoving(speed > 0.1f);
}
```

Se Speed resta 0, significa che **UNA** di queste condizioni è falsa:

1. ❌ `animatorController` è null (component non trovato in Awake)
2. ❌ `navAgent` è null (component non trovato)
3. ❌ `navAgent.isOnNavMesh` è false (nemico non è sul NavMesh)
4. ❌ `isInitialized` è false (Update esce presto alla linea 163)
5. ❌ `navAgent.velocity.magnitude` è effettivamente 0 (NavMeshAgent non si sta muovendo)

---

## 🛠️ PROCEDURA DEBUG COMPLETA

### ✅ Step 1: Abilita Debug Mode

**QUESTO È CRITICO!**

1. **Apri Unity Editor**
2. **Play Mode** → Spawna un nemico
3. **Hierarchy** → Seleziona lo scheletro spawned "W_Skeleton_Minion"
4. **Inspector** → Component `EnemyController`
5. **Trova checkbox** `debugMode` (potrebbe essere nascosto in una sezione "Debug")
6. **ABILITA** `debugMode` ✅ (spunta la checkbox)

**Risultato atteso in Console**:

Se tutto funziona, dovresti vedere ogni ~1 secondo:
```
[EnemyController] W_Skeleton_Minion Animator Update: Speed=3.45, IsMoving=True, Velocity=(0.0, 0.0, 3.5)
```

Se invece vedi:
```
[EnemyController] W_Skeleton_Minion Animator NOT updated: animatorController=False, navAgent=True, isOnNavMesh=True
```

→ **animatorController è NULL!** (GetComponent ha fallito in Awake)

---

### ✅ Step 2: Verifica Inspector in Runtime

**In Play Mode con nemico spawned**:

1. **Seleziona** nemico in Hierarchy
2. **Inspector** → Guarda questi componenti:

#### A) EnemyController
```
✅ isInitialized: [Valore?]  ← SE FALSE, Update NON gira!
✅ debugMode: True  ← DEVE essere abilitato per vedere log
```

#### B) NavMeshAgent
```
✅ Speed: 3.5
✅ Stopping Distance: ~1.6
✅ Agent Type: Humanoid
✅ Is On NavMesh: [Valore?]  ← DEVE essere True!
```

#### C) Animator
```
✅ Controller: KayKit_Enemy_Controller
✅ Apply Root Motion: False
✅ Parameters (espandi "Parameters" tab):
   - Speed: [Valore in tempo reale]  ← Dovrebbe cambiare da 0 a 3.5!
   - IsMoving: [Valore in tempo reale]  ← Dovrebbe diventare true!
```

#### D) EnemyAnimatorController
```
✅ Animator: [Riferimento assegnato]
✅ Is Initialized: [Valore?]  ← DEVE essere True!
```

---

### ✅ Step 3: Test Velocità NavMeshAgent

**Problema possibile**: NavMeshAgent.velocity potrebbe essere 0 anche se il nemico si muove.

**Debug in Play Mode**:

1. Seleziona nemico spawned
2. Inspector → NavMeshAgent component
3. **Espandi "Debug Info"** (se disponibile)
4. Guarda:
   - **Velocity**: Dovrebbe essere > 0 quando si muove
   - **Desired Velocity**: Dovrebbe essere ~(0, 0, 3.5) quando va verso player
   - **Remaining Distance**: Dovrebbe diminuire

**Se Velocity = 0 ma il nemico si muove**:
- NavMesh potrebbe non esistere nella scena
- Nemico potrebbe essere fuori dal NavMesh
- NavMeshAgent potrebbe essere disabilitato

---

### ✅ Step 4: Verifica NavMesh nella Scena

**Problema comune**: NavMesh non è stato generato (baked).

1. **Window** → **AI** → **Navigation**
2. Tab **"Bake"**
3. Verifica se c'è un NavMesh nella scena:
   - Guarda Scene View con **NavMesh visualization** abilitata
   - Dovrebbe essere visibile come overlay blu sulle superfici calpestabili

**Se NavMesh non esiste**:
1. Navigation window → Bake tab
2. Click **"Bake"** button
3. Aspetta generazione
4. Test di nuovo

---

### ✅ Step 5: Aggiungi Log Temporaneo

**Se i log di debug NON appaiono**, aggiungi log manuale in EnemyController.cs.

**Apri** `EnemyController.cs` e modifica `Update()` (linea ~161):

```csharp
private void Update()
{
    // TEMPORARY DEBUG LOG
    Debug.Log($"[EnemyController] {name} Update() called. isInitialized={isInitialized}");

    if (!isInitialized)
    {
        Debug.LogWarning($"[EnemyController] {name} NOT initialized! Skipping Update.");
        return;
    }

    // ... resto del codice
}
```

**Risultato atteso**:
- Se vedi "NOT initialized!" → Il problema è che `Initialize()` non è stato chiamato dal spawner
- Se vedi "Update() called. isInitialized=True" → Update gira, il problema è altrove

---

## 🚨 POSSIBILI CAUSE E FIX

### Causa 1: `isInitialized = false`

**Sintomo**: Update esce subito, log non appaiono.

**Causa**: `EnemySpawner` non chiama `Initialize(EnemyData)` sul nemico spawned.

**Fix**: Verifica EnemySpawner.cs:

```csharp
// In EnemySpawner.cs, dopo GetPooledEnemy():
EnemyController enemyController = enemy.GetComponent<EnemyController>();
if (enemyController != null)
{
    enemyController.Initialize(enemyData, ...); // DEVE essere chiamato!
}
```

---

### Causa 2: `animatorController = null`

**Sintomo**: Log mostra "animatorController=False".

**Causa**: `GetComponent<EnemyAnimatorController>()` in Awake() fallisce.

**Debug**:

1. Apri EnemyController.cs
2. Modifica Awake() (linea ~124):

```csharp
private void Awake()
{
    navAgent = GetComponent<NavMeshAgent>();
    animatorController = GetComponent<EnemyAnimatorController>();

    // TEMPORARY DEBUG
    Debug.Log($"[EnemyController] {name} Awake(): animatorController={animatorController != null}, navAgent={navAgent != null}");

    if (animatorController == null)
    {
        Debug.LogError($"[EnemyController] {name} EnemyAnimatorController NOT FOUND on GameObject!", this);
    }

    // ... resto del codice
}
```

**Se log mostra "EnemyAnimatorController NOT FOUND"**:
- Significa che il component NON è sul GameObject (inspiegabile dato che è sul prefab!)
- Possibile: stai spawnando un prefab diverso (non W_Skeleton_Minion)

---

### Causa 3: `navAgent.isOnNavMesh = false`

**Sintomo**: Log mostra "isOnNavMesh=False".

**Causa**: Nemico spawna fuori dal NavMesh o NavMesh non esiste.

**Fix**:

1. **Verifica posizione spawn**:
   - Spawn point deve essere su terreno con NavMesh
   - Y position corretta (non troppo alta)

2. **Forza posizione su NavMesh** in EnemySpawner:

```csharp
NavMeshHit hit;
if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
{
    enemy.transform.position = hit.position; // Usa posizione NavMesh valida
}
```

---

### Causa 4: `navAgent.velocity = 0` (NavMeshAgent non si muove)

**Sintomo**: Nemico si muove visivamente ma velocity.magnitude = 0.

**Causa possibile**:
- NavMeshAgent è disabilitato
- NavMeshAgent.isStopped = true
- Destination non è impostata

**Fix**:

Verifica in `EnemyController.UpdateDestination()` (linea ~301):

```csharp
private void UpdateDestination()
{
    if (targetTransform == null) return;

    if (navAgent != null && navAgent.isOnNavMesh)
    {
        navAgent.isStopped = false; // IMPORTANTE!
        navAgent.SetDestination(targetTransform.position);

        Debug.Log($"[EnemyController] {name} Destination set to {targetTransform.position}, velocity={navAgent.velocity.magnitude}");
    }
}
```

---

## 📊 CHECKLIST FINALE

Verifica TUTTI questi punti prima di arrenderti:

- [ ] ✅ `debugMode` abilitato in EnemyController (Inspector)
- [ ] ✅ Console mostra log "Animator Update" ogni ~1 secondo quando nemico si muove
- [ ] ✅ NavMesh esiste nella scena (Window → AI → Navigation → Bake)
- [ ] ✅ Nemico spawna SU NavMesh (Scene view con NavMesh visualization)
- [ ] ✅ NavMeshAgent.isOnNavMesh = True (Inspector in Play Mode)
- [ ] ✅ NavMeshAgent.velocity.magnitude > 0 quando si muove (Inspector)
- [ ] ✅ EnemyController.isInitialized = True (verifica con log temporaneo)
- [ ] ✅ EnemyAnimatorController component esiste sul prefab (già verificato ✅)
- [ ] ✅ Animator.Controller = KayKit_Enemy_Controller (già verificato ✅)
- [ ] ✅ Parameters Speed e IsMoving cambiano in runtime (Animator → Parameters tab)

---

## 🎯 RISULTATO ATTESO

Dopo il fix, con `debugMode = true`:

**Console log** (ogni ~1 secondo):
```
[EnemyController] W_Skeleton_Minion Animator Update: Speed=3.45, IsMoving=True, Velocity=(0.0, 0.0, 3.5)
```

**Inspector → Animator → Parameters** (in tempo reale):
```
Speed: 3.45  (cambia dinamicamente: 0 → 3.5 → 0)
IsMoving: True  (cambia: false → true → false)
```

**Animazione visibile**:
✅ Nemico cammina smoothly con animazione Walk
✅ Quando si ferma, torna in Idle
✅ Quando muore, esegue animazione Death

---

## 🆘 SE ANCORA NON FUNZIONA

**Posta in Console**:

1. Output completo di `debugMode = true` con nemico in movimento
2. Screenshot dell'Inspector con nemico selezionato (mostra TUTTI i component)
3. Log di Awake() (dopo aver aggiunto log temporaneo)
4. Screenshot della Navigation window (tab Bake) per verificare NavMesh

**Informazioni critiche**:
- Unity version?
- NavMesh è generato?
- Stai usando pooling? (potrebbe influire su inizializzazione)
- Spawner chiama `Initialize(enemyData)` sul nemico?

---

**Passo successivo immediato**: Abilita `debugMode` in EnemyController e posta il log della Console quando il nemico si muove!
