using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Istanza runtime di un worker nel gioco.
    /// Ogni worker nel gioco è una WorkerInstance con riferimento a WorkerData.
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
        // RUNTIME
        // ============================================

        public GameObject WorldObject { get; set; }
        public Vector3 Position => WorldObject != null ? WorldObject.transform.position : Vector3.zero;
        public bool IsAssigned => AssignedStructure != null;
        public bool IsIdle => CurrentState == WorkerState.Idle && !IsAssigned;
        public bool IsWorking => CurrentState == WorkerState.Working && IsAssigned;

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
            return $"{CustomName} ({Data?.Role}) - {status}";
        }

        /// <summary>
        /// Ottiene una descrizione dettagliata per UI/debug
        /// </summary>
        public string GetDetailedDescription()
        {
            if (Data == null) return "Invalid Worker";

            string desc = $"<b>{CustomName}</b>\n";
            desc += $"Role: {Data.Role}\n";
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

    // ============================================
    // WORKER STATE ENUM
    // ============================================

    public enum WorkerState
    {
        /// <summary>Non sta facendo nulla, disponibile</summary>
        Idle,
        
        /// <summary>Si sta muovendo verso destinazione</summary>
        Moving,
        
        /// <summary>Sta lavorando in una struttura</summary>
        Working,
        
        /// <summary>Sta riposando (stamina bassa)</summary>
        Resting,
        
        /// <summary>Sta fuggendo (notte/pericolo)</summary>
        Fleeing,
        
        /// <summary>Sta costruendo</summary>
        Building
    }
}
