using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Manager per la selezione delle strutture basata su griglia.
    /// Sostituisce la selezione basata su collider/physics con una lookup deterministica.
    ///
    /// Flow:
    /// 1. Click LMB (non sopra UI, non in build mode)
    /// 2. Raycast su Ground layer per ottenere hitPoint
    /// 3. cell = WorldToCell(hitPoint)
    /// 4. if occupiedCells.TryGetValue(cell) => Select(structure)
    /// </summary>
    public class StructureSelectionManager : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static StructureSelectionManager Instance { get; private set; }

        // ============================================
        // SETUP
        // ============================================

        [TitleGroup("Setup")]
        [SerializeField]
        [Tooltip("Layer del terreno per raycast")]
        private LayerMask groundLayer = 1 << 8; // Default: Ground layer

        [TitleGroup("Setup")]
        [SerializeField]
        [Tooltip("Camera principale (se null usa Camera.main)")]
        private Camera mainCamera;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime State")]
        [ShowInInspector, ReadOnly]
        private StructureController selectedStructure;

        [TitleGroup("Runtime State")]
        [ShowInInspector, ReadOnly]
        private Vector2Int lastClickedCell;

        // ============================================
        // PROPERTIES
        // ============================================

        /// <summary>
        /// Struttura attualmente selezionata (null se nessuna)
        /// </summary>
        public StructureController SelectedStructure => selectedStructure;

        /// <summary>
        /// Evento chiamato quando la selezione cambia
        /// </summary>
        public event System.Action<StructureController> OnSelectionChanged;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[StructureSelectionManager] Duplicate instance destroyed!");
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogError("[StructureSelectionManager] No camera found! Selection disabled.");
                enabled = false;
            }
        }

        private void Update()
        {
            // Solo click LMB
            if (!Input.GetMouseButtonDown(0))
                return;

            // Ignora se sopra UI
            if (IsPointerOverUI())
                return;

            // Ignora se in build mode
            if (IsBuildModeActive())
                return;

            // Esegui selezione grid-based
            TrySelectStructureAtMousePosition();
        }

        // ============================================
        // SELECTION LOGIC
        // ============================================

        /// <summary>
        /// Tenta di selezionare una struttura alla posizione del mouse.
        /// </summary>
        private void TrySelectStructureAtMousePosition()
        {
            if (StructureSystem.Instance == null)
            {
                Debug.LogWarning("[StructureSelectionManager] StructureSystem not available!");
                return;
            }

            // Raycast su ground
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                // Click fuori dal terreno - deseleziona
                ClearSelection();
                return;
            }

            // Converti world position in cella griglia
            Vector2Int cell = StructureSystem.Instance.WorldToCell(hit.point);
            lastClickedCell = cell;

            // Cerca struttura che occupa questa cella
            if (StructureSystem.Instance.TryGetStructureAtCell(cell, out StructureController structure))
            {
                // Cella occupata - seleziona struttura
                SelectStructure(structure);
            }
            else
            {
                // Cella vuota - deseleziona
                ClearSelection();
            }
        }

        /// <summary>
        /// Seleziona una struttura specifica.
        /// </summary>
        public void SelectStructure(StructureController structure)
        {
            if (structure == null)
            {
                ClearSelection();
                return;
            }

            // Se già selezionata, apri direttamente UI
            if (selectedStructure == structure)
            {
                OpenStructureUI(structure);
                return;
            }

            // Cambia selezione
            StructureController previousSelection = selectedStructure;
            selectedStructure = structure;

            Debug.Log($"<color=cyan>[Selection]</color> Selected: {structure.Data?.DisplayName ?? structure.name} at cell {structure.OriginCell}");

            // Apri UI worker assignment
            OpenStructureUI(structure);

            // Notifica cambio selezione
            OnSelectionChanged?.Invoke(structure);
        }

        /// <summary>
        /// Deseleziona la struttura corrente.
        /// </summary>
        public void ClearSelection()
        {
            if (selectedStructure == null)
                return;

            Debug.Log($"<color=cyan>[Selection]</color> Cleared selection");

            selectedStructure = null;

            // Chiudi UI se aperta
            if (UI.WorkerAssignmentUI.Instance != null)
            {
                UI.WorkerAssignmentUI.Instance.Close();
            }

            OnSelectionChanged?.Invoke(null);
        }

        /// <summary>
        /// Apre l'UI di assegnazione worker per la struttura.
        /// </summary>
        private void OpenStructureUI(StructureController structure)
        {
            if (structure == null) return;

            // Usa il metodo OnClick esistente che apre WorkerAssignmentUI
            structure.OnClick();
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        /// <summary>
        /// Verifica se il puntatore è sopra un elemento UI.
        /// </summary>
        private bool IsPointerOverUI()
        {
            // Check EventSystem
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return true;

            return false;
        }

        /// <summary>
        /// Verifica se il build mode è attivo.
        /// </summary>
        private bool IsBuildModeActive()
        {
            // Controlla BuildModeController se esiste
            if (BuildModeController.Instance != null)
            {
                return BuildModeController.Instance.IsInBuildMode;
            }

            return false;
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Tenta di selezionare una struttura a una posizione world specifica.
        /// </summary>
        public bool TrySelectAtWorldPosition(Vector3 worldPos)
        {
            if (StructureSystem.Instance == null)
                return false;

            Vector2Int cell = StructureSystem.Instance.WorldToCell(worldPos);

            if (StructureSystem.Instance.TryGetStructureAtCell(cell, out StructureController structure))
            {
                SelectStructure(structure);
                return true;
            }

            ClearSelection();
            return false;
        }

        /// <summary>
        /// Tenta di selezionare una struttura a una cella specifica.
        /// </summary>
        public bool TrySelectAtCell(Vector2Int cell)
        {
            if (StructureSystem.Instance == null)
                return false;

            if (StructureSystem.Instance.TryGetStructureAtCell(cell, out StructureController structure))
            {
                SelectStructure(structure);
                return true;
            }

            ClearSelection();
            return false;
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Log Occupied Cells")]
        private void DebugLogOccupiedCells()
        {
            if (StructureSystem.Instance == null)
            {
                Debug.Log("[Selection] StructureSystem not available");
                return;
            }

            Debug.Log($"[Selection] Total occupied cells: {StructureSystem.Instance.OccupiedCellCount}");
        }

        [TitleGroup("Debug")]
        [Button("Clear Selection")]
        private void DebugClearSelection()
        {
            ClearSelection();
        }

        private void OnDrawGizmos()
        {
            // Disegna la cella dell'ultimo click
            if (StructureSystem.Instance != null)
            {
                Vector3 cellWorldPos = StructureSystem.Instance.CellToWorld(lastClickedCell);
                float gridSize = StructureSystem.Instance.GridSize;

                Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                Gizmos.DrawWireCube(cellWorldPos + Vector3.up * 0.1f, new Vector3(gridSize, 0.2f, gridSize));
            }

            // Evidenzia la struttura selezionata
            if (selectedStructure != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(selectedStructure.transform.position + Vector3.up * 2f, 0.5f);
            }
        }
#endif
    }
}
