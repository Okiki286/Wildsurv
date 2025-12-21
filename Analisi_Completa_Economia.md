# Analisi Completa Economia (Gains, Spend, UI)
**Progetto:** WildernessSurvival  
**Ruolo:** Senior Unity Engineer

---

## 1. Mappa dei Punti Critici (Tabella Hack/Modify)

| File | Metodo | Risorsa | +/- | Importo | Call Chain | World Pos |
| :--- | :--- | :--- | :---: | :--- | :--- | :---: |
| `EnemyInstance.cs` | `DropRewards` | `shard` | + | `shardDrop` (var) | `Die` | SÌ |
| `StructureController.cs` | `ProduceResources` | `warmwood`/`food` | + | `amountToAdd` (var) | `Update` -> `UpdateProduction` | SÌ |
| `BuildModeController.cs` | `TryPlaceStructure` | Variabile | - | `BuildCosts` (array) | Mouse Click -> `TryPlace` | SÌ |
| `StructureController.cs` | `TryUpgrade` | N/A | - | **GRATIS** | Debug Button / UI | SÌ |
| `StructureController.cs` | `Repair` | N/A | - | **GRATIS** | Debug Button / Logic | SÌ |
| `ResourceSystem.cs` | `ApplyStartingResources`| Variabile | + | Configurato | `Start` | NO |

---

## 2. Call Chain Principali

### A) Enemy Drop (Guadagno Combat)
`EnemyInstance.TakeDamage` → `Die()` → `DropRewards()` → **`ResourceSystem.AddResource("shard", amount)`**

### B) Produzione (Guadagno Passivo)
`Update()` → `UpdateProduction()` → `ProduceResources()` → **`ResourceSystem.AddResource(producesId, amount)`**

### C) Build Cost (Spesa Costruzione)
`Update()` → `TryPlaceStructure()` → `ResourceSystem.CanAfford()` → **`ResourceSystem.PayCost()`** → `StructureSystem.SpawnStructure()`

---

## 3. Identificazione Sistemi Core

*   ** ResourceManager / Inventory:** Il **`ResourceSystem.cs`** è il "Single Source of Truth". È usato quasi ovunque (`AddResource`, `PayCost`), ma alcuni script (come `GameHUD.cs`) accedono direttamente a stringhe hardcoded.
*   ** Aggiornamento HUD:** 
    *   `ResourceDisplayUI.cs`: Gestore moderno con polling (0.25s), animazioni di pulsazione e floating text.
    *   `GameHUD.cs`: Gestore legacy con polling per il counter base.
    *   `BuildMenuUI.cs`: Aggiorna i pulsanti di costruzione in base alla disponibilità.

---

## 4. Raccomandazioni e Conclusioni

### A) Hook Point da migrare (Top 5)
1.  **`ResourceSystem.AddResource`**: Centralizzare qui l'emissione del suono/particella "generic reward".
2.  **`ResourceSystem.PayCost`**: Centralizzare qui il feedback "negative change" (suono errore/spesa).
3.  **`EnemyInstance.DropRewards`**: Inserire evento per Shard VFX (orb che volano verso la Waystone).
4.  **`ResourceDisplayUI.ShowChangeAnimation`**: Punto migliore per il feedback estetico "finale" del counter.
5.  **`StructureController.TryUpgrade`**: BUG/GAP identificato; attualmente i livelli successivi non costano risorse. Da implementare agganciandosi a `ResourceSystem.PayCost`.

### B) EconomySystem
Propongo il nome **`EconomyManager`** o l'espansione dell'attuale `ResourceSystem` in un sistema a eventi non-polling (`onEconomyChanged`).

### C) Rischio Pooling (Mobile VFX)
Prima che il nemico venga disattivato in `Die()`, è obbligatorio catturare `transform.position`. Esempio:
```csharp
Vector3 deathPos = transform.position;
EnemyPooler.Instance.ReturnEnemy(gameObject);
// Esegui VFX usando deathPos qui
```

---
**Documento prodotto da:** Antigravity (Senior Unity Engineer)  
**Data:** 20 Dicembre 2025
