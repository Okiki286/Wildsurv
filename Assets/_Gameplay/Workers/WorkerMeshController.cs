using UnityEngine;
using System.Collections;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Gestisce le SkinnedMeshRenderer del worker.
    /// Responsabile di mesh swap, material override, color tint e fading.
    /// </summary>
    public class WorkerMeshController : MonoBehaviour
    {
        // ============================================
        // MESH RENDERERS
        // ============================================

        [Header("Mesh Renderers")]
        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per la testa (opzionale)")]
        private SkinnedMeshRenderer headRenderer;

        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per il corpo")]
        private SkinnedMeshRenderer bodyRenderer;

        [SerializeField]
        [Tooltip("SkinnedMeshRenderer per le gambe (opzionale)")]
        private SkinnedMeshRenderer legsRenderer;

        // ============================================
        // COLOR TINT
        // ============================================

        [Header("Color Tint")]
        [SerializeField]
        [Tooltip("Renderer per elementi colorabili (fascia, mantello, ecc.)")]
        private Renderer[] tintableRenderers;

        [SerializeField]
        [Tooltip("Nome della property color nel material")]
        private string colorPropertyName = "_Color";

        // ============================================
        // PROPERTIES
        // ============================================

        public SkinnedMeshRenderer HeadRenderer => headRenderer;
        public SkinnedMeshRenderer BodyRenderer => bodyRenderer;
        public SkinnedMeshRenderer LegsRenderer => legsRenderer;

        // ============================================
        // INITIALIZATION
        // ============================================

        private void Awake()
        {
            // Auto-find renderers if not assigned
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            }
        }

        // ============================================
        // MESH SWAP
        // ============================================

        /// <summary>
        /// Applica un WorkerVisualSet cambiando mesh, materiali e colori.
        /// </summary>
        public void ApplyMeshSwap(WorkerVisualSet visualSet)
        {
            if (visualSet == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerMeshController] ApplyMeshSwap called with null visualSet.");
#endif
                return;
            }

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
                // Use sharedMaterial to avoid per-instance material allocations at runtime.
                bodyRenderer.sharedMaterial = visualSet.bodyMaterialOverride;
            }

            // ═══════════════════════════════════════════════════════════
            // COLOR TINT
            // ═══════════════════════════════════════════════════════════
            ApplyColorTint(visualSet.roleColorTint);

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerMeshController]</color> Applied mesh swap");
#endif
        }

        // ============================================
        // COLOR TINT
        // ============================================

        /// <summary>
        /// Applica un colore tint agli elementi configurati.
        /// </summary>
        public void ApplyColorTint(Color color)
        {
            if (tintableRenderers == null || tintableRenderers.Length == 0) return;

            foreach (var renderer in tintableRenderers)
            {
                if (renderer == null) continue;

                var mat = renderer.sharedMaterial;
                if (mat != null && mat.HasProperty(colorPropertyName))
                {
                    mat.SetColor(colorPropertyName, color);
                }
            }
        }

        // ============================================
        // FADING
        // ============================================

        /// <summary>
        /// Nasconde tutti i renderer.
        /// </summary>
        public void HideRenderers()
        {
            if (bodyRenderer != null) bodyRenderer.enabled = false;
            if (headRenderer != null) headRenderer.enabled = false;
            if (legsRenderer != null) legsRenderer.enabled = false;
        }

        /// <summary>
        /// Mostra i renderer in base al visual set.
        /// </summary>
        public void ShowRenderers(WorkerVisualSet visualSet)
        {
            if (bodyRenderer != null) bodyRenderer.enabled = true;

            if (headRenderer != null && visualSet?.headMesh != null)
            {
                headRenderer.enabled = true;
            }

            if (legsRenderer != null && visualSet?.legsMesh != null)
            {
                legsRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Fade out coroutine (per future implementazioni).
        /// </summary>
        public IEnumerator FadeOut(float duration)
        {
            // TODO: Implementare fade tramite material alpha se necessario
            yield return new WaitForSeconds(duration);
        }

        /// <summary>
        /// Fade in coroutine (per future implementazioni).
        /// </summary>
        public IEnumerator FadeIn(float duration)
        {
            // TODO: Implementare fade tramite material alpha se necessario
            yield return new WaitForSeconds(duration);
        }
    }
}
