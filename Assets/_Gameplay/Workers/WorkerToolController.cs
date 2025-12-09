using UnityEngine;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Gestisce il tool/strumento del worker.
    /// Responsabile di tool socket detection, equipaggiamento e unequip.
    /// </summary>
    public class WorkerToolController : MonoBehaviour
    {
        // ============================================
        // TOOL SOCKET
        // ============================================

        [Header("Tool Socket")]
        [SerializeField]
        [Tooltip("Transform dove attachare il tool (es. mano destra)")]
        private Transform toolSocket;

        [SerializeField]
        [Tooltip("Tool correntemente equipaggiato (runtime)")]
        private GameObject currentToolInstance;

        // ============================================
        // PROPERTIES
        // ============================================

        public Transform ToolSocket => toolSocket;
        public GameObject CurrentTool => currentToolInstance;
        public bool HasTool => currentToolInstance != null;

        // ============================================
        // INITIALIZATION
        // ============================================

        private void Awake()
        {
            // TOOL SOCKET AUTO-DETECTION
            if (toolSocket == null)
            {
                toolSocket = FindToolSocket();
            }

#if UNITY_EDITOR
            if (toolSocket == null)
            {
                Debug.LogWarning($"[WorkerToolController] No tool socket found on {gameObject.name}. Tool equipping will be disabled.");
            }
            else
            {
                Debug.Log($"<color=cyan>[WorkerToolController]</color> Tool socket found: {toolSocket.name}");
            }
#endif
        }

        // ============================================
        // SOCKET DETECTION
        // ============================================

        /// <summary>
        /// Cerca automaticamente il tool socket nel hierarchy.
        /// Prova vari nomi comuni.
        /// </summary>
        private Transform FindToolSocket()
        {
            // Cerca socket comuni per nome
            string[] socketNames = {
                "ToolSocket",
                "RightHand",
                "Hand_R",
                "Hand.R",
                "Weapon_Socket",
                "ItemSocket",
                "RightHandSocket",
                "R_Hand"
            };

            foreach (var name in socketNames)
            {
                var socket = transform.FindDeepChild(name);
                if (socket != null)
                {
#if UNITY_EDITOR
                    Debug.Log($"<color=green>[WorkerToolController]</color> Auto-detected tool socket: {name}");
#endif
                    return socket;
                }
            }

            return null;
        }

        // ============================================
        // TOOL EQUIPPING
        // ============================================

        /// <summary>
        /// Equipaggia un tool con offset.
        /// </summary>
        public void EquipTool(GameObject toolPrefab, Vector3 positionOffset, Vector3 rotationOffset)
        {
            // Distruggi tool corrente
            UnequipTool();

            // Spawn nuovo tool
            if (toolPrefab != null && toolSocket != null)
            {
                currentToolInstance = Instantiate(toolPrefab, toolSocket);
                currentToolInstance.transform.localPosition = positionOffset;
                currentToolInstance.transform.localRotation = Quaternion.Euler(rotationOffset);
                currentToolInstance.name = $"Tool_{toolPrefab.name}";

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[WorkerToolController]</color> Equipped tool: {toolPrefab.name}");
#endif
            }
            else if (toolPrefab != null && toolSocket == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[WorkerToolController] Cannot equip tool {toolPrefab.name}: no tool socket found!");
#endif
            }
        }

        /// <summary>
        /// Equipaggia un tool senza offset (convenienza).
        /// </summary>
        public void EquipTool(GameObject toolPrefab)
        {
            EquipTool(toolPrefab, Vector3.zero, Vector3.zero);
        }

        // ============================================
        // TOOL UNEQUIPPING
        // ============================================

        /// <summary>
        /// Rimuove il tool corrente.
        /// </summary>
        public void UnequipTool()
        {
            if (currentToolInstance != null)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=orange>[WorkerToolController]</color> Unequipped tool: {currentToolInstance.name}");
#endif
                Destroy(currentToolInstance);
                currentToolInstance = null;
            }
        }

        // ============================================
        // CLEANUP
        // ============================================

        private void OnDestroy()
        {
            // Cleanup tool on destroy
            if (currentToolInstance != null)
            {
                Destroy(currentToolInstance);
            }
        }
    }

    // ============================================
    // EXTENSION METHODS
    // ============================================

    public static class TransformExtensionsWorker
    {
        /// <summary>
        /// Cerca ricorsivamente un figlio per nome.
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
