using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// WorkerMeshController v2 - MOBILE OPTIMIZED & PRODUCTION READY
    ///
    /// Responsabilità:
    /// - Gestione mesh swap modulare (head/body/legs)
    /// - Material override senza instancing
    /// - Color tint su tutti i renderer rilevanti
    /// - Supporto LODGroup
    /// - Zero allocazioni GC
    /// - Editor preview compatible
    ///
    /// Ottimizzazioni:
    /// - Caching completo componenti in Initialize()
    /// - NO renderer.material (solo sharedMaterial)
    /// - NO LINQ
    /// - NO Find() a runtime
    /// - Fallback robusti per mesh/material null
    ///
    /// API:
    /// - Initialize() : setup e caching (chiamato una volta)
    /// - ApplyMeshSwap(WorkerVisualSet) : applica meshes + materials + tint
    /// - ApplyMaterialOverride(Material) : override materiale body
    /// - ApplyColorTint(Color) : applica tint a tutti i renderer
    /// - ResetToDefault() : ripristina stato originale
    /// - HideRenderers() / ShowRenderers() : per transizioni
    /// </summary>
    public class WorkerMeshController : MonoBehaviour
    {
        // ============================================
        // MESH COMPONENTS (auto-detected or assigned)
        // ============================================

        [TitleGroup("Mesh Components")]
        [InfoBox("Supporta sia mesh modulari (head/body/legs) che mesh intere (fullBodyRenderer).\n" +
                 "Se usi mesh intere, assegna solo fullBodyRenderer e lascia gli altri vuoti.", InfoMessageType.Info)]

        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per mesh intera (full-body). Usalo se hai un'unica mesh per tutto il personaggio.")]
        private SkinnedMeshRenderer fullBodyRenderer;

        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per la testa (opzionale, solo se usi sistema modulare)")]
        private SkinnedMeshRenderer headRenderer;

        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per il corpo (opzionale, solo se usi sistema modulare)")]
        private SkinnedMeshRenderer bodyRenderer;

        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per le gambe (opzionale, solo se usi sistema modulare)")]
        private SkinnedMeshRenderer legsRenderer;

        // ============================================
        // COLOR TINT CONFIGURATION
        // ============================================

        [TitleGroup("Color Tint")]
        [InfoBox("Renderer per color tint (fascia, mantello, accessori). Se vuoto, usa tutti i renderer trovati.", InfoMessageType.None)]

        [SerializeField]
        [Tooltip("Renderer specifici per color tint (opzionale)")]
        private Renderer[] tintableRenderers;

        [SerializeField]
        [Tooltip("Property name per color tint (_BaseColor per URP, _Color per Standard)")]
        private string[] colorPropertyNames = new string[] { "_BaseColor", "_Color" };

        // ============================================
        // LOD SUPPORT
        // ============================================

        [TitleGroup("LOD Support")]
        [SerializeField]
        [Tooltip("LODGroup componente (opzionale, auto-detected)")]
        private LODGroup lodGroup;

        [SerializeField]
        [Tooltip("Applica tint anche ai renderer nei LOD")]
        private bool applyTintToLODs = true;

        // ============================================
        // RUNTIME CACHE (no allocations)
        // ============================================

        // Original state per reset
        private Mesh originalFullBodyMesh;
        private Mesh originalHeadMesh;
        private Mesh originalBodyMesh;
        private Mesh originalLegsMesh;
        private Material[] originalFullBodyMaterials;
        private Material[] originalBodyMaterials;
        private Material[] originalHeadMaterials;
        private Material[] originalLegsMaterials;

        // All renderers cache (per color tint)
        private List<Renderer> allRenderersCache = new List<Renderer>(8);

        // LOD renderers cache
        private List<Renderer> lodRenderersCache = new List<Renderer>(16);

        // Initialization flag
        private bool isInitialized = false;

        // ============================================
        // PROPERTIES (public read-only access)
        // ============================================

        public SkinnedMeshRenderer FullBodyRenderer => fullBodyRenderer;
        public SkinnedMeshRenderer HeadRenderer => headRenderer;
        public SkinnedMeshRenderer BodyRenderer => bodyRenderer;
        public SkinnedMeshRenderer LegsRenderer => legsRenderer;
        public bool IsInitialized => isInitialized;
        public bool IsFullBodyMode => fullBodyRenderer != null;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Auto-initialize (può essere chiamato anche manualmente)
            Initialize();
        }

        // ============================================
        // PUBLIC API - INITIALIZATION
        // ============================================

        /// <summary>
        /// Inizializza il controller: rileva componenti, cache originali, prepara strutture.
        /// Chiamato automaticamente in Awake, può essere richiamato manualmente.
        /// Thread-safe per editor preview.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            // 1. Detect mesh components
            DetectMeshComponents();

            // 2. Cache original state
            CacheOriginalState();

            // 3. Detect LOD group
            DetectLODGroup();

            // 4. Build renderers cache
            BuildRenderersCache();

            isInitialized = true;

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerMeshController]</color> Initialized on {gameObject.name}", this);
#endif
        }

        // ============================================
        // COMPONENT DETECTION (chiamato UNA volta)
        // ============================================

        /// <summary>
        /// Rileva automaticamente i mesh components se non assegnati.
        /// Supporta sia sistema full-body che modulare.
        /// </summary>
        private void DetectMeshComponents()
        {
            // Ottieni tutti i SkinnedMeshRenderer nei figli
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerMeshController] No SkinnedMeshRenderer found in children!", this);
#endif
                return;
            }

            // Se c'è solo UN renderer → full-body mode
            if (renderers.Length == 1 && fullBodyRenderer == null)
            {
                fullBodyRenderer = renderers[0];
#if UNITY_EDITOR
                Debug.Log($"[WorkerMeshController] Full-body mode detected: {fullBodyRenderer.name}", this);
#endif
                return;
            }

            // Se fullBodyRenderer è già assegnato, usa solo quello
            if (fullBodyRenderer != null)
            {
#if UNITY_EDITOR
                Debug.Log($"[WorkerMeshController] Using full-body renderer: {fullBodyRenderer.name}", this);
#endif
                return;
            }

            // Altrimenti, modalità modulare (head/body/legs)
            // Se già assegnati tutti, skip
            if (headRenderer != null && bodyRenderer != null && legsRenderer != null)
                return;

            // Auto-assign per nome
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer smr = renderers[i];
                string name = smr.gameObject.name.ToLower();

                // Full-body detection (se contiene "full", "complete", "character")
                if (fullBodyRenderer == null &&
                    (name.Contains("full") || name.Contains("complete") || name.Contains("character") || name.Contains("whole")))
                {
                    fullBodyRenderer = smr;
#if UNITY_EDITOR
                    Debug.Log($"[WorkerMeshController] Full-body renderer detected: {fullBodyRenderer.name}", this);
#endif
                    return; // Usa solo full-body
                }

                // Head detection
                if (headRenderer == null && (name.Contains("head") || name.Contains("testa") || name.Contains("face")))
                {
                    headRenderer = smr;
                    continue;
                }

                // Body detection
                if (bodyRenderer == null && (name.Contains("body") || name.Contains("torso") || name.Contains("chest")))
                {
                    bodyRenderer = smr;
                    continue;
                }

                // Legs detection
                if (legsRenderer == null && (name.Contains("leg") || name.Contains("gamba") || name.Contains("pant")))
                {
                    legsRenderer = smr;
                    continue;
                }
            }

            // Fallback: assegna primi renderer trovati se ancora null
            if (fullBodyRenderer == null && bodyRenderer == null && renderers.Length > 0)
            {
                bodyRenderer = renderers[0];
            }

            if (headRenderer == null && renderers.Length > 1)
            {
                headRenderer = renderers[1];
            }

            if (legsRenderer == null && renderers.Length > 2)
            {
                legsRenderer = renderers[2];
            }

