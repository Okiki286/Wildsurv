/*
 * ============================================
 * STRUTTURE CONTROLLER - WORKER EXTENSIONS
 * ============================================
 * 
 * Questi metodi e campi devono essere AGGIUNTI al file StructureController.cs esistente.
 * NON è un file separato - copia questo codice dentro la classe StructureController.
 * 
 * ISTRUZIONI:
 * 1. Apri Assets/_Gameplay/Structures/StructureController.cs
 * 2. Aggiungi gli using in cima al file
 * 3. Aggiungi i campi nella sezione appropriata
 * 4. Aggiungi i metodi prima della chiusura della classe
 */

// ============================================
// USING DA AGGIUNGERE (in cima al file)
// ============================================
/*
using WildernessSurvival.Gameplay.Workers;
using System.Collections.Generic;
*/

// ============================================
// CAMPI DA AGGIUNGERE
// ============================================
/*
        [TitleGroup("Workers")]
        [ShowInInspector]
        [ReadOnly]
        private List<WorkerInstance> assignedWorkers = new List<WorkerInstance>();

        // Properties
        public int AssignedWorkerCount => assignedWorkers?.Count ?? 0;
        public int CurrentLevel { get; private set; } = 1;
        public int CurrentHealth { get; private set; }
*/

// ============================================
// METODI DA AGGIUNGERE
// ============================================

