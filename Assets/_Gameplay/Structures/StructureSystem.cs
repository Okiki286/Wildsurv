using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.UI;
using Wilderness.Core;

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

        [BoxGroup("Setup/Placement Settings")]
        [LabelWidth(150)]
        [Tooltip("Padding aggiuntivo (in world units) per impedire che strutture si tocchino. 0.15 = 15cm di gap minimo")]
        [Range(0f, 1f)]
        [SerializeField] private float overlapPadding = 0.15f;

        /// <summary>
        /// Epsilon fisso aggiunto sempre al padding per sicurezza (evita edge cases floating point)
        /// </summary>
        private const float OVERLAP_EPSILON = 0.02f;

        [BoxGroup("Setup/Placement Settings")]
        [LabelWidth(150)]
        [Tooltip("Offset Y per mesh base per evitare z-fighting con terreno")]
        [Range(0f, 0.1f)]
        [SerializeField] private float baseYOffset = 0.01f;

        [BoxGroup("Setup/Production Settings")]
        [LabelWidth(150)]
        [Tooltip("Intervallo tick produzione globale")]
        [SerializeField] private float productionTickInterval = 1f;

        // FIX2: Flag per disabilitare tick globale quando WorkerSystem gestisce i tick
        [BoxGroup("Setup/Production Settings")]
        [LabelWidth(150)]
        [Tooltip("Se false, il tick globale produzione è disabilitato (WorkerSystem gestisce i tick)")]
        [SerializeField] private bool enableGlobalProductionTick = false;

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
        // EVENT: Notifica cambiamenti strutture (no polling)
        // ============================================

        /// <summary>
        /// Evento invocato quando i conteggi strutture cambiano.
        /// Usalo per aggiornare UI senza polling.
        /// </summary>
        public event System.Action OnStructuresChanged;

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Dev-only: timer per check invarianti contatori
        private float _devCheckTimer = 0f;
        private const float DEV_CHECK_INTERVAL = 10f;
