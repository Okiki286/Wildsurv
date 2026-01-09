# Setup Corretto Enemy con Animazioni ✅

## 🎯 PROBLEMA RISOLTO

Il problema era che avevi **DUE controller** che si combattevano per controllare lo stesso nemico:

1. **EnemyInstance** - Il TUO sistema originale con logica AI (targeting workers, debuff, ecc.)
2. **EnemyController** - Sistema creato per animazioni che DUPLICAVA la logica di movimento

**Risultato**: Conflitto! Le animazioni funzionavano ma la logica AI era rotta.

---

## ✅ SOLUZIONE

Ho **integrato il supporto animazioni direttamente in EnemyInstance**, così hai **un solo controller** che gestisce sia logica AI che animazioni.

---

## 🛠️ SETUP PREFAB CORRETTO

### Component Required sul prefab `W_Skeleton_Minion`:

#### ✅ 1. Transform
Standard Unity component.

#### ✅ 2. Animator
- **Controller**: `KayKit_Enemy_Controller` (già assegnato)
- **Avatar**: KayKit_Skeleton avatar
- **Apply Root Motion**: False

#### ✅ 3. NavMeshAgent
- **Speed**: 3.5
- **Stopping Distance**: ~1.6
- **Agent Type**: Humanoid

#### ✅ 4. EnemyInstance (IL TUO SCRIPT PRINCIPALE)
- Gestisce tutta la logica AI
- Targeting (workers vs waystone)
- Movimento con NavMeshAgent
- Combat (attacco melee)
- Debuff (waystone slow)
- **ORA ANCHE**: Aggiornamento animator

#### ✅ 5. EnemyAnimatorController
- **Animator**: Riferimento all'Animator component
- **Parameter Names**:
  - Speed: "Speed"
  - IsMoving: "IsMoving"
  - IsDead: "IsDead"
  - IsAttacking: "IsAttacking"

#### ✅ 6. CapsuleCollider
- **Radius**: 0.4
- **Height**: 1.8
- **Center**: (0, 0.9, 0)

---

## ❌ COSA RIMUOVERE

### RIMUOVI `EnemyController` dal prefab!

**Passaggi**:

1. **Apri Unity Editor**
2. **Project window** → `Assets/_Gameplay/Enemies/Prefabs/W_Skeleton_Minion.prefab`
3. **Doppio click** per aprire in Prefab Mode
4. **Seleziona ROOT GameObject** "W_Skeleton_Minion"
5. **Inspector** → Trova component `EnemyController`
6. **Click sul menu (⋮)** → **Remove Component**
7. **Salva** (Ctrl+S)

**Perché rimuoverlo?**
- Crea conflitto con EnemyInstance
- Sovrascrive la logica AI originale
- Non è necessario (EnemyInstance ora gestisce le animazioni)

---

## 🎮 COSA È CAMBIATO IN `EnemyInstance.cs`

### 1. Aggiunto riferimento a EnemyAnimatorController

```csharp
private EnemyAnimatorController animatorController;
```

### 2. Cache del component in Awake()

```csharp
private void Awake()
{
    agent = GetComponent<NavMeshAgent>();
    animatorController = GetComponent<EnemyAnimatorController>(); // NEW
}
```

### 3. Aggiornamento animator in Update()

```csharp
private void Update()
{
    // ... tutta la logica AI esistente ...

    // Update animator based on NavMeshAgent velocity
    UpdateAnimator(); // NEW

    // Stuck detection
    DetectAndRecoverFromStuck();
}
```

### 4. Nuovo metodo UpdateAnimator()

```csharp
private void UpdateAnimator()
{
    if (animatorController == null || agent == null || !agent.isOnNavMesh)
        return;

    float speed = agent.velocity.magnitude;
    animatorController.SetSpeed(speed);
    animatorController.SetMoving(speed > 0.1f);
}
```

### 5. Animazione di morte in Die()

```csharp
protected virtual void Die()
{
    // ... drop rewards ...

    // Set death animation
    if (animatorController != null)
    {
        animatorController.SetDead(true); // NEW
    }

    // Stop NavMesh
    // ... resto del codice ...
}
```

