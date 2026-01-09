# Enemy Combat: Animation Events Integration 🗡️

## 📊 ANALISI SISTEMA ATTUALE

### ✅ Come Funziona Ora (EnemyInstance.cs)

**Linee 768-810: Metodo `TryAttack()`**

```csharp
private void TryAttack()
{
    if (attackCooldown > 0f) return;
    if (currentTarget == null || !currentTarget.IsAlive) return;

    // Calcola danno effettivo
    float effectiveDamage = damage * attackMultiplier;

    // Infliggi danno IMMEDIATAMENTE
    currentTarget.TakeDamage(effectiveDamage, damageType); // ❌ PROBLEMA QUI!

    // Audio
    AudioManager.Instance?.PlaySwordHit();

    // Reset cooldown
    attackCooldown = interval;
}
```

**Quando viene chiamato**:
- **Update() linea 269**: Quando nemico è in melee range, chiama `TryAttack()`
- **Problema**: Il danno viene inflitto **SUBITO** quando `attackCooldown` scade, NON quando l'arma colpisce visivamente!

**Flusso attuale**:
```
1. Update() rileva nemico in melee range
2. TryAttack() viene chiamato
3. ❌ DANNO INFLITTO ISTANTANEAMENTE
4. Cooldown reset
5. (Animazione parte ma danno già fatto)
```

---

## 🎯 SOLUZIONE: Animation Event System

### Architettura Proposta

**Separazione logica in 3 fasi**:

1. **StartAttack()** - Chiamato quando nemico entra in range (trigger animazione)
2. **OnAttackHit()** - Chiamato dall'Animation Event al frame di impatto (infligge danno)
3. **EndAttack()** - Chiamato dall'Animation Event alla fine (reset stato)

---

## 🛠️ IMPLEMENTAZIONE

### STEP 1: Modificare EnemyInstance.cs

#### A) Aggiungi nuove variabili di stato

```csharp
// ============================================
// COMBAT STATE
// ============================================

[TitleGroup("Combat")]
[ShowInInspector, ReadOnly]
private IDamageable currentTarget;

[ShowInInspector, ReadOnly]
private Transform currentTargetTransform;

[ShowInInspector, ReadOnly]
private float attackCooldown;

// NEW: Animation-driven attack state
private bool isAttacking = false;  // True mentre esegue animazione attacco
private bool hasDealtDamage = false;  // Guard per evitare danno multiplo in 1 attacco
```

#### B) Modifica TryAttack() → StartAttack()

```csharp
/// <summary>
/// FASE 1: Inizia attacco (trigger animazione).
/// Chiamato da Update() quando in melee range e cooldown pronto.
/// </summary>
private void StartAttack()
{
    if (attackCooldown > 0f) return;
    if (currentTarget == null || !currentTarget.IsAlive) return;
    if (isAttacking) return;  // NEW: Previene attacco multiplo durante animazione

    isAttacking = true;
    hasDealtDamage = false;

    // Trigger animation
    if (animatorController != null)
    {
        animatorController.SetAttacking(true);
    }

    if (debugCombat)
    {
        Debug.Log($"<color=yellow>[Combat]</color> {name} START attack on {currentTargetTransform?.name}");
    }
}
```

#### C) Aggiungi OnAttackHit() - CHIAMATO DA ANIMATION EVENT

