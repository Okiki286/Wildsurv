using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Manager singleton per gestire tutte le strutture.
    /// Coordina spawn, produzione, validazione placement.
    /// </summary>
    public class StructureSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static StructureSystem Instance { get; private set; }

        // ============================================
        // SETUP
        // ============================================

        [TitleGroup("Setup")]
        [BoxGroup("Setup/Structure Definitions")]
        [Required("Serve almeno 1 StructureData!")]
        [AssetsOnly]
        [SerializeField] private StructureData[] availableStructures;

        [BoxGroup("Setup/Placement Settings")]
        [LabelWidth(150)]
        [Tooltip("Dimensione griglia per snap")]
        [SerializeField] private float gridSize = 1f;

        [BoxGroup("Setup/Placement Settings")]
        [LabelWidth(150)]
        [Tooltip("Layer terreno per validazione")]
        [SerializeField] private LayerMask groundLayer = 1 << 8; // Layer "Ground"

        [BoxGroup("Setup/Production Settings")]
        [LabelWidth(150)]
        [Tooltip("Intervallo tick produzione globale")]
        [SerializeField] private float productionTickInterval = 1f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime State")]
        [BoxGroup("Runtime State/Structures")]
        [ReadOnly]
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<StructureController> allStructures = new List<StructureController>();

        [BoxGroup("Runtime State/Stats")]
        [ReadOnly]
        [ShowInInspector]
        [ProgressBar(0, 100, ColorGetter = "GetStructureCountColor")]
        private int totalStructureCount = 0;

        [BoxGroup("Runtime State/Stats")]
        [ReadOnly]
        [ShowInInspector]
        private int operationalCount = 0;

        [BoxGroup("Runtime State/Stats")]
        [ReadOnly]
        [ShowInInspector]
        private int buildingCount = 0;

        // ============================================
        // REGISTRI & LOOKUP
        // ============================================

        // Strutture per categoria
        private Dictionary<StructureCategory, List<StructureController>> structuresByCategory = new Dictionary<StructureCategory, List<StructureController>>();

        // Strutture per ID
        private Dictionary<string, List<StructureController>> structuresById = new Dictionary<string, List<StructureController>>();

        // Lookup dati
        private Dictionary<string, StructureData> structureDataLookup = new Dictionary<string, StructureData>();

        // Timer produzione
        private float productionTimer = 0f;

        // ============================================
        // PROPERTIES
        // ============================================

        public int TotalStructures => totalStructureCount;
        public int OperationalStructures => operationalCount;
        public int BuildingStructures => buildingCount;
        public float GridSize => gridSize;
        public List<StructureController> AllStructures => allStructures;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[StructureSystem] Instance duplicata! Distruggo questo GameObject.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeLookups();
        }

        private void Start()
        {
            if (availableStructures == null || availableStructures.Length == 0)
            {
                Debug.LogError("[StructureSystem] Nessuna StructureData assegnata! Sistema disabilitato.", this);
                enabled = false;
                return;
            }

            Debug.Log($"<color=orange>[StructureSystem]</color> Initialized with {availableStructures.Length} structure types");
        }

        private void Update()
        {
            UpdateCounts();
            UpdateProductionTick();
        }

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        private void InitializeLookups()
        {
            structureDataLookup.Clear();
            structuresByCategory.Clear();
            structuresById.Clear();

            // Inizializza dizionari per categoria
            foreach (StructureCategory category in System.Enum.GetValues(typeof(StructureCategory)))
            {
                structuresByCategory[category] = new List<StructureController>();
            }

            // Carica dati
            if (availableStructures == null) return;

            foreach (var data in availableStructures)
            {
                if (data == null) continue;

                if (!structureDataLookup.ContainsKey(data.StructureId))
                {
                    structureDataLookup.Add(data.StructureId, data);
                }
            }

            Debug.Log($"<color=orange>[StructureSystem]</color> Loaded {structureDataLookup.Count} structure definitions");
        }

        private void UpdateCounts()
        {
            totalStructureCount = allStructures.Count;
            operationalCount = allStructures.Count(s => s.IsOperational);
            buildingCount = allStructures.Count(s => s.State == StructureState.Building);
        }

        // ============================================
        // SPAWN SYSTEM
        // ============================================

        /// <summary>
        /// Spawna una struttura basata su StructureData
        /// </summary>
        public StructureController SpawnStructure(StructureData data, Vector3 position, Quaternion rotation = default)
        {
            if (data == null)
            {
                Debug.LogError("[StructureSystem] Cannot spawn: data is null!");
                return null;
            }

            // Verifica prefab
            if (data.Prefab == null)
            {
                Debug.LogError($"[StructureSystem] {data.DisplayName} has no prefab!");
                return null;
            }

            // Verifica unicità
            if (data.IsUnique && IsStructureBuilt(data.StructureId))
            {
                Debug.LogWarning($"[StructureSystem] {data.DisplayName} is unique and already exists!");
                return null;
            }

            // Snap a griglia
            Vector3 snappedPos = SnapToGrid(position);

            // Validazione placement
            if (!ValidatePlacement(data, snappedPos))
            {
                Debug.LogWarning($"[StructureSystem] Cannot place {data.DisplayName} at {snappedPos}");
                return null;
            }

            // Instantiate
            GameObject structureObj = Instantiate(data.Prefab, snappedPos, rotation == default ? Quaternion.identity : rotation);
            structureObj.name = $"Structure_{data.DisplayName}";
            structureObj.transform.SetParent(transform);

            // 🔧 CENTRA PIVOT: Stesso algoritmo del preview
            CenterStructurePivot(structureObj);

            // Setup controller
            StructureController controller = structureObj.GetComponent<StructureController>();
            if (controller == null)
            {
                controller = structureObj.AddComponent<StructureController>();
            }
            controller.Initialize(data);

            // 🔧 Aggiungi collider per overlap detection
            BoxCollider collider = structureObj.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = structureObj.AddComponent<BoxCollider>();
            }

            // Dimensiona collider basato su grid size
            float halfWidth = data.GridSize.x * gridSize / 2f;
            float halfDepth = data.GridSize.y * gridSize / 2f;
            collider.size = new Vector3(halfWidth * 2f, 3f, halfDepth * 2f);
            collider.center = new Vector3(0f, 1.5f, 0f);
            collider.isTrigger = false; // Collider solido per overlap

            // Registra in StructureSystem
            RegisterStructure(controller);

            // 🔧 FIX: Registra esplicitamente in WorkerSystem per auto-assignment
            if (WorkerSystem.Instance != null)
            {
                WorkerSystem.Instance.RegisterStructure(controller);
                Debug.Log($"<color=green>[StructureSystem]</color> ✅ Registered structure with WorkerSystem");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[StructureSystem]</color> ⚠️ WorkerSystem not found, structure won't be auto-assigned workers");
            }

            Debug.Log($"<color=orange>[StructureSystem]</color> Spawned {data.DisplayName} at {snappedPos}");
            return controller;
        }

        /// <summary>
        /// Spawna struttura per ID
        /// </summary>
        public StructureController SpawnStructureById(string structureId, Vector3 position, Quaternion rotation = default)
        {
            if (structureDataLookup.TryGetValue(structureId, out StructureData data))
            {
                return SpawnStructure(data, position, rotation);
            }

            Debug.LogError($"[StructureSystem] Structure ID '{structureId}' not found!");
            return null;
        }

        // ============================================
        // REGISTRAZIONE STRUTTURE
        // ============================================

        private void RegisterStructure(StructureController structure)
        {
            if (structure == null) return;

            // Lista principale
            if (!allStructures.Contains(structure))
            {
                allStructures.Add(structure);
            }

            // Per categoria
            var category = structure.Data.Category;
            if (!structuresByCategory[category].Contains(structure))
            {
                structuresByCategory[category].Add(structure);
            }

            // Per ID
            string id = structure.Data.StructureId;
            if (!structuresById.ContainsKey(id))
            {
                structuresById[id] = new List<StructureController>();
            }
            if (!structuresById[id].Contains(structure))
            {
                structuresById[id].Add(structure);
            }

            Debug.Log($"<color=orange>[StructureSystem]</color> Registered {structure.Data.DisplayName}");
        }

        private void UnregisterStructure(StructureController structure)
        {
            if (structure == null) return;

            allStructures.Remove(structure);
            structuresByCategory[structure.Data.Category].Remove(structure);

            string id = structure.Data.StructureId;
            if (structuresById.ContainsKey(id))
            {
                structuresById[id].Remove(structure);
            }

            Debug.Log($"<color=orange>[StructureSystem]</color> Unregistered {structure.Data.DisplayName}");
        }

        // ============================================
        // QUERY STRUTTURE
        // ============================================

        /// <summary>
        /// Ottieni tutte le strutture di una categoria
        /// </summary>
        public List<StructureController> GetStructuresByCategory(StructureCategory category)
        {
            if (structuresByCategory.TryGetValue(category, out var list))
            {
                return new List<StructureController>(list);
            }
            return new List<StructureController>();
        }

        /// <summary>
        /// Ottieni tutte le strutture di un ID specifico
        /// </summary>
        public List<StructureController> GetStructuresById(string structureId)
        {
            if (structuresById.TryGetValue(structureId, out var list))
            {
                return new List<StructureController>(list);
            }
            return new List<StructureController>();
        }

        /// <summary>
        /// Ottieni strutture in un raggio
        /// </summary>
        public List<StructureController> GetStructuresInRadius(Vector3 center, float radius)
        {
            return allStructures.Where(s => Vector3.Distance(s.transform.position, center) <= radius).ToList();
        }

        /// <summary>
        /// Ottieni la struttura più vicina a una posizione
        /// </summary>
        public StructureController GetNearestStructure(Vector3 position, StructureCategory? category = null)
        {
            var candidates = category.HasValue ? GetStructuresByCategory(category.Value) : allStructures;

            StructureController nearest = null;
            float minDistance = float.MaxValue;

            foreach (var structure in candidates)
            {
                float distance = Vector3.Distance(position, structure.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = structure;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Verifica se esiste già una struttura con questo ID
        /// </summary>
        public bool IsStructureBuilt(string structureId)
        {
            return structuresById.ContainsKey(structureId) && structuresById[structureId].Count > 0;
        }

        // ============================================
        // PLACEMENT VALIDATION
        // ============================================

        /// <summary>
        /// Snap posizione alla griglia
        /// </summary>
        public Vector3 SnapToGrid(Vector3 position)
        {
            float x = Mathf.Round(position.x / gridSize) * gridSize;
            float z = Mathf.Round(position.z / gridSize) * gridSize;
            return new Vector3(x, position.y, z);
        }

        /// <summary>
        /// Valida se è possibile piazzare una struttura
        /// </summary>
        public bool ValidatePlacement(StructureData data, Vector3 position)
        {
            // 1. Verifica terreno (BLOCKING)
            if (!IsGroundValid(position, data.GridSize))
            {
                return false;
            }

            // 2. Verifica overlap con altre strutture (NON-BLOCKING)
            // Overlap è ora solo un warning, non impedisce il piazzamento
            HasOverlap(position, data.GridSize); // Log warning se overlap presente

            // 3. Verifica risorse (gestito esternamente da BuildMode)
            return true;
        }

        /// <summary>
        /// Verifica se il terreno è valido
        /// </summary>
        private bool IsGroundValid(Vector3 position, Vector2Int gridSize)
        {
            // Raycast per verificare terreno
            Vector3 rayStart = position + Vector3.up * 10f;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            {
                // Verifica se è abbastanza piatto
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > 30f) // Max 30° di pendenza
                {
                    // Solo log per slope eccessivo (raro)
                    Debug.LogWarning($"<color=yellow>[StructureSystem]</color> Slope too steep: {slopeAngle:F1}°");
                    return false;
                }

                return true;
            }

            // Log solo quando raycast fallisce completamente (errore critico)
            Debug.LogWarning($"<color=red>[StructureSystem]</color> ❌ NO GROUND FOUND at {position}");
            return false;
        }

        /// <summary>
        /// Verifica overlap con altre strutture (NON-BLOCKING)
        /// Logga warnings ma permette comunque il piazzamento
        /// </summary>
        private bool HasOverlap(Vector3 position, Vector2Int gridSize)
        {
            float halfWidth = gridSize.x * this.gridSize / 2f;
            float halfDepth = gridSize.y * this.gridSize / 2f;

            Vector3 boxCenter = position + Vector3.up * 2.5f;
            Vector3 boxSize = new Vector3(halfWidth * 2f, 5f, halfDepth * 2f);

            // Usa layermask per escludere Ground
            int layerMask = ~groundLayer; // Inverte mask per escludere Ground
            Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize / 2f, Quaternion.identity, layerMask);

            // Log warning se trova overlap, ma NON blocca il piazzamento
            bool hasAnyOverlap = false;
            foreach (var col in colliders)
            {
                // Controlla se è una struttura
                var structure = col.GetComponent<StructureController>();
                if (structure != null)
                {
                    // ⚠️ WARNING: overlap rilevato ma non bloccante
                    Debug.LogWarning($"<color=yellow>[StructureSystem]</color> ⚠️ Overlap detected with {structure.Data.DisplayName} (placement allowed)");
                    hasAnyOverlap = true;
                }
            }

            // Ritorna sempre false per permettere il piazzamento
            // (Il valore di ritorno non viene più usato per bloccare in ValidatePlacement)
            return false;
        }

        // ============================================
        // PRODUZIONE GLOBALE
        // ============================================

        private void UpdateProductionTick()
        {
            productionTimer += Time.deltaTime;

            if (productionTimer >= productionTickInterval)
            {
                TickAllProduction(productionTickInterval);
                productionTimer = 0f;
            }
        }

        /// <summary>
        /// Tick di produzione per tutte le strutture
        /// </summary>
        private void TickAllProduction(float deltaTime)
        {
            var resourceStructures = GetStructuresByCategory(StructureCategory.Resource);

            foreach (var structure in resourceStructures)
            {
                if (structure != null && structure.IsOperational)
                {
                    structure.TickProduction(deltaTime);
                }
            }
        }

        // ============================================
        // COSTRUZIONE & UPGRADE
        // ============================================

        /// <summary>
        /// Paga i costi per costruire una struttura
        /// </summary>
        public bool PayConstructionCost(StructureData data)
        {
            // 🔧 TEMP: Bypass payment for testing
            // Riabilitare quando ResourceSystem ha risorse configurate
            Debug.Log($"<color=yellow>[StructureSystem]</color> Payment BYPASSED (testing mode)");
            return true;

            /* ORIGINALE - Da riabilitare:
            if (ResourceSystem.Instance == null) return false;

            var costs = data.BuildCosts;
            if (costs == null || costs.Length == 0) return true;

            // Converte in ResourceCost[]
            List<ResourceCost> resourceCosts = new List<ResourceCost>();
            foreach (var cost in costs)
            {
                resourceCosts.Add(new ResourceCost 
                { 
                    resourceId = cost.resourceId, 
                    amount = cost.amount 
                });
            }

            return ResourceSystem.Instance.PayCost(resourceCosts.ToArray());
            */
        }

        /// <summary>
        /// Callback quando struttura completata
        /// </summary>
        public void OnStructureCompleted(StructureController structure)
        {
            Debug.Log($"<color=green>[StructureSystem]</color> {structure.Data.DisplayName} construction completed!");
            // Trigger eventi, achievement, etc.
        }

        /// <summary>
        /// Callback quando struttura distrutta
        /// </summary>
        public void OnStructureDestroyed(StructureController structure)
        {
            UnregisterStructure(structure);
            Debug.Log($"<color=red>[StructureSystem]</color> {structure.Data.DisplayName} destroyed!");
            // Trigger eventi, game over check, etc.
        }

        // ============================================
        // UTILITY
        // ============================================

        /// <summary>
        /// Ottieni StructureData per ID
        /// </summary>
        public StructureData GetStructureData(string structureId)
        {
            if (structureDataLookup.TryGetValue(structureId, out StructureData data))
            {
                return data;
            }
            return null;
        }

        /// <summary>
        /// Distruggi tutte le strutture (per game over o reset)
        /// </summary>
        public void DestroyAllStructures()
        {
            var toDestroy = new List<StructureController>(allStructures);
            foreach (var structure in toDestroy)
            {
                if (structure != null)
                {
                    Destroy(structure.gameObject);
                }
            }

            allStructures.Clear();
            structuresByCategory.Clear();
            structuresById.Clear();

            Debug.Log("<color=red>[StructureSystem]</color> All structures destroyed!");
        }

        // ============================================
        // DEBUG & ODIN
        // ============================================

#if UNITY_EDITOR
        private Color GetStructureCountColor()
        {
            if (totalStructureCount >= 50) return Color.green;
            if (totalStructureCount >= 20) return Color.yellow;
            return Color.red;
        }

        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("🏗️ Spawn Test Structure", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void DebugSpawnStructure()
        {
            if (availableStructures != null && availableStructures.Length > 0)
            {
                Vector3 randomPos = new Vector3(
                    UnityEngine.Random.Range(-10f, 10f),
                    0f,
                    UnityEngine.Random.Range(-10f, 10f)
                );
                SpawnStructure(availableStructures[0], randomPos);
            }
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("📊 Print Stats", ButtonSizes.Large)]
        private void DebugPrintStats()
        {
            Debug.Log($"=== STRUCTURE SYSTEM STATS ===\n" +
                $"Total Structures: {totalStructureCount}\n" +
                $"Operational: {operationalCount}\n" +
                $"Building: {buildingCount}\n" +
                $"By Category:\n" +
                $"  Resource: {GetStructuresByCategory(StructureCategory.Resource).Count}\n" +
                $"  Defense: {GetStructuresByCategory(StructureCategory.Defense).Count}\n" +
                $"  Utility: {GetStructuresByCategory(StructureCategory.Utility).Count}");
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("💥 Destroy All", ButtonSizes.Large)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugDestroyAll()
        {
            DestroyAllStructures();
        }

        // ============================================
        // PIVOT CENTERING
        // ============================================

        /// <summary>
        /// Centra il pivot della struttura basato sui bounds dei renderer
        /// Identico all'algoritmo usato in BuildModeController per il preview
        /// </summary>
        private void CenterStructurePivot(GameObject structure)
        {
            var renderers = structure.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            // Calcola bounds totale di tutti i mesh
            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                totalBounds.Encapsulate(renderers[i].bounds);
            }

            // Calcola offset per centrare (solo X e Z, mantieni Y)
            Vector3 centerOffset = totalBounds.center - structure.transform.position;
            centerOffset.y = 0; // Non modificare altezza

            // Sposta tutti i child per centrare il pivot
            foreach (Transform child in structure.transform)
            {
                child.localPosition -= centerOffset;
            }
        }

        // ============================================
        // DEBUG VISUALIZATION
        // ============================================

        private void OnDrawGizmos()
        {
            // Disegna griglia
            if (Application.isPlaying)
            {
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                for (int x = -25; x <= 25; x++)
                {
                    for (int z = -25; z <= 25; z++)
                    {
                        Vector3 pos = new Vector3(x * gridSize, 0, z * gridSize);
                        Gizmos.DrawWireCube(pos, new Vector3(gridSize, 0.1f, gridSize));
                    }
                }

                // Disegna collider strutture per debug overlap
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Arancione
                foreach (var structure in allStructures)
                {
                    if (structure != null)
                    {
                        var data = structure.Data;
                        float halfWidth = data.GridSize.x * gridSize / 2f;
                        float halfDepth = data.GridSize.y * gridSize / 2f;
                        Vector3 size = new Vector3(halfWidth * 2f, 3f, halfDepth * 2f);
                        Vector3 center = structure.transform.position + Vector3.up * 1.5f;
                        Gizmos.DrawWireCube(center, size);
                    }
                }
            }
        }
#endif
    }
}