#if UNITY_EDITOR
            if (fullBodyRenderer != null)
            {
                Debug.Log($"[WorkerMeshController] Full-body mode: {fullBodyRenderer.name}", this);
            }
            else
            {
                Debug.Log($"[WorkerMeshController] Modular mode: Head={headRenderer?.name}, Body={bodyRenderer?.name}, Legs={legsRenderer?.name}", this);
            }
#endif
        }

        /// <summary>
        /// Cache lo stato originale dei mesh e materiali per reset.
        /// </summary>
        private void CacheOriginalState()
        {
            // Cache full-body (priorità)
            if (fullBodyRenderer != null)
            {
                originalFullBodyMesh = fullBodyRenderer.sharedMesh;
                originalFullBodyMaterials = fullBodyRenderer.sharedMaterials;
            }

            // Cache meshes modulari (solo se non full-body)
            if (headRenderer != null)
            {
                originalHeadMesh = headRenderer.sharedMesh;
                originalHeadMaterials = headRenderer.sharedMaterials;
            }

            if (bodyRenderer != null)
            {
                originalBodyMesh = bodyRenderer.sharedMesh;
                originalBodyMaterials = bodyRenderer.sharedMaterials;
            }

            if (legsRenderer != null)
            {
                originalLegsMesh = legsRenderer.sharedMesh;
                originalLegsMaterials = legsRenderer.sharedMaterials;
            }
        }

        /// <summary>
        /// Rileva LODGroup se presente.
        /// </summary>
        private void DetectLODGroup()
        {
            if (lodGroup == null)
            {
                lodGroup = GetComponentInChildren<LODGroup>();
            }

            if (lodGroup != null && applyTintToLODs)
            {
                // Cache LOD renderers
                LOD[] lods = lodGroup.GetLODs();
                lodRenderersCache.Clear();

                for (int i = 0; i < lods.Length; i++)
                {
                    Renderer[] lodRenderers = lods[i].renderers;
                    for (int j = 0; j < lodRenderers.Length; j++)
                    {
                        if (lodRenderers[j] != null)
                        {
                            lodRenderersCache.Add(lodRenderers[j]);
                        }
                    }
                }

#if UNITY_EDITOR
                Debug.Log($"[WorkerMeshController] LODGroup detected with {lodRenderersCache.Count} renderers across {lods.Length} LOD levels", this);
#endif
            }
        }

        /// <summary>
        /// Costruisce cache di tutti i renderer per color tint.
        /// Se tintableRenderers è assegnato, usa quelli. Altrimenti auto-detect.
        /// </summary>
        private void BuildRenderersCache()
        {
            allRenderersCache.Clear();

            // Se tintableRenderers è assegnato, usa solo quelli
            if (tintableRenderers != null && tintableRenderers.Length > 0)
            {
                for (int i = 0; i < tintableRenderers.Length; i++)
                {
                    if (tintableRenderers[i] != null)
                    {
                        allRenderersCache.Add(tintableRenderers[i]);
                    }
                }
            }
            else
            {
                // Auto-detect: aggiungi tutti i renderer principali
                if (headRenderer != null) allRenderersCache.Add(headRenderer);
                if (bodyRenderer != null) allRenderersCache.Add(bodyRenderer);
                if (legsRenderer != null) allRenderersCache.Add(legsRenderer);

                // Aggiungi renderer figli (accessori, fasce, mantelli, ecc.)
                Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < childRenderers.Length; i++)
                {
                    Renderer r = childRenderers[i];
                    if (r != null && !allRenderersCache.Contains(r))
                    {
                        allRenderersCache.Add(r);
                    }
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[WorkerMeshController] Built renderers cache: {allRenderersCache.Count} renderers for color tint", this);
#endif
        }

        // ============================================
        // PUBLIC API - MESH SWAP
        // ============================================

        /// <summary>
        /// Applica un WorkerVisualSet: mesh swap + material override + color tint.
        /// MOBILE OPTIMIZED: zero allocazioni, solo sharedMesh e sharedMaterial.
        /// </summary>
        public void ApplyMeshSwap(WorkerVisualSet visualSet)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (visualSet == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerMeshController] ApplyMeshSwap called with null visualSet.", this);
#endif
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // PRIORITÀ 1: FULL BODY MESH (Single Mesh Support)
            // ═══════════════════════════════════════════════════════════

            if (visualSet.fullBodyMesh != null)
            {
                // Usa fullBodyRenderer se disponibile, altrimenti fallback a bodyRenderer
                SkinnedMeshRenderer targetRenderer = fullBodyRenderer != null ? fullBodyRenderer : bodyRenderer;

                if (targetRenderer != null)
                {
                    targetRenderer.sharedMesh = visualSet.fullBodyMesh;
                    targetRenderer.enabled = true;

#if UNITY_EDITOR
                    Debug.Log($"<color=cyan>[WorkerMeshController]</color> Applied FULL BODY mesh: {visualSet.fullBodyMesh.name} to {targetRenderer.name}", this);
#endif
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[WorkerMeshController] No fullBodyRenderer or bodyRenderer available for full-body mesh!", this);
#endif
                }

                // Nascondi renderer modulari se esistono (mesh unificata)
                if (headRenderer != null && headRenderer != targetRenderer)
                {
                    headRenderer.enabled = false;
                }
                if (bodyRenderer != null && bodyRenderer != targetRenderer)
                {
                    bodyRenderer.enabled = false;
                }
                if (legsRenderer != null && legsRenderer != targetRenderer)
                {
                    legsRenderer.enabled = false;
                }

                // Material override (se presente) - applica al fullBodyRenderer
                if (visualSet.bodyMaterialOverride != null && targetRenderer != null)
                {
                    targetRenderer.sharedMaterial = visualSet.bodyMaterialOverride;
                }

                // EARLY RETURN: non processare logica modulare head/body/legs
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // PRIORITÀ 2: MODULAR MESH SWAP (Head/Body/Legs)
            // ═══════════════════════════════════════════════════════════

            // Assicurati che i renderer modulari siano visibili
            if (headRenderer != null) headRenderer.enabled = true;
            if (legsRenderer != null) legsRenderer.enabled = true;

            // Head mesh (opzionale)
            if (headRenderer != null)
            {
                if (visualSet.headMesh != null)
                {
                    headRenderer.sharedMesh = visualSet.headMesh;
                }
                // else: mantieni mesh corrente (nessun cambio)
            }

            // Body mesh (required)
            if (bodyRenderer != null)
            {
                if (visualSet.bodyMesh != null)
                {
                    bodyRenderer.sharedMesh = visualSet.bodyMesh;
                }
                else
                {
                    // Fallback: ripristina mesh originale
                    if (originalBodyMesh != null)
                    {
                        bodyRenderer.sharedMesh = originalBodyMesh;
                    }
                }
            }

            // Legs mesh (opzionale)
            if (legsRenderer != null)
            {
                if (visualSet.legsMesh != null)
                {
                    legsRenderer.sharedMesh = visualSet.legsMesh;
                }
                // else: mantieni mesh corrente
            }

            // ═══════════════════════════════════════════════════════════
            // 2. MATERIAL OVERRIDE (solo body, usa sharedMaterial)
            // ═══════════════════════════════════════════════════════════

            if (visualSet.bodyMaterialOverride != null)
            {
                ApplyMaterialOverride(visualSet.bodyMaterialOverride);
            }

            // ═══════════════════════════════════════════════════════════
            // 3. COLOR TINT (gestito da WorkerVisualController via palette)
            // ═══════════════════════════════════════════════════════════

            // NOTE: il tint è applicato da WorkerVisualController dopo aver
            // risolto il colore finale (VisualSet vs JobRolePalette).
            // Non applicarlo qui per evitare duplicazione.

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerMeshController]</color> Applied modular mesh swap", this);
#endif
        }

        // ============================================
        // PUBLIC API - MATERIAL OVERRIDE
        // ============================================

        /// <summary>
        /// Applica un material override al body (e opzionalmente legs).
        /// Se material è null, ripristina materiali originali.
        /// Usa sempre sharedMaterial per zero GC.
        /// </summary>
        public void ApplyMaterialOverride(Material overrideMaterial)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (overrideMaterial != null)
            {
                // Applica override
                if (bodyRenderer != null)
                {
                    bodyRenderer.sharedMaterial = overrideMaterial;
                }

                // Opzionale: applica anche a legs per uniformità
                if (legsRenderer != null)
                {
                    legsRenderer.sharedMaterial = overrideMaterial;
                }
            }
            else
            {
                // Ripristina materiali originali
                if (bodyRenderer != null && originalBodyMaterials != null && originalBodyMaterials.Length > 0)
                {
                    bodyRenderer.sharedMaterials = originalBodyMaterials;
                }

                if (legsRenderer != null && originalLegsMaterials != null && originalLegsMaterials.Length > 0)
                {
                    legsRenderer.sharedMaterials = originalLegsMaterials;
                }
            }
        }

        // ============================================
        // PUBLIC API - COLOR TINT
        // ============================================

        /// <summary>
        /// Applica un color tint a TUTTI i renderer rilevanti.
        /// Supporta _BaseColor (URP) e _Color (Standard/Built-in).
        /// Applica anche ai LOD se configurato.
        /// ZERO allocazioni GC.
        /// </summary>
        public void ApplyColorTint(Color tint)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            // ═══════════════════════════════════════════════════════════
            // 1. Applica tint ai renderer principali
            // ═══════════════════════════════════════════════════════════

            for (int i = 0; i < allRenderersCache.Count; i++)
            {
                Renderer renderer = allRenderersCache[i];
                if (renderer == null) continue;

                Material mat = renderer.sharedMaterial;
                if (mat == null) continue;

                // Prova tutte le property names configurate
                bool applied = false;
                for (int p = 0; p < colorPropertyNames.Length; p++)
                {
                    if (mat.HasProperty(colorPropertyNames[p]))
                    {
                        mat.SetColor(colorPropertyNames[p], tint);
                        applied = true;
                        break; // Applica solo alla prima property trovata
                    }
                }

#if UNITY_EDITOR
                if (!applied)
                {
                    Debug.LogWarning($"[WorkerMeshController] Material '{mat.name}' on '{renderer.name}' has no color property ({string.Join(", ", colorPropertyNames)})", this);
                }
#endif
            }

            // ═══════════════════════════════════════════════════════════
            // 2. Applica tint ai LOD renderers (se abilitato)
            // ═══════════════════════════════════════════════════════════

            if (applyTintToLODs && lodRenderersCache.Count > 0)
            {
                for (int i = 0; i < lodRenderersCache.Count; i++)
                {
                    Renderer lodRenderer = lodRenderersCache[i];
                    if (lodRenderer == null) continue;

                    // Evita duplicati se il renderer è già in allRenderersCache
                    if (allRenderersCache.Contains(lodRenderer))
                        continue;

                    Material lodMat = lodRenderer.sharedMaterial;
                    if (lodMat == null) continue;

                    for (int p = 0; p < colorPropertyNames.Length; p++)
                    {
                        if (lodMat.HasProperty(colorPropertyNames[p]))
                        {
                            lodMat.SetColor(colorPropertyNames[p], tint);
                            break;
                        }
                    }
                }
            }
        }

        // ============================================
        // PUBLIC API - RESET
        // ============================================

        /// <summary>
        /// Ripristina lo stato originale: meshes, materiali, tint.
        /// Utile per cleanup o reset del worker.
        /// </summary>
        public void ResetToDefault()
        {
            if (!isInitialized)
            {
                Initialize();
            }

            // Reset meshes
            if (headRenderer != null && originalHeadMesh != null)
            {
                headRenderer.sharedMesh = originalHeadMesh;
            }

            if (bodyRenderer != null && originalBodyMesh != null)
            {
                bodyRenderer.sharedMesh = originalBodyMesh;
            }

            if (legsRenderer != null && originalLegsMesh != null)
            {
                legsRenderer.sharedMesh = originalLegsMesh;
            }

            // Reset materials
            if (bodyRenderer != null && originalBodyMaterials != null && originalBodyMaterials.Length > 0)
            {
                bodyRenderer.sharedMaterials = originalBodyMaterials;
            }

            if (headRenderer != null && originalHeadMaterials != null && originalHeadMaterials.Length > 0)
            {
                headRenderer.sharedMaterials = originalHeadMaterials;
            }

            if (legsRenderer != null && originalLegsMaterials != null && originalLegsMaterials.Length > 0)
            {
                legsRenderer.sharedMaterials = originalLegsMaterials;
            }

            // Reset tint to white
            ApplyColorTint(Color.white);

#if UNITY_EDITOR
            Debug.Log("[WorkerMeshController] Reset to default state", this);
#endif
        }

        // ============================================
        // PUBLIC API - VISIBILITY (for transitions)
        // ============================================

        /// <summary>
        /// Nasconde tutti i renderer principali (per transizioni).
        /// </summary>
        public void HideRenderers()
        {
            if (headRenderer != null) headRenderer.enabled = false;
            if (bodyRenderer != null) bodyRenderer.enabled = false;
            if (legsRenderer != null) legsRenderer.enabled = false;
        }

        /// <summary>
        /// Mostra i renderer in base al VisualSet (alcuni mesh potrebbero essere null).
        /// </summary>
        public void ShowRenderers(WorkerVisualSet visualSet)
        {
            // Body: sempre visibile
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = true;
            }

            // Head: visibile solo se c'è mesh
            if (headRenderer != null && visualSet?.headMesh != null)
            {
                headRenderer.enabled = true;
            }

            // Legs: visibile solo se c'è mesh
            if (legsRenderer != null && visualSet?.legsMesh != null)
            {
                legsRenderer.enabled = true;
            }
        }

        // ============================================
        // DEBUG TOOLS (EDITOR ONLY)
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Tools")]
        [Button("Print Mesh State", ButtonSizes.Medium)]
        private void DebugPrintState()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[WorkerMeshController] Not initialized yet!", this);
                return;
            }

            string state = "=== WORKER MESH STATE ===\n";
            state += $"Initialized: {isInitialized}\n";
            state += $"Head Mesh: {(headRenderer?.sharedMesh?.name ?? "None")}\n";
            state += $"Body Mesh: {(bodyRenderer?.sharedMesh?.name ?? "None")}\n";
            state += $"Legs Mesh: {(legsRenderer?.sharedMesh?.name ?? "None")}\n";
            state += $"Renderers Cache: {allRenderersCache.Count}\n";
            state += $"LOD Renderers Cache: {lodRenderersCache.Count}\n";
            state += $"LODGroup: {(lodGroup != null ? lodGroup.name : "None")}\n";

            Debug.Log(state, this);
        }

        [Button("Force Reinitialize", ButtonSizes.Medium)]
        private void DebugReinitialize()
        {
            isInitialized = false;
            Initialize();
            Debug.Log("[WorkerMeshController] Force reinitialized", this);
        }

        [Button("Test Color Tint (Red)", ButtonSizes.Small)]
        private void DebugTestTintRed()
        {
            ApplyColorTint(Color.red);
        }

        [Button("Test Color Tint (Blue)", ButtonSizes.Small)]
        private void DebugTestTintBlue()
        {
            ApplyColorTint(Color.blue);
        }

        [Button("Reset to Default", ButtonSizes.Small)]
        private void DebugReset()
        {
            ResetToDefault();
        }
#endif
    }
}
