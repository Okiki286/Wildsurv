using UnityEngine;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.UI;

namespace WildernessSurvival.Gameplay
{
    /// <summary>
    /// Gestisce i click sulle strutture per aprire il panel di assegnazione worker.
    /// Aggiungi questo component ai prefab delle strutture.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StructureClickHandler : MonoBehaviour
    {
        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [Header("Configurazione")]
        [Tooltip("Se true, il click apre il panel solo se la struttura ha worker slots")]
        [SerializeField] private bool requireWorkerSlots = true;
        
        [Tooltip("Layer da ignorare per il raycast (es. UI)")]
        [SerializeField] private LayerMask ignoreLayerMask;

        [Header("Visual Feedback")]
        [SerializeField] private bool showHoverOutline = true;
        [SerializeField] private Color hoverOutlineColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private float hoverOutlineWidth = 0.05f;

        [Header("Audio")]
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip hoverSound;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME
        // ============================================

        private StructureController structureController;
        private AudioSource audioSource;
        private bool isHovered = false;
        private Renderer[] renderers;
        private Material outlineMaterial;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Find StructureController
            structureController = GetComponent<StructureController>();
            if (structureController == null)
            {
                structureController = GetComponentInParent<StructureController>();
            }

            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (clickSound != null || hoverSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Cache renderers for outline effect
            renderers = GetComponentsInChildren<Renderer>();

            // Ensure we have a collider
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"[StructureClickHandler] No collider on {gameObject.name}, adding BoxCollider");
                gameObject.AddComponent<BoxCollider>();
            }
        }

        private void Start()
        {
            if (structureController == null)
            {
                Debug.LogError($"[StructureClickHandler] No StructureController found on {gameObject.name}!");
            }
        }

        // ============================================
        // MOUSE EVENTS
        // ============================================

        private void OnMouseDown()
        {
            if (!CanInteract()) return;

            // Check if we should open the panel
            if (ShouldOpenPanel())
            {
                OpenWorkerAssignmentPanel();
            }
        }

        private void OnMouseEnter()
        {
            if (!CanInteract()) return;

            isHovered = true;

            if (showHoverOutline)
            {
                EnableHoverEffect();
            }

            PlaySound(hoverSound);

            if (debugMode)
            {
                Debug.Log($"[StructureClickHandler] Hover enter: {structureController?.Data?.DisplayName}");
            }
        }

        private void OnMouseExit()
        {
            isHovered = false;

            if (showHoverOutline)
            {
                DisableHoverEffect();
            }

            if (debugMode)
            {
                Debug.Log($"[StructureClickHandler] Hover exit: {structureController?.Data?.DisplayName}");
            }
        }

        // ============================================
        // INTERACTION LOGIC
        // ============================================

        private bool CanInteract()
        {
            // Don't interact if in build mode
            if (BuildModeController.Instance != null && BuildModeController.Instance.IsInBuildMode)
            {
                return false;
            }

            // Don't interact if pointer is over UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }

            // Don't interact if no structure controller
            if (structureController == null)
            {
                return false;
            }

            return true;
        }

        private bool ShouldOpenPanel()
        {
            if (structureController == null || structureController.Data == null)
            {
                return false;
            }

            // Check if structure has worker slots (if required)
            if (requireWorkerSlots && structureController.Data.WorkerSlots <= 0)
            {
                if (debugMode)
                {
                    Debug.Log($"[StructureClickHandler] {structureController.Data.DisplayName} has no worker slots");
                }
                return false;
            }

            return true;
        }

        private void OpenWorkerAssignmentPanel()
        {
            PlaySound(clickSound);

            if (WorkerAssignmentUI.Instance != null)
            {
                WorkerAssignmentUI.Instance.OpenForStructure(structureController);
                
                if (debugMode)
                {
                    Debug.Log($"<color=green>[StructureClickHandler]</color> Opened panel for {structureController.Data.DisplayName}");
                }
            }
            else
            {
                Debug.LogWarning("[StructureClickHandler] WorkerAssignmentUI.Instance is null!");
                
                // Fallback: call OnClick on controller
                structureController.OnClick();
            }
        }

        // ============================================
        // VISUAL FEEDBACK
        // ============================================

        private void EnableHoverEffect()
        {
            // Simple approach: tint renderers slightly
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    foreach (var mat in renderer.materials)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            Color original = mat.color;
                            mat.color = Color.Lerp(original, hoverOutlineColor, 0.3f);
                        }
                    }
                }
            }
        }

        private void DisableHoverEffect()
        {
            // Reset materials
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    foreach (var mat in renderer.materials)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            // Note: This is simplified - in production you'd cache original colors
                            mat.color = Color.white;
                        }
                    }
                }
            }
        }

        // ============================================
        // AUDIO
        // ============================================

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Forza apertura del panel (chiamabile da altri script)
        /// </summary>
        public void ForceOpenPanel()
        {
            if (structureController != null)
            {
                OpenWorkerAssignmentPanel();
            }
        }

        /// <summary>
        /// Ottiene il controller associato
        /// </summary>
        public StructureController GetStructureController()
        {
            return structureController;
        }

        /// <summary>
        /// Verifica se questa struttura supporta worker assignment
        /// </summary>
        public bool SupportsWorkerAssignment()
        {
            return structureController != null && 
                   structureController.Data != null && 
                   structureController.Data.WorkerSlots > 0;
        }
    }
}
