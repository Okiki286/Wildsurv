using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.UI;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Controlla la modalità costruzione con preview, validazione e placement.
    /// VERSION: Economy Reale + AssetDatabase FIX
    /// </summary>
    public class BuildModeController : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static BuildModeController Instance { get; private set; }

        // ============================================
        // SETUP
        // ============================================

        [TitleGroup("Setup")]
        [BoxGroup("Setup/References")]
        [Required]
        [ChildGameObjectsOnly]
        [SerializeField] private Camera mainCamera;

        [BoxGroup("Setup/Preview Materials")]
        [Required("Materiale per preview valida")]
        [AssetsOnly]
        [SerializeField] private Material validPlacementMaterial;

        [BoxGroup("Setup/Preview Materials")]
        [Required("Materiale per preview invalida")]
        [AssetsOnly]
        [SerializeField] private Material invalidPlacementMaterial;

        [BoxGroup("Setup/Footprint Preview")]
        [Required("Footprint renderer component")]
        [ChildGameObjectsOnly]
        [SerializeField] private FootprintPreviewRenderer footprintRenderer;

        // FIX1: Rimossa gridSize locale - usa StructureSystem.Instance.GridSize
        // [SerializeField] private float gridSize = 2f;

        [TitleGroup("Grid Settings")]
        [SerializeField] private LayerMask groundLayer;

        [TitleGroup("Grid Settings")]
        [Tooltip("Layer da usare per il preview ghost (deve essere diverso da Structures layer 9)")]
        [SerializeField] private int previewLayer = 14; // Default: layer 14 "Preview"

        [TitleGroup("Input")]
        [InfoBox("Keyboard shortcuts: B=toggle, Q=rotate CCW, E=rotate CW")]
        [EnumToggleButtons]
        [SerializeField] private KeyCode toggleBuildKey = KeyCode.B;

        [EnumToggleButtons]
        [SerializeField] private KeyCode rotateCWKey = KeyCode.E;

        [EnumToggleButtons]
        [SerializeField] private KeyCode rotateCCWKey = KeyCode.Q;

        [TitleGroup("Debug")]
        [ToggleLeft]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [ShowInInspector, ReadOnly]
        [BoxGroup("Runtime Status")]
        private bool isBuildModeActive = false;

        /// <summary>
        /// Property pubblica per verificare se il build mode è attivo.
        /// Usata da StructureSelectionManager per disabilitare selezione durante il piazzamento.
        /// </summary>
        public bool IsInBuildMode => isBuildModeActive;

        [ShowInInspector, ReadOnly]
        [BoxGroup("Runtime Status")]
        private GameObject currentPreview;

        [ShowInInspector, ReadOnly]
        [BoxGroup("Runtime Status")]
        private StructureData selectedStructure;

        [ShowInInspector, ReadOnly]
        [BoxGroup("Runtime Status")]
        [Range(0, 3)]
        private int rotationStep = 0; // 0..3 → 0°, 90°, 180°, 270°

        private struct PlacementPose
        {
            public Vector3 worldPos;
            public Vector2Int cell;
            public int rotation;
            public Vector2Int size;
        }

        private PlacementPose lastPose;

        // Anti double-apply centering flag (usato solo per inizializzazione iniziale)
        private bool ghostCenteringApplied = false;

        // Cached prefab reference
        private GameObject currentPrefabForCentering = null;

        // ============================================
        // PREVIEW CENTERING STATE (per evitare drift)
        // ============================================

        // Posizione locale BASE del VisualRoot (quella originale del prefab, mai modificata)
        private Vector3 _previewVisualBaseLocalPos;

        // Flag per indicare che la base è stata catturata
        private bool _previewBaseCaptured = false;

        // Cached reference al VisualRoot del ghost
        private Transform _ghostVisualRoot = null;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Toggle build mode
            if (Input.GetKeyDown(toggleBuildKey))
            {
                ToggleBuildMode();
            }

            if (!isBuildModeActive) return;

            // Rotate preview with Q/E keys
            if (Input.GetKeyDown(rotateCWKey))
            {
                RotatePreview(1); // Clockwise (+90°)
            }
            else if (Input.GetKeyDown(rotateCCWKey))
            {
                RotatePreview(-1); // Counter-clockwise (-90°)
            }

            // Update preview position
            if (currentPreview != null)
            {
                UpdatePreviewPosition();
            }

            // Place structure
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                TryPlaceStructure();
            }

            // Cancel build mode
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                ExitBuildMode();
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        public void ToggleBuildMode()
        {
            if (isBuildModeActive)
                ExitBuildMode();
            else
                EnterBuildMode();
        }

        public void EnterBuildMode()
        {
            isBuildModeActive = true;
            if (debugMode)
                Debug.Log("[BuildMode] Build mode activated (Press B to exit)");
        }

        public void ExitBuildMode()
        {
            isBuildModeActive = false;

            if (currentPreview != null)
            {
                Destroy(currentPreview);
                currentPreview = null;
            }

            // Nascondi footprint
            if (footprintRenderer != null)
            {
                footprintRenderer.HideFootprint();
            }

            selectedStructure = null;
            rotationStep = 0;
            ghostCenteringApplied = false;
            currentPrefabForCentering = null;

            // Reset preview centering state
            _previewVisualBaseLocalPos = Vector3.zero;
            _previewBaseCaptured = false;
            _ghostVisualRoot = null;

            if (debugMode)
                Debug.Log("[BuildMode] Build mode deactivated");
        }

        /// <summary>
        /// Seleziona struttura da costruire (chiamato da UI)
        /// </summary>
        public void SelectStructure(StructureData structure)
        {
            if (structure == null || structure.Prefab == null)
            {
                Debug.LogError("[BuildMode] Invalid structure selected!");
                return;
            }

            selectedStructure = structure;
            rotationStep = 0;
            ghostCenteringApplied = false;
            currentPrefabForCentering = structure.Prefab;

            // Reset preview centering state
            _previewBaseCaptured = false;
            _ghostVisualRoot = null;

            // Distruggi preview esistente
            if (currentPreview != null)
                Destroy(currentPreview);

            // ═══════════════════════════════════════════════════════════
            // PARENT/CHILD PATTERN: Crea un wrapper vuoto per il pivot
            // ═══════════════════════════════════════════════════════════

            // 1. Crea il parent wrapper (questo è quello che seguirà il mouse)
            currentPreview = new GameObject($"Preview_{structure.StructureId}");

            // Imposta layer Preview su tutto il ghost per escluderlo da Physics.OverlapBox
            SetLayerRecursively(currentPreview, previewLayer);

            // 2. Istanzia il prefab come figlio (INATTIVO per evitare Awake prematura)
            bool originalPrefabState = structure.Prefab.activeSelf;
            structure.Prefab.SetActive(false);

            GameObject visualChild = Instantiate(structure.Prefab, currentPreview.transform);
            visualChild.name = "Visual";

            // Imposta layer Preview anche sul figlio appena istanziato
            SetLayerRecursively(visualChild, previewLayer);

            // Ripristina stato originale del prefab
            structure.Prefab.SetActive(originalPrefabState);

            // 3. Trova il VisualRoot e cattura la posizione BASE (una sola volta!)
            _ghostVisualRoot = StructureVisualCenteringUtility.FindVisualRoot(visualChild);
            if (_ghostVisualRoot != null)
            {
                // Cattura la posizione BASE originale del prefab - questa non cambia MAI
                _previewVisualBaseLocalPos = _ghostVisualRoot.localPosition;
                _previewBaseCaptured = true;

                // Calcola delta dal stato pulito (baseLocalPos)
                Vector3 deltaLocal = StructureVisualCenteringUtility.ComputeCenteringDeltaFromCleanState(
                    currentPreview.transform,  // root
                    _ghostVisualRoot,          // visualRoot
                    _previewVisualBaseLocalPos,// base local pos
                    currentPrefabForCentering, // prefab
                    rotationStep               // rotation 0..3
                );

                // Applica centering ASSOLUTO: base + delta
                StructureVisualCenteringUtility.ApplyCenteringAbsolute(
                    _ghostVisualRoot,
                    _previewVisualBaseLocalPos,
                    deltaLocal
                );

                ghostCenteringApplied = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"[BuildMode] ═══ PREVIEW CREATED ═══\n" +
                              $"  Base localPos captured: {_previewVisualBaseLocalPos}\n" +
                              $"  Delta for rot {rotationStep}: {deltaLocal}\n" +
                              $"  Final localPos: {_ghostVisualRoot.localPosition}");
                }
