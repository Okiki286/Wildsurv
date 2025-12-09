using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// ORCHESTRATORE per la gestione visuale del worker - MOBILE OPTIMIZED.
    ///
    /// Responsabilità:
    /// - Coordina WorkerMeshController, WorkerAnimatorController, WorkerToolController
    /// - Ascolta eventi OnJobChanged da WorkerInstance
    /// - Applica WorkerVisualSet in modo data-driven
    /// - Integra JobRolePalette per colori standard
    /// - Gestisce transizioni visive lightweight (zero GC)
    ///
    /// Ottimizzazioni mobile:
    /// - NO LINQ
    /// - NO renderer.material (solo sharedMaterial)
    /// - NO allocazioni in runtime
    /// - NO Find() a runtime
    /// - Caching completo di componenti
    ///
    /// API pubblica:
    /// - Initialize(WorkerInstance, JobRolePalette) : setup runtime
    /// - OnJobChanged(WorkerJobData) : listener per job change
    /// - ApplyJobVisual(WorkerJobData, bool useTransition) : applicazione diretta (editor/preview)
    /// </summary>
    [RequireComponent(typeof(WorkerMeshController))]
    [RequireComponent(typeof(WorkerAnimatorController))]
    [RequireComponent(typeof(WorkerToolController))]
    public class WorkerVisualController : MonoBehaviour
    {
        // ============================================
        // SUB-CONTROLLERS (cached in Awake)
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
        // ROLE PALETTE & CONFIGURATION
        // ============================================

        [TitleGroup("Configuration")]
        [SerializeField]
        [Tooltip("Palette colori standard per ruoli (opzionale). Override in Initialize()")]
        private JobRolePalette rolePalette;

        [SerializeField]
        [Tooltip("Abilita transizioni visive con VFX durante cambio job runtime")]
        private bool useRuntimeTransitions = true;

        // ============================================
        // VFX
        // ============================================

        [TitleGroup("VFX")]
        [SerializeField]
        [Tooltip("ParticleSystem built-in per effetto cambio job (opzionale)")]
        private ParticleSystem changeJobVFX;

        [SerializeField]
        [Tooltip("Transform spawn point per VFX custom dal VisualSet")]
        private Transform vfxSpawnPoint;

        // ============================================
        // LEGACY SUPPORT
        // ============================================

        [TitleGroup("Legacy Support")]
        [SerializeField]
        [Tooltip("Transform contenitore per sistema legacy (prefab swap). Deprecato.")]
        private Transform legacyVisualRoot;

        // ============================================
        // RUNTIME STATE (no allocations)
        // ============================================

        private WorkerInstance linkedInstance;
        private WorkerJobData currentJobData;
        private Coroutine transitionCoroutine;

        [TitleGroup("Runtime State")]
        [ShowInInspector, ReadOnly]
        private bool isTransitioning = false;

        [ShowInInspector, ReadOnly]
        private string currentJobName = "None";

        [ShowInInspector, ReadOnly]
        private bool hasInstance = false;

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
            CacheComponents();
        }

        private void OnDestroy()
        {
            // Cleanup eventi
            if (linkedInstance != null)
            {
                linkedInstance.OnJobChanged -= OnJobChanged;
            }

            // Stop coroutine se attiva
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }
        }

        // ============================================
        // COMPONENT CACHING (chiamato UNA volta in Awake)
        // ============================================

        private void CacheComponents()
        {
            // Cache sub-controllers (RequireComponent garantisce che esistano)
            meshController = GetComponent<WorkerMeshController>();
            animatorController = GetComponent<WorkerAnimatorController>();
            toolController = GetComponent<WorkerToolController>();

            // Auto-find legacy visual root se non assegnato
            if (legacyVisualRoot == null)
            {
                var existing = transform.Find("VisualRoot");
                if (existing != null)
                {
                    legacyVisualRoot = existing;
                }
            }

#if UNITY_EDITOR
            // Validazione componenti (solo editor)
            if (meshController == null)
            {
                Debug.LogError("[WorkerVisualController] WorkerMeshController not found!", this);
            }
            if (animatorController == null)
            {
                Debug.LogError("[WorkerVisualController] WorkerAnimatorController not found!", this);
            }
            if (toolController == null)
            {
                Debug.LogError("[WorkerVisualController] WorkerToolController not found!", this);
            }
#endif
        }

        // ============================================
        // PUBLIC API - INITIALIZATION
        // ============================================

        /// <summary>
        /// Inizializza il controller per uso runtime.
        /// Chiamato da WorkerSystem/WorkerController dopo spawn.
        /// </summary>
        /// <param name="instance">WorkerInstance collegato (required)</param>
        /// <param name="palette">JobRolePalette override (opzionale, usa field inspector se null)</param>
        public void Initialize(WorkerInstance instance, JobRolePalette palette = null)
        {
            if (instance == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerVisualController] Initialize called with null instance.", this);
#endif
                return;
            }

            // Override palette se fornita
            if (palette != null)
            {
                rolePalette = palette;
            }

            // Unsubscribe da istanza precedente
            if (linkedInstance != null)
            {
                linkedInstance.OnJobChanged -= OnJobChanged;
            }

            // Link nuova istanza
            linkedInstance = instance;
            hasInstance = true;
            linkedInstance.OnJobChanged += OnJobChanged;

            // Applica job corrente immediatamente (senza transizione iniziale)
            if (linkedInstance.CurrentJob != null)
            {
                ApplyJobVisual(linkedInstance.CurrentJob, useTransition: false);
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerVisualController]</color> Initialized for {instance.CustomName}", this);
#endif
        }

        // ============================================
        // PUBLIC API - JOB CHANGE (EVENT HANDLER)
        // ============================================

        /// <summary>
        /// Handler per evento OnJobChanged da WorkerInstance.
        /// Può anche essere chiamato manualmente se necessario.
        /// </summary>
        public void OnJobChanged(WorkerJobData newJob)
        {
            if (newJob == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerVisualController] OnJobChanged called with null job.", this);
#endif
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=magenta>[WorkerVisualController]</color> Job changed to: {newJob.JobName}", this);
#endif

            // Applica con transizione se abilitata
            ApplyJobVisual(newJob, useTransition: useRuntimeTransitions);
        }

        // ============================================
        // PUBLIC API - APPLY JOB VISUAL (EDITOR/PREVIEW)
        // ============================================

        /// <summary>
        /// Applica direttamente un job visual (overload senza parametri - backward compatibility).
        /// Usato da JobAuthoringWindow per preview.
        /// Applica immediatamente senza transizione.
        /// </summary>
        public void ApplyJobVisual(WorkerJobData jobData)
        {
            ApplyJobVisual(jobData, useTransition: false);
        }

        /// <summary>
        /// Applica direttamente un job visual.
        /// Usato da:
        /// - JobAuthoringWindow (preview)
        /// - Setup iniziale
        /// - Chiamate manuali
        ///
        /// NON richiede WorkerInstance, funziona anche in editor.
        /// </summary>
        /// <param name="jobData">Job da applicare</param>
        /// <param name="useTransition">Se true, usa coroutine con VFX. Se false, applica immediatamente.</param>
        public void ApplyJobVisual(WorkerJobData jobData, bool useTransition)
        {
            if (jobData == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerVisualController] ApplyJobVisual called with null jobData.", this);
#endif
                return;
            }

            // Stop transizione corrente se in corso
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
                isTransitioning = false;
            }

            if (useTransition)
            {
                // Transizione con VFX (runtime)
                transitionCoroutine = StartCoroutine(JobTransitionRoutine(jobData));
            }
            else
            {
                // Applicazione immediata (editor, setup iniziale)
                ApplyVisualSetInternal(jobData.VisualSet, jobData.Role, immediate: true);
                currentJobData = jobData;
                currentJobName = jobData.JobName;

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[WorkerVisualController]</color> Applied job visual for {jobData.JobName} (immediate)", this);
#endif
            }
        }

        // ============================================
        // VISUAL TRANSITION (MOBILE OPTIMIZED - ZERO GC)
        // ============================================

        /// <summary>
        /// Coroutine per transizione visiva con VFX.
        /// Ottimizzata: nessuna allocazione, nessun LINQ, nessun renderer.material.
        /// </summary>
        private IEnumerator JobTransitionRoutine(WorkerJobData newJob)
        {
            isTransitioning = true;

            WorkerVisualSet visualSet = newJob.VisualSet;
            float transitionDuration = visualSet?.transitionDuration ?? 0.5f;
            float halfDuration = transitionDuration * 0.5f;

            // ═══════════════════════════════════════════════════════════
            // 1. VFX START + HIDE RENDERERS
            // ═══════════════════════════════════════════════════════════
            PlayChangeVFX(visualSet);

            if (meshController != null)
            {
                meshController.HideRenderers();
            }

            // ═══════════════════════════════════════════════════════════
            // 2. WAIT (prima metà transizione)
            // ═══════════════════════════════════════════════════════════
            yield return new WaitForSeconds(halfDuration);

            // ═══════════════════════════════════════════════════════════
            // 3. APPLY VISUAL SET (al centro della transizione)
            // ═══════════════════════════════════════════════════════════
            ApplyVisualSetInternal(visualSet, newJob.Role, immediate: false);
            currentJobData = newJob;
            currentJobName = newJob.JobName;

            // ═══════════════════════════════════════════════════════════
            // 4. WAIT (seconda metà transizione)
            // ═══════════════════════════════════════════════════════════
            yield return new WaitForSeconds(halfDuration);

            // ═══════════════════════════════════════════════════════════
            // 5. SHOW RENDERERS + VFX END
            // ═══════════════════════════════════════════════════════════
            if (meshController != null)
            {
                meshController.ShowRenderers(visualSet);
            }

            PlayChangeVFX(visualSet);

            // ═══════════════════════════════════════════════════════════
            // 6. NOTIFICA COMPLETAMENTO (se c'è WorkerInstance)
            // ═══════════════════════════════════════════════════════════
            if (linkedInstance != null)
            {
                linkedInstance.CompleteJobTransition();
            }

            // ═══════════════════════════════════════════════════════════
            // 7. CLEANUP
            // ═══════════════════════════════════════════════════════════
            isTransitioning = false;
            transitionCoroutine = null;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[WorkerVisualController]</color> Transition to {newJob.JobName} complete!", this);
#endif
        }

        // ============================================
        // APPLY VISUAL SET (CORE LOGIC - DATA DRIVEN)
        // ============================================

        /// <summary>
        /// Applica un WorkerVisualSet delegando ai sub-controllers.
        /// Integra JobRolePalette per colori standard.
        ///
        /// MOBILE OPTIMIZED:
        /// - No allocations
        /// - No LINQ
        /// - Solo sharedMaterial
        /// </summary>
        private void ApplyVisualSetInternal(WorkerVisualSet visualSet, WorkerRole role, bool immediate)
        {
            if (visualSet == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerVisualController] ApplyVisualSetInternal called with null visualSet.", this);
#endif
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // 1. MESH + MATERIALS (via MeshController)
            // ═══════════════════════════════════════════════════════════
            if (meshController != null)
            {
                meshController.ApplyMeshSwap(visualSet);
            }

            // ═══════════════════════════════════════════════════════════
            // 2. COLOR TINT (con JobRolePalette integration)
            // ═══════════════════════════════════════════════════════════
            if (meshController != null)
            {
                Color finalColor = GetFinalTintColor(visualSet, role);
                meshController.ApplyColorTint(finalColor);
            }

            // ═══════════════════════════════════════════════════════════
            // 3. TOOL (via ToolController)
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
            // 4. ANIMATOR (via AnimatorController)
            // ═══════════════════════════════════════════════════════════
            if (animatorController != null && visualSet.animatorController != null)
            {
                animatorController.SetAnimatorController(visualSet.animatorController);
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerVisualController]</color> Applied visual set for role {role}", this);
#endif
        }

        // ============================================
        // COLOR TINT LOGIC (with JobRolePalette)
        // ============================================

        /// <summary>
        /// Determina il colore finale da applicare:
        /// - Se visualSet ha colore custom (non bianco) → usa quello
        /// - Altrimenti, se rolePalette esiste → usa colore standard del ruolo
        /// - Altrimenti → usa bianco
        /// </summary>
        private Color GetFinalTintColor(WorkerVisualSet visualSet, WorkerRole role)
        {
            Color visualSetColor = visualSet.roleColorTint;

            // Se il visual set ha un colore custom (diverso da bianco), usalo
            if (visualSetColor != Color.white)
            {
                return visualSetColor;
            }

            // Altrimenti, prova a usare il colore dalla palette
            if (rolePalette != null)
            {
                return rolePalette.GetColor(role, Color.white);
            }

            // Fallback: bianco
            return Color.white;
        }

        // ============================================
        // VFX (lightweight, no allocations)
        // ============================================

        /// <summary>
        /// Riproduce VFX cambio job.
        /// - Built-in ParticleSystem (se assegnato)
        /// - Custom VFX dal VisualSet (se assegnato)
        /// </summary>
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
                GameObject vfx = Instantiate(visualSet.jobChangeVFXPrefab, vfxSpawnPoint.position, Quaternion.identity);
                Destroy(vfx, 3f); // Auto-destroy dopo 3 secondi
            }
        }

        // ============================================
        // LEGACY SYSTEM (DEPRECATO - da migrare)
        // ============================================

        /// <summary>
        /// Sistema legacy per prefab swap.
        /// DEPRECATO: usare mesh swap invece.
        /// Mantenuto per backward compatibility.
        /// </summary>
        private void ApplyLegacyPrefabSwap(WorkerJobData job)
        {
            if (legacyVisualRoot == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[WorkerVisualController] Legacy prefab swap requested but no legacyVisualRoot assigned!", this);
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

#if UNITY_EDITOR
                Debug.LogWarning($"<color=yellow>[WorkerVisualController]</color> Applied LEGACY prefab swap for {job.JobName}. Consider migrating to mesh swap.", this);
#endif
            }
        }

        // ============================================
        // PUBLIC API - ANIMATOR CONTROL (delegate)
        // ============================================

        /// <summary>
        /// Aggiorna parametri animator (chiamato da WorkerController ogni frame).
        /// Delega ad AnimatorController.
        /// </summary>
        public void UpdateAnimator(float speed, bool isMoving, bool isWorking)
        {
            if (animatorController != null && !isTransitioning)
            {
                animatorController.UpdateAnimator(speed, isMoving, isWorking);
            }
        }

        /// <summary>
        /// Forza stato idle dell'animator.
        /// Delega ad AnimatorController.
        /// </summary>
        public void ForceIdleAnimation()
        {
            if (animatorController != null)
            {
                animatorController.ForceIdleAnimation();
            }
        }

        // ============================================
        // PUBLIC API - TOOL CONTROL (delegate)
        // ============================================

        /// <summary>
        /// Forza equipaggiamento di un tool specifico (override temporaneo).
        /// Delega a ToolController.
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
        /// Delega a ToolController.
        /// </summary>
        public void UnequipTool()
        {
            if (toolController != null)
            {
                toolController.UnequipTool();
            }
        }

        // ============================================
        // DEBUG TOOLS (EDITOR ONLY)
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Tools")]
        [InfoBox("Test job transitions in Play Mode. Requires JobDatabase.", InfoMessageType.None)]

        [Button("Test → Builder", ButtonSizes.Medium)]
        private void DebugTestBuilder()
        {
            if (!Application.isPlaying) return;

            var db = FindFirstObjectByType<JobDatabase>();
            if (db != null)
            {
                var job = db.GetJobData(WorkerRole.Builder);
                if (job != null) OnJobChanged(job);
            }
        }

        [Button("Test → Gatherer", ButtonSizes.Medium)]
        private void DebugTestGatherer()
        {
            if (!Application.isPlaying) return;

            var db = FindFirstObjectByType<JobDatabase>();
            if (db != null)
            {
                var job = db.GetJobData(WorkerRole.Gatherer);
                if (job != null) OnJobChanged(job);
            }
        }

        [Button("Print Visual State", ButtonSizes.Medium)]
        private void DebugPrintState()
        {
            Debug.Log($"=== WORKER VISUAL STATE ===\n" +
                     $"Current Job: {currentJobName}\n" +
                     $"Is Transitioning: {isTransitioning}\n" +
                     $"Has Instance: {hasInstance}\n" +
                     $"Body Mesh: {(meshController?.BodyRenderer?.sharedMesh?.name ?? "None")}\n" +
                     $"Tool: {(toolController?.CurrentTool?.name ?? "None")}\n" +
                     $"Animator: {(animatorController?.CurrentController?.name ?? "None")}\n" +
                     $"Role Palette: {(rolePalette != null ? rolePalette.name : "None")}", this);
        }
#endif
    }
}
