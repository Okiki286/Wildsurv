using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// WorkerToolController v2 - Mobile Optimized
    ///
    /// Responsabilità:
    /// - Gestione tool/armi/strumenti nelle mani del worker
    /// - Attach al socket corretto (Hand_R / Hand_L)
    /// - Offset locale (posizione/rotazione/scala) da WorkerVisualSet
    /// - Pooling leggero (riuso istanza se stessa prefab)
    /// - Supporto editor preview (JobAuthoringWindow)
    ///
    /// Integrazione:
    /// - Chiamato da WorkerVisualController durante ApplyJobVisual
    /// - Configurato via WorkerVisualSet (toolPrefab, offsets)
    /// </summary>
    public class WorkerToolController : MonoBehaviour
    {
        // ============================================
        // TOOL SOCKETS
        // ============================================

        [TitleGroup("Sockets")]
        [SerializeField]
        [Tooltip("Socket mano destra (auto-detected se null)")]
        [ChildGameObjectsOnly]
        private Transform rightHandSocket;

        [TitleGroup("Sockets")]
        [SerializeField]
        [Tooltip("Socket mano sinistra (auto-detected se null)")]
        [ChildGameObjectsOnly]
        private Transform leftHandSocket;

        [TitleGroup("Sockets")]
        [SerializeField]
        [Tooltip("Socket di default per tool (RightHand se non specificato)")]
        private ToolSide defaultToolSide = ToolSide.RightHand;

        // ============================================
        // CURRENT TOOL STATE
        // ============================================

        [TitleGroup("Current Tool")]
        [ShowInInspector, ReadOnly]
        private GameObject currentToolInstance;

        [ShowInInspector, ReadOnly]
        private GameObject currentToolPrefab; // Prefab source (per pooling check)

        [ShowInInspector, ReadOnly]
        private ToolSide currentToolSide = ToolSide.None;

        // ============================================
        // STATE FLAGS
        // ============================================

        private bool isInitialized = false;
        private bool loggedMissingSocketWarning = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public Transform RightHandSocket => rightHandSocket;
        public Transform LeftHandSocket => leftHandSocket;
        public GameObject CurrentTool => currentToolInstance;
        public bool HasTool => currentToolInstance != null;
        public ToolSide CurrentSide => currentToolSide;

        // ============================================
        // INITIALIZATION
        // ============================================

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Inizializza il controller.
        /// Auto-detect sockets se non assegnati.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            // Auto-detect sockets se non assegnati
            if (rightHandSocket == null || leftHandSocket == null)
            {
                DetectHandSockets();
            }

            // Validation
            if (rightHandSocket == null && leftHandSocket == null)
            {
                if (!loggedMissingSocketWarning)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[WorkerToolController] No hand sockets found on {gameObject.name}. Tool equipping will be disabled.", this);
#endif
                    loggedMissingSocketWarning = true;
                }
            }

            isInitialized = true;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Editor preview mode - silent
                return;
            }