#endif
            }
            else
            {
                Debug.LogWarning($"[BuildMode] Could not find VisualRoot in ghost for {structure.StructureId}");
            }

            // Disabilita collider sul preview
            var colliders = currentPreview.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
                col.enabled = false;

            // ═══════════════════════════════════════════════════════════
            // CONFIGURA PREVIEW (mentre è ancora inattivo)
            // ═══════════════════════════════════════════════════════════

            // Marca la struttura come preview PRIMA di attivarla
            // Questo impedisce l'inizializzazione e la registrazione con il Foreman
            var structureController = visualChild.GetComponent<StructureController>();
            if (structureController != null)
            {
                structureController.IsPreview = true;
                structureController.enabled = false;
            }

            // Disabilita StructureStatusUI (nasconde progress bar)
            var statusUI = visualChild.GetComponent<StructureStatusUI>();
            if (statusUI != null)
            {
                statusUI.enabled = false;
            }

            // Nascondi anche il container UI se esiste
            var uiContainer = visualChild.transform.Find("StatusUI");
            if (uiContainer != null)
            {
                uiContainer.gameObject.SetActive(false);
            }

            // Ora è sicuro attivare il visualChild (IsPreview è già true)
            visualChild.SetActive(true);

            if (debugMode)
                Debug.Log($"[BuildMode] Selected: {structure.DisplayName}");

            // Entra automaticamente in build mode
            if (!isBuildModeActive)
                EnterBuildMode();
        }

        // ============================================
        // PREVIEW MANAGEMENT
        // ============================================

        private void UpdatePreviewPosition()
        {
            // Auto-find camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("[BuildMode] No Main Camera found!");
                    return;
                }
            }

            // FIX1: Verifica che StructureSystem sia disponibile
            if (StructureSystem.Instance == null)
            {
                Debug.LogError("[BuildMode] StructureSystem.Instance is null!");
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                // FIX1: Usa SnapWorld() di StructureSystem (single source of truth)
                Vector3 snappedPos = StructureSystem.Instance.SnapWorld(hit.point);
                currentPreview.transform.position = snappedPos;

                // FIX1: Usa WorldToCell() di StructureSystem
                Vector2Int cell = StructureSystem.Instance.WorldToCell(snappedPos);

                lastPose = new PlacementPose
                {
                    worldPos = snappedPos,
                    cell = cell,
                    rotation = rotationStep * 90, // Convert step to degrees
                    size = selectedStructure != null ? selectedStructure.GridSize : Vector2Int.one
                };

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode && Input.GetKeyDown(KeyCode.P))
                {
                    Transform visualChild = currentPreview.transform.childCount > 0 ? currentPreview.transform.GetChild(0) : null;
                    Debug.Log($"[BuildMode] ═══ GHOST STATUS ═══\n" +
                              $"Ghost worldPos: {lastPose.worldPos}\n" +
                              $"Ghost snappedPos: {snappedPos}\n" +
                              $"Ghost cell: {lastPose.cell}\n" +
                              $"Ghost root position: {currentPreview.transform.position}\n" +
                              $"Ghost visualChild: {(visualChild != null ? visualChild.name : "null")}\n" +
                              $"Ghost visualChild.localPos: {(visualChild != null ? visualChild.localPosition.ToString() : "null")}\n" +
                              $"Ghost visualChild.worldPos: {(visualChild != null ? visualChild.position.ToString() : "null")}");
                }
