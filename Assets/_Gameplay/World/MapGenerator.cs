using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildernessSurvival.World
{
    /// <summary>
    /// Procedural map generator that spawns environment objects based on BiomeDefinition data.
    /// Optimized for mobile with static batching and NavMesh support.
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════

        [BoxGroup("Biome")]
        [Required("A biome definition is required for map generation!")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        [Tooltip("The biome definition to use for generation")]
        public BiomeDefinition currentBiome;

        [BoxGroup("Map Settings")]
        [Tooltip("Seed for random generation (0 = random)")]
        public int seed = 0;

        [BoxGroup("Map Settings")]
        [Tooltip("Map size in world units (X, Z)")]
        public Vector2Int mapSize = new Vector2Int(50, 50);

        [BoxGroup("Map Settings")]
        [Tooltip("Size of each chunk for density calculations")]
        public float chunkSize = 10f;

        [BoxGroup("Map Settings")]
        [Tooltip("Automatically regenerate when parameters change")]
        public bool autoRegenerate = false;

        [BoxGroup("Safety Zone")]
        [Tooltip("Position of the bonfire (safe zone center)")]
        public Transform bonfirePosition;

        [BoxGroup("Safety Zone")]
        [Tooltip("Override biome's safe radius (0 = use biome setting)")]
        [Range(0f, 20f)]
        public float overrideSafeRadius = 0f;

        [BoxGroup("Ground")]
        [Tooltip("Existing ground/terrain to use (optional)")]
        public Transform existingGround;

        [BoxGroup("Ground")]
        [Tooltip("Ground layer for raycasting")]
        public LayerMask groundLayer = 1; // Default layer

        // ═══════════════════════════════════════════════════════════════════
        // RUNTIME DATA
        // ═══════════════════════════════════════════════════════════════════

        [FoldoutGroup("Debug Info")]
        [ReadOnly, ShowInInspector]
        private int lastSeedUsed;

        [FoldoutGroup("Debug Info")]
        [ReadOnly, ShowInInspector]
        private int totalObjectsSpawned;

        [FoldoutGroup("Debug Info")]
        [ReadOnly, ShowInInspector]
        private int treesSpawned;

        [FoldoutGroup("Debug Info")]
        [ReadOnly, ShowInInspector]
        private int rocksSpawned;

        [FoldoutGroup("Debug Info")]
        [ReadOnly, ShowInInspector]
        private int foliageSpawned;

        [FoldoutGroup("Debug Info")]
        [ReadOnly, ShowInInspector]
        private int decorSpawned;

        // Internal references
        private Transform environmentRoot;
        private List<Vector3> occupiedTreePositions = new List<Vector3>();
        private List<Vector3> occupiedRockPositions = new List<Vector3>();

        // Layer cache
        private int layerEnvironmentBlocking;
        private int layerEnvironmentDecor;

        // ═══════════════════════════════════════════════════════════════════
        // GENERATION METHODS
        // ═══════════════════════════════════════════════════════════════════

        [BoxGroup("Actions")]
        [Button("🌍 Generate Map", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.5f)]
        public void GenerateMap()
        {
            if (currentBiome == null)
            {
                Debug.LogError("[MapGenerator] No biome definition assigned!");
                return;
            }

            // Initialize
            InitializeLayers();
            InitializeSeed();
            CleanupPreviousGeneration();
            CreateEnvironmentRoot();

            // Reset counters
            totalObjectsSpawned = 0;
            treesSpawned = 0;
            rocksSpawned = 0;
            foliageSpawned = 0;
            decorSpawned = 0;
            occupiedTreePositions.Clear();
            occupiedRockPositions.Clear();

            // Generate ground if needed
            GenerateGround();

            // Calculate chunks
            int chunksX = Mathf.CeilToInt(mapSize.x / chunkSize);
            int chunksZ = Mathf.CeilToInt(mapSize.y / chunkSize);

            Debug.Log($"[MapGenerator] Generating {chunksX}x{chunksZ} chunks...");

            // Generate each chunk
            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cz = 0; cz < chunksZ; cz++)
                {
                    Vector2 chunkOrigin = new Vector2(cx * chunkSize, cz * chunkSize);
                    GenerateChunk(chunkOrigin);
                }
            }

            // Bake NavMesh
            BakeNavMesh();

            // Summary
            totalObjectsSpawned = treesSpawned + rocksSpawned + foliageSpawned + decorSpawned;
            Debug.Log($"<color=green>[MapGenerator]</color> Generation complete!\n" +
                     $"• Seed: {lastSeedUsed}\n" +
                     $"• Trees: {treesSpawned}\n" +
                     $"• Rocks: {rocksSpawned}\n" +
                     $"• Foliage: {foliageSpawned}\n" +
                     $"• Decor: {decorSpawned}\n" +
                     $"• Total: {totalObjectsSpawned}");
        }

        [BoxGroup("Actions")]
        [Button("🎲 Randomize Seed & Generate", ButtonSizes.Medium)]
        [GUIColor(0.9f, 0.7f, 1f)]
        private void RandomizeSeedAndGenerate()
        {
            seed = Random.Range(1, 999999);
            GenerateMap();
        }

        [BoxGroup("Actions")]
        [Button("🗑️ Clear Generated Objects", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.6f)]
        private void ClearGenerated()
        {
            CleanupPreviousGeneration();
            Debug.Log("[MapGenerator] Generated objects cleared.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════════════

        private void InitializeLayers()
        {
            layerEnvironmentBlocking = LayerMask.NameToLayer("Environment_Blocking");
            layerEnvironmentDecor = LayerMask.NameToLayer("Environment_Decor");

            if (layerEnvironmentBlocking == -1)
            {
                Debug.LogWarning("[MapGenerator] Layer 'Environment_Blocking' not found. Using Default.");
                layerEnvironmentBlocking = 0;
            }

            if (layerEnvironmentDecor == -1)
            {
                Debug.LogWarning("[MapGenerator] Layer 'Environment_Decor' not found. Using Default.");
                layerEnvironmentDecor = 0;
            }
        }

        private void InitializeSeed()
        {
            lastSeedUsed = seed == 0 ? Random.Range(1, 999999) : seed;
            Random.InitState(lastSeedUsed);
        }

        private void CleanupPreviousGeneration()
        {
            // Find and destroy existing environment root
            Transform existing = transform.Find("EnvironmentRoot");
            if (existing != null)
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(existing.gameObject);
                }
                else
                #endif
                {
                    Destroy(existing.gameObject);
                }
            }
        }

        private void CreateEnvironmentRoot()
        {
            GameObject root = new GameObject("EnvironmentRoot");
            root.transform.SetParent(transform);
            root.transform.localPosition = Vector3.zero;
            environmentRoot = root.transform;

            // Create category containers
            CreateContainer("Rocks");
            CreateContainer("Trees");
            CreateContainer("Foliage");
            CreateContainer("Decor");
        }

        private Transform CreateContainer(string name)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(environmentRoot);
            container.transform.localPosition = Vector3.zero;
            return container.transform;
        }

        private Transform GetContainer(string name)
        {
            return environmentRoot.Find(name);
        }

        // ═══════════════════════════════════════════════════════════════════
        // GROUND GENERATION
        // ═══════════════════════════════════════════════════════════════════

        private void GenerateGround()
        {
            if (existingGround != null)
            {
                Debug.Log("[MapGenerator] Using existing ground.");
                return;
            }

            // Create a simple ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GeneratedGround";
            ground.transform.SetParent(environmentRoot);
            ground.transform.localPosition = new Vector3(mapSize.x / 2f, 0, mapSize.y / 2f);
            ground.transform.localScale = new Vector3(mapSize.x / 10f, 1, mapSize.y / 10f);

            // Set layer and static flags
            ground.layer = 0; // Default
            SetStaticFlags(ground, true, true);

            Debug.Log("[MapGenerator] Generated default ground plane.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // CHUNK GENERATION
        // ═══════════════════════════════════════════════════════════════════

        private void GenerateChunk(Vector2 chunkOrigin)
        {
            // 1. ROCKS (Blocking) - Spawn first as they affect navigation
            int rockCount = currentBiome.GetRandomRockCount();
            for (int i = 0; i < rockCount; i++)
            {
                SpawnRock(chunkOrigin);
            }

            // 2. TREES (Semi-blocking)
            int treeCount = currentBiome.GetRandomTreeCount();
            for (int i = 0; i < treeCount; i++)
            {
                SpawnTree(chunkOrigin);
            }

            // 3. FOLIAGE (Decor)
            int foliageCount = currentBiome.GetRandomFoliageCount();
            for (int i = 0; i < foliageCount; i++)
            {
                SpawnFoliage(chunkOrigin);
            }

            // 4. DECOR
            int decorCount = currentBiome.GetRandomDecorCount();
            for (int i = 0; i < decorCount; i++)
            {
                SpawnDecor(chunkOrigin);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // SPAWN METHODS
        // ═══════════════════════════════════════════════════════════════════

        private void SpawnRock(Vector2 chunkOrigin)
        {
            GameObject prefab = currentBiome.GetRandomRock();
            if (prefab == null) return;

            Vector3? position = FindValidPosition(chunkOrigin, currentBiome.minDistanceBetweenRocks, occupiedRockPositions);
            if (!position.HasValue) return;

            GameObject instance = InstantiatePrefab(prefab, position.Value, GetContainer("Rocks"));
            if (instance == null) return;

            // Configure rock
            instance.layer = layerEnvironmentBlocking;
            instance.tag = "Env_Rock";

            // Ensure it has a collider
            if (instance.GetComponent<Collider>() == null)
            {
                instance.AddComponent<BoxCollider>();
            }

            // Set static flags (NavigationStatic for NavMesh obstacle)
            SetStaticFlags(instance, true, true);

            // Apply randomization
            ApplyRandomization(instance);

            occupiedRockPositions.Add(position.Value);
            rocksSpawned++;
        }

        private void SpawnTree(Vector2 chunkOrigin)
        {
            GameObject prefab = currentBiome.GetRandomTree();
            if (prefab == null) return;

            Vector3? position = FindValidPosition(chunkOrigin, currentBiome.minDistanceBetweenTrees, occupiedTreePositions, true);
            if (!position.HasValue) return;

            // Check slope
            if (!CheckSlope(position.Value, currentBiome.maxSlopeForTrees)) return;

            GameObject instance = InstantiatePrefab(prefab, position.Value, GetContainer("Trees"));
            if (instance == null) return;

            // Configure tree
            instance.tag = "Env_Tree";

            // Set static flags
            SetStaticFlags(instance, true, true);

            // Apply randomization
            ApplyRandomization(instance);

            occupiedTreePositions.Add(position.Value);
            treesSpawned++;
        }

        private void SpawnFoliage(Vector2 chunkOrigin)
        {
            GameObject prefab = currentBiome.GetRandomFoliage();
            if (prefab == null) return;

            Vector3? position = FindValidPosition(chunkOrigin, 0.5f, null);
            if (!position.HasValue) return;

            // Check slope
            if (!CheckSlope(position.Value, currentBiome.maxSlopeForFoliage)) return;

            GameObject instance = InstantiatePrefab(prefab, position.Value, GetContainer("Foliage"));
            if (instance == null) return;

            // Configure foliage
            instance.layer = layerEnvironmentDecor;

            // Disable colliders for performance
            Collider[] colliders = instance.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // Set static flags (BatchingStatic only, no navigation impact)
            SetStaticFlags(instance, true, false);

            // Apply randomization
            ApplyRandomization(instance);

            foliageSpawned++;
        }

        private void SpawnDecor(Vector2 chunkOrigin)
        {
            GameObject prefab = currentBiome.GetRandomDecor();
            if (prefab == null) return;

            Vector3? position = FindValidPosition(chunkOrigin, 1f, null);
            if (!position.HasValue) return;

            GameObject instance = InstantiatePrefab(prefab, position.Value, GetContainer("Decor"));
            if (instance == null) return;

            // Configure decor
            instance.layer = layerEnvironmentDecor;
            instance.tag = "Env_Decor";

            // Set static flags
            SetStaticFlags(instance, true, false);

            // Apply randomization
            ApplyRandomization(instance);

            decorSpawned++;
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════

        private GameObject InstantiatePrefab(GameObject prefab, Vector3 position, Transform parent)
        {
            GameObject instance;

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance != null)
                {
                    instance.transform.position = position;
                    instance.transform.SetParent(parent);
                }
            }
            else
            #endif
            {
                instance = Instantiate(prefab, position, Quaternion.identity, parent);
            }

            return instance;
        }

        private Vector3? FindValidPosition(Vector2 chunkOrigin, float minDistance, List<Vector3> occupiedPositions, bool checkSafeZone = true)
        {
            float safeRadius = overrideSafeRadius > 0 ? overrideSafeRadius : currentBiome.bonfireSafeRadius;
            float edgeMargin = currentBiome.edgeMargin;

            int maxAttempts = 10;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float x = chunkOrigin.x + Random.Range(0f, chunkSize);
                float z = chunkOrigin.y + Random.Range(0f, chunkSize);

                // Clamp to map bounds with edge margin
                x = Mathf.Clamp(x, edgeMargin, mapSize.x - edgeMargin);
                z = Mathf.Clamp(z, edgeMargin, mapSize.y - edgeMargin);

                Vector3 position = new Vector3(x, 0f, z);

                // Raycast to find ground height
                if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, groundLayer))
                {
                    position.y = hit.point.y;
                }

                // Check safe zone (bonfire)
                if (checkSafeZone && bonfirePosition != null)
                {
                    float distToBonfire = Vector3.Distance(position, bonfirePosition.position);
                    if (distToBonfire < safeRadius)
                    {
                        continue;
                    }
                }

                // Check minimum distance from occupied positions
                if (occupiedPositions != null && minDistance > 0)
                {
                    bool tooClose = false;
                    foreach (var occupied in occupiedPositions)
                    {
                        if (Vector3.Distance(position, occupied) < minDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;
                }

                return position;
            }

            return null;
        }

        private bool CheckSlope(Vector3 position, float maxSlope)
        {
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                return angle <= maxSlope;
            }
            return true; // Default to allow if no ground hit
        }

        private void ApplyRandomization(GameObject obj)
        {
            // Random rotation
            obj.transform.rotation = currentBiome.GetRandomRotation();

            // Random scale
            float scale = currentBiome.GetRandomScale();
            obj.transform.localScale = Vector3.one * scale;
        }

        private void SetStaticFlags(GameObject obj, bool batching, bool navigation)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                StaticEditorFlags flags = 0;
                
                if (batching)
                {
                    flags |= StaticEditorFlags.BatchingStatic;
                }
                
                // Note: NavigationStatic is deprecated, but we set the object as static
                // NavMesh uses NavMeshSurface or legacy system based on project setup
                
                GameObjectUtility.SetStaticEditorFlags(obj, flags);
                
                // Also set on children
                foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
                }
            }
            else
            #endif
            {
                obj.isStatic = true;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // NAVMESH
        // ═══════════════════════════════════════════════════════════════════

        [BoxGroup("Actions")]
        [Button("🗺️ Bake NavMesh", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.6f, 1f)]
        private void BakeNavMesh()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                #pragma warning disable CS0618
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                #pragma warning restore CS0618
                Debug.Log("<color=blue>[MapGenerator]</color> NavMesh baked.");
            }
            else
            #endif
            {
                Debug.LogWarning("[MapGenerator] NavMesh baking is only available in the editor.");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════════

        private void OnDrawGizmosSelected()
        {
            // Draw map bounds
            Gizmos.color = Color.green;
            Vector3 center = new Vector3(mapSize.x / 2f, 0, mapSize.y / 2f);
            Vector3 size = new Vector3(mapSize.x, 1f, mapSize.y);
            Gizmos.DrawWireCube(center, size);

            // Draw bonfire safe zone
            if (bonfirePosition != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                float safeRadius = overrideSafeRadius > 0 ? overrideSafeRadius : 
                    (currentBiome != null ? currentBiome.bonfireSafeRadius : 8f);
                Gizmos.DrawSphere(bonfirePosition.position, safeRadius);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(bonfirePosition.position, safeRadius);
            }

            // Draw chunk grid
            if (chunkSize > 0)
            {
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.2f);
                int chunksX = Mathf.CeilToInt(mapSize.x / chunkSize);
                int chunksZ = Mathf.CeilToInt(mapSize.y / chunkSize);

                for (int cx = 0; cx <= chunksX; cx++)
                {
                    Vector3 start = new Vector3(cx * chunkSize, 0, 0);
                    Vector3 end = new Vector3(cx * chunkSize, 0, mapSize.y);
                    Gizmos.DrawLine(start, end);
                }

                for (int cz = 0; cz <= chunksZ; cz++)
                {
                    Vector3 start = new Vector3(0, 0, cz * chunkSize);
                    Vector3 end = new Vector3(mapSize.x, 0, cz * chunkSize);
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}
