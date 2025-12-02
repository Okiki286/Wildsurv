using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Controlla il comportamento runtime di una struttura.
    /// Gestisce health, produzione, worker assignment, e stati.
    /// </summary>
    public class StructureController : MonoBehaviour
    {
        // ============================================
        // RIFERIMENTI
        // ============================================

        [TitleGroup("Setup")]
        [Required("StructureData è richiesto!")]
        [AssetsOnly]
        [SerializeField] private StructureData structureData;

        [BoxGroup("Setup/Components")]
        [ChildGameObjectsOnly]
        [Tooltip("Root visivo della struttura")]
        [SerializeField] private Transform visualRoot;

        [BoxGroup("Setup/Components")]
        [ChildGameObjectsOnly]
        [Tooltip("Transform per VFX")]
        [SerializeField] private Transform vfxSpawnPoint;

        // ============================================
        // STATO RUNTIME
        // ============================================

        [TitleGroup("Runtime State")]
        [BoxGroup("Runtime State/Core")]
        [ReadOnly]
        [ShowInInspector]
        [EnumToggleButtons]
        private StructureState currentState = StructureState.Idle;

        [BoxGroup("Runtime State/Core")]
        [ReadOnly]
        [ShowInInspector]
        [PropertyRange(1, 5)]
        private int currentLevel = 1;

        [BoxGroup("Runtime State/Core")]
        [ReadOnly]
        [ShowInInspector]
        private bool isOperational = false;

        // ============================================
        // HEALTH SYSTEM
        // ============================================

        [TitleGroup("Health")]
        [BoxGroup("Health/Stats")]
        [ReadOnly]
        [ShowInInspector]
        [ProgressBar(0, "maxHealth", ColorGetter = "GetHealthColor")]
        private float currentHealth;

        [BoxGroup("Health/Stats")]
        [ReadOnly]
        [ShowInInspector]
        private float maxHealth;

        // ============================================
        // WORKER ASSIGNMENT
        // ============================================

        [TitleGroup("Workers")]
        [BoxGroup("Workers/Assigned")]
        [ReadOnly]
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<WorkerController> assignedWorkers = new List<WorkerController>();

        [BoxGroup("Workers/Assigned")]
        [ReadOnly]
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<WorkerController> assignedHeroes = new List<WorkerController>();

        // [MODIFICATION 1] Worker Instances for UI Assignment System
        [BoxGroup("Workers/Assigned")]
        [ReadOnly]
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<Workers.WorkerInstance> assignedWorkerInstances = new List<Workers.WorkerInstance>();

        [BoxGroup("Workers/Stats")]
        [ReadOnly]
        [ShowInInspector]
        [ProgressBar(0, "structureData.WorkerSlots", ColorGetter = "GetWorkerSlotColor")]
        private int workerCount = 0;

        [BoxGroup("Workers/Stats")]
        [ReadOnly]
        [ShowInInspector]
        [PropertyRange(0f, 3f)]
        [Tooltip("Moltiplicatore produttività totale")]
        private float totalProductivityBonus = 1f;

        // ============================================
        // PRODUZIONE
        // ============================================

        [TitleGroup("Production")]
        [BoxGroup("Production/Stats")]
        [ReadOnly]
        [ShowInInspector]
        [ShowIf("@structureData != null && structureData.Category == StructureCategory.Resource")]
        private float currentProductionRate = 0f;

        [BoxGroup("Production/Stats")]
        [ReadOnly]
        [ShowInInspector]
        [ShowIf("@structureData != null && structureData.Category == StructureCategory.Resource")]
        private float productionAccumulator = 0f;

        private float productionTickInterval = 1f; // Produce ogni secondo
        private float productionTimer = 0f;

        // ============================================
        // COSTRUZIONE
        // ============================================

        [TitleGroup("Construction")]
        [BoxGroup("Construction/Progress")]
        [ReadOnly]
        [ShowInInspector]
        [ProgressBar(0, 1, ColorGetter = "GetConstructionColor")]
        private float buildProgress = 0f;

        [BoxGroup("Construction/Progress")]
        [ReadOnly]
        [ShowInInspector]
        private float buildTimeRemaining = 0f;

        private List<WorkerController> buildersAssigned = new List<WorkerController>();

        // [NEW] Current build speed from assigned workers
        private float currentBuildSpeed = 1f;

        // ============================================
        // VFX & VISUALS
        // ============================================

        private GameObject currentVFX;
        private Renderer[] renderers;
        private Material[] originalMaterials;

        // ============================================
        // PROPERTIES
        // ============================================

        public StructureData Data => structureData;
        public StructureState State => currentState;
        public int CurrentLevel => currentLevel;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsOperational => isOperational;
        public int WorkerCount => workerCount;
        public List<WorkerController> AssignedWorkers => assignedWorkers;

        // [MODIFICATION 2] Added Property
        public int AssignedWorkerInstanceCount => assignedWorkerInstances.Count;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (structureData == null)
            {
                Debug.LogError($"[StructureController] {gameObject.name} has no StructureData!", this);
                enabled = false;
                return;
            }

            InitializeStructure();
        }

        private void Start()
        {
            Debug.Log($"<color=orange>[Structure]</color> {structureData.DisplayName} initialized at {transform.position}");
        }

        private void Update()
        {
            if (!IsAlive) return;

            UpdateState();
            UpdateProduction();
        }

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        private void InitializeStructure()
        {
            // Stats iniziali
            maxHealth = structureData.MaxHealth;
            currentHealth = maxHealth;
            currentLevel = 1;

            // Cache renderers per cambio materiali
            if (visualRoot != null)
            {
                renderers = visualRoot.GetComponentsInChildren<Renderer>();
                CacheOriginalMaterials();
            }

            // Setup VFX spawn point
            if (vfxSpawnPoint == null)
            {
                vfxSpawnPoint = transform;
            }

            // Se richiede builder, inizia in costruzione
            if (structureData.RequiresBuilder)
            {
                ChangeState(StructureState.Building);
                buildTimeRemaining = structureData.BuildTime;
                buildProgress = 0f;
                isOperational = false;
                ApplyConstructionVisuals();
            }
            else
            {
                // Struttura instant
                ChangeState(StructureState.Operating);
                buildProgress = 1f;
                isOperational = true;
                CompleteConstruction();
            }
        }

        /// <summary>
        /// Setup esterno con StructureData (chiamato da StructureSystem)
        /// </summary>
        public void Initialize(StructureData data, int level = 1)
        {
            structureData = data;
            currentLevel = level;

            // 🔧 SAFETY: Verifica BuildTime
            if (structureData.BuildTime <= 0f)
            {
                Debug.LogWarning($"<color=yellow>[Structure]</color> ⚠️ {structureData.DisplayName} has BuildTime <= 0! Setting to 1f to prevent instant construction.");
                // Non possiamo modificare lo ScriptableObject, ma possiamo loggare
                // Il sistema userà comunque il valore dallo ScriptableObject
            }

            InitializeStructure();
        }

        // ============================================
        // STATE MACHINE
        // ============================================

        private void UpdateState()
        {
            switch (currentState)
            {
                case StructureState.Building:
                    UpdateBuilding();
                    break;

                case StructureState.Operating:
                    // Struttura attiva, produzione gestita in UpdateProduction()
                    break;

                case StructureState.Damaged:
                    // Produzione ridotta
                    break;

                case StructureState.Destroyed:
                    // Nessuna azione
                    break;
            }
        }

        private void ChangeState(StructureState newState)
        {
            if (currentState == newState) return;

            Debug.Log($"<color=orange>[Structure]</color> {structureData.DisplayName}: {currentState} → {newState}");
            currentState = newState;

            // Callback cambio stato
            OnStateChanged(newState);
        }

        private void OnStateChanged(StructureState newState)
        {
            switch (newState)
            {
                case StructureState.Building:
                    isOperational = false;
                    ApplyConstructionVisuals();
                    break;

                case StructureState.Operating:
                    isOperational = true;
                    ApplyNormalVisuals();
                    RecalculateProduction();
                    break;

                case StructureState.Damaged:
                    ApplyDamagedVisuals();
                    break;

                case StructureState.Destroyed:
                    isOperational = false;
                    OnStructureDestroyed();
                    break;
            }
        }

        // ============================================
        // COSTRUZIONE
        // ============================================

        private void UpdateBuilding()
        {
            if (buildProgress >= 1f) return;

            // Calcola velocità costruzione
            float buildSpeed = 1f;

            if (structureData.RequiresBuilder && buildersAssigned.Count > 0)
            {
                // Somma velocità di tutti i builder
                foreach (var builder in buildersAssigned)
                {
                    if (builder != null && builder.IsAlive)
                    {
                        buildSpeed += builder.Data.BuildSpeedMultiplier;
                    }
                }
            }
            else if (!structureData.RequiresBuilder)
            {
                // Costruzione automatica
                buildSpeed = 1f;
            }
            else
            {
                // Nessun builder assegnato, pausa costruzione
                return;
            }

            // Aggiorna progresso
            float progressDelta = (buildSpeed / structureData.BuildTime) * Time.deltaTime;
            buildProgress += progressDelta;
            buildTimeRemaining -= Time.deltaTime * buildSpeed;

            // Costruzione completata
            if (buildProgress >= 1f)
            {
                CompleteConstruction();
            }
        }

        private void CompleteConstruction()
        {
            buildProgress = 1f;
            buildTimeRemaining = 0f;

            Debug.Log($"<color=green>[Structure]</color> {structureData.DisplayName} costruzione completata!");

            // Spawna VFX completamento
            SpawnVFX("completion");

            // 🔧 FIX: Rilascia tutti i worker PRIMA di cambiare stato
            // Crea una copia della lista per evitare errori di modifica durante l'iterazione
            var workersToRelease = new List<Workers.WorkerInstance>(assignedWorkerInstances);
            int releasedCount = 0;

            foreach (var worker in workersToRelease)
            {
                if (worker != null && WorkerSystem.Instance != null)
                {
                    WorkerSystem.Instance.UnassignWorker(worker);
                    releasedCount++;
                }
            }

            // Pulizia finale (dovrebbe essere già vuota dopo UnassignWorker)
            assignedWorkerInstances.Clear();
            workerCount = 0;

            Debug.Log($"<color=green>[Structure]</color> 🏗️ Construction complete. Released {releasedCount} builders.");

            // Cambia stato
            ChangeState(StructureState.Operating);

            // Rilascia builder legacy (se presenti)
            ReleaseAllBuilders();

            // [IMPORTANT] Ora che la struttura è operativa, ricalcola la produzione
            // (Sarà 0 perché non ci sono worker assegnati, ma è corretto)
            RecalculateProduction();

            // Notifica StructureSystem
            // StructureSystem.Instance?.OnStructureCompleted(this);
        }

        /// <summary>
        /// Tick di costruzione chiamato da WorkerSystem.
        /// Avanza il progresso basandosi su currentBuildSpeed.
        /// </summary>
        public void TickConstruction(float deltaTime)
        {
            if (buildProgress >= 1f) return;
            if (currentState != StructureState.Building) return;

            // Se non ha worker assegnati e richiede builder, pausa costruzione
            if (structureData.RequiresBuilder && assignedWorkerInstances.Count == 0)
            {
                return;
            }

            // Ricalcola velocità costruzione ogni tick (per aggiornamenti dinamici)
            RecalculateBuildSpeed();

            // Calcola progresso
            // Formula: progress += (currentBuildSpeed / buildTime) * dt
            float progressDelta = (currentBuildSpeed / structureData.BuildTime) * deltaTime;
            buildProgress += progressDelta;

            // Aggiorna tempo rimanente
            buildTimeRemaining = Mathf.Max(0, structureData.BuildTime * (1f - buildProgress) / currentBuildSpeed);

            // Completa costruzione se raggiunto 100%
            if (buildProgress >= 1f)
            {
                CompleteConstruction();
            }
        }

        /// <summary>
        /// Assegna builder alla costruzione
        /// </summary>
        public bool AssignBuilder(WorkerController worker)
        {
            if (worker == null) return false;
            if (buildersAssigned.Contains(worker)) return false;
            if (buildProgress >= 1f) return false;

            buildersAssigned.Add(worker);
            Debug.Log($"<color=orange>[Structure]</color> Builder {worker.Data.DisplayName} assigned to {structureData.DisplayName}");
            return true;
        }

        /// <summary>
        /// Rimuovi builder dalla costruzione
        /// </summary>
        public void RemoveBuilder(WorkerController worker)
        {
            buildersAssigned.Remove(worker);
        }

        private void ReleaseAllBuilders()
        {
            foreach (var builder in buildersAssigned)
            {
                if (builder != null)
                {
                    // WorkerSystem.Instance?.UnassignWorker(builder);
                }
            }
            buildersAssigned.Clear();
        }

        // ============================================
        // WORKER ASSIGNMENT
        // ============================================

        /// <summary>
        /// Assegna un worker a questa struttura (Legacy/Physical)
        /// </summary>
        public bool AssignWorker(WorkerController worker, bool isHero = false)
        {
            if (worker == null) return false;
            if (!isOperational) return false;

            // Verifica slot disponibili
            if (isHero)
            {
                if (assignedHeroes.Count >= structureData.HeroSlots)
                {
                    Debug.LogWarning($"[Structure] {structureData.DisplayName} hero slots full!");
                    return false;
                }
                assignedHeroes.Add(worker);
            }
            else
            {
                if (assignedWorkers.Count >= structureData.WorkerSlots)
                {
                    Debug.LogWarning($"[Structure] {structureData.DisplayName} worker slots full!");
                    return false;
                }
                assignedWorkers.Add(worker);
            }

            workerCount = assignedWorkers.Count + assignedHeroes.Count;
            RecalculateProduction();

            Debug.Log($"<color=orange>[Structure]</color> {worker.Data.DisplayName} assigned to {structureData.DisplayName}");
            return true;
        }

        /// <summary>
        /// Rimuovi worker da questa struttura (Legacy/Physical)
        /// </summary>
        public void UnassignWorker(WorkerController worker)
        {
            if (worker == null) return;

            assignedWorkers.Remove(worker);
            assignedHeroes.Remove(worker);
            workerCount = assignedWorkers.Count + assignedHeroes.Count;

            RecalculateProduction();

            Debug.Log($"<color=orange>[Structure]</color> {worker.Data.DisplayName} unassigned from {structureData.DisplayName}");
        }

        /// <summary>
        /// Calcola bonus produttività da worker assegnati
        /// </summary>
        public void RecalculateProduction()
        {
            if (structureData.Category != StructureCategory.Resource) return;

            totalProductivityBonus = 1f;

            // Bonus da worker instances
            foreach (var instance in assignedWorkerInstances)
            {
                if (instance != null)
                {
                     totalProductivityBonus += instance.GetProductionBonus(structureData);
                }
            }

            // Calcola produzione effettiva
            currentProductionRate = structureData.GetProductionAtLevel(currentLevel) * totalProductivityBonus;

            Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} production rate: {currentProductionRate:F1}/min (bonus: {totalProductivityBonus:F2}x)");
        }

        /// <summary>
        /// Calcola velocità di costruzione da worker Builder assegnati
        /// </summary>
        public void RecalculateBuildSpeed()
        {
            currentBuildSpeed = 1f;

            // Somma i bonus di costruzione da tutti i worker instance assegnati
            foreach (var instance in assignedWorkerInstances)
            {
                if (instance != null)
                {
                    currentBuildSpeed += (instance.GetConstructionBonus() - 1f);
                }
            }

            Debug.Log($"<color=orange>[Structure]</color> {structureData.DisplayName} build speed: {currentBuildSpeed:F2}x ({assignedWorkerInstances.Count} workers)");
        }

        // ============================================
        // PRODUZIONE RISORSE
        // ============================================

        private void UpdateProduction()
        {
            if (!isOperational) return;
            if (structureData.Category != StructureCategory.Resource) return;
            if (string.IsNullOrEmpty(structureData.ProducesResourceId)) return;
            if (workerCount == 0) return; // Serve almeno 1 worker

            productionTimer += Time.deltaTime;

            if (productionTimer >= productionTickInterval)
            {
                ProduceResources();
                productionTimer = 0f;
            }
        }

        private void ProduceResources()
        {
            // Calcola quanto produrre in questo tick (1 secondo)
            float amountPerSecond = currentProductionRate / 60f; // Rate è per minuto
            productionAccumulator += amountPerSecond;

            // Se accumulato >= 1, aggiungi risorsa
            if (productionAccumulator >= 1f)
            {
                int amountToAdd = Mathf.FloorToInt(productionAccumulator);
                productionAccumulator -= amountToAdd;

                // Aggiungi al ResourceSystem
                if (ResourceSystem.Instance != null)
                {
                    ResourceSystem.Instance.AddResource(structureData.ProducesResourceId, amountToAdd);
                }

                Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} produced {amountToAdd}x {structureData.ProducesResourceId}");
            }
        }

        /// <summary>
        /// Chiamato da StructureSystem per tick di produzione globale
        /// </summary>
        public void TickProduction(float deltaTime)
        {
            if (!isOperational) return;
            if (structureData.Category != StructureCategory.Resource) return;
            if (workerCount == 0) return;

            // Produzione basata su deltaTime
            float amountPerSecond = currentProductionRate / 60f;
            float amountThisTick = amountPerSecond * deltaTime;

            if (ResourceSystem.Instance != null)
            {
                ResourceSystem.Instance.AddResource(structureData.ProducesResourceId, amountThisTick);
            }
        }

        // ============================================
        // HEALTH & COMBAT
        // ============================================

        /// <summary>
        /// Infliggi danno a questa struttura
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            // Calcola danno con armor
            float actualDamage = Mathf.Max(0, damage - structureData.Armor);
            currentHealth -= actualDamage;

            Debug.Log($"<color=red>[Structure]</color> {structureData.DisplayName} took {actualDamage:F1} damage ({currentHealth:F0}/{maxHealth:F0} HP)");

            // Soglia damaged
            float healthPercent = currentHealth / maxHealth;
            if (healthPercent <= 0.3f && currentState != StructureState.Damaged)
            {
                ChangeState(StructureState.Damaged);
            }

            // Morte
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Ripara questa struttura
        /// </summary>
        public void Repair(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

            Debug.Log($"<color=green>[Structure]</color> {structureData.DisplayName} repaired {amount:F1} HP ({currentHealth:F0}/{maxHealth:F0})");

            // Ripristina stato Operating se riparato sopra 30%
            float healthPercent = currentHealth / maxHealth;
            if (healthPercent > 0.3f && currentState == StructureState.Damaged)
            {
                ChangeState(StructureState.Operating);
            }
        }

        private void Die()
        {
            Debug.Log($"<color=red>[Structure]</color> {structureData.DisplayName} destroyed!");

            ChangeState(StructureState.Destroyed);

            // Rilascia tutti i worker
            ReleaseAllWorkers();

            // Spawna VFX distruzione
            SpawnVFX("destruction");

            // Notifica StructureSystem
            // StructureSystem.Instance?.OnStructureDestroyed(this);

            // Distruggi dopo delay
            Destroy(gameObject, 2f);
        }

        private void OnStructureDestroyed()
        {
            // Callback per eventi custom
        }

        private void ReleaseAllWorkers()
        {
            foreach (var worker in assignedWorkers)
            {
                if (worker != null)
                {
                    // WorkerSystem.Instance?.UnassignWorker(worker);
                }
            }
            foreach (var hero in assignedHeroes)
            {
                if (hero != null)
                {
                    // WorkerSystem.Instance?.UnassignWorker(hero);
                }
            }
            assignedWorkers.Clear();
            assignedHeroes.Clear();
            buildersAssigned.Clear();
            workerCount = 0;
        }

        // ============================================
        // UPGRADE
        // ============================================

        /// <summary>
        /// Upgrade struttura al livello successivo
        /// </summary>
        public bool TryUpgrade()
        {
            if (currentLevel >= structureData.MaxLevel)
            {
                Debug.LogWarning($"[Structure] {structureData.DisplayName} already at max level!");
                return false;
            }

            if (!isOperational)
            {
                Debug.LogWarning($"[Structure] {structureData.DisplayName} must be operational to upgrade!");
                return false;
            }

            // Verifica costi (gestito da StructureSystem)
            currentLevel++;
            maxHealth = structureData.MaxHealth * currentLevel; // Scala HP con livello
            currentHealth = maxHealth; // Ripristina full HP

            RecalculateProduction();

            Debug.Log($"<color=green>[Structure]</color> {structureData.DisplayName} upgraded to level {currentLevel}!");
            SpawnVFX("completion");

            return true;
        }

        // ============================================
        // VISUALS & VFX
        // ============================================

        private void CacheOriginalMaterials()
        {
            if (renderers == null || renderers.Length == 0) return;

            List<Material> mats = new List<Material>();
            foreach (var r in renderers)
            {
                mats.AddRange(r.materials);
            }
            originalMaterials = mats.ToArray();
        }

        private void ApplyConstructionVisuals()
        {
            // TODO: Applica materiale costruzione
            SpawnVFX("construction");
        }

        private void ApplyNormalVisuals()
        {
            // TODO: Ripristina materiali originali
            DestroyCurrentVFX();
        }

        private void ApplyDamagedVisuals()
        {
            // TODO: Applica materiale danneggiato
        }

        private void SpawnVFX(string vfxType)
        {
            GameObject vfxPrefab = null;

            switch (vfxType)
            {
                case "construction":
                    // vfxPrefab = structureData.ConstructionVFX;
                    break;
                case "completion":
                    // vfxPrefab = structureData.CompletionVFX;
                    break;
                case "destruction":
                    // vfxPrefab = structureData.DestructionVFX;
                    break;
            }

            if (vfxPrefab != null && vfxSpawnPoint != null)
            {
                currentVFX = Instantiate(vfxPrefab, vfxSpawnPoint.position, Quaternion.identity, vfxSpawnPoint);
            }
        }

        private void DestroyCurrentVFX()
        {
            if (currentVFX != null)
            {
                Destroy(currentVFX);
            }
        }

        // [MODIFICATION 3] Added Worker Instance Management Methods
        // ============================================
        // WORKER INSTANCE MANAGEMENT (for UI Assignment)
        // ============================================

        /// <summary>
        /// Verifica se c'è uno slot worker libero
        /// </summary>
        public bool HasFreeWorkerSlot()
        {
            if (structureData == null) return false;
            return assignedWorkerInstances.Count < structureData.WorkerSlots;
        }

        /// <summary>
        /// Aggiunge un WorkerInstance a questa struttura
        /// </summary>
        public bool AssignWorker(Workers.WorkerInstance worker)
        {
            if (worker == null) return false;
            if (!HasFreeWorkerSlot()) return false;
            if (assignedWorkerInstances.Contains(worker)) return false;

            assignedWorkerInstances.Add(worker);
            workerCount = assignedWorkers.Count + assignedWorkerInstances.Count;

            // Link bidirezionale
            worker.AssignTo(this);

            // Ricalcola produzione o costruzione a seconda dello stato
            if (currentState == StructureState.Operating)
            {
                RecalculateProduction();
            }
            else if (currentState == StructureState.Building)
            {
                RecalculateBuildSpeed();
            }

            Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} now has {assignedWorkerInstances.Count} worker instances");
            return true;
        }

        /// <summary>
        /// Rimuove un WorkerInstance da questa struttura
        /// </summary>
        public void RemoveWorker(Workers.WorkerInstance worker)
        {
            if (worker == null) return;
            
            if (assignedWorkerInstances.Contains(worker))
            {
                assignedWorkerInstances.Remove(worker);
                workerCount = assignedWorkers.Count + assignedWorkerInstances.Count;

                // Unlink bidirezionale
                worker.Unassign();

                // Ricalcola produzione o costruzione a seconda dello stato
                if (currentState == StructureState.Operating)
                {
                    RecalculateProduction();
                }
                else if (currentState == StructureState.Building)
                {
                    RecalculateBuildSpeed();
                }

                Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} now has {assignedWorkerInstances.Count} worker instances");
            }
        }

        /// <summary>
        /// Ottiene tutti i WorkerInstance assegnati
        /// </summary>
        public List<Workers.WorkerInstance> GetAssignedWorkerInstances()
        {
            return new List<Workers.WorkerInstance>(assignedWorkerInstances);
        }

        /// <summary>
        /// Calcola il bonus totale dei worker instance assegnati
        /// </summary>
        public float GetTotalWorkerBonus()
        {
            float totalBonus = 0f;
            foreach (var worker in assignedWorkerInstances)
            {
                if (worker != null && worker.Data != null)
                {
                    totalBonus += worker.GetCurrentBonus();
                }
            }
            return totalBonus;
        }

        /// <summary>
        /// Chiamato quando si clicca sulla struttura (per aprire UI assignment)
        /// </summary>
        public void OnClick()
        {
            // Apre il panel di assegnazione worker
            if (UI.WorkerAssignmentUI.Instance != null)
            {
                UI.WorkerAssignmentUI.Instance.OpenForStructure(this);
                Debug.Log($"<color=green>[Structure]</color> Opened worker assignment UI for {structureData.DisplayName}");
            }
            else
            {
                Debug.LogWarning("[Structure] WorkerAssignmentUI not found in scene!");
            }
        }

        // ============================================
        // DEBUG & ODIN
        // ============================================

