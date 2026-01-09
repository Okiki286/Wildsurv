# 🔍 DIAGNOSI: Animation Event Non Infligge Danno

## ✅ ANALISI COMPLETATA

### 📊 TASK 1: ANALYZE - Risultati

---

## 1️⃣ LOGICA ATTACCO ESISTENTE ✅

### Come Gestiva l'Attacco Prima?

**VECCHIO SISTEMA** (prima dell'implementazione Animation Events):

```csharp
// File: EnemyInstance.cs (VECCHIO - ora sostituito)
private void TryAttack()
{
    if (attackCooldown > 0f) return;
    if (currentTarget == null || !currentTarget.IsAlive) return;

    // ❌ Danno inflitto IMMEDIATAMENTE
    currentTarget.TakeDamage(effectiveDamage, damageType);

    attackCooldown = interval;
}
```

**Chiamato da**: `Update()` linea 277 (ora chiama `StartAttack()`)

---

### NUOVO SISTEMA (Attuale - Animation Events)

**Assets\_Gameplay\Enemies\EnemyInstance.cs**

#### FASE 1: StartAttack() - Linee 779-798

```csharp
private void StartAttack()
{
    if (attackCooldown > 0f) return;
    if (currentTarget == null || !currentTarget.IsAlive) return;  // ⚠️ CHECK 1
    if (isAttacking) return;  // Previene attacco doppio

    isAttacking = true;
    hasDealtDamage = false;

    // Trigger animazione
    animatorController.SetAttacking(true);
}
```

**Chiamato da**: `Update()` linea 277 quando nemico è in melee range

---

#### FASE 2: OnAttackHit() - Linee 805-842 ⚠️ PUNTO CRITICO

```csharp
public void OnAttackHit()  // ✅ PUBLIC (corretto per Animation Event)
{
    // Guard: infliggi danno solo 1 volta
    if (hasDealtDamage) return;  // ⚠️ CHECK 2
    if (currentTarget == null || !currentTarget.IsAlive) return;  // ⚠️ CHECK 3

    hasDealtDamage = true;

    // Valida range
    if (ValidateAttackHit())  // ⚠️ CHECK 4
    {
        float effectiveDamage = damage * attackMultiplier;
        DamageType damageType = DamageType.Physical;

        // ✅ Dovrebbe infliggere danno QUI
        currentTarget.TakeDamage(effectiveDamage, damageType);

        // Audio
        AudioManager.Instance?.PlaySwordHit();

        // Debug log
        if (debugCombat)
        {
            Debug.Log($"<color=red>[Combat]</color> {name} HIT {currentTargetTransform?.name} for {effectiveDamage:F1} damage");
        }
    }
    else
    {
        // MISS
        if (debugCombat)
        {
            Debug.LogWarning($"<color=orange>[Combat]</color> {name} attack MISSED");
        }
    }
}
```

**Chiamato da**: Animation Event sulla clip di attacco (configurato dall'utente)

---

#### FASE 3: ValidateAttackHit() - Linee 848-857

```csharp
private bool ValidateAttackHit()
{
    if (currentTargetTransform == null) return false;  // ⚠️ CHECK 5

    float distance = Vector3.Distance(transform.position, currentTargetTransform.position);
    float effectiveAttackRange = enemyData != null ? enemyData.AttackRange : 1.0f;
    float maxRange = effectiveAttackRange + 0.5f;

    return distance <= maxRange;  // ⚠️ CHECK 6
}
```

---

## 2️⃣ SISTEMA TARGET (IDamageable) ✅

### Interface Definition

**File**: `Assets\_Gameplay\Combat\IDamageable.cs`

```csharp
public interface IDamageable
{
    void TakeDamage(float damage, DamageType damageType = DamageType.None);
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsAlive { get; }
}
```

**Implementato da**:
- ✅ `EnemyInstance` (nemici)
- ✅ `StructureController` (edifici)
- ✅ `WorkerDamageable` (workers - BRIDGE component) ⚠️
- ✅ `WaystoneBeaconController` (waystone)

---

### Come Viene Salvato il Target

**Assets\_Gameplay\Enemies\EnemyInstance.cs:48-51**

```csharp
[ShowInInspector, ReadOnly]
private IDamageable currentTarget;  // ✅ Interfaccia (corretto)

[ShowInInspector, ReadOnly]
private Transform currentTargetTransform;  // ✅ Transform separato
```

**Come viene acquisito** (linea 567):

```csharp
// In ScanForTarget() - Cerca workers/structures in aggro range
IDamageable damageable = col.GetComponent<IDamageable>();

if (damageable != null && damageable.IsAlive)
{
    // ... calcolo priorità ...
    SetTarget(bestTarget, bestTransform);  // Salva in currentTarget
}
```

**SetTarget()** (linea 666):

```csharp
private void SetTarget(IDamageable target, Transform targetTransform)
{
    if (currentTarget == target) return;  // Evita spam

    currentTarget = target;  // ✅ Salvato
    currentTargetTransform = targetTransform;  // ✅ Salvato

    agent.SetDestination(targetTransform.position);
}
```

---

### WorkerDamageable Implementation

**File**: `Assets\_Gameplay\Workers\WorkerDamageable.cs`

**IMPORTANTE**: Workers usano un BRIDGE component!

```csharp
[RequireComponent(typeof(WorkerController))]
public class WorkerDamageable : MonoBehaviour, IDamageable
{
    private WorkerInstance linkedInstance;  // ⚠️ DEVE essere linkato!

    public bool IsAlive => linkedInstance?.IsAlive ?? false;

    public void TakeDamage(float damage, DamageType damageType)
    {
        if (linkedInstance == null)  // ⚠️ CHECK CRITICO
        {
            Debug.LogWarning($"[WorkerDamageable] {gameObject.name} has no linked instance!");
            return;
        }

        // Delega a WorkerInstance
        linkedInstance.TakeDamage(damage, damageType);

        #if UNITY_EDITOR
        Debug.Log($"<color=red>[WorkerDamageable]</color> {linkedInstance.CustomName} took {damage:F1} {damageType} damage");
        #endif
    }
}
```

**Questo log DOVREBBE apparire** se il danno arriva! Se non lo vedi, `TakeDamage()` non viene chiamato.

---

## 3️⃣ DIAGNOSI: Perché il Danno Non Arriva? ⚠️

### 🔴 6 POSSIBILI CAUSE

#### ❌ CAUSA 1: Animation Event Non Configurato Correttamente

**Sintomo**: `OnAttackHit()` non viene MAI chiamato.

**Verifica**:
- Animation Event esiste sulla clip?
- Function name è ESATTAMENTE `OnAttackHit` (case-sensitive)?
- Animation Event è sul GameObject CORRETTO (quello con EnemyInstance)?

**Test**: Aggiungi questo all'inizio di `OnAttackHit()`:

```csharp
public void OnAttackHit()
{
    Debug.Log($"<color=magenta>[DEBUG]</color> OnAttackHit() CALLED on {name}");  // TEST

    if (hasDealtDamage) return;
    // ... resto del codice
}
```

**Se NON vedi questo log**: Animation Event è configurato male o non viene triggerato.

---

#### ❌ CAUSA 2: `hasDealtDamage` è Già True

**Sintomo**: `OnAttackHit()` viene chiamato ma esce subito al primo check.

**Causa possibile**:
- `hasDealtDamage` non viene resettato correttamente
- Animation Event viene chiamato multiple volte nello stesso attacco

**Test**: Aggiungi log:

```csharp
public void OnAttackHit()
{
    Debug.Log($"<color=magenta>[DEBUG]</color> OnAttackHit() called. hasDealtDamage={hasDealtDamage}");

    if (hasDealtDamage)
    {
        Debug.LogWarning($"<color=orange>[DEBUG]</color> Already dealt damage! Skipping.");
        return;
    }
    // ... resto
}
```

**Fix**: Verifica che `StartAttack()` resetti `hasDealtDamage = false` (linea 786 - già presente ✅)

---

#### ❌ CAUSA 3: `currentTarget` è NULL

**Sintomo**: `OnAttackHit()` viene chiamato ma `currentTarget` è null.

**Causa possibile**:
- Target è morto tra `StartAttack()` e `OnAttackHit()`
- Target non è stato acquisito correttamente da `ScanForTarget()`
- WorkerDamageable non ha `linkedInstance` collegato

**Test**: Aggiungi log:

```csharp
public void OnAttackHit()
{
    Debug.Log($"<color=magenta>[DEBUG]</color> OnAttackHit() called. currentTarget={currentTarget != null}, IsAlive={currentTarget?.IsAlive}");

    if (hasDealtDamage) return;
    if (currentTarget == null || !currentTarget.IsAlive)
    {
        Debug.LogWarning($"<color=orange>[DEBUG]</color> No valid target! currentTarget={currentTarget}, IsAlive={currentTarget?.IsAlive}");
        return;
    }
    // ... resto
}
```

**Fix possibile**:
- Verifica che `debugMode = true` in `ScanForTarget()` mostri log di acquisizione target
- Verifica che WorkerDamageable sia sul prefab worker con `linkedInstance` collegato

---

#### ❌ CAUSA 4: `ValidateAttackHit()` Fallisce (MISS)

**Sintomo**: `OnAttackHit()` viene chiamato ma log mostra "attack MISSED".

**Causa**:
- `currentTargetTransform` è null
- Distance check fallisce (target fuori range)

**Test**: Già presente debug log (linea 839), verifica Console per:

```
[Combat] W_Skeleton_Minion attack MISSED (target out of range)
```

**Fix**:
- Aumenta `maxRange` in `ValidateAttackHit()` da 0.5f a 1.0f (linea 854)
- Verifica che `currentTargetTransform` non sia null

---

#### ❌ CAUSA 5: `damage` o `attackMultiplier` è 0

**Sintomo**: `TakeDamage()` viene chiamato ma con danno 0.

**Causa**:
- `damage` non è stato inizializzato in `Initialize()`
- `attackMultiplier` è 0 (debuff estremo?)

**Test**: Aggiungi log:

```csharp
if (ValidateAttackHit())
{
    float effectiveDamage = damage * attackMultiplier;
    Debug.Log($"<color=magenta>[DEBUG]</color> Damage calc: {damage} × {attackMultiplier} = {effectiveDamage}");

    currentTarget.TakeDamage(effectiveDamage, damageType);
    // ...
}
```

**Fix**: Verifica che `EnemyData` ScriptableObject abbia `baseDamage > 0`

---

#### ❌ CAUSA 6: `debugCombat` è Disabilitato

**Sintomo**: Tutto funziona ma NON vedi log.

**Causa**: `debugCombat = false` (linea 829 check)

**Fix**: Abilita `debugCombat = true` in Inspector su EnemyInstance component.

---

## 📋 PROCEDURA DEBUG STEP-BY-STEP

### ✅ Step 1: Verifica Animation Event

1. **Project** → Trova clip attacco (es. `Skeleton_Attack.anim`)
2. **Seleziona clip** → Inspector
3. **Animation** window → Verifica eventi
4. **Cerca**: Event con Function = `OnAttackHit`
5. **Verifica**: Frame è quello corretto (momento impatto)

**Se evento NON esiste**: Aggiungilo come da guida `COMBAT_ANIMATION_EVENTS.md`

---

### ✅ Step 2: Abilita Debug Massivo

**Apri EnemyInstance.cs** e modifica `OnAttackHit()`:

```csharp
public void OnAttackHit()
{
    // === DEBUG MASSIVO ===
    Debug.Log($"<color=magenta>═══ OnAttackHit() CALLED ═══</color>");
    Debug.Log($"  hasDealtDamage: {hasDealtDamage}");
    Debug.Log($"  currentTarget: {currentTarget != null}");
    Debug.Log($"  IsAlive: {currentTarget?.IsAlive}");
    Debug.Log($"  currentTargetTransform: {currentTargetTransform != null}");

    if (hasDealtDamage)
    {
        Debug.LogWarning($"<color=orange>[SKIP]</color> Already dealt damage!");
        return;
    }

    if (currentTarget == null || !currentTarget.IsAlive)
    {
        Debug.LogError($"<color=red>[SKIP]</color> No valid target! currentTarget={currentTarget}, IsAlive={currentTarget?.IsAlive}");
        return;
    }

    hasDealtDamage = true;

    Debug.Log($"  Calling ValidateAttackHit()...");
    bool validHit = ValidateAttackHit();
    Debug.Log($"  ValidateAttackHit() returned: {validHit}");

    if (validHit)
    {
        float effectiveDamage = damage * attackMultiplier;
        Debug.Log($"  Damage: {damage} × {attackMultiplier} = {effectiveDamage}");
        Debug.Log($"  Calling currentTarget.TakeDamage({effectiveDamage}, {DamageType.Physical})");

        currentTarget.TakeDamage(effectiveDamage, DamageType.Physical);

        Debug.Log($"<color=green>✅ DAMAGE DEALT!</color>");

        AudioManager.Instance?.PlaySwordHit();
        CombatTelemetry.Instance?.RecordEnemyDamage(effectiveDamage);
    }
    else
    {
        Debug.LogWarning($"<color=orange>[MISS]</color> ValidateAttackHit() failed!");
    }
}
```

---

### ✅ Step 3: Abilita debugCombat

1. **Play Mode**
2. **Hierarchy** → Seleziona nemico spawned
3. **Inspector** → EnemyInstance component
4. **Trova** `debugCombat` checkbox
5. **Abilita** ✅

---

### ✅ Step 4: Test e Analizza Log

**Play Mode** → Spawna nemico vicino a worker

**Log attesi**:

```
[Combat] W_Skeleton_Minion START attack on Worker_0
═══ OnAttackHit() CALLED ═══
  hasDealtDamage: False
  currentTarget: True
  IsAlive: True
  currentTargetTransform: True
  Calling ValidateAttackHit()...
  ValidateAttackHit() returned: True
  Damage: 15 × 1 = 15
  Calling currentTarget.TakeDamage(15, Physical)
✅ DAMAGE DEALT!
[WorkerDamageable] Worker_Builder took 15.0 Physical damage (85/100 HP)  ← DA WorkerDamageable
[Combat] W_Skeleton_Minion END attack (cooldown=1.5s)
```

---

## 🔍 INTERPRETAZIONE RISULTATI

### Scenario A: Nessun log "OnAttackHit() CALLED"

**Causa**: Animation Event NON sta triggerando il metodo.

**Fix**:
1. Verifica che Animation Event esista sulla clip
2. Verifica function name = `OnAttackHit` (case-sensitive)
3. Verifica che EnemyInstance sia sul GameObject root (stesso GO dell'Animator)

---

### Scenario B: Log "No valid target!"

**Causa**: `currentTarget` è null o morto.

**Sotto-cause possibili**:

**B1**: `ScanForTarget()` non trova workers

**Debug**: Abilita `debugMode = true` e verifica log:
```
[Enemy] W_Skeleton_Minion target: Worker_0
```

Se NON c'è questo log, `ScanForTarget()` non funziona.

**Fix**:
- Verifica che worker abbia `WorkerDamageable` component
- Verifica che worker sia su layer corretto
- Verifica `AggroRange` in EnemyData > 0

---

**B2**: WorkerDamageable ha `linkedInstance = null`

**Debug**: Cerca log:
```
[WorkerDamageable] Worker_Builder has no linked instance!
```

**Fix**: Verifica che `WorkerController.LinkToInstance()` sia chiamato quando worker spawna.

---

### Scenario C: Log "ValidateAttackHit() returned: False"

**Causa**: Distance check fallisce.

**Debug**: Aggiungi log in `ValidateAttackHit()`:

```csharp
private bool ValidateAttackHit()
{
    if (currentTargetTransform == null)
    {
        Debug.LogError($"[ValidateAttackHit] currentTargetTransform is NULL!");
        return false;
    }

    float distance = Vector3.Distance(transform.position, currentTargetTransform.position);
    float effectiveAttackRange = enemyData != null ? enemyData.AttackRange : 1.0f;
    float maxRange = effectiveAttackRange + 0.5f;

    Debug.Log($"[ValidateAttackHit] distance={distance:F2}, maxRange={maxRange:F2}, hit={distance <= maxRange}");

    return distance <= maxRange;
}
```

**Fix**: Se distance > maxRange, aumenta tolleranza da 0.5f a 1.0f o 1.5f.

---

### Scenario D: Tutto OK ma danno = 0

**Causa**: `damage` è 0.

**Debug**: Verifica log mostra `Damage: 0 × 1 = 0`

**Fix**:
1. Verifica che `EnemyData` ScriptableObject abbia `baseDamage > 0`
2. Verifica che `Initialize()` sia stato chiamato (`isInitialized = true`)

---

## 🎯 NEXT STEPS IMMEDIATI

1. **Aggiungi debug massivo** a `OnAttackHit()` come mostrato sopra
2. **Abilita `debugCombat = true`** in Inspector
3. **Play Mode** → Test con nemico vs worker
4. **Posta i log completi** della Console

I log ti diranno ESATTAMENTE quale dei 6 check sta fallendo!

---

## 📊 CHECKLIST RAPIDA

- [ ] Animation Event esiste sulla clip con function = `OnAttackHit`
- [ ] EnemyInstance component è sul GameObject root (stesso GO dell'Animator)
- [ ] `debugCombat = true` in Inspector
- [ ] Debug massivo aggiunto a `OnAttackHit()`
- [ ] Worker ha `WorkerDamageable` component
- [ ] WorkerDamageable ha `linkedInstance` collegato
- [ ] EnemyData ha `baseDamage > 0` e `AttackRange > 0`
- [ ] Console mostra log "OnAttackHit() CALLED"
- [ ] Console mostra log "✅ DAMAGE DEALT!" o error specifico

---

**Prossimo step**: Aggiungi i debug log e posta l'output completo della Console!
