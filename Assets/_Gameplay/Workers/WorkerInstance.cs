using UnityEngine;
using Sirenix.OdinInspector;
using System;
using WildernessSurvival.Gameplay.Combat;
using WildernessSurvival.Gameplay.Enemies;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Rappresenta un'istanza runtime di un worker.
    /// Gestisce job, stats, assegnazione e stato.
    /// </summary>
    [Serializable]
    public class WorkerInstance : IDamageable
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
        // JOB SYSTEM
        // ============================================

        [ShowInInspector, ReadOnly]
        [TitleGroup("Current Job")]
        public WorkerJobData CurrentJob { get; private set; }

        [ShowInInspector, ReadOnly]
        [TitleGroup("Current Job")]
        public WorkerJobData PendingJob { get; set; }

        [ShowInInspector, ReadOnly]
        [TitleGroup("Current Job")]
        public bool IsJobChanging { get; private set; } = false;

        /// <summary>
        /// Evento invocato quando il job cambia (per aggiornare UI, etc.)
        /// </summary>
        public event Action<WorkerJobData> OnJobChanged;

        /// <summary>
        /// Evento invocato quando la transizione visiva è completata.
        /// </summary>
        public event Action OnJobTransitionComplete;

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

            // Initialize with default job
            InitializeDefaultJob();
        }

        /// <summary>
        /// Inizializza il job di default dal database.
        /// </summary>
        private void InitializeDefaultJob()
        {
            var jobDb = JobDatabase.Instance;
            if (jobDb != null)
            {
                CurrentJob = jobDb.GetDefaultJob();
                
                if (CurrentJob == null && Data != null)
                {
                    CurrentJob = jobDb.GetJobData(Data.DefaultRole);
                }
            }

            if (CurrentJob != null)
            {
                Debug.Log($"<color=cyan>[WorkerInstance]</color> {CustomName} initialized with job: {CurrentJob.JobName}");
            }
        }

        // ============================================
        // JOB MANAGEMENT
        // ============================================

        /// <summary>
        /// Cambia il job del worker.
        /// Se il worker è già in transizione, salva il job come pending.
        /// Il WorkerVisualController riceverà l'evento OnJobChanged e aggiornerà la skin.
        /// </summary>
        public void SetJob(WorkerJobData newJob)
        {
            if (newJob == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[WorkerInstance] {CustomName}: SetJob called with null. Ignoring.");
#endif
                return;
            }

            // Skip se stesso job
            if (CurrentJob == newJob)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=gray>[WorkerInstance]</color> {CustomName}: Already has job {newJob.JobName}. Skipping.");
#endif
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // JOB PENDING BUFFER
            // Se il worker è in transizione, salva come pending
            // ═══════════════════════════════════════════════════════════
            if (IsJobChanging)
            {
                PendingJob = newJob;
#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[WorkerInstance]</color> {CustomName} is changing job. " +
                    $"Buffering {newJob.JobName} as pending.");
#endif
                return;
            }

            var previousJob = CurrentJob;
            CurrentJob = newJob;
            IsJobChanging = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[WorkerInstance]</color> {CustomName} job changed: " +
                $"{previousJob?.JobName ?? "None"} -> {newJob.JobName}");
