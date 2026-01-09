# Fix Animazioni Nemici - Guida Rapida 🎯

## ✅ PROBLEMA RISOLTO

**Sintomo**: Lo scheletro spawna e si muove ma resta in **Idle/T-Pose** (nessuna animazione di camminata).

**Causa**: `EnemyController.cs` muove il nemico con NavMeshAgent ma **NON aggiorna l'Animator**.

**Soluzione**: Aggiungere `EnemyAnimatorController` che collega NavMeshAgent → Animator.

---

## 🎮 PARAMETRI CORRETTI (KayKit_Enemy_Controller)

Il controller usa questi parametri:

| Parametro | Tipo | Scopo | Valore Default |
|-----------|------|-------|----------------|
| **Speed** | Float | Velocità per blend tree (0=Idle, 1.5=Walk, 3.5=Run) | 0.0 |
| **IsMoving** | Bool | Transition Idle ↔ Walk | false |
| **IsAttacking** | Bool | Stato attacco | false |
| **IsDead** | Bool | Stato morte | false |
| **OnHit** | Trigger | Hit reaction | - |

**IMPORTANTE**: Il controller si chiama `KayKit_Enemy_Controller` e usa **IsAttacking** (Bool), NON "Attack" (Trigger)!

---

## 🔧 INTEGRAZIONE STEP-BY-STEP

### Step 1: Aggiungi EnemyAnimatorController al Prefab

1. Apri `W_Skeleton_Minion.prefab`
2. **Add Component** → `EnemyAnimatorController`
3. L'Animator si assegna automaticamente
4. **Verifica parametri** (Inspector):
   - Speed Param Name: `Speed` ✅
   - Is Moving Param Name: `IsMoving` ✅
   - Is Dead Param Name: `IsDead` ✅
   - Is Attacking Param Name: `IsAttacking` ✅
5. **Salva Prefab**

---

### Step 2: Modifica EnemyController.cs

Aggiungi queste 3 modifiche:

#### **A) Aggiungi campo (line ~103)**
```csharp
private NavMeshAgent navAgent;
private EnemyAnimatorController animatorController; // NUOVO
```

#### **B) Cache riferimento in Awake() (line ~123)**
```csharp
private void Awake()
{
    navAgent = GetComponent<NavMeshAgent>();
    animatorController = GetComponent<EnemyAnimatorController>(); // NUOVO
}
```

#### **C) Aggiorna animazioni in Update() (line ~159-176)**
```csharp
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

    // ============ NUOVO: AGGIORNA ANIMATOR ============
    if (animatorController != null && navAgent != null && navAgent.isOnNavMesh)
    {
        float currentSpeed = navAgent.velocity.magnitude;
        bool isMoving = currentSpeed > 0.1f;

        animatorController.SetSpeed(currentSpeed);
        animatorController.SetMoving(isMoving);
    }
    // ===================================================

    // If not using NavMesh, do manual movement
    if (!useNavMesh || navAgent == null || !navAgent.isOnNavMesh)
    {
        MoveTowardsTarget();
    }
}
```

#### **D) Aggiungi animazione morte in Die() (line ~441)**
```csharp
protected virtual void Die()
{
    // Stop movement
    if (navAgent != null && navAgent.isOnNavMesh)
    {
        navAgent.isStopped = true;
    }

    // ============ NUOVO: ANIMAZIONE MORTE ============
    if (animatorController != null)
    {
        animatorController.SetDead(true);
    }
    // ==================================================

    // Calculate rewards...
    // ... resto del codice Die()
}
```

---

## ✅ TEST RAPIDO

### In Play Mode:

1. **Spawna un nemico**
2. **Seleziona** lo scheletro spawned nella Hierarchy
3. **Inspector** → `EnemyAnimatorController` component
4. **Guarda i parametri** in tempo reale:
   - Speed dovrebbe cambiare (es. 3.5 quando cammina)
   - IsMoving dovrebbe essere `true` quando si muove
5. **Verifica animazione**: Lo scheletro dovrebbe camminare, non più T-Pose!

### Test Debug Button (In Editor):

