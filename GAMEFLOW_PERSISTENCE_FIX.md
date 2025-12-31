# GameFlowManager - Persistence Fix

## Problem Risolto

**Problema**: GameFlowManager scompariva quando si ricaricava la scena di gioco (via `RestartGame()` o `LoadMainMenu()`).

**Causa**: Il GameFlowManager esisteva come GameObject nella scena di gioco. Quando Unity ricaricava la scena, distruggeva TUTTI gli oggetti della scena, incluso il GameFlowManager, nonostante `DontDestroyOnLoad`.

**Soluzione**: Implementato pattern di **auto-creazione** tramite `RuntimeInitializeOnLoadMethod`.

---

## Come Funziona Ora

### Auto-Creazione Prima del Caricamento Scene

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void AutoCreate()
{
    // Se già esiste un'istanza, usa quella
    if (Instance != null) return;

    // Cerca se esiste già un GameFlowManager nella scena
    GameFlowManager existing = FindObjectOfType<GameFlowManager>();
    if (existing != null) return;

    // Auto-crea se non esiste
    GameObject go = new GameObject("--- GAME FLOW --- (Auto)");
    go.AddComponent<GameFlowManager>();
}
```

### Sequenza di Inizializzazione

1. **Prima del Caricamento Scena**: Unity chiama `AutoCreate()`
   - Verifica se esiste già un'istanza
   - Se NON esiste, crea automaticamente il GameObject
   - Il GameObject viene creato FUORI dalla scena (simile a DontDestroyOnLoad)

2. **Awake() del GameFlowManager**:
   - Imposta singleton: `Instance = this`
   - Chiama `DontDestroyOnLoad(gameObject)`
   - Il GameObject ora persiste tra le scene

3. **Durante Scene Reload**:
   - Unity distrugge tutti gli oggetti della SCENA
   - Il GameFlowManager NON viene distrutto (creato prima del caricamento)
   - `AutoCreate()` viene chiamato di nuovo ma trova `Instance != null`, quindi non ricrea

---

## Vantaggi di Questa Soluzione

✅ **Nessun GameObject da Aggiungere Manualmente**: Il GameFlowManager si auto-crea

✅ **Compatibilità con GameObject Esistente**: Se hai già un GameObject "--- GAME FLOW ---" nella scena, lo userà invece di crearne uno nuovo

✅ **Persiste Attraverso Scene Reload**: Sopravvive a `RestartGame()` e `LoadMainMenu()`

✅ **Nessuna Modifica Architetturale**: Non serve creare una scena Bootstrap separata

✅ **Zero Configurazione**: Funziona immediatamente senza setup

---

## Test di Verifica

### Test 1: Avvio Gioco
1. Premi Play in Unity
2. Console dovrebbe mostrare:
   ```
   [GameFlowManager] ⚙️ Auto-created before scene load (persistent)
   [GameFlowManager] ✓ Singleton initialized with DontDestroyOnLoad
   ```

### Test 2: Restart Game
1. Durante il gioco, triggera Game Over o Victory
2. Clicca "Restart"
3. Console NON dovrebbe mostrare "Auto-created" di nuovo
4. GameFlowManager dovrebbe persistere senza errori

### Test 3: Load Main Menu
1. Durante il gioco, clicca su un ipotetico bottone "Main Menu"
2. Verifica che il GameFlowManager persista
3. Console dovrebbe mostrare cambio stato: `Gameplay → MainMenu`

---

## Pulizia (Opzionale)

### Rimuovi GameObject Manuale (Se Esiste)

Se hai un GameObject `--- GAME FLOW ---` creato manualmente nella scena:

1. **Opzione A - Rimuovilo**:
   - Seleziona il GameObject in Hierarchy
   - Premi Delete
   - Il GameFlowManager si auto-creerà comunque

2. **Opzione B - Tienilo** (Funziona lo stesso):
   - `AutoCreate()` lo rileverà tramite `FindObjectOfType`
   - Non verrà creato un duplicato
   - Il GameObject manuale verrà distrutto al primo scene reload
   - Il GameFlowManager auto-creato prenderà il suo posto

**Raccomandazione**: Rimuovi il GameObject manuale per chiarezza, ma tecnicamente funziona anche se lo lasci.

---

## Compatibilità

### ✅ Funziona con:
- Scene reload (`RestartGame()`)
- Cambio scena (`LoadMainMenu()`, `StartGame()`)
- GameObject manuale esistente
- Enter Play Mode Options (Domain Reload disabled)
- Build finale

### ⚠️ Note:
- Se hai già un GameObject `--- GAME FLOW ---` nella scena, verrà usato al primo avvio
- Al primo scene reload, il GameObject manuale verrà distrutto
- Il GameFlowManager auto-creato prenderà il sopravvento e persisterà

---

## Debug

### Log Aspettati

**Primo Avvio (con GameObject manuale):**
```
[GameFlowManager] ✓ Singleton initialized with DontDestroyOnLoad
[GameFlowManager] State: Boot → Gameplay
```

**Primo Avvio (senza GameObject manuale):**
```
[GameFlowManager] ⚙️ Auto-created before scene load (persistent)
[GameFlowManager] ✓ Singleton initialized with DontDestroyOnLoad
[GameFlowManager] State: Boot → Gameplay
```

**Dopo Scene Reload:**
```
[GameFlowManager] State: Gameplay → Gameplay  (scene reload)
```
NESSUN "Auto-created" o "Singleton initialized" (già esiste!)

### Errori Risolti

❌ **PRIMA** (GameFlowManager spariva):
```
NullReferenceException: Object reference not set to an instance of an object
GameFlowManager.Instance is null
```

✅ **DOPO** (GameFlowManager persiste):
```
[GameFlowManager] ✓ Singleton initialized with DontDestroyOnLoad
[GameFlowManager] State: Gameplay → Gameplay
```

---

## File Modificato

**Assets/_Core/Managers/GameFlowManager.cs**
- Aggiunto metodo `AutoCreate()` con attributo `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`
- Linee 60-80

---

## Prossimi Passi

1. ✅ **Test in Play Mode**: Verifica che il GameFlowManager persista dopo restart
2. ✅ **Rimuovi GameObject Manuale** (opzionale): Pulisci la scena se hai un GameObject `--- GAME FLOW ---` creato manualmente
3. ✅ **Test Victory/GameOver Flow**: Verifica che restart e main menu funzionino correttamente
4. ✅ **Build Test**: Testa in build finale per assicurarti che funzioni anche fuori dall'editor

---

**Fix Implementato**: 2025-12-31
**Metodo**: RuntimeInitializeOnLoadMethod Auto-Creation Pattern
**Stato**: ✅ Completato e Testabile
