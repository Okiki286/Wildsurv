using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Core.Navigation;
using WildernessSurvival.Gameplay.Combat;
using WildernessSurvival.Gameplay.Enemies;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.UI;

namespace WildernessSurvival.Gameplay.Structures
{
    public enum ConstructionVisualMode
    {
        None,
        OverlayFlatten,
        GrowFromFlat
    }

    public class StructureController : MonoBehaviour, IDamageable
    {
        // ============================================
        // RIFERIMENTI
        // ============================================

        [TitleGroup("Setup")]
        [Required("StructureData è richiesto!")]
        [AssetsOnly]
        [SerializeField] private StructureData structureData;

        /// <summary>
        /// Flag per indicare se questa è una preview (build mode) e non una struttura reale
        /// </summary>
        [HideInInspector]
        public bool IsPreview = false;

        // ============================================
        // GRID PLACEMENT DATA (per selezione grid-based)
        // ============================================

        /// <summary>
        /// Cella di origine (bottom-left) dove è stata piazzata la struttura.
        /// Usata per calcolare tutte le celle occupate dal footprint.
        /// </summary>
        [HideInInspector]
        public Vector2Int OriginCell { get; private set; }

        /// <summary>
        /// Step di rotazione (0-3) al momento del piazzamento.
        /// 0=0°, 1=90°, 2=180°, 3=270°
        /// </summary>
        [HideInInspector]
        public int PlacementRotationStep { get; private set; }

        [BoxGroup("Setup/Components")]
        [ChildGameObjectsOnly]
        [SerializeField] private Transform visualRoot;

        [BoxGroup("Setup/Components")]
        [ChildGameObjectsOnly]
        [SerializeField] private Transform vfxSpawnPoint;

        // ============================================
        // STATO RUNTIME
        // ============================================

        [TitleGroup("Runtime State")]
        [BoxGroup("Runtime State/Core")]
        [ReadOnly, ShowInInspector, EnumToggleButtons]
        private StructureState currentState = StructureState.Idle;

        [BoxGroup("Runtime State/Core")]
        [ReadOnly, ShowInInspector, PropertyRange(1, 5)]
        private int currentLevel = 1;

        [BoxGroup("Runtime State/Core")]
        [ReadOnly, ShowInInspector]
        private bool isOperational = false;

        // FIX2: Guard per evitare doppia inizializzazione
        private bool _initialized = false;

        // ============================================
        // HEALTH SYSTEM
        // ============================================

        [TitleGroup("Health")]
        [BoxGroup("Health/Stats")]
        [ReadOnly, ShowInInspector]
        [ProgressBar(0, "maxHealth", ColorGetter = "GetHealthColor")]
        private float currentHealth;

        [BoxGroup("Health/Stats")]
        [ReadOnly, ShowInInspector]
        private float maxHealth;

        // ============================================
        // WORKER ASSIGNMENT
        // ============================================

