using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Provider per spawn points dei nemici nella scena.
    /// Permette di definire punti di spawn senza riferimenti in ScriptableObject.
    /// </summary>
    public class EnemySpawnPointProvider : MonoBehaviour
    {
        // ============================================
        // SINGLETON (optional - for easy access)
        // ============================================

        public static EnemySpawnPointProvider Instance { get; private set; }

        // ============================================
        // SPAWN POINTS
        // ============================================

        [TitleGroup("Spawn Points")]
        [InfoBox("Definisci i punti di spawn dei nemici. Usa child transforms o references esplicite.")]
        [SerializeField] private Transform[] spawnPoints;

        [TitleGroup("Settings")]
        [Tooltip("Se true, usa automaticamente tutti i child transforms come spawn points")]
        [SerializeField] private bool useChildrenAsSpawnPoints = false;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private Color gizmoColor = Color.red;
        [SerializeField] private float gizmoRadius = 0.5f;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Set singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SpawnPointProvider] Multiple instances found. Using first one.");
            }
            else
            {
                Instance = this;
            }

            // Auto-collect children if enabled
            if (useChildrenAsSpawnPoints)
            {
                CollectChildSpawnPoints();
            }

            // Validate
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[SpawnPointProvider] No spawn points configured!");
            }
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
        /// Ritorna un punto di spawn casuale
        /// </summary>
        public Transform GetRandomSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("[SpawnPointProvider] No spawn points available!");
                return null;
            }

            // Filter out null entries
            Transform[] validPoints = System.Array.FindAll(spawnPoints, p => p != null);

            if (validPoints.Length == 0)
            {
                Debug.LogError("[SpawnPointProvider] All spawn points are null!");
                return null;
            }

            int index = Random.Range(0, validPoints.Length);
            Transform selected = validPoints[index];

            if (debugMode)
            {
                Debug.Log($"<color=red>[SpawnPointProvider]</color> Selected spawn point: {selected.name}");
            }

            return selected;
        }

        /// <summary>
        /// Ritorna una posizione casuale di spawn
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            Transform point = GetRandomSpawnPoint();
            return point != null ? point.position : Vector3.zero;
        }

        /// <summary>
        /// Ritorna tutti i punti di spawn validi
        /// </summary>
        public Transform[] GetAllSpawnPoints()
        {
            if (spawnPoints == null) return new Transform[0];
            return System.Array.FindAll(spawnPoints, p => p != null);
        }

        /// <summary>
        /// Ritorna il numero di spawn points disponibili
        /// </summary>
        public int SpawnPointCount
        {
            get
            {
                if (spawnPoints == null) return 0;
                return System.Array.FindAll(spawnPoints, p => p != null).Length;
            }
        }

        /// <summary>
        /// Ritorna uno spawn point per indice
        /// </summary>
        public Transform GetSpawnPoint(int index)
        {
            if (spawnPoints == null || index < 0 || index >= spawnPoints.Length)
            {
                return null;
            }
            return spawnPoints[index];
        }

        // ============================================
        // INTERNAL
        // ============================================

        private void CollectChildSpawnPoints()
        {
            int childCount = transform.childCount;
            if (childCount == 0)
            {
                Debug.LogWarning("[SpawnPointProvider] useChildrenAsSpawnPoints enabled but no children found!");
                return;
            }

            spawnPoints = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                spawnPoints[i] = transform.GetChild(i);
            }

            if (debugMode)
            {
                Debug.Log($"[SpawnPointProvider] Collected {childCount} spawn points from children");
            }
        }

        // ============================================
        // GIZMOS
        // ============================================

        private void OnDrawGizmos()
        {
            if (spawnPoints == null) return;

            Gizmos.color = gizmoColor;

            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, gizmoRadius);
                    Gizmos.DrawLine(point.position, point.position + Vector3.up * 2f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnPoints == null) return;

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);

            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, gizmoRadius);
                }
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Collect Children", ButtonSizes.Medium)]
        private void DebugCollectChildren()
        {
            CollectChildSpawnPoints();
        }

        [Button("Test Random Point", ButtonSizes.Medium)]
        private void DebugTestRandomPoint()
        {
            var point = GetRandomSpawnPoint();
            if (point != null)
            {
                Debug.Log($"Random spawn point: {point.name} at {point.position}");
            }
        }

        [Button("Log All Points", ButtonSizes.Medium)]
        private void DebugLogAllPoints()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.Log("[SpawnPointProvider] No spawn points");
                return;
            }

            Debug.Log($"[SpawnPointProvider] {spawnPoints.Length} spawn points:");
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var p = spawnPoints[i];
                Debug.Log($"  [{i}] {(p != null ? p.name : "NULL")}");
            }
        }
#endif
    }
}