#if UNITY_EDITOR
        private Color GetHealthColor()
        {
            if (maxHealth == 0) return Color.gray;
            float percent = currentHealth / maxHealth;
            if (percent > 0.6f) return Color.green;
            if (percent > 0.3f) return Color.yellow;
            return Color.red;
        }

        private Color GetWorkerSlotColor()
        {
            if (structureData == null) return Color.gray;
            float percent = (float)workerCount / structureData.WorkerSlots;
            if (percent >= 1f) return Color.green;
            if (percent >= 0.5f) return Color.yellow;
            return Color.red;
        }

        private Color GetConstructionColor()
        {
            if (buildProgress >= 1f) return Color.green;
            if (buildProgress >= 0.5f) return Color.yellow;
            return Color.red;
        }

        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("💥 Take 50 Damage", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugTakeDamage()
        {
            TakeDamage(50f);
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("🔧 Repair 100 HP", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void DebugRepair()
        {
            Repair(100f);
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("⬆️ Upgrade", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void DebugUpgrade()
        {
            TryUpgrade();
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("✅ Complete Construction", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.5f)]
        [ShowIf("@currentState == StructureState.Building")]
        private void DebugCompleteConstruction()
        {
            buildProgress = 1f;
            CompleteConstruction();
        }

        private void OnDrawGizmos()
        {
            if (structureData == null) return;

            // Disegna griglia
            Gizmos.color = Color.yellow;
            Vector3 size = new Vector3(structureData.GridSize.x, 0.1f, structureData.GridSize.y);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, size);

            // Disegna range (per torri difensive)
            if (structureData.Category == StructureCategory.Defense)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, structureData.AttackRange);
            }

            // Disegna worker assegnati
            Gizmos.color = Color.cyan;
            foreach (var worker in assignedWorkers)
            {
                if (worker != null)
                {
                    Gizmos.DrawLine(transform.position + Vector3.up, worker.transform.position + Vector3.up);
                }
            }
        }

        [Button("📊 Print Full Stats", ButtonSizes.Large)]
        [TitleGroup("Debug")]
        private void DebugPrintStats()
        {
            Debug.Log($"=== {structureData.DisplayName} Stats ===\n" +
                $"State: {currentState}\n" +
                $"Level: {currentLevel}/{structureData.MaxLevel}\n" +
                $"Health: {currentHealth:F0}/{maxHealth:F0}\n" +
                $"Workers: {workerCount}/{structureData.WorkerSlots}\n" +
                $"Production Rate: {currentProductionRate:F1}/min\n" +
                $"Productivity Bonus: {totalProductivityBonus:F2}x\n" +
                $"Build Progress: {buildProgress:P0}");
        }
#endif
    }

    // ============================================
    // ENUMS
    // ============================================

    public enum StructureState
    {
        Idle,
        Building,
        Operating,
        Damaged,
        Destroyed
    }
}