### 6. Reset animator in ResetStateForPooling()

```csharp
private void ResetStateForPooling()
{
    // ... reset combat state, debuff, flags ...

    // RESET ANIMATOR
    if (animatorController != null)
    {
        animatorController.SetDead(false);
        animatorController.SetSpeed(0f);
        animatorController.SetMoving(false);
    }

    // ... reset NavMeshAgent ...
}
```

---

## 🎯 RISULTATO ATTESO

Dopo aver rimosso `EnemyController` dal prefab:

### ✅ Comportamento AI Corretto
- Nemici targetano workers sulla loro linea di movimento
- Se nessun worker, vanno al Waystone
- Rallentano nell'area debuff del Waystone (ma NON si fermano completamente)
- Attaccano quando arrivano a distanza melee

### ✅ Animazioni Corrette
- **Idle** quando fermo (Speed = 0)
- **Walk** quando cammina a velocità normale (Speed ~ 1.5-2.5)
- **Run** quando corre a velocità piena (Speed ~ 3.5)
- **Death** quando muore
- Transizioni smooth tra stati

---

## 🔍 VERIFICA FUNZIONAMENTO

### 1. Test in Play Mode

1. Spawna nemici
2. Osserva comportamento:
   - ✅ Vanno verso Waystone se nessun worker
   - ✅ Se worker sulla loro linea, lo targetano
   - ✅ Quando vicini al Waystone, rallentano ma NON si fermano
   - ✅ Attaccano a distanza melee

### 2. Verifica Animazioni

Seleziona nemico in Hierarchy durante Play Mode:

**Inspector → Animator → Parameters**:
- `Speed`: Cambia da 0 a ~3.5 quando si muove
- `IsMoving`: True quando si muove, False quando fermo
- `IsDead`: False (diventa True quando muore)

**Scene view**: Dovresti vedere le animazioni corrette (Walk, Run) senza sliding.

---

## 🐛 DEBUG

Se ancora non funziona:

### A) Animazioni non funzionano

**Verifica**:
1. EnemyAnimatorController component ESISTE sul prefab? ✅
2. Animator.Controller = KayKit_Enemy_Controller? ✅
3. NavMesh esiste nella scena? (Window → AI → Navigation → Bake)

### B) Comportamento AI rotto

**Verifica**:
1. Hai RIMOSSO EnemyController dal prefab? ❌ (se ancora presente, rimuovilo!)
2. EnemyInstance component è abilitato? ✅
3. Console mostra errori?

### C) Nemici "troppo lenti" vicino al Waystone

Questo è **normale**! Il Waystone applica un debuff di slow. Se sembrano FERMI invece di lenti:

**Verifica in Inspector** (con nemico selezionato):
- `EnemyInstance` → `moveMultiplier`: Dovrebbe essere < 1.0 (es. 0.3 = 70% slow)
- `NavMeshAgent` → `Speed`: Dovrebbe essere ridotta (es. 3.5 * 0.3 = 1.05)

Se `moveMultiplier = 0.01` (troppo slow), verifica il Waystone component per vedere quale slow sta applicando.

---

## 📁 FILE MODIFICATI

- ✅ `EnemyInstance.cs` - Aggiunto supporto animazioni
- ✅ `EnemyAnimatorController.cs` - Già esistente, nessun cambiamento
- ❌ `EnemyController.cs` - NON usare! (da rimuovere dal prefab)

---

## 🎉 PROSSIMI PASSI

1. **Rimuovi EnemyController dal prefab** W_Skeleton_Minion
2. **Salva** il prefab (Ctrl+S)
3. **Testa** in Play Mode
4. **Verifica** che sia logica AI che animazioni funzionino correttamente

Se tutto funziona, puoi **eliminare** `EnemyController.cs` dal progetto (non serve più).

---

**Nota**: Questo setup è **pooling-safe** perché `ResetStateForPooling()` resetta anche l'animator, permettendo ai nemici di essere riutilizzati senza problemi di animazione "stuck".