#endif
        }

        // ============================================
        // SOCKET DETECTION
        // ============================================

        /// <summary>
        /// Auto-detect hand sockets nei children.
        /// Mobile-optimized: una sola passata, zero LINQ.
        /// </summary>
        private void DetectHandSockets()
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(includeInactive: true);

            for (int i = 0; i < allChildren.Length; i++)
            {
                string name = allChildren[i].name;

                // Check right hand socket
                if (rightHandSocket == null && IsRightHandSocket(name))
                {
                    rightHandSocket = allChildren[i];
#if UNITY_EDITOR
                    if (Application.isPlaying)
                    {
                        Debug.Log($"<color=cyan>[WorkerToolController]</color> Auto-detected right hand socket: {name}", this);
                    }
#endif
                }

                // Check left hand socket
                if (leftHandSocket == null && IsLeftHandSocket(name))
                {
                    leftHandSocket = allChildren[i];
#if UNITY_EDITOR
                    if (Application.isPlaying)
                    {
                        Debug.Log($"<color=cyan>[WorkerToolController]</color> Auto-detected left hand socket: {name}", this);
                    }
#endif
                }

                // Exit early if both found
                if (rightHandSocket != null && leftHandSocket != null)
                    break;
            }
        }

        /// <summary>
        /// Check se il nome corrisponde a un socket di mano destra.
        /// Zero allocations - usa Contains inline.
        /// </summary>
        private bool IsRightHandSocket(string name)
        {
            // Common right hand socket names (case-insensitive check)
            string lowerName = name.ToLower();

            return lowerName.Contains("hand_r") ||
                   lowerName.Contains("righthand") ||
                   lowerName.Contains("right_hand") ||
                   lowerName.Contains("r_hand") ||
                   lowerName.Contains("hand.r") ||
                   lowerName.Contains("socket_weapon_r") ||
                   lowerName.Contains("toolsocket") ||
                   lowerName.Contains("weaponsocket");
        }

        /// <summary>
        /// Check se il nome corrisponde a un socket di mano sinistra.
        /// </summary>
        private bool IsLeftHandSocket(string name)
        {
            string lowerName = name.ToLower();

            return lowerName.Contains("hand_l") ||
                   lowerName.Contains("lefthand") ||
                   lowerName.Contains("left_hand") ||
                   lowerName.Contains("l_hand") ||
                   lowerName.Contains("hand.l") ||
                   lowerName.Contains("socket_weapon_l");
        }

        // ============================================
        // EQUIP TOOL (DATA-DRIVEN)
        // ============================================

        /// <summary>
        /// Equipaggia tool da WorkerVisualSet.
        /// Riusa istanza se stessa prefab (pooling leggero).
        /// </summary>
        public void EquipTool(WorkerVisualSet visualSet)
        {
            if (visualSet == null)
            {
                ClearTool();
                return;
            }

            GameObject toolPrefab = visualSet.toolPrefab;

            if (toolPrefab == null)
            {
                ClearTool();
                return;
            }

            // Determina side (default RightHand se non specificato)
            ToolSide targetSide = defaultToolSide;

            // Determina offsets
            Vector3 localPos = visualSet.toolPositionOffset;
            Vector3 localRot = visualSet.toolRotationOffset;
            Vector3 localScale = Vector3.one; // WorkerVisualSet non ha scale field per ora

            // Equip con parametri
            EquipToolInternal(toolPrefab, targetSide, localPos, localRot, localScale);
        }

        /// <summary>
        /// Equipaggia tool con offset (legacy overload per WorkerVisualController).
        /// Backward compatibility.
        /// </summary>
        public void EquipTool(GameObject toolPrefab, Vector3 positionOffset, Vector3 rotationOffset)
        {
            if (toolPrefab == null)
            {
                ClearTool();
                return;
            }

            EquipToolInternal(toolPrefab, defaultToolSide, positionOffset, rotationOffset, Vector3.one);
        }

        /// <summary>
        /// Equipaggia tool senza offset (legacy overload).
        /// </summary>
        public void EquipTool(GameObject toolPrefab)
        {
            EquipTool(toolPrefab, Vector3.zero, Vector3.zero);
        }

        // ============================================
        // INTERNAL EQUIP LOGIC (POOLING)
        // ============================================

        /// <summary>
        /// Core logic per equipaggiare tool.
        /// Riusa istanza se stessa prefab + stesso side (pooling).
        /// </summary>
        private void EquipToolInternal(GameObject toolPrefab, ToolSide side, Vector3 localPos, Vector3 localRot, Vector3 localScale)
        {
            if (toolPrefab == null)
            {
                ClearTool();
                return;
            }

            // Get target socket
            Transform targetSocket = GetSocketForSide(side);

            if (targetSocket == null)
            {
                if (!loggedMissingSocketWarning)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[WorkerToolController] No socket found for side {side}. Cannot equip tool.", this);
#endif
                    loggedMissingSocketWarning = true;
                }
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // POOLING CHECK: Se stessa prefab + stesso side → riusa istanza
            // ═══════════════════════════════════════════════════════════
            if (currentToolInstance != null &&
                currentToolPrefab == toolPrefab &&
                currentToolSide == side)
            {
                // Riusa istanza - aggiorna solo transform
                UpdateToolTransform(currentToolInstance, targetSocket, localPos, localRot, localScale);

#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Debug.Log($"<color=cyan>[WorkerToolController]</color> Reused tool instance: {toolPrefab.name}", this);
                }
#endif
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // DESTROY OLD TOOL (prefab diversa o side diverso)
            // ═══════════════════════════════════════════════════════════
            if (currentToolInstance != null)
            {
                DestroyToolInstance(currentToolInstance);
            }

            // ═══════════════════════════════════════════════════════════
            // INSTANTIATE NEW TOOL
            // ═══════════════════════════════════════════════════════════
            currentToolInstance = Instantiate(toolPrefab, targetSocket);
            currentToolPrefab = toolPrefab;
            currentToolSide = side;

            // Apply transform
            UpdateToolTransform(currentToolInstance, targetSocket, localPos, localRot, localScale);

            // Naming
            currentToolInstance.name = $"Tool_{toolPrefab.name}";

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Debug.Log($"<color=cyan>[WorkerToolController]</color> Equipped tool: {toolPrefab.name} on {side}", this);
            }