#endif

                // FIX3: Valida placement usando IsAreaFree (occupancy grid, no Physics)
                bool isValid = ValidatePlacement(snappedPos);
                UpdatePreviewMaterial(isValid);

                // Aggiorna footprint preview
                // FIX1: Usa GridSize da StructureSystem
                if (footprintRenderer != null && selectedStructure != null)
                {
                    footprintRenderer.UpdateFootprint(
                        lastPose.cell,
                        selectedStructure.GridSize,
                        rotationStep,
                        StructureSystem.Instance.GridSize,
                        isValid
                    );
                }
            }
            else
            {
                // Nasconde footprint se non c'è raycast hit
                if (footprintRenderer != null)
                {
                    footprintRenderer.HideFootprint();
                }
            }
        }

        private void UpdatePreviewMaterial(bool isValid)
        {
            Material mat = isValid ? validPlacementMaterial : invalidPlacementMaterial;

            var renderers = currentPreview.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                rend.material = mat;
            }
        }

        private void RotatePreview(int direction)
        {
            if (currentPreview == null) return;

            // Update rotation step (0..3)
            rotationStep = (rotationStep + direction + 4) % 4;

            // Apply rotation al root del preview
            int degrees = rotationStep * 90;
            currentPreview.transform.rotation = Quaternion.Euler(0, degrees, 0);

            // ═══════════════════════════════════════════════════════════
            // CENTERING ASSOLUTO: usa sempre baseLocalPos + delta
            // Questo previene il drift accumulativo
            // ═══════════════════════════════════════════════════════════

            if (_ghostVisualRoot != null && _previewBaseCaptured)
            {
                // 1. SEMPRE resetta alla posizione BASE prima di calcolare il delta
                //    Questo garantisce che bounds siano calcolati dallo stato pulito
                _ghostVisualRoot.localPosition = _previewVisualBaseLocalPos;

                // 2. Calcola delta dal stato pulito (usa cache se disponibile)
                Vector3 deltaLocal = StructureVisualCenteringUtility.ComputeCenteringDeltaFromCleanState(
                    currentPreview.transform,
                    _ghostVisualRoot,
                    _previewVisualBaseLocalPos,
                    currentPrefabForCentering,
                    rotationStep
                );

                // 3. Applica centering ASSOLUTO: base + delta (idempotente)
                StructureVisualCenteringUtility.ApplyCenteringAbsolute(
                    _ghostVisualRoot,
                    _previewVisualBaseLocalPos,
                    deltaLocal
                );

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"[BuildMode] ═══ ROTATE PREVIEW ═══\n" +
                              $"  Rotation step: {rotationStep} ({degrees}°)\n" +
                              $"  Base localPos: {_previewVisualBaseLocalPos}\n" +
                              $"  Delta for this rot: {deltaLocal}\n" +
                              $"  Final localPos: {_ghostVisualRoot.localPosition}\n" +
                              $"  (Expected: base + delta = {_previewVisualBaseLocalPos + new Vector3(deltaLocal.x, 0, deltaLocal.z)})");
                }
