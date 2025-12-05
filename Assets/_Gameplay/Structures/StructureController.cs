using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.Gameplay.Structures
{
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
                ChangeState(StructureState.Building);
                buildTimeRemaining = structureData.BuildTime;
                buildProgress = 0f;
                isOperational = false;
                ApplyConstructionVisuals();
            }
            else
            {
                ChangeState(StructureState.Operating);
                buildProgress = 1f;
                isOperational = true;
                CompleteConstruction();
            }
        }

        public void Initialize(StructureData data, int level = 1)
        {
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
            Debug.Log($"<color=orange>[Structure]</color> {structureData.DisplayName}: {currentState} → {newState}");
            currentState = newState;
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

            RecalculateBuildSpeed();

            float progressDelta = (currentBuildSpeed / structureData.BuildTime) * deltaTime;
            buildProgress += progressDelta;

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

            Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} production rate: {currentProductionRate:F1}/min ({workersAtSite}/{assignedWorkerInstances.Count} at site)");
        }

        public void RecalculateBuildSpeed()
        {
            currentBuildSpeed = 0f;
            int buildersAtSite = 0;

            foreach (var instance in assignedWorkerInstances)
            {
                if (instance != null && instance.IsAtWorksite)
                {
                    currentBuildSpeed += instance.GetConstructionBonus();
                    buildersAtSite++;
                }
            }

            if (buildersAtSite == 0)
            {
                currentBuildSpeed = 0f;
            }

            Debug.Log($"<color=orange>[Structure]</color> {structureData.DisplayName} build speed: {currentBuildSpeed:F2}x ({buildersAtSite}/{assignedWorkerInstances.Count} at site)");
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
                    ResourceSystem.Instance.AddResource(structureData.ProducesResourceId, amountToAdd);
                }

                Debug.Log($"<color=cyan>[Structure]</color> {structureData.DisplayName} produced {amountToAdd}x {structureData.ProducesResourceId}");
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
                ResourceSystem.Instance.AddResource(structureData.ProducesResourceId, amountThisTick);
            }
        }

        // ============================================
        // HEALTH & COMBAT
        // ============================================

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

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
            assignedWorkers.Clear();
            assignedHeroes.Clear();
            buildersAssigned.Clear();
            CurrentBuilder = null;
            workerCount = 0;
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
        }

        private void ApplyNormalVisuals()
        {
            DestroyCurrentVFX();
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