#endif
        }

        /// <summary>
        /// Aggiorna transform del tool (posizione/rotazione/scala).
        /// Zero allocations.
        /// </summary>
        private void UpdateToolTransform(GameObject toolInstance, Transform parentSocket, Vector3 localPos, Vector3 localRot, Vector3 localScale)
        {
            if (toolInstance == null) return;

            Transform toolTransform = toolInstance.transform;

            // Parent (se non già figlio)
            if (toolTransform.parent != parentSocket)
            {
                toolTransform.SetParent(parentSocket, worldPositionStays: false);
            }

            // Apply local transform
            toolTransform.localPosition = localPos;
            toolTransform.localEulerAngles = localRot;
            toolTransform.localScale = localScale;
        }

        // ============================================
        // CLEAR TOOL
        // ============================================

        /// <summary>
        /// Rimuove il tool corrente.
        /// Safe: può essere chiamato anche se non c'è tool.
        /// </summary>
        public void ClearTool()
        {
            if (currentToolInstance != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Debug.Log($"<color=orange>[WorkerToolController]</color> Cleared tool: {currentToolInstance.name}", this);
                }
#endif
                DestroyToolInstance(currentToolInstance);
            }

            currentToolInstance = null;
            currentToolPrefab = null;
            currentToolSide = ToolSide.None;
        }

        /// <summary>
        /// Legacy alias per ClearTool.
        /// </summary>
        public void UnequipTool()
        {
            ClearTool();
        }

        // ============================================
        // FORCE EQUIP (EDITOR/DEBUG)
        // ============================================

        /// <summary>
        /// Forza equipaggiamento di un tool arbitrario.
        /// Usato per debug e editor tools.
        /// </summary>
        public void ForceEquipToolPrefab(GameObject toolPrefab, ToolSide side, Vector3 localPos, Vector3 localRot, Vector3 localScale)
        {
            EquipToolInternal(toolPrefab, side, localPos, localRot, localScale);
        }

        // ============================================
        // UTILITY
        // ============================================

        /// <summary>
        /// Ottiene il socket per il side specificato.
        /// </summary>
        private Transform GetSocketForSide(ToolSide side)
        {
            switch (side)
            {
                case ToolSide.RightHand:
                    return rightHandSocket;
                case ToolSide.LeftHand:
                    return leftHandSocket;
                case ToolSide.None:
                default:
                    return rightHandSocket; // Fallback a destra
            }
        }

        /// <summary>
        /// Distrugge istanza tool.
        /// Usa DestroyImmediate in editor, Destroy in runtime.
        /// </summary>
        private void DestroyToolInstance(GameObject toolInstance)
        {
            if (toolInstance == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(toolInstance);
                return;
            }
#endif
            Destroy(toolInstance);
        }

        // ============================================
        // CLEANUP
        // ============================================

        private void OnDisable()
        {
            // Cleanup in editor preview quando viene disabilitato
#if UNITY_EDITOR
            if (!Application.isPlaying && currentToolInstance != null)
            {
                DestroyToolInstance(currentToolInstance);
                currentToolInstance = null;
                currentToolPrefab = null;
                currentToolSide = ToolSide.None;
            }
#endif
        }

        private void OnDestroy()
        {
            // Cleanup definitivo
            if (currentToolInstance != null)
            {
                DestroyToolInstance(currentToolInstance);
                currentToolInstance = null;
                currentToolPrefab = null;
                currentToolSide = ToolSide.None;
            }
        }

        // ============================================
        // EDITOR DEBUG TOOLS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Print Tool State", ButtonSizes.Medium)]
        private void DebugPrintState()
        {
            string toolInfo = currentToolInstance != null
                ? $"{currentToolInstance.name} (Prefab: {currentToolPrefab?.name ?? "None"})"
                : "None";

            string socketsInfo = $"Right: {(rightHandSocket != null ? rightHandSocket.name : "None")}, " +
                               $"Left: {(leftHandSocket != null ? leftHandSocket.name : "None")}";

            Debug.Log($"=== TOOL CONTROLLER STATE ===\n" +
                     $"Current Tool: {toolInfo}\n" +
                     $"Tool Side: {currentToolSide}\n" +
                     $"Sockets: {socketsInfo}\n" +
                     $"Has Tool: {HasTool}", this);
        }

        [TitleGroup("Debug")]
        [Button("Clear Tool Now", ButtonSizes.Small)]
        private void DebugClearTool()
        {
            ClearTool();
            Debug.Log("[WorkerToolController] Tool cleared via debug button.", this);
        }

        [TitleGroup("Debug")]
        [Button("Re-detect Sockets", ButtonSizes.Small)]
        private void DebugRedetectSockets()
        {
            rightHandSocket = null;
            leftHandSocket = null;
            loggedMissingSocketWarning = false;
            isInitialized = false;
            Initialize();
            Debug.Log("[WorkerToolController] Sockets re-detected.", this);
        }

        [TitleGroup("Debug")]
        [InfoBox("Test tool equipping with a sample prefab")]
        [SerializeField]
        private GameObject debugToolPrefab;

        [TitleGroup("Debug")]
        [Button("Test Equip Debug Tool (Right Hand)", ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.4f)]
        [EnableIf("@debugToolPrefab != null")]
        private void DebugEquipToolRight()
        {
            if (debugToolPrefab == null)
            {
                Debug.LogWarning("[WorkerToolController] No debug tool prefab assigned.");
                return;
            }

            ForceEquipToolPrefab(debugToolPrefab, ToolSide.RightHand, Vector3.zero, Vector3.zero, Vector3.one);
            Debug.Log($"[WorkerToolController] Equipped debug tool {debugToolPrefab.name} on RIGHT hand.", this);
        }

        [TitleGroup("Debug")]
        [Button("Test Equip Debug Tool (Left Hand)", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        [EnableIf("@debugToolPrefab != null")]
        private void DebugEquipToolLeft()
        {
            if (debugToolPrefab == null)
            {
                Debug.LogWarning("[WorkerToolController] No debug tool prefab assigned.");
                return;
            }

            ForceEquipToolPrefab(debugToolPrefab, ToolSide.LeftHand, Vector3.zero, Vector3.zero, Vector3.one);
            Debug.Log($"[WorkerToolController] Equipped debug tool {debugToolPrefab.name} on LEFT hand.", this);
        }
#endif
    }

    // ============================================
    // TOOL SIDE ENUM
    // ============================================

    /// <summary>
    /// Enum per specificare quale mano usa il tool.
    /// </summary>
    public enum ToolSide
    {
        None = 0,
        RightHand = 1,
        LeftHand = 2
    }

    // ============================================
    // EXTENSION METHODS (LEGACY - manteniamo per compatibilità)
    // ============================================

    public static class TransformExtensionsWorker
    {
        /// <summary>
        /// Cerca ricorsivamente un figlio per nome.
        /// NOTA: Non usato in v2 (usiamo GetComponentsInChildren per performance).
        /// Mantenuto per backward compatibility se usato altrove.
        /// </summary>
        public static Transform FindDeepChild(this Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var result = child.FindDeepChild(name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