```csharp
/// <summary>
/// FASE 2: Frame di impatto (infligge danno).
/// Chiamato da Animation Event sulla clip di attacco al momento esatto del colpo.
/// </summary>
public void OnAttackHit()
{
    // Guard: infliggi danno solo 1 volta per attacco
    if (hasDealtDamage) return;
    if (currentTarget == null || !currentTarget.IsAlive) return;

    hasDealtDamage = true;

    // RAYCAST per verificare se il target è ancora in range e visibile
    if (ValidateAttackHit())
    {
        // Calcola danno effettivo
        float effectiveDamage = damage * attackMultiplier;
        DamageType damageType = DamageType.Physical;

        // ✅ INFLIGGI DANNO AL FRAME CORRETTO
        currentTarget.TakeDamage(effectiveDamage, damageType);

        // Audio
        AudioManager.Instance?.PlaySwordHit();

        // Telemetry
        CombatTelemetry.Instance?.RecordEnemyDamage(effectiveDamage);

        if (debugCombat)
        {
            Debug.Log($"<color=red>[Combat]</color> {name} HIT {currentTargetTransform?.name} for {effectiveDamage:F1} damage");
        }
    }
    else
    {
        if (debugCombat)
        {
            Debug.LogWarning($"<color=orange>[Combat]</color> {name} attack MISSED (target out of range)");
        }
    }
}
```

#### D) Aggiungi ValidateAttackHit() - Raycast o Distance Check

**Opzione A: SphereCast (CONSIGLIATO per melee)**

```csharp
/// <summary>
/// Valida se l'attacco può colpire il target.
/// Usa SphereCast per rilevare hit anche se target si è mosso leggermente.
/// </summary>
private bool ValidateAttackHit()
{
    if (currentTargetTransform == null) return false;

    Vector3 origin = transform.position + Vector3.up * 1f;  // Altezza torace
    Vector3 toTarget = currentTargetTransform.position - origin;
    float distance = toTarget.magnitude;

    float effectiveAttackRange = enemyData != null ? enemyData.AttackRange : 1.0f;
    float maxRange = effectiveAttackRange + 0.5f;  // Tolleranza per movimento

    // SphereCast verso target
    RaycastHit hit;
    if (Physics.SphereCast(origin, 0.3f, toTarget.normalized, out hit, maxRange, LayerMask.GetMask("Worker", "Structures")))
    {
        // Verifica se ha colpito il target corretto
        if (hit.transform == currentTargetTransform || hit.transform.IsChildOf(currentTargetTransform))
        {
            return true;
        }
    }

    // Fallback: Distance check semplice se SphereCast non ha colpito
    return distance <= maxRange;
}
```

**Opzione B: Distance Check Semplice (PIÙ PERFORMANTE)**

```csharp
/// <summary>
/// Valida se l'attacco può colpire il target.
/// Usa semplice distance check (più performante, meno preciso).
/// </summary>
private bool ValidateAttackHit()
{
    if (currentTargetTransform == null) return false;

    float distance = Vector3.Distance(transform.position, currentTargetTransform.position);
    float effectiveAttackRange = enemyData != null ? enemyData.AttackRange : 1.0f;
    float maxRange = effectiveAttackRange + 0.5f;  // Tolleranza

    return distance <= maxRange;
}
```

#### E) Aggiungi EndAttack() - CHIAMATO DA ANIMATION EVENT

```csharp
/// <summary>
/// FASE 3: Fine attacco (reset stato).
/// Chiamato da Animation Event alla fine della clip di attacco.
/// </summary>
public void OnAttackEnd()
{
    isAttacking = false;

    // Reset animator
    if (animatorController != null)
    {
        animatorController.SetAttacking(false);
    }

    // Imposta cooldown SOLO alla fine dell'animazione
    float interval = enemyData != null ? enemyData.AttackInterval : 1.5f;
    attackCooldown = interval;

    if (debugCombat)
    {
        Debug.Log($"<color=green>[Combat]</color> {name} END attack (cooldown={attackCooldown:F1}s)");
    }

    // Se target è morto, rescan
    if (currentTarget != null && !currentTarget.IsAlive)
    {
        currentTarget = null;
        currentTargetTransform = null;
        targetScanTimer = 0f;
    }
}
```

#### F) Aggiorna Update() per chiamare StartAttack()

```csharp
// In Update(), linea ~269, sostituisci:
// TryAttack();  // OLD

// Con:
StartAttack();  // NEW
```

---

### STEP 2: Configurare Animation Events

#### A) Identificare la clip di attacco