#endif
            }
            else if (debugMode)
            {
                Debug.LogWarning($"[BuildMode] RotatePreview: _ghostVisualRoot={_ghostVisualRoot != null}, _previewBaseCaptured={_previewBaseCaptured}");
            }

            if (debugMode)
                Debug.Log($"[BuildMode] Rotated to {degrees}° (step {rotationStep})");
        }

        /// <summary>
        /// Centra il pivot del preview per alignment perfetto
        /// </summary>
        private void CenterPreviewPivot()
        {
            if (currentPreview == null) return;

            Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 offset = currentPreview.transform.position - combinedBounds.center;
            offset.y = 0; // Mantieni altezza invariata

            currentPreview.transform.position += offset;
        }

        // ============================================
        // PLACEMENT - ECONOMIA REALE (StructureCost[])
        // ============================================

        private void TryPlaceStructure()
        {
            if (selectedStructure == null || currentPreview == null)
                return;

            Vector3 position = lastPose.worldPos;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Transform ghostVisualChild = currentPreview.transform.childCount > 0 ? currentPreview.transform.GetChild(0) : null;
                Transform ghostVisualRoot = ghostVisualChild != null ? StructureVisualCenteringUtility.FindVisualRoot(ghostVisualChild.gameObject) : null;

                Debug.Log($"[BuildMode] ═══ PLACEMENT REQUEST ═══\n" +
                          $"Ghost root worldPos: {lastPose.worldPos}\n" +
                          $"Ghost cell: {lastPose.cell}\n" +
                          $"Ghost visualRoot.localPos: {(ghostVisualRoot != null ? ghostVisualRoot.localPosition.ToString() : "null")}\n" +
                          $"Rotation: {lastPose.rotation}\n" +
                          $"Size: {lastPose.size}");
            }
