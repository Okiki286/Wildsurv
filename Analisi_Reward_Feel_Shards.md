# Analisi Tecnica: Implementazione "Reward Feel" Shards
**Progetto:** WildernessSurvival  
**Ruolo:** Senior Unity Engineer

---

## 1. Hook Point Consigliato (Single Source of Truth)
**File:** `EnemyInstance.cs`  
**Metodo:** `DropRewards()` (Linee 845-881)

*   **Perché:** È l'unico punto centralizzato dove viene calcolato l'ammontare finale (`shardDrop`) dopo l'applicazione dei moltiplicatori (`rewardMultiplier`) e dei dati dell'ondata. Qui avviene l'effettivo accredito al `ResourceSystem`.
*   **Logica esistente:** Attualmente esegue già un log diagnostico: `"shards BEFORE=... +... AFTER=..."` (Linea 867).
*   **Azione consigliata:** Inserire l'emissione di un evento (es. `OnShardsGained`) passando l'ammontare e la posizione del nemico (`transform.position`).

---

## 2. Call Chain (Flusso di Esecuzione)
Il flusso dall'uccisione all'aggiornamento UI è il seguente:
1.  **Morte Nemico:** `EnemyInstance.TakeDamage()` rileva salute <= 0 e chiama `Die()`.
2.  **Reward Logic:** `Die()` chiama `DropRewards()`, che calcola la quantità di shards.
3.  **Sistema Risorse:** `DropRewards()` invoca `ResourceSystem.Instance.AddResource("shard", amount)`.
4.  **Backend Update:** `ResourceSystem.cs` aggiorna il totale nel dizionario `resourceAmounts` e applica eventuali limiti di storage.
5.  **Notifica Evento:** `ResourceSystem` solleva l'evento `onResourceChanged.Raise("shard")` (ScriptableObject Event).
6.  **Update UI:** `ResourceDisplayUI.cs` rileva il cambio (tramite polling ogni 0.25s) e aggiorna il counter a schermo.

---

## 3. UI Counter e Meccanismo HUD
*   **Gestore Principale:** `ResourceDisplayUI.cs` (situato in `_UI\Scripts\HUD\`).
*   **Metodo di Aggiornamento:** Usa un sistema di **polling** invece di eventi diretti, aggiornando i valori ogni `0.25` secondi tramite `UpdateAllDisplays()`.
*   **Feature pronte all'uso:** Lo script possiede già i metodi `AnimateChange` (per pulsazione) e `ShowFloatingText` (per testo volante), pronti per essere agganciati alla logica di arrivo delle shards.

---

## 4. Integrazione Audio/SFX
*   **Feedback 3D (Posizionale):** Da innescare in `EnemyInstance.DropRewards()`. Poiché il nemico viene disattivato immediatamente dopo (`ReturnEnemy`), è necessario emettere l'audio tramite `AudioSource.PlayClipAtPoint` o un manager globale per evitare che il suono venga interrotto.
*   **Feedback 2D (Interfaccia):** Da innescare in `ResourceDisplayUI.ShowChangeAnimation()`. Questo garantisce che il suono di "incasso" sia perfettamente sincronizzato con l'aggiornamento numerico nell'HUD.

---

## 5. Event System ESISTENTE
Il progetto utilizza un sistema di **ScriptableObject Events** situato in `_Core\Events\`.
*   **Consiglio:** Utilizzare il `Vector3Event` già presente in `TypedGameEvents.cs` per notificare la posizione della morte a un gestore VFX globale. Questo permette di mantenere la logica del nemico pulita, delegando l'estetica a un sistema dedicato.

---

## 6. Pooling e Ciclo di Vita
*   **EnemyPooler:** I nemici sono totalmente pooled.
*   **Reset:** In `OnEnable()`, il metodo `ResetStateForPooling()` resetta correttamente il flag `hasDroppedRewards = false`, garantendo che ogni nemico appena spawnato possa generare ricompense.
*   **Vincolo Estetico:** Gli effetti visivi (es. "orb" luminosi) non devono essere figli del nemico, poiché quest'ultimo viene disattivato all'istante dopo la morte per tornare nel pool.

---

## 7. Rischi ed Edge Case
*   **Bug Identificato (Plurale):** `ResourceSystem` usa l'ID risorsa `"shard"`, ma `GameHUD.cs` cerca `"shards"`. Bisogna standardizzare su `"shard"` (singolare) per evitare che parti della UI rimangano fisse.
*   **Kill Simultanee:** Un'esplosione che uccide molti nemici contemporaneamente potrebbe generare un "muro armonico" se non si limita il numero di istanze audio prodotte nello stesso frame.
*   **Tower Kills:** Già correttamente integrate. Qualunque fonte di danno che riduca gli HP a zero attiva la catena dei reward nel nemico.
*   **Target Visivo:** Per gli effetti di "volo" delle shards, il punto di destinazione deve essere recuperato da `BaseCenterSystem.Instance.CurrentCenter` (la Waystone).

---
**Documento prodotto da:** Antigravity (Senior Unity Engineer)  
**Data:** 20 Dicembre 2025