        [TitleGroup("Workers")]
        [BoxGroup("Workers/Assigned")]
        [ReadOnly, ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<WorkerController> assignedWorkers = new List<WorkerController>();

        [BoxGroup("Workers/Assigned")]
        [ReadOnly, ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<WorkerController> assignedHeroes = new List<WorkerController>();

        [BoxGroup("Workers/Assigned")]
        [ReadOnly, ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<WorkerInstance> assignedWorkerInstances = new List<WorkerInstance>();

        [BoxGroup("Workers/Stats")]
        [ReadOnly, ShowInInspector]
        [ProgressBar(0, "structureData.WorkerSlots", ColorGetter = "GetWorkerSlotColor")]
        private int workerCount = 0;

        [BoxGroup("Workers/Building")]
        [ShowInInspector, ReadOnly]
        public WorkerInstance CurrentBuilder { get; private set; }

        [BoxGroup("Workers/Building")]
        [SerializeField]
        [Tooltip("Distanza dal centro a cui i worker si fermano per lavorare (raggio sul piano XZ)")]
        [PropertyRange(0.5f, 10f)]
        private float workRadius = 1.5f;

        [BoxGroup("Workers/Building")]
        [SerializeField]
        [Tooltip("Punti preferiti dove i worker si posizionano per lavorare (opzionale)")]
        private Transform[] workSpots;

        [BoxGroup("Workers/Approach Slots")]
        [SerializeField]
        [Tooltip("Optional: ApproachSlots component for managing work positions. Auto-created if null.")]
        private ApproachSlots workSlots;

        [BoxGroup("Workers/Approach Slots")]
        [SerializeField]
        [Tooltip("Auto-create ApproachSlots at runtime if not assigned")]
        private bool autoCreateWorkSlots = true;

        [BoxGroup("Workers/Approach Slots")]
        [SerializeField]
        [Range(2, 8)]
        [Tooltip("Number of work slots to auto-create")]
        private int autoWorkSlotCount = 4;

        [BoxGroup("Workers/Stats")]
        [ReadOnly, ShowInInspector]
        [PropertyRange(0f, 3f)]
        private float totalProductivityBonus = 1f;

        // ============================================
        // PRODUZIONE
        // ============================================

        [TitleGroup("Production")]
        [BoxGroup("Production/Stats")]
        [ReadOnly, ShowInInspector]
        [ShowIf("@structureData != null && structureData.Category == StructureCategory.Resource")]
        private float currentProductionRate = 0f;

        [BoxGroup("Production/Stats")]
        [ReadOnly, ShowInInspector]
        [ShowIf("@structureData != null && structureData.Category == StructureCategory.Resource")]
        private float productionAccumulator = 0f;

        private float productionTickInterval = 1f;
        private float productionTimer = 0f;

        // ============================================
        // COSTRUZIONE
        // ============================================

        [TitleGroup("Construction")]
        [BoxGroup("Construction/Progress")]
        [ReadOnly, ShowInInspector]
        [ProgressBar(0, 1, ColorGetter = "GetConstructionColor")]
        private float buildProgress = 0f;

        [BoxGroup("Construction/Progress")]
        [ReadOnly, ShowInInspector]
        private float buildTimeRemaining = 0f;

        private List<WorkerController> buildersAssigned = new List<WorkerController>();
        private float currentBuildSpeed = 1f;

        // ============================================
        // VFX & VISUALS
        // ============================================

        private GameObject currentVFX;
        private Renderer[] renderers;
        private Material[] originalMaterials;

        // ============================================
        // CONSTRUCTION VISUAL
        // ============================================

        [Header("Construction Visual Mode")]
        [SerializeField]
        private ConstructionVisualMode constructionVisualMode = ConstructionVisualMode.GrowFromFlat;

        [SerializeField, Min(0.01f)]
        private float flatStartYScale = 0.05f;

        [Header("Construction Overlay (OverlayFlatten Mode)")]
        [SerializeField]
        private Transform constructionOverlayRoot;

        [SerializeField]
        private float overlayDarken = 0.35f;

        [SerializeField]
        private float overlayDesaturate = 0.75f;

        private MaterialPropertyBlock overlayMPB;
        private float totalHeight;
        private float baseHeight;
        private bool overlayInitialized;

        private Vector3 initialVisualRootLocalPos;
        private Vector3 initialVisualRootLocalScale;
        private bool growFromFlatInitialized;

        private float lastAppliedProgress = -1f;
        private int lastAppliedFrame = -1;

        // Anti double-apply centering flag (runtime)
        private bool runtimeCenteringApplied = false;

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
        public float BuildProgress => buildProgress;
        public int WorkerCount => workerCount;
        public List<WorkerController> AssignedWorkers => assignedWorkers;
        public int AssignedWorkerInstanceCount => assignedWorkerInstances.Count;

        /// <summary>
        /// Distanza dal centro a cui i worker si fermano per lavorare
        /// </summary>
        public float WorkRadius
        {
            get => workRadius;
            set => workRadius = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// The ApproachSlots component for work positions, if available.
        /// </summary>
        public ApproachSlots WorkSlots => workSlots;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            // FIX2: Awake NON deve chiamare InitializeStructure()
            // L'inizializzazione avviene in:
            // - Initialize() chiamato da SpawnStructure (spawn runtime)
            // - Start() per strutture piazzate manualmente in scena

            // Se è una preview (build mode), non fare nulla
            if (IsPreview)
            {
                return;
            }

            InitializeWorkSlots();
        }

        private void InitializeWorkSlots()
        {
            // Try to find existing ApproachSlots if not assigned
            if (workSlots == null)
            {
                workSlots = GetComponent<ApproachSlots>();
            }

            // Auto-create if enabled and still null
            if (workSlots == null && autoCreateWorkSlots)
            {
                workSlots = gameObject.AddComponent<ApproachSlots>();
                workSlots.Initialize(autoWorkSlotCount, workRadius);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=cyan>[StructureController]</color> {name}: Auto-created ApproachSlots with {autoWorkSlotCount} work slots at radius {workRadius}");
#endif
            }
        }

        private void Start()
        {
            // FIX2: Per strutture piazzate manualmente in scena (non via SpawnStructure)
            // Se non ancora inizializzate ma hanno structureData, inizializza ora
            if (!_initialized && !IsPreview && structureData != null)
            {
                InitializeStructure();
            }

            // Abilita StructureStatusUI (era disabilitato nel prefab per evitare che apparisse durante il piazzamento)
            var statusUI = GetComponent<StructureStatusUI>();
            if (statusUI != null)
            {
                statusUI.enabled = true;
            }

            if (_initialized && structureData != null)
            {
                Debug.Log($"<color=orange>[Structure]</color> {structureData.DisplayName} initialized at {transform.position}");
            }
        }

        private void Update()
        {
            if (!IsAlive) return;

            // FIX2: Se WorkerSystem esiste, esso gestisce tick di costruzione/produzione
            // Questo evita doppi/tripli tick
            if (WorkerSystem.Instance != null)
            {
                return;
            }

            UpdateState();
            UpdateProduction();
        }

        private void OnDestroy()
        {
            // Cleanup: unregister from Foreman if needed
            if (WorkerSystem.Instance != null)
            {
                WorkerSystem.Instance.UnregisterStructureNeedingBuilders(this);
            }

            // FIX3: Libera celle nella occupancy grid quando la struttura viene distrutta
            StructureSystem.Instance?.FreeArea(this);

            // Clear base center se questa struttura era il center
            if (structureData != null && structureData.IsBaseCenter)
            {
                BaseCenterSystem.Instance?.ClearCenterIfMatch(transform);
            }
        }

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        private void InitializeStructure()
        {
            // FIX2: Guard per evitare doppia inizializzazione
            if (_initialized) return;

            maxHealth = structureData.MaxHealth;
            currentHealth = maxHealth;
            currentLevel = 1;

            if (visualRoot != null)
            {
                renderers = visualRoot.GetComponentsInChildren<Renderer>();
                CacheOriginalMaterials();
            }

            if (vfxSpawnPoint == null)
            {
                vfxSpawnPoint = transform;
            }

            if (structureData.RequiresBuilder)
            {
                // FIX3: ChangeState(Building) chiama OnStateChanged che già chiama ApplyConstructionVisuals()
                // NON chiamare ApplyConstructionVisuals() qui - sarebbe duplicato
                ChangeState(StructureState.Building);
                buildTimeRemaining = structureData.BuildTime;
                buildProgress = 0f;
                isOperational = false;
                // ApplyConstructionVisuals(); // RIMOSSO - già chiamata da OnStateChanged(Building)
            }
            else
            {
                ChangeState(StructureState.Operating);
                buildProgress = 1f;
                isOperational = true;
                CompleteConstruction();
            }

            // FIX2: Marca come inizializzato
            _initialized = true;
        }

        public void Initialize(StructureData data, int level = 1)
        {
            // FIX2: Guard per evitare doppia inizializzazione
            if (_initialized)
            {
                Debug.LogWarning($"[StructureController] {gameObject.name} already initialized, skipping.");
                return;
            }

            structureData = data;
            currentLevel = level;

            if (structureData.BuildTime <= 0f)
            {
                Debug.LogWarning($"<color=yellow>[Structure]</color> {structureData.DisplayName} has BuildTime <= 0!");
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
                case StructureState.Damaged:
                case StructureState.Destroyed:
                    break;
            }
        }

        private void ChangeState(StructureState newState)
        {
            if (currentState == newState) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=orange>[Structure]</color> {structureData.DisplayName}: {currentState} → {newState}");
#endif

            StructureState previousState = currentState;
            currentState = newState;

            // Notifica StructureSystem del cambio stato per aggiornare i contatori
            StructureSystem.Instance?.NotifyStructureStateChanged(previousState, newState);

            OnStateChanged(newState, previousState);
        }

        private void OnStateChanged(StructureState newState, StructureState previousState)
        {
            switch (newState)
            {
                case StructureState.Building:
                    isOperational = false;
                    ApplyConstructionVisuals();

                    // Notify Foreman: structure needs builders (event-driven)
                    if (structureData.RequiresBuilder && WorkerSystem.Instance != null)
                    {
                        WorkerSystem.Instance.RegisterStructureNeedingBuilders(this);
                    }

                    // Se è base center, disabilita aura durante costruzione
                    if (structureData != null && structureData.IsBaseCenter)
                    {
                        EnableWaystoneAura(false);
                    }
                    break;

                case StructureState.Operating:
                    isOperational = true;
                    ApplyNormalVisuals();
                    RecalculateProduction();

                    // Notify Foreman: structure no longer needs builders (event-driven)
                    if (previousState == StructureState.Building && WorkerSystem.Instance != null)
                    {
                        WorkerSystem.Instance.UnregisterStructureNeedingBuilders(this);
                    }

                    // Se è base center (Waystone), registra come centro
                    if (structureData != null && structureData.IsBaseCenter)
                    {
                        BaseCenterSystem.Instance?.SetCenter(transform);
                        EnableWaystoneAura(true);
                    }
                    break;

                case StructureState.Damaged:
                    ApplyDamagedVisuals();
                    break;

                case StructureState.Destroyed:
                    isOperational = false;

                    // Notify Foreman: structure destroyed, no longer needs builders (event-driven)
                    if (WorkerSystem.Instance != null)
                    {
                        WorkerSystem.Instance.UnregisterStructureNeedingBuilders(this);
                    }
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

            float buildSpeed = 1f;

            if (structureData.RequiresBuilder && buildersAssigned.Count > 0)
            {
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
                buildSpeed = 1f;
            }
            else
            {
                return;
            }

            float progressDelta = (buildSpeed / structureData.BuildTime) * Time.deltaTime;
            buildProgress += progressDelta;
            buildTimeRemaining -= Time.deltaTime * buildSpeed;

            UpdateConstructionProgressVisual(buildProgress);

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

            SpawnVFX("completion");

            var workersToRelease = new List<WorkerInstance>(assignedWorkerInstances);
            int releasedCount = 0;

            foreach (var worker in workersToRelease)
            {
                if (worker != null && WorkerSystem.Instance != null)
                {
                    WorkerSystem.Instance.UnassignWorker(worker);
                    releasedCount++;
                }
            }

            assignedWorkerInstances.Clear();
            CurrentBuilder = null;
            workerCount = 0;

            Debug.Log($"<color=green>[Structure]</color> Construction complete. Released {releasedCount} builders.");

            ChangeState(StructureState.Operating);
            ReleaseAllBuilders();
            RecalculateProduction();
        }

        public void TickConstruction(float deltaTime)
        {
            if (buildProgress >= 1f) return;
            if (currentState != StructureState.Building) return;

            if (structureData.RequiresBuilder && assignedWorkerInstances.Count == 0)
            {
                return;
            }

            // [REMOVED] RecalculateBuildSpeed() - ora event-driven (chiamato da OnWorkerArrivedAtSite)

            float progressDelta = (currentBuildSpeed / structureData.BuildTime) * deltaTime;
            buildProgress += progressDelta;

            UpdateConstructionProgressVisual(buildProgress);

            if (currentBuildSpeed > 0)
            {
                buildTimeRemaining = Mathf.Max(0, structureData.BuildTime * (1f - buildProgress) / currentBuildSpeed);
            }

            if (buildProgress >= 1f)
            {
                CompleteConstruction();
            }
        }

        public bool AssignBuilder(WorkerController worker)
        {
            if (worker == null) return false;
            if (buildersAssigned.Contains(worker)) return false;
            if (buildProgress >= 1f) return false;

            buildersAssigned.Add(worker);
            Debug.Log($"<color=orange>[Structure]</color> Builder {worker.Data.DisplayName} assigned to {structureData.DisplayName}");
            return true;
        }

        public void RemoveBuilder(WorkerController worker)
        {
            buildersAssigned.Remove(worker);
        }

        private void ReleaseAllBuilders()
        {
            buildersAssigned.Clear();
        }

        // ============================================
        // WORKER ASSIGNMENT (Legacy/Physical)
        // ============================================

        public bool AssignWorker(WorkerController worker, bool isHero = false)
        {
            if (worker == null) return false;
            if (!isOperational) return false;

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

        public void UnassignWorker(WorkerController worker)
        {
            if (worker == null) return;

            assignedWorkers.Remove(worker);
            assignedHeroes.Remove(worker);
            workerCount = assignedWorkers.Count + assignedHeroes.Count;

            RecalculateProduction();

            Debug.Log($"<color=orange>[Structure]</color> {worker.Data.DisplayName} unassigned from {structureData.DisplayName}");
        }

        // ============================================
        // PRODUCTION & BUILD SPEED CALCULATION
        // ============================================

        public void RecalculateProduction()
        {
            if (structureData.Category != StructureCategory.Resource) return;

            totalProductivityBonus = 1f;
            int workersAtSite = 0;

            foreach (var instance in assignedWorkerInstances)
            {
                if (instance != null && instance.IsAtWorksite)
                {
                    totalProductivityBonus += instance.GetProductionBonus(structureData);
                    workersAtSite++;
                }
            }

            currentProductionRate = structureData.GetProductionAtLevel(currentLevel) * totalProductivityBonus;

            // Log solo quando cambia significativamente (evita spam)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} production rate: {currentProductionRate:F1}/min ({workersAtSite}/{assignedWorkerInstances.Count} at site)");
#endif
        }

        public void RecalculateBuildSpeed()
        {
            float previousSpeed = currentBuildSpeed;
            currentBuildSpeed = 0f;
            int buildersAtSite = 0;

            // ════════════════════════════════════════════════════════════════════
            // [GHOST FIX SIMPLIFIED] Only rely on IsAtWorksite flag.
            // The IsSheltered check in WorkerSystem prevents sheltered workers
            // from being assigned, preventing ghost construction.
            // Physical distance check removed - it was comparing to center pivot,
            // not to the worker's assigned slot position.
            // ════════════════════════════════════════════════════════════════════

            foreach (var instance in assignedWorkerInstances)
            {
                if (instance == null) continue;
                if (!instance.IsAtWorksite) continue;

                // Safety: Skip sheltered workers (belt-and-suspenders)
                if (instance.IsSheltered) continue;

                currentBuildSpeed += instance.GetConstructionBonus();
                buildersAtSite++;
            }

            if (buildersAtSite == 0)
            {
                currentBuildSpeed = 0f;
            }

            // Log solo quando cambia (evita spam ogni tick)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Mathf.Abs(currentBuildSpeed - previousSpeed) > 0.01f)
            {
                Debug.Log($"<color=orange>[Structure]</color> {structureData.DisplayName} build speed: {currentBuildSpeed:F2}x ({buildersAtSite}/{assignedWorkerInstances.Count} at site)");
            }
#endif
        }

        // ============================================
        // EVENT-DRIVEN RECALCULATION (MOBILE OPTIMIZATION)
        // ============================================

        /// <summary>
        /// [NEW] Called when a worker arrives/departs from worksite.
        /// Triggers reactive recalculation instead of per-frame polling.
        /// </summary>
        public void OnWorkerArrivedAtSite()
        {
            if (currentState == StructureState.Building)
            {
                RecalculateBuildSpeed();
            }
            else if (currentState == StructureState.Operating)
            {
                RecalculateProduction();
            }
        }

        /// <summary>
        /// [NEW] Called when a worker departs from worksite.
        /// </summary>
        public void OnWorkerDepartedFromSite()
        {
            OnWorkerArrivedAtSite(); // Same recalculation logic

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Telemetry: count workers still at site
            int workersAtSite = 0;
            foreach (var instance in assignedWorkerInstances)
            {
                if (instance != null && instance.IsAtWorksite)
                    workersAtSite++;
            }

            string metric = currentState == StructureState.Building 
                ? $"buildSpeed={currentBuildSpeed:F2}x" 
                : $"prodRate={currentProductionRate:F1}/min";

            Debug.Log($"<color=magenta>[StructureController]</color> {structureData.DisplayName} " +
                $"OnWorkerDeparted: workersAtSite={workersAtSite}/{assignedWorkerInstances.Count}, {metric}");
#endif
        }

        // ============================================
        // PRODUZIONE RISORSE
        // ============================================

        private void UpdateProduction()
        {
            if (!isOperational) return;
            if (structureData.Category != StructureCategory.Resource) return;
            if (string.IsNullOrEmpty(structureData.ProducesResourceId)) return;
            if (workerCount == 0) return;

            productionTimer += Time.deltaTime;

            if (productionTimer >= productionTickInterval)
            {
                ProduceResources();
                productionTimer = 0f;
            }
        }

        private void ProduceResources()
        {
            float amountPerSecond = currentProductionRate / 60f;
            productionAccumulator += amountPerSecond;

            if (productionAccumulator >= 1f)
            {
                int amountToAdd = Mathf.FloorToInt(productionAccumulator);
                productionAccumulator -= amountToAdd;

                if (ResourceSystem.Instance != null)
                {
                    // [ECONOMY FEEDBACK] Usa nuovo overload con worldPos e sourceTag
                    ResourceSystem.Instance.AddResource(
                        structureData.ProducesResourceId,
                        amountToAdd,
                        transform.position,
                        ResourceSourceTags.Production
                    );
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} produced {amountToAdd}x {structureData.ProducesResourceId}");
#endif
            }
        }

        public void TickProduction(float deltaTime)
        {
            if (!isOperational) return;
            if (structureData.Category != StructureCategory.Resource) return;
            if (workerCount == 0) return;

            float amountPerSecond = currentProductionRate / 60f;
            float amountThisTick = amountPerSecond * deltaTime;

            if (ResourceSystem.Instance != null)
            {
                // [ECONOMY FEEDBACK] Usa nuovo overload con worldPos e sourceTag
                // Nota: per tick frazionari potrebbe non emettere eventi (delta < 1)
                ResourceSystem.Instance.AddResource(
                    structureData.ProducesResourceId,
                    amountThisTick,
                    transform.position,
                    ResourceSourceTags.Production
                );
            }
        }

        // ============================================
        // HEALTH & COMBAT (IDamageable)
        // ============================================

        // IDamageable explicit implementation
        float IDamageable.CurrentHealth => currentHealth;
        float IDamageable.MaxHealth => maxHealth;

        /// <summary>
        /// Applica danno alla struttura (legacy, senza tipo danno)
        /// </summary>
        public void TakeDamage(float damage)
        {
            TakeDamage(damage, DamageType.None);
        }

        /// <summary>
        /// Applica danno alla struttura con tipo danno (IDamageable)
        /// </summary>
        public void TakeDamage(float damage, DamageType damageType)
        {
            if (!IsAlive) return;

            // Per ora ignora damageType - strutture non hanno resistenze
            float actualDamage = Mathf.Max(0, damage - structureData.Armor);
            currentHealth -= actualDamage;

            Debug.Log($"<color=red>[Structure]</color> {structureData.DisplayName} took {actualDamage:F1} damage ({currentHealth:F0}/{maxHealth:F0} HP)");

            float healthPercent = currentHealth / maxHealth;
            if (healthPercent <= 0.3f && currentState != StructureState.Damaged)
            {
                ChangeState(StructureState.Damaged);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Repair(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

            Debug.Log($"<color=green>[Structure]</color> {structureData.DisplayName} repaired {amount:F1} HP ({currentHealth:F0}/{maxHealth:F0})");

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
            ReleaseAllWorkers();
            SpawnVFX("destruction");
            Destroy(gameObject, 2f);
        }

        private void OnStructureDestroyed() { }

        private void ReleaseAllWorkers()
        {
            // ════════════════════════════════════════════════════════════════════
            // PROPER UNASSIGNMENT: Notify WorkerSystem for each assigned worker
            // This ensures workers reset to Idle/Villager and become available
            // ════════════════════════════════════════════════════════════════════
            if (WorkerSystem.Instance != null)
            {
                // 1. Unassign all regular workers
                foreach (var worker in assignedWorkerInstances.ToArray()) // ToArray to avoid concurrent modification
                {
                    if (worker != null)
                    {
                        WorkerSystem.Instance.UnassignWorker(worker);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"<color=orange>[Structure]</color> {worker.CustomName} unassigned because {structureData?.DisplayName} destroyed -> default job restored");
#endif
                    }
                }

                // 2. Unassign current builder (special case for Building state)
                if (CurrentBuilder != null)
                {
                    WorkerSystem.Instance.UnassignWorker(CurrentBuilder);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"<color=orange>[Structure]</color> {CurrentBuilder.CustomName} (builder) unassigned because {structureData?.DisplayName} destroyed -> default job restored");
#endif
                }
            }

            // Safety clear of all lists
            assignedWorkers.Clear();
            assignedHeroes.Clear();
            buildersAssigned.Clear();
            assignedWorkerInstances.Clear();
            CurrentBuilder = null;
            workerCount = 0;
        }

        // ============================================
        // WAYSTONE AURA (Base Center)
        // ============================================

        /// <summary>
        /// Abilita/disabilita l'aura del Waystone (WaystoneDebuffAura).
        /// Chiamato quando la struttura diventa operativa o viene distrutta.
        /// </summary>
        private void EnableWaystoneAura(bool enable)
        {
            // Cerca WaystoneDebuffAura nei children
            var aura = GetComponentInChildren<WaystoneDebuffAura>(true);
            if (aura != null)
            {
                aura.enabled = enable;

                // Attiva/disattiva anche il gameObject se necessario
                if (aura.gameObject != gameObject)
                {
                    aura.gameObject.SetActive(enable);
                }

                Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} aura {(enable ? "enabled" : "disabled")}");
            }
        }

        // ============================================
        // UPGRADE
        // ============================================

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

            currentLevel++;
            maxHealth = structureData.MaxHealth * currentLevel;
            currentHealth = maxHealth;

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
            SpawnVFX("construction");

            lastAppliedProgress = -1f;
            lastAppliedFrame = -1;

            if (constructionVisualMode == ConstructionVisualMode.GrowFromFlat)
            {
                // =================================================================================
                // [MODIFICA] Logica di centraggio avanzata con supporto rotazione e prefab delta
                // =================================================================================
                if (visualRoot != null && structureData != null && structureData.Prefab != null)
                {
                    Vector3 originalLocalPos = visualRoot.localPosition;

                    // Calcola lo step di rotazione (0, 1, 2, 3) basato sull'asse Y
                    int rotationStep = Mathf.RoundToInt(transform.rotation.eulerAngles.y / 90f) % 4;

                    // Gestione rotazioni negative
                    if (rotationStep < 0) rotationStep += 4;

                    // Calcola il delta necessario per centrare la visuale rispetto al pivot logico
                    Vector3 deltaLocal = StructureVisualCenteringUtility.GetOrComputeCenteringLocalDelta(
                        transform,
                        visualRoot,
                        structureData.Prefab,
                        rotationStep
                    );

                    // Applica il centraggio UNA SOLA VOLTA usando il flag ref
                    StructureVisualCenteringUtility.ApplyCenteringOnce(
                        visualRoot,
                        originalLocalPos,
                        deltaLocal,
                        ref runtimeCenteringApplied
                    );
                }
                // =================================================================================

                InitializeGrowFromFlat();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (visualRoot != null)
                {
                    Debug.Log($"[Structure] ═══ SPAWN VISUAL ROOT ═══\n" +
                              $"After Centering + InitializeGrowFromFlat:\n" +
                              $"  root.position: {transform.position}\n" +
                              $"  visualRoot.localPosition: {visualRoot.localPosition}\n" +
                              $"  Rotation Step: {Mathf.RoundToInt(transform.rotation.eulerAngles.y / 90f) % 4}\n" +
                              $"  Centering Applied: {runtimeCenteringApplied}");
                }
#endif
            }
            else if (constructionVisualMode == ConstructionVisualMode.OverlayFlatten)
            {
                CreateConstructionOverlay();
            }

            UpdateConstructionProgressVisual(0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (visualRoot != null && constructionVisualMode == ConstructionVisualMode.GrowFromFlat)
            {
                Debug.Log($"[Structure] After UpdateConstructionProgressVisual(0):\n" +
                          $"  visualRoot.localPosition: {visualRoot.localPosition}\n" +
                          $"  visualRoot.position: {visualRoot.position}");
            }
#endif
        }

        private void ApplyNormalVisuals()
        {
            DestroyCurrentVFX();

            if (constructionVisualMode == ConstructionVisualMode.GrowFromFlat)
            {
                ResetGrowFromFlat();
            }
            else if (constructionVisualMode == ConstructionVisualMode.OverlayFlatten)
            {
                DestroyConstructionOverlay();
            }
        }

        private void ApplyDamagedVisuals() { }

        private void SpawnVFX(string vfxType)
        {
            GameObject vfxPrefab = null;
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

        // ============================================
        // CONSTRUCTION VISUAL METHODS (OVERLAY APPROACH)
        // ============================================

        private void CreateConstructionOverlay()
        {
            if (overlayInitialized || visualRoot == null) return;

            if (constructionOverlayRoot == null)
            {
                GameObject overlayObj = new GameObject("ConstructionOverlayRoot");
                constructionOverlayRoot = overlayObj.transform;
                constructionOverlayRoot.SetParent(transform, false);
                constructionOverlayRoot.localPosition = Vector3.zero;
                constructionOverlayRoot.localRotation = Quaternion.identity;
                constructionOverlayRoot.localScale = Vector3.one;

                GameObject visualCopy = Instantiate(visualRoot.gameObject, constructionOverlayRoot);
                visualCopy.name = "OverlayVisual";

                Component[] components = visualCopy.GetComponentsInChildren<Component>(true);
                foreach (var comp in components)
                {
                    if (comp == null || comp is Transform || comp is MeshFilter || comp is Renderer)
                        continue;

                    if (comp is MonoBehaviour || comp is Collider)
                    {
                        Destroy(comp);
                    }
                }
            }

            if (overlayMPB == null)
            {
                overlayMPB = new MaterialPropertyBlock();
            }

            Renderer[] overlayRenderers = constructionOverlayRoot.GetComponentsInChildren<Renderer>();
            foreach (var r in overlayRenderers)
            {
                if (r == null) continue;

                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;

                    r.GetPropertyBlock(overlayMPB, i);

                    Color darkenedColor = Color.white;
                    bool foundColorProp = false;

                    if (mats[i].HasProperty("_BaseColor"))
                    {
                        darkenedColor = mats[i].GetColor("_BaseColor");
                        foundColorProp = true;
                    }
                    else if (mats[i].HasProperty("_Color"))
                    {
                        darkenedColor = mats[i].GetColor("_Color");
                        foundColorProp = true;
                    }

                    if (foundColorProp)
                    {
                        float gray = darkenedColor.r * 0.299f + darkenedColor.g * 0.587f + darkenedColor.b * 0.114f;
                        darkenedColor.r = Mathf.Lerp(darkenedColor.r, gray, overlayDesaturate);
                        darkenedColor.g = Mathf.Lerp(darkenedColor.g, gray, overlayDesaturate);
                        darkenedColor.b = Mathf.Lerp(darkenedColor.b, gray, overlayDesaturate);

                        darkenedColor.r *= (1f - overlayDarken);
                        darkenedColor.g *= (1f - overlayDarken);
                        darkenedColor.b *= (1f - overlayDarken);

                        if (mats[i].HasProperty("_BaseColor"))
                        {
                            overlayMPB.SetColor("_BaseColor", darkenedColor);
                        }
                        else if (mats[i].HasProperty("_Color"))
                        {
                            overlayMPB.SetColor("_Color", darkenedColor);
                        }
                    }

                    if (mats[i].HasProperty("_EmissionColor"))
                    {
                        overlayMPB.SetColor("_EmissionColor", Color.black);
                    }

                    r.SetPropertyBlock(overlayMPB, i);
                }
            }

            Renderer[] originalRenderers = visualRoot.GetComponentsInChildren<Renderer>();
            bool hasBounds = false;
            Bounds totalBounds = new Bounds();

            foreach (var r in originalRenderers)
            {
                if (r == null) continue;

                if (!hasBounds)
                {
                    totalBounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    totalBounds.Encapsulate(r.bounds);
                }
            }

            if (hasBounds)
            {
                baseHeight = totalBounds.min.y;
                totalHeight = totalBounds.size.y;
            }

            overlayInitialized = true;
        }

        public void UpdateConstructionProgressVisual(float progress01)
        {
            progress01 = Mathf.Clamp01(progress01);

            if (lastAppliedFrame == Time.frameCount)
            {
                return;
            }

            if (Mathf.Abs(progress01 - lastAppliedProgress) < 0.001f)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (progress01 + 0.0001f < lastAppliedProgress)
            {
                Debug.LogWarning($"[StructureController] Progress decreased: {lastAppliedProgress:F4} -> {progress01:F4} on {gameObject.name}");
            }
#endif

            lastAppliedFrame = Time.frameCount;
            lastAppliedProgress = progress01;

            if (constructionVisualMode == ConstructionVisualMode.GrowFromFlat)
            {
                UpdateGrowFromFlat(progress01);
            }
            else if (constructionVisualMode == ConstructionVisualMode.OverlayFlatten)
            {
                UpdateOverlayFlatten(progress01);
            }
        }

        private void UpdateOverlayFlatten(float progress01)
        {
            if (constructionOverlayRoot == null) return;

            float remainingHeight = 1f - progress01;

            if (remainingHeight <= 0.001f)
            {
                constructionOverlayRoot.gameObject.SetActive(false);
                return;
            }

            constructionOverlayRoot.gameObject.SetActive(true);

            Vector3 localScale = constructionOverlayRoot.localScale;
            localScale.y = remainingHeight;
            constructionOverlayRoot.localScale = localScale;

            Vector3 localPos = constructionOverlayRoot.localPosition;
            localPos.y = totalHeight * progress01;
            constructionOverlayRoot.localPosition = localPos;
        }

        private void DestroyConstructionOverlay()
        {
            if (constructionOverlayRoot != null)
            {
                Destroy(constructionOverlayRoot.gameObject);
                constructionOverlayRoot = null;
            }

            overlayInitialized = false;
        }

        private void InitializeGrowFromFlat()
        {
            if (growFromFlatInitialized || visualRoot == null) return;

            initialVisualRootLocalPos = visualRoot.localPosition;
            initialVisualRootLocalScale = visualRoot.localScale;

            growFromFlatInitialized = true;
        }

        private void UpdateGrowFromFlat(float progress01)
        {
            if (visualRoot == null || !growFromFlatInitialized) return;

            float currentYScale = Mathf.Lerp(flatStartYScale, 1f, progress01);

            visualRoot.localScale = new Vector3(
                initialVisualRootLocalScale.x,
                initialVisualRootLocalScale.y * currentYScale,
                initialVisualRootLocalScale.z
            );

            float scaleRatio = currentYScale;
            float heightLoss = (1f - scaleRatio) * initialVisualRootLocalScale.y;
            float offsetY = heightLoss * 0.5f;

            Vector3 localPos = initialVisualRootLocalPos;
            localPos.y += offsetY;
            visualRoot.localPosition = localPos;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Mathf.Abs(visualRoot.localPosition.x - initialVisualRootLocalPos.x) > 0.0001f ||
                Mathf.Abs(visualRoot.localPosition.z - initialVisualRootLocalPos.z) > 0.0001f)
            {
                Debug.LogWarning($"[Structure] ⚠️ VisualRoot X/Z changed during GrowFromFlat!\n" +
                                 $"  Expected X/Z: ({initialVisualRootLocalPos.x}, {initialVisualRootLocalPos.z})\n" +
                                 $"  Actual X/Z: ({visualRoot.localPosition.x}, {visualRoot.localPosition.z})\n" +
                                 $"  Progress: {progress01}");
            }
#endif
        }

        private void ResetGrowFromFlat()
        {
            if (visualRoot == null) return;

            visualRoot.localScale = initialVisualRootLocalScale;
            visualRoot.localPosition = initialVisualRootLocalPos;

            growFromFlatInitialized = false;
        }

        // ============================================
        // WORKER INSTANCE MANAGEMENT (for UI Assignment)
        // ============================================

        public bool HasFreeWorkerSlot()
        {
            if (structureData == null) return false;

            if (currentState == StructureState.Building)
            {
                return CurrentBuilder == null && assignedWorkerInstances.Count == 0;
            }

            return assignedWorkerInstances.Count < structureData.WorkerSlots;
        }

        public bool AssignWorker(WorkerInstance worker)
        {
            if (worker == null) return false;
            if (assignedWorkerInstances.Contains(worker)) return false;

            // SAFETY: Housing structures cannot accept workers for production jobs (Operating state)
            // Workers should be assigned via ShelterHome.AssignResident() instead
            // BUT: allow builders during construction phase
            if (structureData != null && structureData.Category == StructureCategory.Housing && currentState != StructureState.Building)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"<color=orange>[Structure]</color> {structureData.DisplayName} is HOUSING - use ShelterHome.AssignResident() instead!");
#endif
                return false;
            }

            if (currentState == StructureState.Building)
            {
                if (CurrentBuilder != null)
                {
                    Debug.LogWarning($"[Structure] {structureData.DisplayName} already has a builder assigned!");
                    return false;
                }

                CurrentBuilder = worker;
                assignedWorkerInstances.Add(worker);
                workerCount = assignedWorkerInstances.Count;
                worker.AssignTo(this);
                RecalculateBuildSpeed();

                Debug.Log($"<color=orange>[Structure]</color> {structureData.DisplayName} builder assigned: {worker.CustomName}");
                return true;
            }

            if (!HasFreeWorkerSlot()) return false;

            assignedWorkerInstances.Add(worker);
            workerCount = assignedWorkers.Count + assignedWorkerInstances.Count;
            worker.AssignTo(this);
            RecalculateProduction();

            Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} now has {assignedWorkerInstances.Count} worker instances");
            return true;
        }