#endif

            // ═══════════════════════════════════════════════════════════
            // FIRE EVENT - WorkerVisualController si sottoscrive a questo
            // e gestirà automaticamente il cambio visivo (mesh swap o legacy)
            // ═══════════════════════════════════════════════════════════
            OnJobChanged?.Invoke(newJob);
        }

        /// <summary>
        /// Notifica che la transizione visiva è completata.
        /// Chiamato dal WorkerVisualController.
        /// </summary>
        public void CompleteJobTransition()
        {
            IsJobChanging = false;
            OnJobTransitionComplete?.Invoke();

            // Se c'è un job pending, applicalo ora
            if (PendingJob != null)
            {
                var pending = PendingJob;
                PendingJob = null;

#if UNITY_EDITOR
                Debug.Log($"<color=magenta>[WorkerInstance]</color> {CustomName} applying pending job: {pending.JobName}");
#endif

                SetJob(pending);
            }
        }

        /// <summary>
        /// Resetta il job al default (Villager).
        /// </summary>
        public void ResetToDefaultJob()
        {
            var jobDb = JobDatabase.Instance;
            if (jobDb == null) return;

            var defaultJob = jobDb.GetDefaultJob();
            if (defaultJob != null)
            {
                SetJob(defaultJob);
            }
            else
            {
                Debug.LogWarning($"[WorkerInstance] {CustomName}: No default job in database.");
            }
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

                // Usa GetClosestWorkSpot per posizionare il worker sul perimetro
                Vector3 workPosition = structure.GetClosestWorkSpot(PhysicalWorker.transform.position);
                PhysicalWorker.CommandMoveTo(workPosition);
            }
            else
            {
                IsAtWorksite = true;
                CurrentState = WorkerState.Working;
            }

            Debug.Log($"<color=cyan>[WorkerInstance]</color> {CustomName} assigned to {structure.Data?.DisplayName}");
        }

        /// <summary>
        /// Disassegna il worker dalla struttura.
        /// </summary>
        public void Unassign()
        {
            var previousStructure = AssignedStructure;
            AssignedStructure = null;
            IsAtWorksite = false;
            CurrentState = WorkerState.Idle;

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

        /// <summary>
        /// Ottiene il bonus costruzione.
        /// </summary>
        public float GetConstructionBonus()
        {
            float baseBuildSpeed = CurrentJob?.BuildSpeedBonus ?? Data?.BuildSpeedMultiplier ?? 1f;
            return baseBuildSpeed * (1f + (Level - 1) * 0.1f);
        }

        /// <summary>
        /// Ottiene il moltiplicatore di produzione finale (>= 1).
        /// Usa il bonus del job se presente, altrimenti il multiplier di base del worker.
        /// Applica anche un piccolo scaling per livello.
        /// </summary>
        public float GetProductionBonus(StructureData structureData)
        {
            float baseProductivity = CurrentJob?.ProductivityBonus ?? Data?.ProductivityMultiplier ?? 1f;

            // 5% per livello sopra il livello 1
            float levelMultiplier = 1f + (Level - 1) * 0.05f;

            // Ritorniamo un MULTIPLICATORE, non un delta:
            // es: base 1.2 * livello 1.1 = 1.32x produzione
            return baseProductivity * levelMultiplier;
        }

        /// <summary>
        /// Ottiene il bonus corrente generico.
        /// </summary>
        public float GetCurrentBonus()
        {
            if (CurrentJob != null)
            {
                if (CurrentJob.Role == WorkerRole.Builder)
                {
                    return GetConstructionBonus();
                }
                return CurrentJob.ProductivityBonus - 1f;
            }

            if (Data == null) return 0f;
            return Data.DefaultRole == WorkerRole.Builder ? GetConstructionBonus() : Data.ProductivityMultiplier - 1f;
        }

        /// <summary>
        /// Verifica se il worker è un match ideale per la struttura assegnata.
        /// </summary>
        public bool IsIdealMatch()
        {
            if (AssignedStructure == null) return false;
            WorkerRole effectiveRole = GetEffectiveRole();
            if (effectiveRole == WorkerRole.None) return false;
            return (AssignedStructure.Data.AllowedRoles & effectiveRole) != 0;
        }

        /// <summary>
        /// Verifica se questo worker è ideale per una struttura specifica.
        /// </summary>
        public bool IsIdealMatchFor(StructureData structureData)
        {
            if (structureData == null) return false;
            WorkerRole effectiveRole = GetEffectiveRole();
            if (effectiveRole == WorkerRole.None) return false;
            return (structureData.AllowedRoles & effectiveRole) != 0;
        }

        /// <summary>
        /// Ottiene il ruolo effettivo del worker.
        /// </summary>
        public WorkerRole GetEffectiveRole()
        {
            if (CurrentJob != null) return CurrentJob.Role;
            if (Data != null) return Data.DefaultRole;
            return WorkerRole.None;
        }

        /// <summary>
        /// Ottiene la velocità di movimento effettiva.
        /// </summary>
        public float GetEffectiveMovementSpeed()
        {
            return CurrentJob?.MovementSpeed ?? Data?.MovementSpeed ?? 3.5f;
        }

        // ============================================
        // HEALTH & COMBAT (IDamageable)
        // ============================================

        // IDamageable è implementato esplicitamente dove serve
        float IDamageable.CurrentHealth => CurrentHealth;
        float IDamageable.MaxHealth => MaxHealth;

        /// <summary>
        /// Applica danno al worker (legacy, senza tipo danno)
        /// </summary>
        public void TakeDamage(float damage)
        {
            TakeDamage(damage, DamageType.None);
        }

        /// <summary>
        /// Applica danno al worker con tipo danno (IDamageable)
        /// </summary>
        public void TakeDamage(float damage, DamageType damageType)
        {
            if (!IsAlive) return;
            // Workers non hanno resistenze per ora, ignora damageType
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            if (!IsAlive) OnDeath();
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