using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Gestisce il centro della base (Waystone).
    /// Fornisce accesso al transform del centro per nemici e altri sistemi.
    /// </summary>
    public class BaseCenterSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static BaseCenterSystem Instance { get; private set; }

        // ============================================
        // STATE
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        private Transform currentCenter;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // PROPERTIES
        // ============================================

        /// <summary>
        /// Transform del centro della base corrente (Waystone).
        /// Null se nessun centro è stato impostato.
        /// </summary>
        public Transform CurrentCenter => currentCenter;

        /// <summary>
        /// Posizione del centro della base.
        /// Ritorna Vector3.zero se nessun centro è impostato.
        /// </summary>
        public Vector3 CenterPosition => currentCenter != null ? currentCenter.position : Vector3.zero;

        /// <summary>
        /// True se un centro è stato impostato e il transform è valido.
        /// </summary>
        public bool HasCenter => currentCenter != null;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[BaseCenterSystem] Duplicate instance found!");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Imposta il centro della base (chiamato da StructureController quando Waystone diventa built).
        /// </summary>
        /// <param name="center">Transform del centro (Waystone)</param>
        public void SetCenter(Transform center)
        {
            if (center == null)
            {
                if (debugMode)
                {
                    Debug.Log("<color=cyan>[BaseCenterSystem]</color> Center cleared");
                }
                currentCenter = null;
                return;
            }

            // Se stiamo sostituendo un centro esistente, logga warning
            if (currentCenter != null && currentCenter != center)
            {
                Debug.LogWarning($"[BaseCenterSystem] Replacing existing center '{currentCenter.name}' with '{center.name}'");
            }

            currentCenter = center;

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[BaseCenterSystem]</color> Center set to '{center.name}' at {center.position}");
            }
        }

        /// <summary>
        /// Rimuove il centro se corrisponde al transform passato.
        /// Chiamato da StructureController.OnDestroy() quando il Waystone viene distrutto.
        /// </summary>
        /// <param name="center">Transform da verificare</param>
        public void ClearCenterIfMatch(Transform center)
        {
            if (currentCenter == center)
            {
                if (debugMode)
                {
                    Debug.Log($"<color=cyan>[BaseCenterSystem]</color> Center '{center.name}' cleared (destroyed)");
                }
                currentCenter = null;
            }
        }

        /// <summary>
        /// Forza il reset del centro (per debug/testing).
        /// </summary>
        public void ForceReset()
        {
            currentCenter = null;
            if (debugMode)
            {
                Debug.Log("<color=cyan>[BaseCenterSystem]</color> Center force reset");
            }
        }

        // ============================================
        // UTILITY
        // ============================================

        /// <summary>
        /// Calcola la distanza da un punto al centro della base.
        /// </summary>
        /// <param name="position">Posizione da cui calcolare la distanza</param>
        /// <returns>Distanza dal centro, o float.MaxValue se nessun centro</returns>
        public float GetDistanceToCenter(Vector3 position)
        {
            if (currentCenter == null) return float.MaxValue;
            return Vector3.Distance(position, currentCenter.position);
        }

        /// <summary>
        /// Verifica se una posizione è entro un raggio dal centro.
        /// </summary>
        /// <param name="position">Posizione da verificare</param>
        /// <param name="radius">Raggio massimo</param>
        /// <returns>True se dentro il raggio, false altrimenti o se nessun centro</returns>
        public bool IsWithinRadius(Vector3 position, float radius)
        {
            if (currentCenter == null) return false;
            return Vector3.Distance(position, currentCenter.position) <= radius;
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Log Status", ButtonSizes.Medium)]
        private void DebugLogStatus()
        {
            if (currentCenter != null)
            {
                Debug.Log($"[BaseCenterSystem] Center: '{currentCenter.name}' at {currentCenter.position}");
            }
            else
            {
                Debug.Log("[BaseCenterSystem] No center set");
            }
        }

        [Button("Force Reset", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugForceReset()
        {
            ForceReset();
        }

        private void OnDrawGizmosSelected()
        {
            if (currentCenter != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(currentCenter.position, 1f);
                Gizmos.DrawLine(currentCenter.position, currentCenter.position + Vector3.up * 5f);
            }
        }
#endif
    }
}