**Seleziona** il prefab W_Skeleton_Minion:
- Click **"Test Walk Animation"** → Dovrebbe camminare
- Click **"Test Idle Animation"** → Dovrebbe tornare in Idle
- Click **"Test Death Animation"** → Dovrebbe morire
- Click **"Test Hit Reaction"** → Dovrebbe reagire al colpo

---

## 🎯 COMPORTAMENTO ATTESO

✅ **Idle**: Nemico fermo → Speed = 0, IsMoving = false → Animazione Idle
✅ **Walk**: Nemico si muove → Speed = 3.5, IsMoving = true → Animazione Walk
✅ **Death**: HP = 0 → IsDead = true → Animazione Death
✅ **Hit**: Riceve danno → OnHit trigger → Animazione Hit Reaction

---

## 🐛 TROUBLESHOOTING

### Problema: Nemico ancora in T-Pose

**Verifica**:
1. `EnemyAnimatorController` è sul prefab? ✅
2. Animator component esiste sul prefab? ✅
3. AnimatorController è assegnato? → Deve essere `KayKit_Enemy_Controller`
4. Console mostra log di inizializzazione? → `[EnemyAnimatorController] initialized. Params: Speed=true, IsMoving=true...`

**Se Speed=false o IsMoving=false nel log**:
- Il controller NON ha quei parametri!
- Verifica che il controller sia `KayKit_Enemy_Controller` (non un altro controller)

---

### Problema: Animazione Idle funziona, Walk NO

**Debug in Play Mode**:
1. Seleziona nemico spawned
2. Inspector → Animator component → Parameters
3. Guarda il valore di **Speed** mentre il nemico si muove
4. Se Speed = 0 anche quando si muove → `EnemyController.Update()` non sta chiamando `SetSpeed()`

**Fix**: Verifica che il codice `animatorController.SetSpeed(currentSpeed)` sia in Update() come mostrato sopra.

---

### Problema: Parametro non trovato

**Error Console**: `Parameter 'Speed' does not exist`

**Causa**: Il controller assegnato NON è `KayKit_Enemy_Controller`.

**Fix**:
1. Apri Animator window
2. Seleziona `W_Skeleton_Minion` prefab
3. Guarda quale controller è assegnato
4. Deve essere: `KayKit_Enemy_Controller` (in `Assets/_Gameplay/Workers/Animations/Generated/`)

---

## 📋 CHECKLIST COMPLETA

Prima di testare, verifica:

- [ ] ✅ `EnemyAnimatorController.cs` esiste in `Assets/_Gameplay/Enemies/`
- [ ] ✅ `W_Skeleton_Minion.prefab` ha il componente `EnemyAnimatorController`
- [ ] ✅ Animator è assegnato in EnemyAnimatorController (Inspector)
- [ ] ✅ AnimatorController è `KayKit_Enemy_Controller`
- [ ] ✅ `EnemyController.cs` modificato con:
  - [ ] Campo `animatorController`
  - [ ] Cache in `Awake()`
  - [ ] Update animator in `Update()`
  - [ ] SetDead in `Die()`
- [ ] ✅ Compilazione Unity senza errori
- [ ] ✅ Test in Play Mode: animazione walk funziona

---

## 🚀 RISULTATO FINALE

Dopo aver completato tutti gli step:

✅ Nemico spawna in **Idle**
✅ Nemico **cammina** con animazione smooth quando si muove
✅ Velocità animazione matcha velocità NavMeshAgent
✅ Nemico torna in **Idle** quando si ferma
✅ Nemico esegue **Death** animation quando muore
✅ **Nessun errore** in Console

---

## 📄 FILE MODIFICATI

- ✅ **CREATO**: `EnemyAnimatorController.cs`
- ✅ **MODIFICATO**: `EnemyController.cs` (3 modifiche mostrate sopra)
- ✅ **MODIFICATO**: `W_Skeleton_Minion.prefab` (aggiunto componente)

**Tempo stimato**: 5-10 minuti

---

**Fatto!** 🎉 Gli scheletri ora camminano correttamente!
