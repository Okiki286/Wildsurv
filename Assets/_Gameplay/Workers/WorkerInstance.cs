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

        [ShowInInspector, ReadOnly]
        public string InstanceId { get; private set; }

        [ShowInInspector, ReadOnly]
        public WorkerData Data { get; private set; }

        [ShowInInspector, ReadOnly]
        public string CustomName { get; private set; }

        [ShowInInspector, ReadOnly]
        public StructureController AssignedStructure { get; private set; }

        [ShowInInspector, ReadOnly]
        public WorkerState CurrentState { get; private set; }

        // ============================================
        // DUAL-SYSTEM SUPPORT
        // ============================================

        /// <summary>
        /// Riferimento opzionale al WorkerController fisico in scena.
        /// Può essere null per worker "virtuali" (solo dati/UI).
        /// </summary>
        [ShowInInspector, ReadOnly]
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
        // LOGIC & CALCULATIONS
        // ============================================

        /// <summary>
        /// Calcola il bonus di produzione per una specifica struttura.
        /// </summary>
        public float GetProductionBonus(StructureData structure)
        {
            if (Data == null || structure == null) return 0f;

            // Base bonus from productivity multiplier (e.g. 1.2 -> +0.2)
            float bonus = Data.ProductivityMultiplier - 1f;

            // Bitmask check per ruolo
            bool isRoleAllowed = (structure.AllowedRoles & Data.DefaultRole) != 0;

            if (isRoleAllowed)
            {
                // Aggiungi bonus specifico del ruolo
                bonus += Data.GetRoleBonus(Data.DefaultRole);
            }
            else
            {
                // Penalità se il ruolo non è adatto (opzionale, o semplicemente niente bonus extra)
                // bonus -= 0.5f; 
            }

            return bonus;
        }

        // ============================================
        // UI HELPERS (Fix CS1061 Errors)
        // ============================================

        /// <summary>
        /// Calcola il bonus attuale. Se assegnato, usa la struttura corrente.
        /// </summary>
        public float GetCurrentBonus()
        {
            if (AssignedStructure == null || Data == null) return 0f;
            return GetProductionBonus(AssignedStructure.Data);
        }

        /// <summary>
        /// Calcola il bonus di costruzione per questo worker.
        /// I Builder ottengono il loro BuildSpeedMultiplier, altri ottengono 1.0f.
        /// </summary>
        public float GetConstructionBonus()
        {
            if (Data == null) return 1.0f;
            
            // Se è un Builder, restituisci il moltiplicatore di costruzione
            if (Data.DefaultRole == WildernessSurvival.Gameplay.Structures.WorkerRole.Builder)
            {
                return Data.BuildSpeedMultiplier;
            }
            
            // Altrimenti, velocità normale
            return 1.0f;
        }

        /// <summary>
        /// Verifica se il worker è un match ideale per la struttura assegnata.
        /// </summary>
        public bool IsIdealMatch()
        {
            if (AssignedStructure == null || Data == null) return false;
            // Verifica bitmask o uguaglianza diretta
            return (AssignedStructure.Data.AllowedRoles & Data.DefaultRole) != 0;
        }

        // Overload per la UI che controlla "would serve be ideal?"
        public bool IsIdealMatchFor(StructureData structureData)
        {
            if (structureData == null || Data == null) return false;
            return (structureData.AllowedRoles & Data.DefaultRole) != 0;
        }

        // ============================================
        // ASSIGNMENT
        // ============================================

        /// <summary>
        /// Assegna questo worker a una struttura.
        /// Gestisce sia la logica dati che il movimento fisico (se presente).
        /// </summary>
        public void AssignTo(StructureController structure)
        {
            if (structure == null) return;

            AssignedStructure = structure;
            CurrentState = WorkerState.MovingToWork;

            // Se esiste una rappresentazione fisica, muovila
            if (PhysicalWorker != null)
            {
                PhysicalWorker.CommandMoveTo(structure.transform.position);
            }
            else
            {
                // Se virtuale, passa direttamente a working
                CurrentState = WorkerState.Working;
            }
        }

        /// <summary>
        /// Rimuove l'assegnazione dalla struttura corrente.
        /// </summary>
        public void Unassign()
        {
            AssignedStructure = null;
            CurrentState = WorkerState.Idle;
            
            // Ferma il worker fisico
            if (PhysicalWorker != null)
            {
                // Stop movement (move to current position)
                PhysicalWorker.CommandMoveTo(PhysicalWorker.transform.position);
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
    }
}
