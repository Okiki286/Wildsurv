using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// ORCHESTRATORE per la gestione visuale del worker.
    /// Delega le responsabilità ai controller specializzati:
    /// - WorkerMeshController: gestisce mesh, materials, colors
    /// - WorkerAnimatorController: gestisce animator e parametri
    /// - WorkerToolController: gestisce tool socket ed equipaggiamento
    ///
    /// Si sottoscrive a OnJobChanged e coordina la transizione visiva.
    /// </summary>
    [RequireComponent(typeof(WorkerMeshController))]
    [RequireComponent(typeof(WorkerAnimatorController))]
    [RequireComponent(typeof(WorkerToolController))]
    public class WorkerVisualController : MonoBehaviour
    {
        // ============================================
        // SUB-CONTROLLERS
        // ============================================

        [TitleGroup("Sub-Controllers")]
        [SerializeField, ReadOnly]
        [Tooltip("Gestisce mesh, materials e colors")]
        private WorkerMeshController meshController;

        [SerializeField, ReadOnly]
        [Tooltip("Gestisce animator e parametri")]
        private WorkerAnimatorController animatorController;

        [SerializeField, ReadOnly]
        [Tooltip("Gestisce tool socket e equipaggiamento")]
        private WorkerToolController toolController;

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
        public WorkerAnimatorController AnimatorController => animatorController;
        public WorkerMeshController MeshController => meshController;
        public WorkerToolController ToolController => toolController;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Auto-find sub-controllers
            meshController = GetComponent<WorkerMeshController>();
            animatorController = GetComponent<WorkerAnimatorController>();
            toolController = GetComponent<WorkerToolController>();

            // Auto-create legacy visual root se necessario
            if (legacyVisualRoot == null)
            {
                var existing = transform.Find("VisualRoot");
                if (existing != null)
                {
                    legacyVisualRoot = existing;
                }
            }

#if UNITY_EDITOR
            if (meshController == null)
            {
                Debug.LogError("[WorkerVisualController] WorkerMeshController not found! Add it to the GameObject.");
            }
            if (animatorController == null)
            {
                Debug.LogError("[WorkerVisualController] WorkerAnimatorController not found! Add it to the GameObject.");
            }
            if (toolController == null)
            {
                Debug.LogError("[WorkerVisualController] WorkerToolController not found! Add it to the GameObject.");
            }
#endif
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
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerVisualController] Initialize called with null instance.");
#endif
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

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerVisualController]</color> Initialized for {instance.CustomName}");
#endif
        }

        // ============================================
        // JOB CHANGE HANDLER
        // ============================================

        private void HandleJobChanged(WorkerJobData newJob)
        {
            if (newJob == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerVisualController] HandleJobChanged called with null job.");
#endif
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=magenta>[WorkerVisualController]</color> Job changed to: {newJob.JobName}");
#endif

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
            // 2. FADE OUT / HIDE
            // ═══════════════════════════════════════════════════════════
            if (meshController != null)
            {
                meshController.HideRenderers();
            }

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
            if (meshController != null)
            {
                meshController.ShowRenderers(newJob.VisualSet);
            }

            // ═══════════════════════════════════════════════════════════
            // 7. VFX PUFF (Riapparizione)
            // ═══════════════════════════════════════════════════════════
            PlayChangeVFX(newJob.VisualSet);

            isTransitioning = false;
            transitionCoroutine = null;

            // ═══════════════════════════════════════════════════════════
            // 8. NOTIFICA COMPLETAMENTO TRANSIZIONE
            // ═══════════════════════════════════════════════════════════
            if (linkedInstance != null)
            {
                linkedInstance.CompleteJobTransition();
            }

#if UNITY_EDITOR
            Debug.Log($"<color=green>[WorkerVisualController]</color> Transition to {newJob.JobName} complete!");
#endif
        }

        /// <summary>
        /// Applica il visual set di un job.
        /// ORCHESTRATORE: delega ai sub-controllers.
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
                ApplyMeshSwapSystem(job.VisualSet);
            }
            else if (job.UseLegacySystem)
            {
                ApplyLegacyPrefabSwap(job);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[WorkerVisualController] Job {job.JobName} has no valid visual configuration!");
#endif
            }
        }

        // ============================================
        // MESH SWAP SYSTEM (Nuovo) - ORCHESTRATORE
        // ============================================

        /// <summary>
        /// Applica mesh swap delegando ai sub-controllers.
        /// </summary>
        private void ApplyMeshSwapSystem(WorkerVisualSet visualSet)
        {
            if (visualSet == null) return;

            // ═══════════════════════════════════════════════════════════
            // DELEGA A MESH CONTROLLER
            // ═══════════════════════════════════════════════════════════
            if (meshController != null)
            {
                meshController.ApplyMeshSwap(visualSet);
            }

            // ═══════════════════════════════════════════════════════════
            // DELEGA A TOOL CONTROLLER
            // ═══════════════════════════════════════════════════════════
            if (toolController != null)
            {
                toolController.EquipTool(
                    visualSet.toolPrefab,
                    visualSet.toolPositionOffset,
                    visualSet.toolRotationOffset
                );
            }

            // ═══════════════════════════════════════════════════════════
            // DELEGA AD ANIMATOR CONTROLLER
            // ═══════════════════════════════════════════════════════════
            if (animatorController != null && visualSet.animatorController != null)
            {
                animatorController.SetAnimatorController(visualSet.animatorController);
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerVisualController]</color> Applied mesh swap for {currentJobName}");
#endif
        }

        // ============================================
        // LEGACY PREFAB SWAP SYSTEM
        // ============================================

        private void ApplyLegacyPrefabSwap(WorkerJobData job)
        {
            if (legacyVisualRoot == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[WorkerVisualController] Legacy prefab swap requested but no legacyVisualRoot assigned!");
#endif
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

                // Rebind animator nel nuovo modello
                var newAnimator = newModel.GetComponentInChildren<Animator>();
                if (newAnimator != null && animatorController != null)
                {
                    // Sostituisci l'animator nell'AnimatorController
                    // (necessita di reflection o rebuild del component)
                    // Per ora, loggare warning
#if UNITY_EDITOR
                    Debug.LogWarning("[WorkerVisualController] Legacy mode: animator rebinding not fully supported. Consider migrating to mesh swap.");
#endif
                }

#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[WorkerVisualController]</color> Applied LEGACY prefab swap for {job.JobName}");
#endif
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
        // PUBLIC API (chiamate da WorkerController)
        // ============================================

        /// <summary>
        /// Aggiorna i parametri dell'animator (chiamato da WorkerController).
        /// DELEGA AD AnimatorController.
        /// </summary>
        public void UpdateAnimator(float speed, bool isMoving, bool isWorking)
        {
            if (animatorController != null && !isTransitioning)
            {
                animatorController.UpdateAnimator(speed, isMoving, isWorking);
            }
        }

        /// <summary>
        /// Forza lo stato idle dell'animator.
        /// DELEGA AD AnimatorController.
        /// </summary>
        public void ForceIdleAnimation()
        {
            if (animatorController != null)
            {
                animatorController.ForceIdleAnimation();
            }
        }

        /// <summary>
        /// Forza l'equipaggiamento di un tool specifico (override temporaneo).
        /// DELEGA A ToolController.
        /// </summary>
        public void ForceEquipTool(GameObject toolPrefab)
        {
            if (toolController != null)
            {
                toolController.EquipTool(toolPrefab);
            }
        }

        /// <summary>
        /// Rimuove il tool corrente.
        /// DELEGA A ToolController.
        /// </summary>
        public void UnequipTool()
        {
            if (toolController != null)
            {
                toolController.UnequipTool();
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
                     $"Body Mesh: {(meshController?.BodyRenderer?.sharedMesh?.name ?? "None")}\n" +
                     $"Tool: {(toolController?.CurrentTool?.name ?? "None")}\n" +
                     $"Animator: {(animatorController?.CurrentController?.name ?? "None")}");
        }
#endif
    }
}
