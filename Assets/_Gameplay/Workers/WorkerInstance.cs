using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Istanza runtime di un worker nel gioco.
    /// Rappresenta i DATI e la LOGICA di assignment di un worker.
    /// Può avere un WorkerController fisico opzionale per rappresentazione in scena.
    /// </summary>
    [System.Serializable]
    public class WorkerInstance
    {
        // ============================================
        // DATA
        // ============================================

        [ShowInInspector]
        [ReadOnly]
        public string InstanceId { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        public WorkerData Data { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        public string CustomName { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        public StructureController AssignedStructure { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        public WorkerState CurrentState { get; private set; }

        // ============================================
        // DUAL-SYSTEM SUPPORT
        // ============================================

        /// <summary>
        /// Riferimento opzionale al WorkerController fisico in scena.
        /// Può essere null per worker "virtuali" (solo dati/UI).
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        public WorkerController PhysicalWorker { get; set; }

        // ============================================
        // RUNTIME PROPERTIES
        // ============================================

        public Vector3 Position => PhysicalWorker != null ? PhysicalWorker.transform.position : Vector3.zero;
        public bool IsAssigned => AssignedStructure != null;
        public bool IsIdle => CurrentState == WorkerState.Idle && !IsAssigned;
        public bool IsWorking => CurrentState == WorkerState.Working && IsAssigned;
        public bool HasPhysicalRepresentation => PhysicalWorker != null;

        // ============================================
        // CONSTRUCTOR
        // ============================================

        public WorkerInstance(WorkerData data, string customName = null)
        {
            if (data == null)
            {
                Debug.LogError("[WorkerInstance] Cannot create with null WorkerData!");
                return;
            }

            InstanceId = System.Guid.NewGuid().ToString().Substring(0, 8);
            Data = data;
            CustomName = string.IsNullOrEmpty(customName) ? data.DisplayName : customName;
            CurrentState = WorkerState.Idle;
            AssignedStructure = null;
            PhysicalWorker = null;
        }

        // ============================================
        // ASSIGNMENT
        // ============================================

        /// <summary>
        /// Assegna questo worker a una struttura
        /// </summary>
        public bool AssignTo(StructureController structure)
        {
            if (structure == null)
            {
                Debug.LogWarning($"[Worker {CustomName}] Cannot assign to null structure");
                return false;
            }

            if (IsAssigned)
            {
                Debug.LogWarning($"[Worker {CustomName}] Already assigned to {AssignedStructure.Data.DisplayName}. Unassign first.");
                return false;
            }

            if (!structure.HasFreeWorkerSlot())
            {
                Debug.LogWarning($"[Worker {CustomName}] Structure {structure.Data.DisplayName} has no free slots");
                return false;
            }

            AssignedStructure = structure;
            CurrentState = WorkerState.Working;

            // Se ha rappresentazione fisica, muovilo verso la struttura
            if (PhysicalWorker != null)
            {
                // TODO: Implementare movimento verso struttura
                // PhysicalWorker.MoveTo(structure.transform.position);
                Debug.Log($"<color=cyan>[Worker]</color> {CustomName} physical worker moving to structure");
            }

            Debug.Log($"<color=green>[Worker]</color> {CustomName} assigned to {structure.Data.DisplayName}");
            return true;
        }

        /// <summary>
        /// Rimuove questo worker dalla struttura assegnata
        /// </summary>
        public void Unassign()
        {
            if (AssignedStructure != null)
            {
                Debug.Log($"<color=yellow>[Worker]</color> {CustomName} unassigned from {AssignedStructure.Data.DisplayName}");
            }

            AssignedStructure = null;
            CurrentState = WorkerState.Idle;

            // Se ha rappresentazione fisica, fermalo
            if (PhysicalWorker != null)
            {
                // TODO: Implementare ritorno a idle
                Debug.Log($"<color=cyan>[Worker]</color> {CustomName} physical worker returning to idle");
            }
        }

        /// <summary>
        /// Calcola il bonus che questo worker dà alla struttura assegnata
        /// </summary>
        public float GetCurrentBonus()
        {
            if (!IsAssigned || Data == null || AssignedStructure == null)
            {
                return 0f;
            }
            return Data.GetBonusForStructure(AssignedStructure.Data);
        }

        /// <summary>
        /// Verifica se questo worker è ideale per la struttura assegnata
        /// </summary>
        public bool IsIdealMatch()
        {
            if (!IsAssigned || Data == null || AssignedStructure == null)
            {
                return false;
            }
            return Data.IsIdealForStructure(AssignedStructure.Data);
        }

        // ============================================
        // STATE MANAGEMENT
        // ============================================

        public void SetState(WorkerState newState)
        {
            if (CurrentState != newState)
            {
                Debug.Log($"[Worker {CustomName}] State: {CurrentState} -> {newState}");
                CurrentState = newState;

                // Sincronizza con physical worker se presente
                if (PhysicalWorker != null)
                {
                    // TODO: Sincronizzare stato con WorkerController
                }
            }
        }

        /// <summary>
        /// Aggiorna lo stato basato sulle condizioni correnti
        /// </summary>
        public void UpdateState()
        {
            if (IsAssigned)
            {
                CurrentState = WorkerState.Working;
            }
            else
            {
                CurrentState = WorkerState.Idle;
            }
        }

        // ============================================
        // UTILITY
        // ============================================

        public override string ToString()
        {
            string status = IsAssigned ? $"Working at {AssignedStructure.Data.DisplayName}" : "Idle";
            string physical = HasPhysicalRepresentation ? " [Physical]" : " [Virtual]";
            return $"{CustomName} ({Data?.DefaultRole}){physical} - {status}";
        }

        /// <summary>
        /// Ottiene una descrizione dettagliata per UI/debug
        /// </summary>
        public string GetDetailedDescription()
        {
            if (Data == null) return "Invalid Worker";

            string desc = $"<b>{CustomName}</b>\n";
            desc += $"Role: {Data.DefaultRole}\n";
            desc += $"State: {CurrentState}\n";

            if (IsAssigned)
            {
                desc += $"Assigned to: {AssignedStructure.Data.DisplayName}\n";
                desc += $"Bonus: +{GetCurrentBonus() * 100f:F0}%";

                if (IsIdealMatch())
                {
                    desc += " (Ideal Match!)";
                }
            }
            else
            {
                desc += "Available for assignment";
            }

            return desc;
        }
    }
}