#endif

        // ============================================
        // FIX3: OCCUPANCY GRID (no Physics per placement)
        // ============================================

        // Mappa celle occupate -> struttura che le occupa
        private Dictionary<Vector2Int, StructureController> _occupied = new Dictionary<Vector2Int, StructureController>();

        // Lista temporanea per FreeArea (evita allocazioni, usata solo in destroy)
        private readonly List<Vector2Int> _cellsToFree = new List<Vector2Int>(64);

        // ============================================
        // PROPERTIES
        // ============================================

        public int TotalStructures => totalStructureCount;
        public int OperationalStructures => operationalCount;
        public int BuildingStructures => buildingCount;
        public float GridSize => gridSize;
        public float OverlapPadding => overlapPadding;
        public float BaseYOffset => baseYOffset;
        public List<StructureController> AllStructures => allStructures;

        // ============================================
        // FIX1: GRID API UNIFICATA (Single Source of Truth)
        // ============================================

        /// <summary>
        /// Converte coordinate world in coordinate cella griglia.
        /// </summary>
        public Vector2Int WorldToCell(Vector3 world)
        {
            int cellX = Mathf.RoundToInt(world.x / gridSize);
            int cellZ = Mathf.RoundToInt(world.z / gridSize);
            return new Vector2Int(cellX, cellZ);
        }

        /// <summary>
        /// Converte coordinate cella in coordinate world (centro della cella).
        /// </summary>
        public Vector3 CellToWorld(Vector2Int cell)
        {
            float worldX = cell.x * gridSize;
            float worldZ = cell.y * gridSize;
            return new Vector3(worldX, 0f, worldZ);
        }

        /// <summary>
        /// Snappa una posizione world alla griglia. Mantiene Y invariato.
        /// </summary>
        public Vector3 SnapWorld(Vector3 world)
        {
            float x = Mathf.Round(world.x / gridSize) * gridSize;
            float z = Mathf.Round(world.z / gridSize) * gridSize;
            return new Vector3(x, world.y, z);
        }

        /// <summary>
        /// Ruota il footprint di una struttura.
        /// rotStep 0..3 corrisponde a 0°, 90°, 180°, 270°.
        /// Ai passi dispari (90°, 270°) X e Y vengono scambiati.
        /// </summary>
        public static Vector2Int RotateFootprint(Vector2Int size, int rotStep)
        {
            // Normalizza rotStep a 0..3
            rotStep = ((rotStep % 4) + 4) % 4;
            // Ai passi dispari (1, 3) scambia X e Y
            if (rotStep % 2 == 1)
            {
                return new Vector2Int(size.y, size.x);
            }
            return size;
        }

        // ============================================
        // FIX3: OCCUPANCY GRID API
        // ============================================

        /// <summary>
        /// Verifica se un'area è libera nella occupancy grid.
        /// Usa FootprintUtility per calcolare le celle con rotazione corretta.
        /// origin = cella di origine (angolo bottom-left del footprint)
        /// size = dimensione originale struttura
        /// rotStep = rotazione 0..3
        /// </summary>
        public bool IsAreaFree(Vector2Int origin, Vector2Int size, int rotStep)
        {
            // Genera le celle locali e ruotale
            var localCells = FootprintUtility.GenerateRectangularFootprint(size.x, size.y);
            var rotatedCells = FootprintUtility.GetRotatedCells(localCells, size.x, size.y, rotStep);

            // Verifica ogni cella world
            foreach (var localCell in rotatedCells)
            {
                Vector2Int worldCell = new Vector2Int(origin.x + localCell.x, origin.y + localCell.y);
                if (_occupied.ContainsKey(worldCell))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Occupa un'area nella occupancy grid per una struttura.
        /// Usa FootprintUtility per calcolare le celle con rotazione corretta.
        /// origin = cella di origine (angolo bottom-left del footprint)
        /// size = dimensione originale struttura
        /// rotStep = rotazione 0..3
        /// </summary>
        public void OccupyArea(StructureController sc, Vector2Int origin, Vector2Int size, int rotStep)
        {
            if (sc == null) return;

            // Usa FootprintUtility per ottenere le celle world corrette
            var worldCells = FootprintUtility.GetWorldOccupiedCells(sc.Data, origin, rotStep);

            foreach (var cell in worldCells)
            {
                _occupied[cell] = sc;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=green>[StructureSystem]</color> Occupied {worldCells.Count} cells for {sc.Data?.DisplayName ?? sc.name} at origin {origin}, rot {rotStep}");
#endif
        }

        /// <summary>
        /// Libera tutte le celle occupate da una struttura.
        /// O(n) sul dizionario, usare solo quando la struttura viene distrutta.
        /// </summary>
        public void FreeArea(StructureController sc)
        {
            if (sc == null) return;

            _cellsToFree.Clear();

            // Trova tutte le celle occupate da questa struttura
            foreach (var kvp in _occupied)
            {
                if (kvp.Value == sc)
                {
                    _cellsToFree.Add(kvp.Key);
                }
            }

            // Rimuovi le celle trovate
            for (int i = 0; i < _cellsToFree.Count; i++)
            {
                _occupied.Remove(_cellsToFree[i]);
            }

            // Log condensato - solo su destroy, evento raro
        }

        /// <summary>
        /// Tenta di ottenere la struttura che occupa una cella specifica.
        /// Usato per selezione grid-based: click -> WorldToCell -> TryGetStructureAtCell.
        /// </summary>
        /// <param name="cell">Coordinata cella griglia</param>
        /// <param name="structure">Struttura trovata (out)</param>
        /// <returns>True se la cella è occupata da una struttura</returns>
        public bool TryGetStructureAtCell(Vector2Int cell, out StructureController structure)
        {
            return _occupied.TryGetValue(cell, out structure);
        }

        /// <summary>
        /// Verifica se una cella specifica è occupata.
        /// </summary>
        public bool IsCellOccupied(Vector2Int cell)
        {
            return _occupied.ContainsKey(cell);
        }

        /// <summary>
        /// Ottiene il numero totale di celle occupate.
        /// </summary>
        public int OccupiedCellCount => _occupied.Count;

        // ============================================
        // PHYSICS-BASED OVERLAP CHECK (Backup/Visual)
        // ============================================

        /// <summary>
        /// Layer mask per strutture piazzate (escludi Preview layer).
        /// Configura in Inspector: Structures = layer 9, Preview = layer 14
        /// </summary>
        [BoxGroup("Setup/Placement Settings")]
        [LabelWidth(150)]
        [Tooltip("Layer per strutture piazzate (per Physics overlap check). Default: layer 9 'Structure'")]
        [SerializeField] private LayerMask structuresLayer = 1 << 9;

        /// <summary>
        /// Verifica overlap fisico con altre strutture usando Physics.OverlapBox.
        /// Usa overlapPadding + OVERLAP_EPSILON per espandere il box e impedire che strutture si tocchino.
        /// Priorità footprint: 1) BoxCollider "Footprint" nel prefab, 2) StructureData.CustomFootprintSize, 3) gridSize * cellSize
        /// Esclude preview (layer 14) e la struttura stessa se passata.
        /// </summary>
        /// <param name="position">Posizione centro struttura</param>
        /// <param name="gridSizeXZ">Dimensione struttura in celle (es. 2x3)</param>
        /// <param name="rotation">Rotazione struttura</param>
        /// <param name="excludeCollider">Collider da escludere (opzionale, per re-check)</param>
        /// <param name="structureData">StructureData per custom footprint (opzionale)</param>
        /// <param name="previewRoot">Root del preview per cercare BoxCollider "Footprint" (opzionale)</param>
        /// <returns>True se c'è overlap con altra struttura</returns>
        public bool HasPhysicsOverlap(Vector3 position, Vector2Int gridSizeXZ, Quaternion rotation,
            Collider excludeCollider = null, StructureData structureData = null, GameObject previewRoot = null)
        {
            // Calcola footprint size con priorità:
            // 1. BoxCollider "Footprint" child nel prefab/preview
            // 2. StructureData.CustomFootprintSize (se abilitato)
            // 3. Fallback: gridSize * cellSize

            Vector2 footprintXZ = Vector2.zero;
            Vector3 footprintCenter = position;
            float footprintHeight = 5f; // Altezza default per check

            // 1. Cerca BoxCollider "Footprint" nel preview o prefab
            BoxCollider footprintCollider = null;
            if (previewRoot != null)
            {
                footprintCollider = FindFootprintCollider(previewRoot.transform);
            }
            else if (structureData != null && structureData.Prefab != null)
            {
                footprintCollider = FindFootprintCollider(structureData.Prefab.transform);
            }

            if (footprintCollider != null)
            {
                // Usa il BoxCollider "Footprint" - size è già in local space
                Vector3 colliderSize = footprintCollider.size;
                footprintXZ = new Vector2(colliderSize.x, colliderSize.z);
                footprintHeight = colliderSize.y;
                // Il center del collider è relativo al transform, applicalo
                footprintCenter = position + rotation * footprintCollider.center;
            }
            // 2. StructureData custom footprint
            else if (structureData != null && structureData.UseCustomFootprint && structureData.CustomFootprintSize.sqrMagnitude > 0.01f)
            {
                footprintXZ = structureData.CustomFootprintSize;
            }
            // 3. Fallback: gridSize * cellSize
            else
            {
                footprintXZ = new Vector2(gridSizeXZ.x * gridSize, gridSizeXZ.y * gridSize);
            }

            // Calcola half extents CON PADDING + EPSILON
            float totalPadding = overlapPadding + OVERLAP_EPSILON;
            float halfWidth = (footprintXZ.x * 0.5f) + totalPadding;
            float halfDepth = (footprintXZ.y * 0.5f) + totalPadding;
            float halfHeight = footprintHeight * 0.5f;

            Vector3 halfExtents = new Vector3(halfWidth, halfHeight, halfDepth);
            Vector3 boxCenter = footprintCenter + Vector3.up * halfHeight;

            // Query con strutture layer only (layer 9, escludi Preview layer 14, Ground, etc.)
            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, rotation, structuresLayer, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];

                // Escludi self se passato
                if (excludeCollider != null && hit == excludeCollider)
                    continue;

                // Escludi footprint colliders (sono trigger, ma check extra sicurezza)
                if (hit.isTrigger && hit.name.ToLower().Contains("footprint"))
                    continue;

                // Escludi preview (dovrebbe già essere filtrato da layer, ma double-check)
                var sc = hit.GetComponentInParent<StructureController>();
                if (sc != null && sc.IsPreview)
                    continue;

                // Se arriviamo qui, c'è overlap con struttura reale
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=red>[StructureSystem]</color> Physics overlap with {hit.name} at {position} (footprint: {footprintXZ}, padding: {totalPadding})");
#endif
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cerca un BoxCollider chiamato "Footprint" nei figli (case insensitive)
        /// </summary>
        private BoxCollider FindFootprintCollider(Transform root)
        {
            if (root == null) return null;

            // Cerca direttamente sul root
            foreach (Transform child in root)
            {
                if (child.name.ToLower().Contains("footprint"))
                {
                    BoxCollider bc = child.GetComponent<BoxCollider>();
                    if (bc != null) return bc;
                }
            }

            // Cerca ricorsivamente (solo primo livello di profondità per performance)
            foreach (Transform child in root)
            {
                foreach (Transform grandchild in child)
                {
                    if (grandchild.name.ToLower().Contains("footprint"))
                    {
                        BoxCollider bc = grandchild.GetComponent<BoxCollider>();
                        if (bc != null) return bc;
                    }
                }
            }

            return null;
        }

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev-only: check invarianti contatori ogni 10 secondi
            _devCheckTimer += Time.deltaTime;
            if (_devCheckTimer >= DEV_CHECK_INTERVAL)
            {
                _devCheckTimer = 0f;
                ValidateCounterInvariants();
            }
#endif
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
            // FIX: I contatori sono ora gestiti incrementalmente in Register/Unregister.
            // Questo metodo non fa più nulla ma è mantenuto per backward compatibility.
            // totalStructureCount, operationalCount, buildingCount sono già aggiornati.
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

            // FIX1: Usa SnapWorld() per snapping coerente (single source of truth)
            Vector3 snappedPos = SnapWorld(position);

            // FIX: Applica offset Y per evitare z-fighting con terreno
            snappedPos.y += baseYOffset;

            // FIX1/FIX3: Estrai rotStep dalla rotation quaternion
            Quaternion effectiveRotation = (rotation == default) ? Quaternion.identity : rotation;
            int rotStep = Mathf.RoundToInt(effectiveRotation.eulerAngles.y / 90f) % 4;
            if (rotStep < 0) rotStep += 4;

            // FIX1: Calcola cella di origine
            Vector2Int originCell = WorldToCell(snappedPos);

            // Log di spawn condensato - attivare solo per debug specifico
            // Debug.Log($"[StructureSystem] Spawn: {data.DisplayName} at cell {originCell}, rot {rotStep}");

            // Validazione placement
            if (!ValidatePlacement(data, snappedPos, rotStep))
            {
                Debug.LogWarning($"[StructureSystem] Cannot place {data.DisplayName} at {snappedPos}");
                return null;
            }

            // Instantiate
            GameObject structureObj = Instantiate(data.Prefab, snappedPos, rotation == default ? Quaternion.identity : rotation);
            structureObj.name = $"Structure_{data.DisplayName}";

            structureObj.transform.SetParent(transform);

            // CenterStructurePivot disabilitato - non necessario con nuova grid API

            // Setup controller
            StructureController controller = structureObj.GetComponent<StructureController>();
            if (controller == null)
            {
                controller = structureObj.AddComponent<StructureController>();
            }
            controller.Initialize(data);

            // Salva dati di placement per selezione grid-based
            controller.SetPlacementData(originCell, rotStep);

            // StructureClickHandler non più necessario - selezione ora grid-based
            // Lasciamo il vecchio handler ma non lo aggiungiamo se mancante
            // if (structureObj.GetComponent<StructureClickHandler>() == null)
            //     structureObj.AddComponent<StructureClickHandler>();

            if (structureObj.GetComponent<StructureStatusUI>() == null)
                structureObj.AddComponent<StructureStatusUI>();

            // Collider per click/selection (Physics mantenuto solo per questo)
            BoxCollider collider = structureObj.GetComponent<BoxCollider>();
            if (collider == null)
                collider = structureObj.AddComponent<BoxCollider>();

            float halfWidth = data.GridSize.x * gridSize / 2f;
            float halfDepth = data.GridSize.y * gridSize / 2f;
            collider.size = new Vector3(halfWidth * 2f, 3f, halfDepth * 2f);
            collider.center = new Vector3(0f, 1.5f, 0f);
            collider.isTrigger = false;

            // Registra in StructureSystem
            RegisterStructure(controller);

            // FIX3: Occupa le celle nella occupancy grid
            OccupyArea(controller, originCell, data.GridSize, rotStep);

            // Registra in WorkerSystem per auto-assignment
            if (WorkerSystem.Instance != null)
                WorkerSystem.Instance.RegisterStructure(controller);

            Debug.Log($"<color=orange>[StructureSystem]</color> Spawned {data.DisplayName} at {snappedPos} (cell {originCell})");
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

        /// <summary>
        /// Rebuilds a structure from save data, registering it to the Grid and Pathfinding.
        /// Smart restore: reuses pre-existing structures (e.g., Waystone) if found at the same location.
        ///
        /// CRITICAL RESTORATION ORDER:
        /// 1. Find or spawn structure
        /// 2. Restore Level (sets MaxHealth)
        /// 3. Restore Health (clamps to MaxHealth)
        /// 4. Restore State (notifies StructureSystem counters, forces visual completion if Operating)
        /// 5. Restore Build Progress (visual state)
        /// 6. Restore Operational State (production capability)
        /// 7. Occupy grid cells (pathfinding)
        ///
        /// State counters (buildingCount, operationalCount) are automatically updated by RestoreState
        /// via NotifyStructureStateChanged - no manual list management needed.
        /// </summary>
        public StructureController RestoreStructureFromSave(StructureStateData data)
        {
            // FIX: Safety check - validate structure ID exists in database
            if (!structureDataLookup.ContainsKey(data.structureDataId))
            {
                Debug.LogError($"<color=red>[StructureSystem] RESTORE FAILED!</color> Structure ID '{data.structureDataId}' not found in database!\n" +
                    $"Available IDs: {string.Join(", ", structureDataLookup.Keys)}\n" +
                    $"This save file may be corrupted or from a different game version. Skipping this structure.");
                return null;
            }

            // 1. Lookup Data
            StructureData structureData = GetStructureData(data.structureDataId);
            if (structureData == null)
            {
                Debug.LogError($"[StructureSystem] Failed to restore. Unknown ID: {data.structureDataId}");
                return null;
            }

            // 2. Resolve Position (Grid -> World)
            Vector2Int gridPos = new Vector2Int(data.originCellX, data.originCellY);
            Vector3 worldPos = CellToWorld(gridPos);

            // 3. Resolve Rotation (Step -> Quaternion)
            // 0=0°, 1=90°, 2=180°, 3=270°
            Quaternion rotation = Quaternion.Euler(0, data.rotationStep * 90f, 0);

            // 4. SMART FIND: Check for pre-existing structure (Fix Waystone reload)
            Debug.Log($"<color=cyan>[SYSTEM-DEBUG]</color> Restoring {data.structureDataId} at {gridPos}. Looking for existing structure...");

            StructureController structure = null;
            if (TryGetStructureAtCell(gridPos, out StructureController existing))
            {
                Debug.Log($"<color=cyan>[SYSTEM-DEBUG]</color> Found existing structure: {existing.name} (ID: {existing.InstanceId}). Type match: {existing.Data.StructureId == structureData.StructureId}");

                // Found existing structure at this location
                if (existing.Data.StructureId == structureData.StructureId)
                {
                    // Same type - reuse it!
                    structure = existing;
                    Debug.Log($"<color=yellow>[StructureSystem]</color> Reusing pre-existing {structureData.DisplayName} at {gridPos}");
                }
                else
                {
                    // Type mismatch - destroy existing and spawn new
                    Debug.LogWarning($"[StructureSystem] Conflict at {gridPos}. Destroying {existing.Data.DisplayName} to restore {structureData.DisplayName}");
                    OnStructureDestroyed(existing);
                    Destroy(existing.gameObject);
                    structure = null;
                }
            }
            else
            {
                Debug.Log($"<color=yellow>[SYSTEM-DEBUG]</color> No existing structure found at {gridPos}. Will spawn new.");
            }

            // 5. Spawn if no valid structure found
            if (structure == null)
            {
                structure = SpawnStructure(structureData, worldPos, rotation);
            }

            if (structure != null)
            {
                // 6. Inject Identity & Stats (CRITICAL ORDER)
                // Override InstanceId (critical for Waystone to match saved ID)
                structure.InstanceId = data.instanceId;

                // A. Restore Level FIRST (sets correct MaxHealth)
                if (data.currentLevel > 1)
                {
                    structure.RestoreLevel(data.currentLevel);
                }

                // B. Restore Health SECOND (clamps to new MaxHealth)
                structure.RestoreHealth(data.currentHealth);

                // C. Restore State (notifies StructureSystem counters)
                structure.RestoreState(data.structureState);

                // D. Restore Progress & Operational State
                if (data.buildProgress < 1f)
                {
                    // Incomplete structure - restore partial progress
                    structure.RestoreBuildProgress(data.buildProgress);
                    structure.RestoreOperationalState(false); // Not operational while building
                }
                else
                {
                    // Fully built - ensure visual completion
                    // ForceVisualCompletion sets buildProgress=1.0 and isOperational=true
                    // but we override with saved operational state in case it was manually disabled
                    structure.ForceVisualCompletion();
                    structure.RestoreOperationalState(data.isOperational);
                }

                // F. Ensure grid occupation for pathfinding (critical for operational structures)
                StructureState restoredState = (StructureState)data.structureState;
                if (restoredState == StructureState.Operating && data.buildProgress >= 1f)
                {
                    // Ensure grid cells are occupied for pathfinding
                    if (!_occupied.ContainsKey(gridPos))
                    {
                        OccupyArea(structure, gridPos, structureData.GridSize, data.rotationStep);
                        Debug.Log($"<color=lime>[StructureSystem]</color> Occupied grid cells for restored operational structure: {structureData.DisplayName}");
                    }
                }

                Debug.Log($"<color=cyan>[StructureSystem]</color> Restored {structureData.DisplayName} at {gridPos} (ID: {data.instanceId.Substring(0, 8)}, State: {restoredState}, Progress: {data.buildProgress:P0})");
            }

            return structure;
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
                totalStructureCount++;

                // FIX: NON incrementare buildingCount/operationalCount qui!
                // Il conteggio stato avviene già tramite NotifyStructureStateChanged
                // chiamato da StructureController.ChangeState() durante Initialize().
                // Incrementare qui causerebbe doppio conteggio.

                OnStructuresChanged?.Invoke();
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=orange>[StructureSystem]</color> Registered {structure.Data.DisplayName}");
#endif
        }

        /// <summary>
        /// PUBLIC: Allows pre-existing scene structures (e.g., Waystone placed in Editor)
        /// to register themselves during initialization. Called from StructureController.InitializeStructure().
        /// </summary>
        public void RegisterSceneStructure(StructureController structure)
        {
            if (structure == null) return;

            Debug.Log($"<color=green>[SYSTEM-DEBUG]</color> RegisterSceneStructure called for {structure.name}. OriginCell: {structure.OriginCell}. InstanceId: {structure.InstanceId}");

            // Ensure grid data is set for scene-placed structures
            if (structure.OriginCell == Vector2Int.zero)
            {
                // Calculate grid position from world position
                Vector3 worldPos = structure.transform.position;
                Vector2Int gridPos = WorldToCell(worldPos);

                // Calculate rotation step from transform
                int rotStep = Mathf.RoundToInt(structure.transform.rotation.eulerAngles.y / 90f) % 4;
                if (rotStep < 0) rotStep += 4;

                Debug.Log($"<color=green>[SYSTEM-DEBUG]</color> Calculated grid data for {structure.name}: gridPos={gridPos}, rotStep={rotStep}");

                // Set placement data using existing method
                structure.SetPlacementData(gridPos, rotStep);

                // Occupy grid cells for pathfinding
                var footprint = structure.Data.GridSize;
                OccupyArea(structure, gridPos, footprint, rotStep);
            }
            else
            {
                Debug.Log($"<color=yellow>[SYSTEM-DEBUG]</color> {structure.name} already has OriginCell set: {structure.OriginCell}. Skipping grid calculation.");
            }

            // Register normally
            RegisterStructure(structure);

            Debug.Log($"<color=green>[StructureSystem]</color> Scene structure registered: {structure.Data.DisplayName} at {structure.transform.position}. Total structures: {allStructures.Count}");
        }

        private void UnregisterStructure(StructureController structure)
        {
            if (structure == null) return;

            if (allStructures.Remove(structure))
            {
                totalStructureCount = Mathf.Max(0, totalStructureCount - 1);

                // Decrementa contatore stato (REGOLA: State == Operating, non IsOperational)
                if (structure.State == StructureState.Building)
                    buildingCount = Mathf.Max(0, buildingCount - 1);
                else if (structure.State == StructureState.Operating)
                    operationalCount = Mathf.Max(0, operationalCount - 1);

                OnStructuresChanged?.Invoke();
            }

            if (structure.Data != null)
            {
                structuresByCategory[structure.Data.Category].Remove(structure);

                string id = structure.Data.StructureId;
                if (structuresById.ContainsKey(id))
                {
                    structuresById[id].Remove(structure);
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=orange>[StructureSystem]</color> Unregistered {structure.Data?.DisplayName ?? structure.name}");
#endif
        }

        /// <summary>
        /// Chiamato da StructureController quando lo stato cambia (es. Building -> Operating).
        /// Aggiorna i contatori incrementali senza usare LINQ.
        /// </summary>
        /// <param name="oldState">Stato precedente</param>
        /// <param name="newState">Nuovo stato</param>
        public void NotifyStructureStateChanged(StructureState oldState, StructureState newState)
        {
            // Decrementa vecchio stato
            if (oldState == StructureState.Building)
                buildingCount = Mathf.Max(0, buildingCount - 1);
            else if (oldState == StructureState.Operating)
                operationalCount = Mathf.Max(0, operationalCount - 1);

            // Incrementa nuovo stato
            if (newState == StructureState.Building)
                buildingCount++;
            else if (newState == StructureState.Operating)
                operationalCount++;

            OnStructuresChanged?.Invoke();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Dev-only: valida che i contatori incrementali siano coerenti con la lista.
        /// Eseguito ogni 10 secondi. Usa LINQ qui perché è solo dev e raramente.
        /// </summary>
        private void ValidateCounterInvariants()
        {
            int expectedTotal = allStructures.Count;
            int expectedBuilding = allStructures.Count(s => s != null && s.State == StructureState.Building);
            int expectedOperational = allStructures.Count(s => s != null && s.State == StructureState.Operating);

            bool hasMismatch = false;
            string mismatchDetails = "";

            if (totalStructureCount != expectedTotal)
            {
                hasMismatch = true;
                mismatchDetails += $" total({totalStructureCount}!={expectedTotal})";
            }
            if (buildingCount != expectedBuilding)
            {
                hasMismatch = true;
                mismatchDetails += $" building({buildingCount}!={expectedBuilding})";
            }
            if (operationalCount != expectedOperational)
            {
                hasMismatch = true;
                mismatchDetails += $" operational({operationalCount}!={expectedOperational})";
            }

            if (hasMismatch)
            {
                // Trova una struttura "sospetta" per debug
                string suspectInfo = "none";
                for (int i = 0; i < allStructures.Count; i++)
                {
                    var s = allStructures[i];
                    if (s == null) continue;

                    // Sospetta = IsOperational non corrisponde a State == Operating
                    bool stateIsOperating = s.State == StructureState.Operating;
                    if (s.IsOperational != stateIsOperating)
                    {
                        suspectInfo = $"{s.name} State={s.State} IsOperational={s.IsOperational}";
                        break;
                    }
                }

                Debug.LogWarning($"<color=red>[StructureSystem] COUNTER MISMATCH!</color>{mismatchDetails} | Suspect: {suspectInfo}");

                // Auto-fix: risincronizza contatori
                totalStructureCount = expectedTotal;
                buildingCount = expectedBuilding;
                operationalCount = expectedOperational;
            }
        }
#endif

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
        /// Valida se è possibile piazzare una struttura (rotStep = 0)
        /// Usa dual validation: occupancy grid + Physics.OverlapBox per robustezza.
        /// </summary>
        public bool ValidatePlacement(StructureData data, Vector3 position)
        {
            // Delega a overload con rotazione 0
            return ValidatePlacement(data, position, 0);
        }

        /// <summary>
        /// Valida placement con supporto rotazione (0..3 per 0°, 90°, 180°, 270°)
        /// Usa dual validation: occupancy grid + Physics.OverlapBox per robustezza.
        /// Include check per strutture uniche (isBaseCenter).
        /// </summary>
        public bool ValidatePlacement(StructureData data, Vector3 position, int rotationStep, GameObject previewRoot = null)
        {
            // 0. Check unicità isBaseCenter - solo una Waystone può esistere
            if (data.IsBaseCenter && HasBaseCenterStructure())
            {
                Debug.LogWarning($"<color=yellow>[StructureSystem]</color> Cannot place '{data.DisplayName}': Only one Waystone can be built.");
                return false;
            }

            Vector2Int effectiveSize = GetRotatedSize(data.GridSize, rotationStep);
            if (!IsGroundValid(position, effectiveSize)) return false;

            // 1. Grid-based check (primary, più veloce)
            Vector2Int originCell = WorldToCell(position);
            if (!IsAreaFree(originCell, data.GridSize, rotationStep)) return false;

            // 2. Physics-based check (con supporto footprint collider/custom size)
            Quaternion rotation = Quaternion.Euler(0, rotationStep * 90, 0);
            if (HasPhysicsOverlap(position, effectiveSize, rotation, null, data, previewRoot))
            {
                Debug.LogWarning($"<color=yellow>[StructureSystem]</color> Physics overlap detected at {position} - blocking placement");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica se esiste già una struttura con isBaseCenter = true.
        /// </summary>
        /// <returns>True se esiste già un Waystone/base center</returns>
        public bool HasBaseCenterStructure()
        {
            foreach (var structure in allStructures)
            {
                if (structure != null && structure.Data != null && structure.Data.IsBaseCenter)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Ottiene la struttura base center (Waystone) se esiste.
        /// </summary>
        /// <returns>StructureController del Waystone, o null se non esiste</returns>
        public StructureController GetBaseCenterStructure()
        {
            foreach (var structure in allStructures)
            {
                if (structure != null && structure.Data != null && structure.Data.IsBaseCenter)
                {
                    return structure;
                }
            }
            return null;
        }

        /// <summary>
        /// Calcola dimensione effettiva con rotazione (swap X/Y per 90°/270°)
        /// </summary>
        private Vector2Int GetRotatedSize(Vector2Int originalSize, int rotationStep)
        {
            return FootprintUtility.GetRotatedSize(originalSize, rotationStep);
        }

        /// <summary>
        /// Enumeration dei possibili motivi per cui un placement non è valido.
        /// </summary>
        public enum PlacementFailReason
        {
            None,
            NoGround,
            SlopeTooSteep,
            CellOccupied,
            PhysicsOverlap,
            BaseCenterAlreadyExists
        }

        /// <summary>
        /// Verifica se è possibile piazzare una struttura, con motivazione dell'eventuale fallimento.
        /// </summary>
        /// <param name="data">StructureData della struttura</param>
        /// <param name="position">Posizione world</param>
        /// <param name="rotationStep">Step di rotazione (0-3)</param>
        /// <param name="reason">Motivo del fallimento (out)</param>
        /// <param name="previewRoot">Root del preview per physics check (opzionale)</param>
        /// <returns>True se il placement è valido</returns>
        public bool CanPlace(StructureData data, Vector3 position, int rotationStep, out PlacementFailReason reason, GameObject previewRoot = null)
        {
            reason = PlacementFailReason.None;

            if (data == null)
            {
                reason = PlacementFailReason.NoGround;
                return false;
            }

            // 0. Check unicità isBaseCenter - solo una Waystone può esistere
            if (data.IsBaseCenter && HasBaseCenterStructure())
            {
                reason = PlacementFailReason.BaseCenterAlreadyExists;
                return false;
            }

            Vector2Int effectiveSize = GetRotatedSize(data.GridSize, rotationStep);

            // 1. Verifica terreno
            Vector3 rayStart = position + Vector3.up * 10f;
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            {
                reason = PlacementFailReason.NoGround;
                return false;
            }

            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > 30f)
            {
                reason = PlacementFailReason.SlopeTooSteep;
                return false;
            }

            // 2. Verifica occupancy grid
            Vector2Int originCell = WorldToCell(position);
            if (!IsAreaFree(originCell, data.GridSize, rotationStep))
            {
                reason = PlacementFailReason.CellOccupied;
                return false;
            }

            // 3. Verifica physics overlap (backup)
            Quaternion rotation = Quaternion.Euler(0, rotationStep * 90, 0);
            if (HasPhysicsOverlap(position, effectiveSize, rotation, null, data, previewRoot))
            {
                reason = PlacementFailReason.PhysicsOverlap;
                return false;
            }

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
        /// [LEGACY] Verifica overlap con altre strutture usando Physics.OverlapBox.
        /// FIX3: Non più usato per placement rules - usa IsAreaFree() invece.
        /// Mantenuto per compatibilità con click/selection o altri usi futuri.
        /// NOTA: Esclude le preview (IsPreview=true o controller disabilitato)
        /// </summary>
        [System.Obsolete("FIX3: Use IsAreaFree() for placement validation. This method uses Physics which is not reliable for grid-based placement.")]
        private bool HasOverlap(Vector3 position, Vector2Int gridSize)
        {
            float halfWidth = gridSize.x * this.gridSize / 2f;
            float halfDepth = gridSize.y * this.gridSize / 2f;

            Vector3 boxCenter = position + Vector3.up * 2.5f;
            Vector3 boxSize = new Vector3(halfWidth * 2f, 5f, halfDepth * 2f);

            // Usa layermask per escludere Ground
            int layerMask = ~groundLayer; // Inverte mask per escludere Ground
            Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize / 2f, Quaternion.identity, layerMask);

            // Controlla se c'è overlap con strutture esistenti
            for (int i = 0; i < colliders.Length; i++)
            {
                var structure = colliders[i].GetComponent<StructureController>();
                if (structure != null)
                {
                    // ═══════════════════════════════════════════════════════════
                    // ESCLUDI PREVIEW: Non contare le preview come overlap
                    // ═══════════════════════════════════════════════════════════
                    if (structure.IsPreview || !structure.enabled)
                    {
                        continue; // Salta le preview
                    }

                    // ❌ BLOCKING: overlap rilevato con struttura REALE
                    Debug.LogWarning($"<color=red>[StructureSystem]</color> ❌ Overlap detected with {structure.Data.DisplayName} – placement blocked");
                    return true; // Overlap trovato, blocca piazzamento
                }
            }

            // Nessun overlap con strutture reali
            return false;
        }

        // ============================================
        // PRODUZIONE GLOBALE
        // ============================================

        private void UpdateProductionTick()
        {
            // FIX2: Se WorkerSystem esiste e il flag è false, non eseguire tick globale
            // WorkerSystem gestisce i tick di costruzione/produzione
            if (!enableGlobalProductionTick || WorkerSystem.Instance != null)
            {
                return;
            }

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
            // FIX3: Libera celle nella occupancy grid
            FreeArea(structure);

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

        [BoxGroup("Debug")]
        [SerializeField]
        [Tooltip("Mostra celle occupate con Gizmos")]
        private bool showOccupiedCells = true;

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

                // Disegna celle occupate (per debug selezione grid-based)
                if (showOccupiedCells && _occupied != null)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.4f); // Verde semitrasparente
                    foreach (var kvp in _occupied)
                    {
                        Vector3 cellWorld = CellToWorld(kvp.Key);
                        Gizmos.DrawCube(cellWorld + Vector3.up * 0.05f, new Vector3(gridSize * 0.9f, 0.1f, gridSize * 0.9f));
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