#endif

            // Valida placement (include grid check + physics overlap check)
            if (!ValidatePlacement(position))
            {
                Debug.LogWarning($"[BuildMode] ❌ Cannot place {selectedStructure.DisplayName} - area occupied or overlapping!");
                return;
            }

            // ========== ECONOMY CHECK (USA StructureCost[] ESISTENTE) ==========

            // ResourceSystem ha già metodo CanAfford(StructureCost[])
            if (!ResourceSystem.Instance.CanAfford(selectedStructure.BuildCosts))
            {
                Debug.LogWarning($"[BuildMode] ❌ Cannot afford {selectedStructure.DisplayName}!");
                ShowInsufficientResourcesFeedback();
                return;
            }

            // ResourceSystem ha già metodo PayCost(StructureCost[])
            bool paid = ResourceSystem.Instance.PayCost(selectedStructure.BuildCosts);
            if (!paid)
            {
                Debug.LogError("[BuildMode] Failed to pay costs!");
                return;
            }

            // ========== PLACEMENT ESEGUITO ==========

            // Place structure via StructureSystem
            Quaternion rotation = Quaternion.Euler(0, rotationStep * 90, 0);
            StructureSystem.Instance.SpawnStructure(selectedStructure, position, rotation);

            if (debugMode)
                Debug.Log($"[BuildMode] ✅ Placed {selectedStructure.DisplayName} at {position}");
        }

        private bool ValidatePlacement(Vector3 position)
        {
            // Check ground collision
            if (!Physics.Raycast(position + Vector3.up * 10f, Vector3.down, 15f, groundLayer))
                return false;

            if (StructureSystem.Instance == null)
                return false;

            if (selectedStructure == null)
                return false;

            // Usa ValidatePlacement di StructureSystem che include:
            // 1. Grid-based check (occupancy grid)
            // 2. Physics-based check (OverlapBox con structuresLayer + footprint collider se presente)
            bool isValid = StructureSystem.Instance.ValidatePlacement(selectedStructure, position, rotationStep, currentPreview);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode && Input.GetKeyDown(KeyCode.V))
            {
                Vector2Int originCell = StructureSystem.Instance.WorldToCell(position);
                Vector2Int size = selectedStructure.GridSize;
                Debug.Log($"[BuildMode] ═══ VALIDATION ═══\n" +
                          $"Position: {position}\n" +
                          $"Origin Cell: {originCell}\n" +
                          $"Size: {size}\n" +
                          $"RotStep: {rotationStep}\n" +
                          $"IsValid (grid+physics): {isValid}");
            }
#endif

            return isValid;
        }

        // ============================================
        // UI FEEDBACK
        // ============================================

        private void ShowInsufficientResourcesFeedback()
        {
            Debug.Log("[BuildMode] 🔴 Insufficient resources!");

            // Logga cosa manca
            if (selectedStructure.BuildCosts != null)
            {
                foreach (var cost in selectedStructure.BuildCosts)
                {
                    float have = ResourceSystem.Instance.GetResourceAmount(cost.resourceId);
                    int need = cost.amount;

                    if (have < need)
                    {
                        Debug.Log($"  - Need {need - have} more {cost.resourceId} (have {have}/{need})");
                    }
                }
            }

            // TODO: Mostra UI feedback visivo (flash rosso, shake, ecc.)
        }

        // ============================================
        // UTILS
        // ============================================

        // FIX1: Rimosso SnapToGrid locale - usa StructureSystem.Instance.SnapWorld()

        // ============================================
        // LAYER UTILITY
        // ============================================

        /// <summary>
        /// Imposta il layer su un GameObject e tutti i suoi figli ricorsivamente.
        /// Usato per spostare il preview ghost su un layer separato (es. "Preview")
        /// che viene ignorato dal Physics.OverlapBox di validazione.
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        // ============================================
        // EDITOR TOOLS - FIX ASSETDATABASE
        // ============================================

#if UNITY_EDITOR
        [Button("Test: Select Structure"), FoldoutGroup("Editor Tools")]
        private void EditorTestSelectStructure()
        {
            if (Application.isPlaying)
            {
                // ✅ FIX: Cerca StructureData ASSETS (non scene objects!)
                var guids = UnityEditor.AssetDatabase.FindAssets("t:StructureData");

                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    StructureData data = UnityEditor.AssetDatabase.LoadAssetAtPath<StructureData>(path);

                    if (data != null)
                    {
                        SelectStructure(data);
                        Debug.Log($"<color=green>[BuildMode]</color> Test selected: {data.DisplayName}");
                    }
                    else
                    {
                        Debug.LogError("[BuildMode] Failed to load StructureData!");
                    }
                }
                else
                {
                    Debug.LogWarning("[BuildMode] ⚠️ No StructureData assets found!\n\n" +
                        "Create one:\n" +
                        "1. Right-click in Project window\n" +
                        "2. Create → Wilderness Survival → Data → Structure Definition\n" +
                        "3. Configure the asset\n" +
                        "4. Try again!");
                }
            }
            else
            {
                Debug.LogWarning("[BuildMode] Enter Play Mode first!");
            }
        }

        [Button("Force Exit Build Mode"), FoldoutGroup("Editor Tools")]
        private void EditorForceExit()
        {
            if (Application.isPlaying)
                ExitBuildMode();
        }
#endif
    }
}