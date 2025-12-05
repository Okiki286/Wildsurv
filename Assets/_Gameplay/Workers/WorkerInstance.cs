using UnityEngine;
using Sirenix.OdinInspector;
using System;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    [Serializable]
    public class WorkerInstance
    {
        // ============================================
        // IDENTITY
        // ============================================

        [ShowInInspector, ReadOnly]
        public string InstanceId { get; private set; }

        [ShowInInspector, ReadOnly]
        public string CustomName { get; set; }

        [ShowInInspector, ReadOnly]
        public WorkerData Data { get; private set; }

        // ============================================
        // ASSIGNMENT STATE
        // ============================================

        [ShowInInspector, ReadOnly]
        public StructureController AssignedStructure { get; private set; }

        [ShowInInspector, ReadOnly]
        public bool IsAssigned => AssignedStructure != null;

        [ShowInInspector, ReadOnly]
        public WorkerState CurrentState { get; private set; } = WorkerState.Idle;

        [ShowInInspector, ReadOnly]
        public bool IsAtWorksite { get; set; } = false;

        // ============================================
        // PHYSICAL REPRESENTATION
        // ============================================

        [ShowInInspector, ReadOnly]
        public WorkerController PhysicalWorker { get; set; }

        public bool HasPhysicalRepresentation => PhysicalWorker != null;

        // ============================================
        // STATS
        // ============================================

        [ShowInInspector, ReadOnly]
        public float CurrentHealth { get; private set; }

        [ShowInInspector, ReadOnly]
        public float MaxHealth => Data?.BaseHealth ?? 100f;

        [ShowInInspector, ReadOnly]
        public bool IsAlive => CurrentHealth > 0;

        [ShowInInspector, ReadOnly]
        public float Experience { get; private set; } = 0f;

        [ShowInInspector, ReadOnly]
        public int Level { get; private set; } = 1;

        // ============================================
        // CONSTRUCTOR
        // ============================================

        public WorkerInstance(WorkerData data)
        {
            Data = data;
            InstanceId = Guid.NewGuid().ToString();
            CustomName = data?.DisplayName ?? "Worker";
            CurrentHealth = MaxHealth;
            CurrentState = WorkerState.Idle;
            IsAtWorksite = false;
        }

        // ============================================
        // ASSIGNMENT METHODS
        // ============================================

        public void AssignTo(StructureController structure)
        {
            if (structure == null) return;

            AssignedStructure = structure;
            IsAtWorksite = false;

            if (PhysicalWorker != null)
            {
                CurrentState = WorkerState.Moving;
                PhysicalWorker.CommandMoveTo(structure.transform.position);
            }
            else
            {
                IsAtWorksite = true;
                CurrentState = WorkerState.Working;
            }

            Debug.Log($"<color=cyan>[WorkerInstance]</color> {CustomName} assigned to {structure.Data?.DisplayName}. IsAtWorksite: {IsAtWorksite}");
        }

        /// <summary>
        /// Disassegna il worker dalla struttura.
        /// NON chiama CommandMoveTo per evitare di sbloccare il worker se era stato forzato in IDLE.
        /// Il movimento deve essere gestito esternamente (es. WorkerSystem).
        /// </summary>
        public void Unassign()
        {
            var previousStructure = AssignedStructure;
            AssignedStructure = null;
            IsAtWorksite = false;
            CurrentState = WorkerState.Idle;

            // IMPORTANTE: NON chiamare CommandMoveTo qui!
            // Il worker è già stato forzato in IDLE da WorkerSystem.UnassignWorker.
            // Chiamare CommandMoveTo sbloccherebbe il worker.
            
            // Se il PhysicalWorker esiste, assicurati solo che sia in stato corretto
            if (PhysicalWorker != null)
            {
                // Il ForceIdle è già stato chiamato da WorkerSystem.UnassignWorker
                // Non fare nulla qui, il worker è già fermo
            }

            Debug.Log($"<color=orange>[WorkerInstance]</color> {CustomName} unassigned from {previousStructure?.Data?.DisplayName}");
        }

        // ============================================
        // STATE MANAGEMENT
        // ============================================

        public void SetState(WorkerState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
        }

        // ============================================
        // BONUS CALCULATIONS
        // ============================================

        public float GetConstructionBonus()
        {
            if (Data == null) return 1f;
            return Data.BuildSpeedMultiplier * (1f + (Level - 1) * 0.1f);
        }

        public float GetProductionBonus(StructureData structureData)
        {
            if (Data == null) return 0f;
            return (Data.ProductivityMultiplier - 1f) * (1f + (Level - 1) * 0.05f);
        }

        public float GetCurrentBonus()
        {
            if (Data == null) return 0f;

            if (Data.DefaultRole == WorkerRole.Builder)
            {
                return GetConstructionBonus();
            }
            else
            {
                return Data.ProductivityMultiplier - 1f;
            }
        }

        public bool IsIdealMatch()
        {
            if (AssignedStructure == null || Data == null) return false;
            return (AssignedStructure.Data.AllowedRoles & Data.DefaultRole) != 0;
        }

        public bool IsIdealMatchFor(StructureData structureData)
        {
            if (structureData == null || Data == null) return false;
            return (structureData.AllowedRoles & Data.DefaultRole) != 0;
        }

        // ============================================
        // HEALTH & COMBAT
        // ============================================

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

            if (!IsAlive)
            {
                OnDeath();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        }

        private void OnDeath()
        {
            CurrentState = WorkerState.Dead;

            if (AssignedStructure != null)
            {
                AssignedStructure.RemoveWorker(this);
            }

            Debug.Log($"<color=red>[WorkerInstance]</color> {CustomName} has died!");
        }

        // ============================================
        // EXPERIENCE & LEVELING
        // ============================================

        public void AddExperience(float amount)
        {
            Experience += amount;

            float expToLevel = GetExpRequiredForLevel(Level + 1);
            while (Experience >= expToLevel && Level < 10)
            {
                Experience -= expToLevel;
                Level++;
                Debug.Log($"<color=green>[WorkerInstance]</color> {CustomName} leveled up to {Level}!");
                expToLevel = GetExpRequiredForLevel(Level + 1);
            }
        }

        private float GetExpRequiredForLevel(int level)
        {
            return 100f * Mathf.Pow(1.5f, level - 1);
        }
    }
}