        public void RemoveWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            if (assignedWorkerInstances.Contains(worker))
            {
                assignedWorkerInstances.Remove(worker);
                workerCount = assignedWorkers.Count + assignedWorkerInstances.Count;

                if (CurrentBuilder == worker)
                {
                    CurrentBuilder = null;
                }

                // Release any work slot the worker may have reserved
                ReleaseWorkSlot(worker);

                worker.Unassign();

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

        public List<WorkerInstance> GetAssignedWorkerInstances()
        {
            return new List<WorkerInstance>(assignedWorkerInstances);
        }

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

        public void OnClick()
        {
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
        // GRID PLACEMENT API
        // ============================================

        /// <summary>
        /// Imposta i dati di placement (chiamato da StructureSystem.SpawnStructure).
        /// </summary>
        public void SetPlacementData(Vector2Int originCell, int rotationStep)
        {
            OriginCell = originCell;
            PlacementRotationStep = rotationStep;
        }

        /// <summary>
        /// Calcola e ritorna tutte le celle occupate da questa struttura.
        /// Usa FootprintUtility per rotazione corretta delle celle.
        /// </summary>
        public List<Vector2Int> GetOccupiedCells()
        {
            if (structureData == null)
            {
                Debug.LogWarning($"[StructureController] {gameObject.name} has no StructureData!");
                return new List<Vector2Int>();
            }

            // Usa FootprintUtility per calcolare le celle con rotazione corretta
            return FootprintUtility.GetWorldOccupiedCells(structureData, OriginCell, PlacementRotationStep);
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

        // ============================================
        // WORKER POSITIONING
        // ============================================

        /// <summary>
        /// Calcola la posizione di lavoro sul perimetro della struttura verso il worker.
        /// Usa un raggio configurabile per mantenere i worker fuori dal modello.
        /// </summary>
        /// <param name="fromPosition">Posizione del worker che si sta avvicinando</param>
        /// <returns>Posizione sul perimetro dove il worker dovrebbe fermarsi</returns>
        public Vector3 GetWorkPosition(Vector3 fromPosition)
        {
            Vector3 center = transform.position;
            Vector3 dir = fromPosition - center;
            dir.y = 0f; // Lavoriamo sul piano XZ

            // Se il worker è troppo vicino al centro (o sopra), usa una direzione di fallback
            if (dir.sqrMagnitude < 0.01f)
            {
                // Fallback: usa una direzione casuale
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }
            else
            {
                dir.Normalize();
            }

            return center + dir * workRadius;
        }

        /// <summary>
        /// Trova il work spot più vicino al worker, oppure usa GetWorkPosition come fallback.
        /// </summary>
        /// <param name="fromPosition">Posizione del worker</param>
        /// <returns>Posizione del work spot più vicino</returns>
        public Vector3 GetClosestWorkSpot(Vector3 fromPosition)
        {
            // Se non ci sono work spots definiti, usa il raggio
            if (workSpots == null || workSpots.Length == 0)
            {
                return GetWorkPosition(fromPosition);
            }

            Transform best = null;
            float bestDist = float.MaxValue;

            // Trova lo spot più vicino al worker
            foreach (var spot in workSpots)
            {
                if (spot == null) continue;

                float d = (spot.position - fromPosition).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = spot;
                }
            }

            return best != null ? best.position : GetWorkPosition(fromPosition);
        }

        /// <summary>
        /// Gets a work position for a specific worker using approach slots.
        /// Reserves a slot for the worker to prevent blocking.
        /// </summary>
        /// <param name="worker">The worker requesting a work position</param>
        /// <returns>The position to move to (slot position or fallback)</returns>
        public Vector3 GetWorkPositionForWorker(WorkerInstance worker)
        {
            if (workSlots != null && worker != null)
            {
                if (workSlots.TryReserveSlot(worker, out Vector3 slotPos))
                {
                    return slotPos;
                }
                // All slots full, use fallback queue position
                return workSlots.GetFallbackQueuePos(worker);
            }

            // Fallback to legacy position calculation
            if (worker != null && worker.PhysicalWorker != null)
            {
                return GetClosestWorkSpot(worker.PhysicalWorker.transform.position);
            }
            return GetWorkPosition(transform.position + Vector3.forward);
        }

        /// <summary>
        /// Releases any work slot held by the worker.
        /// Called automatically when worker is unassigned or leaves the worksite.
        /// </summary>
        public void ReleaseWorkSlot(WorkerInstance worker)
        {
            if (workSlots != null && worker != null)
            {
                workSlots.ReleaseSlot(worker);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Visualizza il work radius e i work spots nell'editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Disegna il cerchio del work radius
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            DrawCircle(transform.position, workRadius, 32);

            // Disegna i work spots
            if (workSpots != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var spot in workSpots)
                {
                    if (spot != null)
                    {
                        Gizmos.DrawWireSphere(spot.position, 0.3f);
                        Gizmos.DrawLine(transform.position, spot.position);
                    }
                }
            }
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
#endif

        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("Take 50 Damage", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugTakeDamage()
        {
            TakeDamage(50f);
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("Repair 100 HP", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void DebugRepair()
        {
            Repair(100f);
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Upgrade", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void DebugUpgrade()
        {
            TryUpgrade();
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Complete Construction", ButtonSizes.Medium)]
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

            Gizmos.color = Color.yellow;
            Vector3 size = new Vector3(structureData.GridSize.x, 0.1f, structureData.GridSize.y);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, size);

            if (structureData.Category == StructureCategory.Defense)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, structureData.AttackRange);
            }

            Gizmos.color = Color.cyan;
            foreach (var worker in assignedWorkers)
            {
                if (worker != null)
                {
                    Gizmos.DrawLine(transform.position + Vector3.up, worker.transform.position + Vector3.up);
                }
            }
        }

        [Button("Print Full Stats", ButtonSizes.Large)]
        [TitleGroup("Debug")]
        private void DebugPrintStats()
        {
            Debug.Log($"=== {structureData.DisplayName} Stats ===\n" +
                $"State: {currentState}\n" +
                $"Level: {currentLevel}/{structureData.MaxLevel}\n" +
                $"Health: {currentHealth:F0}/{maxHealth:F0}\n" +
                $"Workers: {workerCount}/{structureData.WorkerSlots}\n" +
                $"CurrentBuilder: {(CurrentBuilder != null ? CurrentBuilder.CustomName : "None")}\n" +
                $"Production Rate: {currentProductionRate:F1}/min\n" +
                $"Build Progress: {buildProgress:P0}");
        }
#endif
    }

    public enum StructureState
    {
        Idle,
        Building,
        Operating,
        Damaged,
        Destroyed
    }
}