/*
        // Chiamato in Start() o Awake()
        private void InitializeHealth()
        {
            CurrentHealth = Data != null ? Data.MaxHealth : 100;
        }

        /// <summary>
        /// Verifica se c'è uno slot worker libero
        /// </summary>
        public bool HasFreeWorkerSlot()
        {
            if (Data == null) return false;
            return assignedWorkers.Count < Data.WorkerSlots;
        }

        /// <summary>
        /// Ottiene il numero di slot liberi
        /// </summary>
        public int GetFreeWorkerSlots()
        {
            if (Data == null) return 0;
            return Mathf.Max(0, Data.WorkerSlots - assignedWorkers.Count);
        }

        /// <summary>
        /// Aggiunge un worker a questa struttura
        /// </summary>
        public void AddWorker(WorkerInstance worker)
        {
            if (worker == null)
            {
                Debug.LogWarning($"[StructureController] Cannot add null worker");
                return;
            }

            if (!HasFreeWorkerSlot())
            {
                Debug.LogWarning($"[StructureController] {Data.DisplayName} has no free worker slots");
                return;
            }

            if (assignedWorkers.Contains(worker))
            {
                Debug.LogWarning($"[StructureController] Worker {worker.CustomName} already assigned");
                return;
            }

            assignedWorkers.Add(worker);
            
            Debug.Log($"<color=cyan>[Structure]</color> {Data.DisplayName} now has {assignedWorkers.Count}/{Data.WorkerSlots} workers");
            
            // Notify production system if applicable
            OnWorkersChanged();
        }

        /// <summary>
        /// Rimuove un worker da questa struttura
        /// </summary>
        public void RemoveWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            if (assignedWorkers.Remove(worker))
            {
                Debug.Log($"<color=cyan>[Structure]</color> {Data.DisplayName} now has {assignedWorkers.Count}/{Data.WorkerSlots} workers");
                OnWorkersChanged();
            }
        }

        /// <summary>
        /// Rimuove tutti i worker
        /// </summary>
        public void RemoveAllWorkers()
        {
            assignedWorkers.Clear();
            OnWorkersChanged();
        }

        /// <summary>
        /// Ottiene tutti i worker assegnati (copia della lista)
        /// </summary>
        public List<WorkerInstance> GetAssignedWorkers()
        {
            return new List<WorkerInstance>(assignedWorkers);
        }

        /// <summary>
        /// Calcola il bonus totale dei worker assegnati
        /// </summary>
        public float GetTotalWorkerBonus()
        {
            float totalBonus = 0f;
            
            foreach (var worker in assignedWorkers)
            {
                if (worker != null)
                {
                    totalBonus += worker.GetCurrentBonus();
                }
            }
            
            return totalBonus;
        }

        /// <summary>
        /// Calcola la produzione totale (base + bonus)
        /// </summary>
        public float GetTotalProductionRate()
        {
            if (Data == null) return 0f;
            
            float baseRate = Data.BaseProductionRate;
            float bonusMultiplier = 1f + GetTotalWorkerBonus();
            
            return baseRate * bonusMultiplier;
        }

        /// <summary>
        /// Chiamato quando la lista workers cambia
        /// </summary>
        private void OnWorkersChanged()
        {
            // Override this in subclass or use event
            // Per esempio: ricalcola produzione, aggiorna visuals, etc.
        }

        /// <summary>
        /// Chiamato quando si clicca sulla struttura
        /// </summary>
        public void OnClick()
        {
            // Apre il panel di assegnazione worker
            if (UI.WorkerAssignmentUI.Instance != null)
            {
                UI.WorkerAssignmentUI.Instance.OpenForStructure(this);
            }
            else
            {
                Debug.Log($"[StructureController] Clicked on {Data?.DisplayName}");
            }
        }

        /// <summary>
        /// Applica danno alla struttura
        /// </summary>
        public void TakeDamage(int damage)
        {
            CurrentHealth -= damage;
            
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                OnDestroyed();
            }
        }

        /// <summary>
        /// Chiamato quando la struttura viene distrutta
        /// </summary>
        private void OnDestroyed()
        {
            // Rimuovi tutti i worker prima della distruzione
            if (WorkerSystem.Instance != null)
            {
                WorkerSystem.Instance.UnassignAllFromStructure(this);
            }
            
            // Rimuovi dalla lista strutture
            if (StructureSystem.Instance != null)
            {
                StructureSystem.Instance.RemoveStructure(this);
            }
            
            Debug.Log($"<color=red>[Structure]</color> {Data?.DisplayName} destroyed!");
            
            // Destroy GameObject
            Destroy(gameObject);
        }

        /// <summary>
        /// Ripara la struttura
        /// </summary>
        public void Repair(int amount)
        {
            if (Data == null) return;
            
            CurrentHealth = Mathf.Min(CurrentHealth + amount, Data.MaxHealth);
        }

        /// <summary>
        /// Verifica se la struttura è a piena salute
        /// </summary>
        public bool IsFullHealth()
        {
            return Data != null && CurrentHealth >= Data.MaxHealth;
        }

        /// <summary>
        /// Ottiene percentuale salute
        /// </summary>
        public float GetHealthPercent()
        {
            if (Data == null || Data.MaxHealth <= 0) return 0f;
            return (float)CurrentHealth / Data.MaxHealth;
        }
*/

// ============================================
// DEBUG BUTTONS DA AGGIUNGERE
// ============================================
/*
        [TitleGroup("Debug - Workers")]
        [Button("Log Workers", ButtonSizes.Medium)]
        private void DebugLogWorkers()
        {
            Debug.Log($"=== {Data?.DisplayName} Workers ===");
            Debug.Log($"Slots: {assignedWorkers.Count}/{Data?.WorkerSlots}");
            Debug.Log($"Total Bonus: +{GetTotalWorkerBonus() * 100f:F0}%");
            
            foreach (var worker in assignedWorkers)
            {
                Debug.Log($"  - {worker.CustomName} ({worker.Data?.Role}) +{worker.GetCurrentBonus() * 100f:F0}%");
            }
        }

        [Button("Clear Workers", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        private void DebugClearWorkers()
        {
            if (WorkerSystem.Instance != null)
            {
                WorkerSystem.Instance.UnassignAllFromStructure(this);
            }
            assignedWorkers.Clear();
            Debug.Log($"[StructureController] Cleared all workers from {Data?.DisplayName}");
        }
*/