1. **Project window** → `Assets/KayKit/AnimationsDungeonRemastered/`
2. Trova la clip di attacco dello scheletro (es. `Skeleton_Attack.anim` o simile)

**Nome probabile clip**:
- `Attack` (generico)
- `1H_Melee_Attack_Chop` (se usa spada 1H)
- `Sword_Slash` (se specifico)

#### B) Aggiungere Animation Events

1. **Seleziona clip** in Project window
2. **Animation window** (Window → Animation → Animation)
3. **Timeline** → Trova il frame dove l'arma COLPISCE il target
4. **Click destro sulla timeline** → Add Animation Event
5. **Inspector** → Function: `OnAttackHit`

**Frame di impatto tipici**:
- Attack chop: Frame ~15-20 (su 30-60 fps)
- Cerca il frame dove la spada è al punto più basso/avanti

6. **Aggiungi secondo event** alla FINE dell'animazione (ultimo frame)
7. **Function**: `OnAttackEnd`

#### C) Verifica nel Controller

**KayKit_Enemy_Controller.controller**:

1. Apri controller in Animator window
2. Trova lo stato "Attack"
3. Verifica transizione:
   - **Entry condition**: `IsAttacking == true`
   - **Exit condition**: `IsAttacking == false` (o exit time)

**Se lo stato Attack NON esiste**:
- Crea nuovo stato "Attack"
- Assegna motion: Clip di attacco
- Transition da "Idle" quando `IsAttacking == true`
- Transition a "Idle" quando `IsAttacking == false`

---

## 🎮 TESTING

### Verifica funzionamento

1. **Abilita debugCombat** in EnemyInstance (Inspector)
2. **Play Mode** → Spawna nemico
3. **Console log atteso**:

```
[Combat] W_Skeleton_Minion START attack on Worker_0
[Combat] W_Skeleton_Minion HIT Worker_0 for 15.0 damage
[Combat] W_Skeleton_Minion END attack (cooldown=1.5s)
```

4. **Verifica timing**:
   - "START attack" → Animazione parte
   - "HIT" → Appare al momento dell'impatto visivo (non prima!)
   - "END attack" → Animazione finisce, cooldown inizia

---

## 🔥 RAYCAST vs DISTANCE: Quale Usare?

### ✅ Distance Check (CONSIGLIATO per il tuo caso)

**PRO**:
- ⚡ Molto performante (1 operazione Vector3.Distance)
- ✅ Sufficiente per melee combat in giochi simil-RTS
- ✅ Funziona bene con target che non si muovono molto (workers statici)

**CONTRO**:
- ❌ Non rileva ostacoli tra nemico e target
- ❌ Target può schivare spostandosi rapidamente (ma workers non schivano)

**Usa se**:
- Nemico combatte contro workers/structures che NON si muovono
- Vuoi massima performance (mobile)
- Non hai bisogno di precisione pixel-perfect

---

### ⚔️ SphereCast (Per combat più dinamico)

**PRO**:
- ✅ Rileva ostacoli (pareti, altri nemici)
- ✅ Più preciso per armi con range esteso (lance, spade lunghe)
- ✅ Funziona meglio se target può schivare/muoversi

**CONTRO**:
- ❌ Più costoso (Physics.SphereCast ogni hit)
- ❌ Richiede configurazione corretta dei layer

**Usa se**:
- Hai bisogno di rilevare collisioni con ambiente
- Target possono muoversi velocemente e schivare
- Vuoi combat "fair" (no colpi attraverso muri)

---

### 🎯 Raccomandazione per il Tuo Gioco

**USA DISTANCE CHECK** perché:

1. **Workers sono statici** (non schivano attivamente)
2. **Performance** su mobile è critica (SphereCast costa)
3. **Semplicità** - meno bug, meno configurazione layer
4. **Già usi distance check** per melee range detection (consistente)

**Aggiungi tolleranza** (`maxRange = attackRange + 0.5f`) per evitare miss se target si muove leggermente.

