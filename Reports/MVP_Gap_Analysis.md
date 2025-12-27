# MVP Gap Analysis & Roadmap

Analisi degli script core per identificare i "pezzi mancanti" del Core Loop necessari per un MVP mobile.

## Stato MVP

| Feature | Stato | Priorità | Note |
| :--- | :--- | :--- | :--- |
| **Recruitment Logic** | ⚠️ Parziale | Alta | `WorkerSystem` gestisce lo spawning, ma manca il collegamento UI definitivo/costo. |
| **Resource Generation** | ✅ Presente | Media | Già implementata tramite `ResourceSystem` e `StructureController`. |
| **Persistence (Saving)** | ❌ Mancante | Alta | Nessuna traccia di salvataggio. Cruciale per la progressione mobile. |
| **UI Feedback (SFX/VFX)** | ⚠️ Parziale | Media | Presenti suoni economia e costruzione. Mancano particellari UI e feedback tattile. |

---

## Analisi Dettagliata

### 1. Recruitment Logic
*   **Presente**: `WorkerSystem.CreateWorkerInstance` gestisce l'istanziazione fisica del prefab.
*   **Mancante**: Collegamento tra bottone UI e spesa risorse. Esiste un `RecruitUI` in fase di setup (`WorkerAssignmentRecruitSetup`), ma non sembra ancora integrato nel loop gameplay reale.

### 2. Resource Generation
*   **Presente**: Il sistema è solido. `ResourceSystem` è un singleton che gestisce l'inventario. `StructureController` calcola la produzione in base ai worker assegnati e invia i dati a `ResourceSystem`.

### 3. Persistence
*   **Mancante**: `GameManager.cs` contiene solo un `TODO: SaveSystem.Save()`.
*   **Azione Necessaria**: Implementare un `SaveManager` che utilizzi `PlayerPrefs` (per dati semplici) o `JSON` (per liste di worker/strutture) salvati localmente.

### 4. Feedback
*   **Presente**: `EconomyFeedbackSystem` gestisce popup e suoni per gain/spend.
*   **Mancante**: Mancano feedback visivi più ricchi (particellari all'arrivo dei worker, animazioni di "pulse" più avanzate nella UI).

---

## Roadmap Proposta

### Phase 1: Core Loop Completion (Alta Priorità)
1.  **Persistence System**: Creare `SaveSystem.cs` per salvare risorse e numero di worker.
2.  **Recruit Polish**: Collegare il `RecruitButton` al `ResourceSystem` per scalare il costo (es. 40 Food) al reclutamento.

### Phase 2: UX & Mobile Polish (Media Priorità)
1.  **Particle Feedback**: Aggiungere particellari semplici quando una risorsa viene raccolta o una struttura viene completata.
2.  **Haptic Feedback**: Integrare vibrazioni leggere alla pressione dei bottoni (mobile standard).

### Phase 3: Content Expansion (Bassa Priorità)
1.  **Multiple Worker Types**: Configurare diversi `WorkerData` per specializzazioni.
2.  **Advanced Save**: Salvataggio della posizione di ogni struttura sulla griglia.
