using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Controller dedicato alla gestione visuale del worker.
    /// Si sottoscrive a OnJobChanged e aggiorna mesh, tool, animator e colori.
    /// Supporta sia il nuovo sistema mesh swap che il legacy prefab swap.
    /// </summary>
    public class WorkerVisualController : MonoBehaviour
    {
        // ============================================
        // MESH RENDERERS
        // ============================================

        [TitleGroup("Mesh Renderers")]
        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per la testa (opzionale)")]
        private SkinnedMeshRenderer headRenderer;

        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per il corpo")]
        [Required("Assegna il renderer del corpo")]
        private SkinnedMeshRenderer bodyRenderer;

        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per le gambe (opzionale)")]
        private SkinnedMeshRenderer legsRenderer;

        // ============================================
        // TOOL SOCKET
        // ============================================

        [TitleGroup("Tool Socket")]
        [SerializeField]
        [Tooltip("Transform dove attachare il tool (es. mano destra)")]
        [Required("Assegna il socket per il tool")]
        private Transform toolSocket;

        [SerializeField]
        [Tooltip("Tool correntemente equipaggiato (runtime)")]
        [ReadOnly]
        private GameObject currentToolInstance;

        // ============================================
        // ANIMATOR
        // ============================================

        [TitleGroup("Animation")]
        [SerializeField]
        [Tooltip("Animator del worker")]
        private Animator animator;

        [SerializeField]
        [Tooltip("AnimatorController di default (Idle/Villager)")]
        private RuntimeAnimatorController defaultAnimatorController;

        // ============================================
        // VFX
        // ============================================

        [TitleGroup("VFX")]
        [SerializeField]
        [Tooltip("ParticleSystem per effetto puff (opzionale)")]
        private ParticleSystem changeJobVFX;

        [SerializeField]
        [Tooltip("Transform dove spawnare VFX aggiuntivi")]
        private Transform vfxSpawnPoint;

        // ============================================
        // COLOR TINT SUPPORT
        // ============================================

        [TitleGroup("Color Tint")]
        [SerializeField]
        [Tooltip("Renderer per elementi colorabili (fascia, mantello, ecc.)")]
        private Renderer[] tintableRenderers;

        [SerializeField]
        [Tooltip("Nome della property color nel material")]
        private string colorPropertyName = "_Color";

        // ============================================
        // DATABASE REFERENCE
        // ============================================

        [TitleGroup("Database")]
        [SerializeField]
        [Tooltip("Riferimento al JobDatabase. Se null, usa singleton.")]
        private JobDatabase jobDatabaseOverride;

        private JobDatabase ActiveJobDatabase => jobDatabaseOverride != null ? jobDatabaseOverride : JobDatabase.Instance;

        // ============================================
        // LEGACY SUPPORT
        // ============================================

        [TitleGroup("Legacy Support")]
        [SerializeField]
        [Tooltip("Transform contenitore per modelli legacy (prefab swap)")]
        private Transform legacyVisualRoot;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private WorkerInstance linkedInstance;
        private WorkerJobData currentJobData;
        private Coroutine transitionCoroutine;

        [TitleGroup("Runtime")]
        [ShowInInspector, ReadOnly]
        private bool isTransitioning = false;

        [ShowInInspector, ReadOnly]
        private string currentJobName = "None";

        // ============================================
        // PROPERTIES
        // ============================================

        public bool IsTransitioning => isTransitioning;
        public WorkerJobData CurrentJobData => currentJobData;

        // Animator parameter hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsWorkingHash = Animator.StringToHash("IsWorking");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        private bool hasSpeedParam = false;
        private bool hasIsWorkingParam = false;
        private bool hasIsMovingParam = false;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Auto-find animator se non assegnato
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // Auto-find tool socket se non assegnato
            if (toolSocket == null)
            {
                toolSocket = FindToolSocket();
            }

            // Auto-create legacy visual root se necessario
            if (legacyVisualRoot == null)
            {
                var existing = transform.Find("VisualRoot");
                if (existing != null)
                {
                    legacyVisualRoot = existing;
                }
            }

            CheckAnimatorParameters();
        }

        private Transform FindToolSocket()
        {
            // Cerca socket comuni per nome
            string[] socketNames = { "ToolSocket", "RightHand", "Hand_R", "Weapon_Socket", "ItemSocket" };

            foreach (var name in socketNames)
            {
                var socket = transform.FindDeepChild(name);
                if (socket != null) return socket;
            }

            return null;
        }

        private void OnDestroy()
        {
            // Unsubscribe da eventi
            if (linkedInstance != null)
            {
                linkedInstance.OnJobChanged -= HandleJobChanged;
            }
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        /// <summary>
        /// Inizializza il controller con un WorkerInstance.
        /// Si sottoscrive a OnJobChanged per aggiornamenti automatici.
        /// </summary>
        public void Initialize(WorkerInstance instance)
        {
            if (instance == null)
            {
                Debug.LogWarning("[WorkerVisualController] Initialize called with null instance.");
                return;
            }

            // Unsubscribe da precedente instance
            if (linkedInstance != null)
            {
                linkedInstance.OnJobChanged -= HandleJobChanged;
            }

            linkedInstance = instance;
            linkedInstance.OnJobChanged += HandleJobChanged;

            // Applica job corrente (senza transizione)
            if (linkedInstance.CurrentJob != null)
            {
                ApplyVisualSet(linkedInstance.CurrentJob, immediate: true);
            }

            Debug.Log($"<color=cyan>[WorkerVisualController]</color> Initialized for {instance.CustomName}");
        }

        // ============================================
        // JOB CHANGE HANDLER
        // ============================================

        private void HandleJobChanged(WorkerJobData newJob)
        {
            if (newJob == null)
            {
                Debug.LogWarning("[WorkerVisualController] HandleJobChanged called with null job.");
                return;
            }

            Debug.Log($"<color=magenta>[WorkerVisualController]</color> Job changed to: {newJob.JobName}");

            // Avvia transizione visiva
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            transitionCoroutine = StartCoroutine(TransitionToJob(newJob));
        }

        // ============================================
        // VISUAL TRANSITION
        // ============================================

        /// <summary>
        /// Coroutine per la transizione visiva al nuovo job.
        /// </summary>
        private IEnumerator TransitionToJob(WorkerJobData newJob)
        {
            isTransitioning = true;

            float transitionDuration = newJob.VisualSet?.transitionDuration ?? 0.5f;

            // ═══════════════════════════════════════════════════════════
            // 1. VFX PUFF (Sparizione)
            // ═══════════════════════════════════════════════════════════
            PlayChangeVFX(newJob.VisualSet);

            // ═══════════════════════════════════════════════════════════
            // 2. FADE OUT / HIDE (opzionale)
            // ═══════════════════════════════════════════════════════════
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = false;
            }
            if (headRenderer != null) headRenderer.enabled = false;
            if (legsRenderer != null) legsRenderer.enabled = false;

            // ═══════════════════════════════════════════════════════════
            // 3. WAIT
            // ═══════════════════════════════════════════════════════════
            yield return new WaitForSeconds(transitionDuration * 0.5f);

            // ═══════════════════════════════════════════════════════════
            // 4. APPLY VISUAL SET
            // ═══════════════════════════════════════════════════════════
            ApplyVisualSet(newJob, immediate: false);

            // ═══════════════════════════════════════════════════════════
            // 5. WAIT
            // ═══════════════════════════════════════════════════════════
            yield return new WaitForSeconds(transitionDuration * 0.5f);

            // ═══════════════════════════════════════════════════════════
            // 6. FADE IN / SHOW
            // ═══════════════════════════════════════════════════════════
            if (bodyRenderer != null) bodyRenderer.enabled = true;
            if (headRenderer != null && newJob.VisualSet?.headMesh != null) headRenderer.enabled = true;
            if (legsRenderer != null && newJob.VisualSet?.legsMesh != null) legsRenderer.enabled = true;

            // ═══════════════════════════════════════════════════════════
            // 7. VFX PUFF (Riapparizione)
            // ═══════════════════════════════════════════════════════════
            PlayChangeVFX(newJob.VisualSet);

            isTransitioning = false;
            transitionCoroutine = null;

            Debug.Log($"<color=green>[WorkerVisualController]</color> Transition to {newJob.JobName} complete!");
        }

        /// <summary>
        /// Applica il visual set di un job.
        /// </summary>
        private void ApplyVisualSet(WorkerJobData job, bool immediate)
        {
            if (job == null) return;

            currentJobData = job;
            currentJobName = job.JobName;

            // ═══════════════════════════════════════════════════════════
            // DECIDE: Nuovo sistema (mesh swap) o Legacy (prefab swap)
            // ═══════════════════════════════════════════════════════════
            if (job.HasValidVisualSet)
            {
                ApplyMeshSwap(job.VisualSet);
            }
            else if (job.UseLegacySystem)
            {
                ApplyLegacyPrefabSwap(job);
            }
            else
            {
                Debug.LogWarning($"[WorkerVisualController] Job {job.JobName} has no valid visual configuration!");
            }
        }

        // ============================================
        // MESH SWAP SYSTEM (Nuovo)
        // ============================================

        private void ApplyMeshSwap(WorkerVisualSet visualSet)
        {
            if (visualSet == null) return;

            // ═══════════════════════════════════════════════════════════
            // MESHES
            // ═══════════════════════════════════════════════════════════
            if (headRenderer != null && visualSet.headMesh != null)
            {
                headRenderer.sharedMesh = visualSet.headMesh;
            }

            if (bodyRenderer != null && visualSet.bodyMesh != null)
            {
                bodyRenderer.sharedMesh = visualSet.bodyMesh;
            }

            if (legsRenderer != null && visualSet.legsMesh != null)
            {
                legsRenderer.sharedMesh = visualSet.legsMesh;
            }

            // ═══════════════════════════════════════════════════════════
            // MATERIALS
            // ═══════════════════════════════════════════════════════════
            if (bodyRenderer != null && visualSet.bodyMaterialOverride != null)
            {
                bodyRenderer.material = visualSet.bodyMaterialOverride;
            }

            // ═══════════════════════════════════════════════════════════
            // TOOL
            // ═══════════════════════════════════════════════════════════
            UpdateTool(visualSet.toolPrefab, visualSet.toolPositionOffset, visualSet.toolRotationOffset);

            // ═══════════════════════════════════════════════════════════
            // ANIMATOR
            // ═══════════════════════════════════════════════════════════
            if (animator != null && visualSet.animatorController != null)
            {
                animator.runtimeAnimatorController = visualSet.animatorController;
                CheckAnimatorParameters();
                ResetAnimatorState();
            }

            // ═══════════════════════════════════════════════════════════
            // COLOR TINT
            // ═══════════════════════════════════════════════════════════
            ApplyColorTint(visualSet.roleColorTint);

            Debug.Log($"<color=cyan>[WorkerVisualController]</color> Applied mesh swap for {currentJobName}");
        }

        // ============================================
        // LEGACY PREFAB SWAP SYSTEM
        // ============================================

        private void ApplyLegacyPrefabSwap(WorkerJobData job)
        {
            if (legacyVisualRoot == null)
            {
                Debug.LogError("[WorkerVisualController] Legacy prefab swap requested but no legacyVisualRoot assigned!");
                return;
            }

            // Distruggi figli esistenti
            for (int i = legacyVisualRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(legacyVisualRoot.GetChild(i).gameObject);
            }

            // Istanzia nuovo modello
            if (job.VisualModelPrefab != null)
            {
                GameObject newModel = Instantiate(job.VisualModelPrefab, legacyVisualRoot);
                newModel.transform.localPosition = Vector3.zero;
                newModel.transform.localRotation = Quaternion.identity;
                newModel.name = $"Model_{job.JobName}";

                // Rebind animator
                animator = newModel.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    if (job.AnimatorController != null)
                    {
                        animator.runtimeAnimatorController = job.AnimatorController;
                    }
                    CheckAnimatorParameters();
                    ResetAnimatorState();
                }

                Debug.Log($"<color=yellow>[WorkerVisualController]</color> Applied LEGACY prefab swap for {job.JobName}");
            }
        }

        // ============================================
        // TOOL MANAGEMENT
        // ============================================

        private void UpdateTool(GameObject toolPrefab, Vector3 posOffset, Vector3 rotOffset)
        {
            // Distruggi tool corrente
            if (currentToolInstance != null)
            {
                Destroy(currentToolInstance);
                currentToolInstance = null;
            }

            // Spawn nuovo tool
            if (toolPrefab != null && toolSocket != null)
            {
                currentToolInstance = Instantiate(toolPrefab, toolSocket);
                currentToolInstance.transform.localPosition = posOffset;
                currentToolInstance.transform.localRotation = Quaternion.Euler(rotOffset);
                currentToolInstance.name = $"Tool_{toolPrefab.name}";

                Debug.Log($"<color=cyan>[WorkerVisualController]</color> Equipped tool: {toolPrefab.name}");
            }
        }

        /// <summary>
        /// Forza l'equipaggiamento di un tool specifico (override temporaneo).
        /// </summary>
        public void ForceEquipTool(GameObject toolPrefab)
        {
            UpdateTool(toolPrefab, Vector3.zero, Vector3.zero);
        }

        /// <summary>
        /// Rimuove il tool corrente.
        /// </summary>
        public void UnequipTool()
        {
            if (currentToolInstance != null)
            {
                Destroy(currentToolInstance);
                currentToolInstance = null;
            }
        }

        // ============================================
        // COLOR TINT
        // ============================================

        private void ApplyColorTint(Color color)
        {
            if (tintableRenderers == null || tintableRenderers.Length == 0) return;

            foreach (var renderer in tintableRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    if (renderer.material.HasProperty(colorPropertyName))
                    {
                        renderer.material.SetColor(colorPropertyName, color);
                    }
                }
            }
        }

        // ============================================
        // VFX
        // ============================================

        private void PlayChangeVFX(WorkerVisualSet visualSet)
        {
            // Built-in particle system
            if (changeJobVFX != null)
            {
                changeJobVFX.Play();
            }

            // Custom VFX da visual set
            if (visualSet?.jobChangeVFXPrefab != null && vfxSpawnPoint != null)
            {
                var vfx = Instantiate(visualSet.jobChangeVFXPrefab, vfxSpawnPoint.position, Quaternion.identity);
                Destroy(vfx, 3f); // Auto-destroy dopo 3 secondi
            }
        }

        // ============================================
        // ANIMATOR HELPERS
        // ============================================

        private void CheckAnimatorParameters()
        {
            hasSpeedParam = false;
            hasIsWorkingParam = false;
            hasIsMovingParam = false;

            if (animator == null || animator.runtimeAnimatorController == null) return;

            foreach (var param in animator.parameters)
            {
                if (param.nameHash == SpeedHash) hasSpeedParam = true;
                if (param.nameHash == IsWorkingHash) hasIsWorkingParam = true;
                if (param.nameHash == IsMovingHash) hasIsMovingParam = true;
            }
        }

        private void ResetAnimatorState()
        {
            if (animator == null) return;

            if (hasSpeedParam) animator.SetFloat(SpeedHash, 0f);
            if (hasIsMovingParam) animator.SetBool(IsMovingHash, false);
            if (hasIsWorkingParam) animator.SetBool(IsWorkingHash, false);

            animator.Update(0f);
        }

        /// <summary>
        /// Aggiorna i parametri dell'animator (chiamato da WorkerController).
        /// </summary>
        public void UpdateAnimator(float speed, bool isMoving, bool isWorking)
        {
            if (animator == null || isTransitioning) return;

            if (hasSpeedParam) animator.SetFloat(SpeedHash, speed);
            if (hasIsMovingParam) animator.SetBool(IsMovingHash, isMoving);
            if (hasIsWorkingParam) animator.SetBool(IsWorkingHash, isWorking);
        }

        /// <summary>
        /// Forza lo stato idle dell'animator.
        /// </summary>
        public void ForceIdleAnimation()
        {
            ResetAnimatorState();
            if (animator != null)
            {
                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Test Transition to Builder", ButtonSizes.Medium)]
        private void DebugTestBuilder()
        {
            if (!Application.isPlaying) return;
            var job = ActiveJobDatabase?.GetJobData(WorkerRole.Builder);
            if (job != null) HandleJobChanged(job);
        }

        [Button("Test Transition to Gatherer", ButtonSizes.Medium)]
        private void DebugTestGatherer()
        {
            if (!Application.isPlaying) return;
            var job = ActiveJobDatabase?.GetJobData(WorkerRole.Gatherer);
            if (job != null) HandleJobChanged(job);
        }

        [Button("Reset to Default", ButtonSizes.Medium)]
        private void DebugResetToDefault()
        {
            if (!Application.isPlaying) return;
            var job = ActiveJobDatabase?.GetDefaultJob();
            if (job != null) HandleJobChanged(job);
        }

        [Button("Print Visual State", ButtonSizes.Medium)]
        private void DebugPrintState()
        {
            Debug.Log($"=== VISUAL STATE ===\n" +
                     $"Current Job: {currentJobName}\n" +
                     $"Is Transitioning: {isTransitioning}\n" +
                     $"Body Mesh: {(bodyRenderer?.sharedMesh?.name ?? "None")}\n" +
                     $"Tool: {(currentToolInstance?.name ?? "None")}\n" +
                     $"Animator: {(animator?.runtimeAnimatorController?.name ?? "None")}");
        }
#endif
    }

    // ============================================
    // EXTENSION METHODS
    // ============================================

    public static class TransformExtensions
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