---

## 📋 CHECKLIST FINALE

### Codice

- [ ] Aggiungi `isAttacking` e `hasDealtDamage` a EnemyInstance
- [ ] Rinomina `TryAttack()` → `StartAttack()`
- [ ] Crea metodo `OnAttackHit()` (public, chiamato da animation event)
- [ ] Crea metodo `ValidateAttackHit()` (distance check)
- [ ] Crea metodo `OnAttackEnd()` (public, chiamato da animation event)
- [ ] Aggiorna `Update()` per chiamare `StartAttack()` invece di `TryAttack()`
- [ ] Aggiungi chiamata `animatorController.SetAttacking(true/false)`

### Animation

- [ ] Trova clip di attacco dello scheletro (KayKit animations)
- [ ] Apri clip in Animation window
- [ ] Aggiungi Animation Event al frame di impatto → Function: `OnAttackHit`
- [ ] Aggiungi Animation Event all'ultimo frame → Function: `OnAttackEnd`

### Animator Controller

- [ ] Verifica parametro `IsAttacking` esiste (Bool)
- [ ] Verifica stato "Attack" esiste nel controller
- [ ] Verifica transizioni: Idle → Attack (when IsAttacking=true), Attack → Idle (when IsAttacking=false)

### Testing

- [ ] Abilita `debugCombat = true` in EnemyInstance
- [ ] Play Mode → Spawna nemico
- [ ] Verifica log: "START attack" → "HIT" (al momento visivo) → "END attack"
- [ ] Verifica che danno viene inflitto SOLO al frame di impatto, non prima

---

## 🐛 TROUBLESHOOTING

### "Animation Event has no receiver!"

**Causa**: Il metodo `OnAttackHit()` non è public o non esiste su GameObject con Animator.

**Fix**:
- Assicurati che `OnAttackHit()` e `OnAttackEnd()` siano `public`
- Verifica che EnemyInstance sia sullo STESSO GameObject dell'Animator component

---

### Danno inflitto più volte

**Causa**: Animation Event chiamato multiple volte, guard `hasDealtDamage` non funziona.

**Fix**:
- Aggiungi log in `OnAttackHit()` per vedere quante volte viene chiamato
- Verifica che `hasDealtDamage = false` sia resettato in `StartAttack()`
- Verifica che ci sia SOLO 1 Animation Event "OnAttackHit" nella clip

---

### Animazione non parte

**Causa**: Parametro `IsAttacking` non triggera transizione, o stato Attack non esiste.

**Fix**:
- Apri Animator window con controller
- Verifica transizione da Locomotion → Attack
- Condition: `IsAttacking == true`
- In Play Mode, Inspector → Animator → Parameters → Verifica che `IsAttacking` diventa true

---

### Attack MISS anche se nemico è vicino

**Causa**: `ValidateAttackHit()` troppo restrittivo, range troppo piccolo.

**Fix**:
- Aumenta tolleranza: `maxRange = attackRange + 1.0f` (invece di 0.5f)
- Aggiungi debug log per vedere distance vs maxRange
- Verifica che `currentTargetTransform` non sia null

---

## 📊 PERFORMANCE CONSIDERATIONS

**Distance Check**: ~0.01ms per enemy (trascurabile)
**SphereCast**: ~0.1-0.5ms per enemy (può sommarsi con 50+ nemici)

**Per 100 nemici che attaccano simultaneamente**:
- Distance: ~1ms
- SphereCast: ~10-50ms ❌ (può causare lag)

**Conclusione**: Distance check è MOLTO più performante per il tuo use case.

---

## 🎯 NEXT STEPS

1. **Implementa Distance Check version** (più semplice)
2. **Testa** con debug mode
3. **Se funziona bene**, lascia così
4. **Se hai problemi** (hit through walls), passa a SphereCast

---

**IMPORTANTE**: Ricorda di chiamare `StartAttack()` in Update() invece di `TryAttack()`!
