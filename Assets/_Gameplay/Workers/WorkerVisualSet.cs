using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Definisce il preset visivo per un Job.
    /// Contiene mesh, tool, animator e colori per la skin del worker.
    /// </summary>
    [System.Serializable]
    public class WorkerVisualSet
    {
        // ============================================
        // MESH
        // ============================================

        [TitleGroup("Mesh")]
        [PreviewField(50)]
        [Tooltip("Mesh per la testa (opzionale se il modello è unificato)")]
        public Mesh headMesh;

        [PreviewField(50)]
        [Tooltip("Mesh per il corpo")]
        public Mesh bodyMesh;

        [PreviewField(50)]
        [Tooltip("Mesh per le gambe (opzionale se il modello è unificato)")]
        public Mesh legsMesh;

        // ============================================
        // TOOL
        // ============================================

        [TitleGroup("Tool")]
        [PreviewField(50)]
        [Tooltip("Prefab del tool da attachare al toolSocket (martello, ascia, spada, ecc.)")]
        public GameObject toolPrefab;

        [Tooltip("Offset posizione locale del tool rispetto al socket")]
        public Vector3 toolPositionOffset = Vector3.zero;

        [Tooltip("Offset rotazione locale del tool rispetto al socket")]
        public Vector3 toolRotationOffset = Vector3.zero;

        // ============================================
        // ANIMATION
        // ============================================

        [TitleGroup("Animation")]
        [Tooltip("AnimatorController specifico per questo job. Se null, usa quello di default.")]
        public RuntimeAnimatorController animatorController;

        // ============================================
        // COLORS & MATERIALS
        // ============================================

        [TitleGroup("Colors")]
        [ColorPalette]
        [Tooltip("Colore tint per elementi del ruolo (fascia, mantello, ecc.)")]
        public Color roleColorTint = Color.white;

        [Tooltip("Material override per il corpo (opzionale)")]
        public Material bodyMaterialOverride;

        [Tooltip("Material override per accessori (opzionale)")]
        public Material accessoryMaterialOverride;

        // ============================================
        // VFX
        // ============================================

        [TitleGroup("VFX")]
        [Tooltip("Prefab VFX da spawnare durante il cambio job (puff effect)")]
        public GameObject jobChangeVFXPrefab;

        [Tooltip("Durata della transizione visiva in secondi")]
        [PropertyRange(0.1f, 2f)]
        public float transitionDuration = 0.5f;

        // ============================================
        // VALIDATION
        // ============================================

        /// <summary>
        /// Verifica se il visual set ha almeno una mesh valida.
        /// </summary>
        public bool HasValidMesh => bodyMesh != null || headMesh != null || legsMesh != null;

        /// <summary>
        /// Verifica se il visual set è completamente configurato.
        /// </summary>
        public bool IsFullyConfigured => bodyMesh != null && animatorController != null;
    }
}
