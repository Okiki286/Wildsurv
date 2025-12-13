using System.Collections.Generic;
using UnityEngine;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Renderizza il footprint di una struttura come quad colorati (verde=valido, rosso=invalido).
    /// Usa pooling per zero allocations per frame.
    /// </summary>
    public class FootprintPreviewRenderer : MonoBehaviour
    {
        [Header("Footprint Settings")]
        [SerializeField] private Material footprintMaterial;
        [SerializeField] private float quadHeight = 0.02f; // Ridotto per stare sotto le basi delle strutture
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

        // Pool di quad (max 25 per strutture grandi)
        private List<GameObject> quadPool = new List<GameObject>(25);
        private List<MeshRenderer> quadRenderers = new List<MeshRenderer>(25);
        private MaterialPropertyBlock mpb;

        private int activeQuadCount = 0;
        private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");

        private void Awake()
        {
            mpb = new MaterialPropertyBlock();

            // Pre-alloca 25 quad nel pool
            for (int i = 0; i < 25; i++)
            {
                CreatePooledQuad();
            }
        }

        private void CreatePooledQuad()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"FootprintQuad_{quadPool.Count}";
            quad.transform.SetParent(transform);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Flat on ground
            quad.SetActive(false);

            // Rimuovi collider
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Applica materiale
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            if (renderer != null && footprintMaterial != null)
            {
                renderer.material = footprintMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            quadPool.Add(quad);
            quadRenderers.Add(renderer);
        }

        /// <summary>
        /// Aggiorna il footprint preview.
        /// cell: cella grid di partenza (bottom-left)
        /// size: dimensione in celle (es. 2x3)
        /// rotationStep: 0..3 per rotazioni 90°
        /// gridSize: dimensione di una cella in world units
        /// isValid: se true mostra verde, se false rosso
        /// </summary>
        public void UpdateFootprint(Vector2Int cell, Vector2Int size, int rotationStep, float gridSize, bool isValid)
        {
            // Calcola size effettivo considerando rotazione
            // Per rotazioni dispari (90°, 270°), swap X/Z
            Vector2Int effectiveSize = size;
            if (rotationStep % 2 == 1)
            {
                effectiveSize = new Vector2Int(size.y, size.x);
            }

            int requiredQuads = effectiveSize.x * effectiveSize.y;

            // Espandi pool se necessario (raro)
            while (requiredQuads > quadPool.Count)
            {
                CreatePooledQuad();
            }

            // Attiva i quad necessari
            for (int i = 0; i < requiredQuads; i++)
            {
                GameObject quad = quadPool[i];
                quad.SetActive(true);

                // Calcola posizione cella
                int localX = i % effectiveSize.x;
                int localZ = i / effectiveSize.x;

                // Posizione world (centro della cella)
                float worldX = (cell.x + localX) * gridSize + gridSize * 0.5f;
                float worldZ = (cell.y + localZ) * gridSize + gridSize * 0.5f;

                quad.transform.position = new Vector3(worldX, quadHeight, worldZ);
                quad.transform.localScale = new Vector3(gridSize * 0.95f, gridSize * 0.95f, 1f); // 0.95 per gap

                // Applica colore via MPB
                Color color = isValid ? validColor : invalidColor;
                mpb.SetColor(ColorPropertyID, color);
                quadRenderers[i].SetPropertyBlock(mpb);
            }

            // Disattiva quad in eccesso
            for (int i = requiredQuads; i < activeQuadCount; i++)
            {
                quadPool[i].SetActive(false);
            }

            activeQuadCount = requiredQuads;
        }

        /// <summary>
        /// Nasconde tutti i quad del footprint.
        /// </summary>
        public void HideFootprint()
        {
            for (int i = 0; i < activeQuadCount; i++)
            {
                quadPool[i].SetActive(false);
            }
            activeQuadCount = 0;
        }

        private void OnDestroy()
        {
            // Cleanup pool
            foreach (var quad in quadPool)
            {
                if (quad != null) Destroy(quad);
            }
            quadPool.Clear();
            quadRenderers.Clear();
        }
    }
}
