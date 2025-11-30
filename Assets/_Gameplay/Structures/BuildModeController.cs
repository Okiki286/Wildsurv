using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Resources;

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

        [TitleGroup("Grid Settings")]
        [MinValue(0.5f)]
        [SuffixLabel("meters", true)]
        [SerializeField] private float gridSize = 2f;

        [SerializeField] private LayerMask groundLayer;

        [TitleGroup("Input")]
        [InfoBox("Keyboard shortcuts for build mode")]
        [EnumToggleButtons]
        [SerializeField] private KeyCode toggleBuildKey = KeyCode.B;

        [EnumToggleButtons]
        [SerializeField] private KeyCode rotateKey = KeyCode.R;

        [TitleGroup("Debug")]
        [ToggleLeft]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [ShowInInspector, ReadOnly]
        [BoxGroup("Runtime Status")]
        private bool isBuildModeActive = false;

        [ShowInInspector, ReadOnly]
        [BoxGroup("Runtime Status")]
        private GameObject currentPreview;

        [ShowInInspector, ReadOnly]
        [BoxGroup("Runtime Status")]
        private StructureData selectedStructure;

        [ShowInInspector, ReadOnly]
        [BoxGroup("Runtime Status")]
        [Range(0, 270)]
        private int currentRotation = 0; // 0, 90, 180, 270

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

            // Rotate preview
            if (Input.GetKeyDown(rotateKey))
            {
                RotatePreview();
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

            selectedStructure = null;
            currentRotation = 0;

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
            currentRotation = 0;

            // Distruggi preview esistente
            if (currentPreview != null)
                Destroy(currentPreview);

            // Crea nuovo preview
            currentPreview = Instantiate(structure.Prefab);
            currentPreview.name = $"Preview_{structure.StructureId}";

            // Disabilita collider sul preview
            var colliders = currentPreview.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
                col.enabled = false;

            CenterPreviewPivot();

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
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                Vector3 snappedPos = SnapToGrid(hit.point);
                currentPreview.transform.position = snappedPos;

                // Valida placement
                bool isValid = ValidatePlacement(snappedPos);
                UpdatePreviewMaterial(isValid);
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

        private void RotatePreview()
        {
            if (currentPreview == null) return;

            currentRotation = (currentRotation + 90) % 360;
            currentPreview.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

            if (debugMode)
                Debug.Log($"[BuildMode] Rotated to {currentRotation}°");
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

            Vector3 position = currentPreview.transform.position;

            // Valida placement
            if (!ValidatePlacement(position))
            {
                if (debugMode)
                    Debug.LogWarning("[BuildMode] Invalid placement position!");
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
            Quaternion rotation = Quaternion.Euler(0, currentRotation, 0);
            StructureSystem.Instance.SpawnStructure(selectedStructure, position, rotation);

            if (debugMode)
                Debug.Log($"[BuildMode] ✅ Placed {selectedStructure.DisplayName} at {position}");
        }

        private bool ValidatePlacement(Vector3 position)
        {
            // Check ground collision
            if (!Physics.Raycast(position + Vector3.up * 10f, Vector3.down, 15f, groundLayer))
                return false;

            // Usa ValidatePlacement di StructureSystem (già esistente)
            if (StructureSystem.Instance == null)
                return false;

            return StructureSystem.Instance.ValidatePlacement(selectedStructure, position);
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

        private Vector3 SnapToGrid(Vector3 position)
        {
            float x = Mathf.Round(position.x / gridSize) * gridSize;
            float z = Mathf.Round(position.z / gridSize) * gridSize;
            return new Vector3(x, position.y, z);
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