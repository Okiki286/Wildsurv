using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Map;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.World;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildernessSurvival.Gameplay.World
{
    /// <summary>
    /// Grid-based procedural map generator using pre-made Stamp prefabs.
    /// Places stamps on a grid with randomized selection and rotation.
    /// </summary>
    public class MapGenerator_StampBased : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // GENERATION SETTINGS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Generation Settings")]
        [BoxGroup("Generation Settings/Grid")]
        [Tooltip("Seed for random generation (same seed = same map)")]
        public int seed = 12345;

        [BoxGroup("Generation Settings/Grid")]
        [Tooltip("Grid dimensions (cells)")]
        public Vector2Int gridSize = new Vector2Int(6, 6);

        [BoxGroup("Generation Settings/Grid")]
        [Tooltip("Size of each cell in world units")]
        [Range(10f, 50f)]
        public float cellSize = 20f;

        [BoxGroup("Generation Settings/Grid")]
        [Tooltip("Radius around center reserved for core/base (in cells)")]
        [Range(0, 3)]
        public int coreRadius = 1;

        [BoxGroup("Generation Settings/Rotation")]
        [Tooltip("Enable random 90-degree rotations for stamps")]
        public bool randomRotation = true;

        // ═══════════════════════════════════════════════════════════════════
        // STAMP PREFABS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Stamp Prefabs")]
        [TabGroup("Stamp Prefabs/Tabs", "Core")]
        [AssetList(Path = "_Art/Environment/Stamps/Core")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        [Tooltip("Stamps for the central base area")]
        public List<GameObject> coreStamps = new List<GameObject>();

        [TabGroup("Stamp Prefabs/Tabs", "Forest")]
        [AssetList(Path = "_Art/Environment/Stamps/Forest")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        [Tooltip("Forest biome stamps")]
        public List<GameObject> forestStamps = new List<GameObject>();

        [TabGroup("Stamp Prefabs/Tabs", "Rock")]
        [AssetList(Path = "_Art/Environment/Stamps/Rock")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        [Tooltip("Rocky terrain stamps")]
        public List<GameObject> rockStamps = new List<GameObject>();

        [TabGroup("Stamp Prefabs/Tabs", "Water")]
        [AssetList(Path = "_Art/Environment/Stamps/Water")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        [Tooltip("Water/lake area stamps")]
        public List<GameObject> waterStamps = new List<GameObject>();

        [TabGroup("Stamp Prefabs/Tabs", "OpenField")]
        [AssetList(Path = "_Art/Environment/Stamps/OpenField")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        [Tooltip("Open field/meadow stamps")]
        public List<GameObject> openFieldStamps = new List<GameObject>();

        // ═══════════════════════════════════════════════════════════════════
        // BIOME WEIGHTS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Biome Distribution")]
        [BoxGroup("Biome Distribution/Weights")]
        [Range(0f, 1f)]
        public float forestWeight = 0.4f;

        [BoxGroup("Biome Distribution/Weights")]
        [Range(0f, 1f)]
        public float rockWeight = 0.2f;

        [BoxGroup("Biome Distribution/Weights")]
        [Range(0f, 1f)]
        public float waterWeight = 0.1f;

        [BoxGroup("Biome Distribution/Weights")]
        [Range(0f, 1f)]
        public float openFieldWeight = 0.3f;

        // ═══════════════════════════════════════════════════════════════════
        // GAMEPLAY REFERENCES
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Gameplay")]
        [BoxGroup("Gameplay/Core")]
        [Tooltip("Bonfire prefab to place at center")]
        [Required]
        public GameObject bonfirePrefab;

        [BoxGroup("Gameplay/Core")]
        [Tooltip("Offset from center for player spawn")]
        public Vector3 playerSpawnOffset = new Vector3(3f, 0f, 0f);

        [BoxGroup("Gameplay/Core")]
        [Tooltip("Radius of safe zone around bonfire")]
        [Range(5f, 20f)]
        public float safeZoneRadius = 10f;

        [BoxGroup("Gameplay/Systems")]
        [Tooltip("Reference to WorkerSystem (optional)")]
        public WorkerSystem workerSystem;

        [BoxGroup("Gameplay/Enemy Camps")]
        [Tooltip("Size of enemy camp zones")]
        public Vector3 enemyCampSize = new Vector3(15f, 5f, 15f);

        // ═══════════════════════════════════════════════════════════════════
        // RUNTIME DATA
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Generated Content")]
        [ShowInInspector, ReadOnly]
        private GameObject generatedRoot;

        [ShowInInspector, ReadOnly]
        private int stampCount = 0;

        private System.Random rng;

        // ═══════════════════════════════════════════════════════════════════
        // GENERATION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Actions")]
        [Button("🗺️ Generate Map", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.9f, 0.5f)]
        public void GenerateMap()
        {
            Debug.Log($"<color=cyan>[MapGenerator]</color> Starting generation with seed: {seed}");

            // Initialize RNG
            rng = new System.Random(seed);
            stampCount = 0;

            // 1. Cleanup previous generation
            CleanupPreviousGeneration();

            // 2. Create root container
            generatedRoot = new GameObject("GeneratedMap");
            generatedRoot.transform.position = Vector3.zero;

            #if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(generatedRoot, "Generate Map");
            #endif

            // Create sub-containers
            Transform stampsContainer = CreateContainer("Stamps");
            Transform markersContainer = CreateContainer("Markers");
            Transform zonesContainer = CreateContainer("Zones");

            // 3. Calculate grid center
            Vector2Int gridCenter = new Vector2Int(gridSize.x / 2, gridSize.y / 2);
            Vector3 worldCenter = GridToWorld(gridCenter);

            // 4. Place core/base area
            PlaceCoreArea(stampsContainer, markersContainer, zonesContainer, worldCenter);

            // 5. Grid loop - place stamps
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    Vector2Int cellPos = new Vector2Int(x, z);

                    // Skip cells within core radius
                    if (IsWithinCoreRadius(cellPos, gridCenter))
                        continue;

                    // Select and place stamp
                    PlaceStampAtCell(stampsContainer, cellPos);
                }
            }

            // 6. Place enemy camps at corners
            PlaceEnemyCamps(markersContainer, zonesContainer);

            // 7. Bake NavMesh
            BakeNavMesh();

            Debug.Log($"<color=green>[MapGenerator]</color> ✅ Map generated! Stamps placed: {stampCount}");
        }

        [Button("🎲 Randomize Seed & Generate", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        public void RandomizeAndGenerate()
        {
            seed = Random.Range(0, 999999);
            GenerateMap();
        }

        [Button("🗑️ Clear Generated Map", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.6f)]
        public void CleanupPreviousGeneration()
        {
            // Find and destroy existing generated map
            if (generatedRoot != null)
            {
                #if UNITY_EDITOR
                Undo.DestroyObjectImmediate(generatedRoot);
                #else
                Destroy(generatedRoot);
                #endif
            }

            // Also check for orphaned container
            GameObject existing = GameObject.Find("GeneratedMap");
            if (existing != null)
            {
                #if UNITY_EDITOR
                Undo.DestroyObjectImmediate(existing);
                #else
                Destroy(existing);
                #endif
            }

            stampCount = 0;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CORE AREA
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Places the core base area with bonfire and player spawn.
        /// </summary>
        private void PlaceCoreArea(Transform stampsContainer, Transform markersContainer, Transform zonesContainer, Vector3 center)
        {
            // Place core stamp if available
            if (coreStamps.Count > 0)
            {
                GameObject coreStamp = GetRandomFromList(coreStamps);
                if (coreStamp != null)
                {
                    InstantiateStamp(coreStamp, center, stampsContainer);
                }
            }

            // Create bonfire marker
            if (bonfirePrefab != null)
            {
                GameObject bonfire = InstantiatePrefab(bonfirePrefab, center, markersContainer);
                bonfire.name = "Marker_Bonfire";

                // Ensure it has MapMarker component
                MapMarker bonfireMarker = bonfire.GetComponent<MapMarker>();
                if (bonfireMarker == null)
                {
                    bonfireMarker = bonfire.AddComponent<MapMarker>();
                }
                bonfireMarker.type = MapMarkerType.Bonfire;
                bonfireMarker.radius = safeZoneRadius;
            }
            else
            {
                // Create empty marker if no prefab
                CreateMarker(markersContainer, "Marker_Bonfire", MapMarkerType.Bonfire, center, safeZoneRadius);
            }

            // Create player spawn marker
            Vector3 playerSpawnPos = center + playerSpawnOffset;
            CreateMarker(markersContainer, "Marker_PlayerSpawn", MapMarkerType.PlayerSpawn, playerSpawnPos, 1f);

            // Create build-allowed zone around center
            CreateZone(zonesContainer, "Zone_Base_BuildArea", MapZoneType.BuildAllowed, center, 
                new Vector3(safeZoneRadius * 2f, 10f, safeZoneRadius * 2f));

            Debug.Log($"<color=cyan>[MapGenerator]</color> Core area placed at {center}");
        }

        // ═══════════════════════════════════════════════════════════════════
        // STAMP PLACEMENT
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Places a stamp at a grid cell position.
        /// </summary>
        private void PlaceStampAtCell(Transform container, Vector2Int cellPos)
        {
            Vector3 worldPos = GridToWorld(cellPos);

            // Select biome type based on weights
            StampType biomeType = SelectRandomBiome();
            List<GameObject> stampList = GetStampListForType(biomeType);

            if (stampList == null || stampList.Count == 0)
            {
                // Fallback to forest or any available
                stampList = GetFallbackStampList();
                if (stampList == null || stampList.Count == 0)
                {
                    Debug.LogWarning($"[MapGenerator] No stamps available for cell {cellPos}");
                    return;
                }
            }

            GameObject stampPrefab = GetRandomFromList(stampList);
            if (stampPrefab != null)
            {
                InstantiateStamp(stampPrefab, worldPos, container);
            }
        }

        /// <summary>
        /// Instantiates a stamp with optional random rotation.
        /// </summary>
        private void InstantiateStamp(GameObject prefab, Vector3 position, Transform parent)
        {
            Quaternion rotation = Quaternion.identity;

            if (randomRotation)
            {
                // Random 90-degree increment
                int rotationIndex = rng.Next(0, 4);
                rotation = Quaternion.Euler(0f, rotationIndex * 90f, 0f);
            }

            GameObject instance = InstantiatePrefab(prefab, position, parent);
            instance.transform.rotation = rotation;
            stampCount++;
        }

        /// <summary>
        /// Selects a random biome type based on weights.
        /// </summary>
        private StampType SelectRandomBiome()
        {
            float totalWeight = forestWeight + rockWeight + waterWeight + openFieldWeight;
            float roll = (float)rng.NextDouble() * totalWeight;

            float cumulative = 0f;

            cumulative += forestWeight;
            if (roll < cumulative) return StampType.Forest;

            cumulative += rockWeight;
            if (roll < cumulative) return StampType.Rock;

            cumulative += waterWeight;
            if (roll < cumulative) return StampType.Water;

            return StampType.OpenField;
        }

        /// <summary>
        /// Gets the stamp list for a given type.
        /// </summary>
        private List<GameObject> GetStampListForType(StampType type)
        {
            return type switch
            {
                StampType.Core => coreStamps,
                StampType.Forest => forestStamps,
                StampType.Rock => rockStamps,
                StampType.Water => waterStamps,
                StampType.OpenField => openFieldStamps,
                _ => forestStamps
            };
        }

        /// <summary>
        /// Gets a fallback stamp list if the primary is empty.
        /// </summary>
        private List<GameObject> GetFallbackStampList()
        {
            if (forestStamps.Count > 0) return forestStamps;
            if (openFieldStamps.Count > 0) return openFieldStamps;
            if (rockStamps.Count > 0) return rockStamps;
            if (waterStamps.Count > 0) return waterStamps;
            if (coreStamps.Count > 0) return coreStamps;
            return null;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENEMY CAMPS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Places enemy camp zones at the four corners of the map.
        /// </summary>
        private void PlaceEnemyCamps(Transform markersContainer, Transform zonesContainer)
        {
            // Calculate corner positions
            float halfWidth = (gridSize.x * cellSize) / 2f;
            float halfHeight = (gridSize.y * cellSize) / 2f;

            Vector3[] cornerPositions = new Vector3[]
            {
                new Vector3(-halfWidth + cellSize / 2f, 0f, -halfHeight + cellSize / 2f), // SW
                new Vector3(halfWidth - cellSize / 2f, 0f, -halfHeight + cellSize / 2f),  // SE
                new Vector3(-halfWidth + cellSize / 2f, 0f, halfHeight - cellSize / 2f),  // NW
                new Vector3(halfWidth - cellSize / 2f, 0f, halfHeight - cellSize / 2f)    // NE
            };

            string[] cornerNames = { "SW", "SE", "NW", "NE" };

            for (int i = 0; i < 4; i++)
            {
                // Create enemy spawn marker
                CreateMarker(markersContainer, $"Marker_EnemySpawn_{cornerNames[i]}", 
                    MapMarkerType.EnemySpawn, cornerPositions[i], 5f);

                // Create enemy camp zone
                CreateZone(zonesContainer, $"Zone_EnemyCamp_{cornerNames[i]}", 
                    MapZoneType.EnemyCamp, cornerPositions[i], enemyCampSize);
            }

            Debug.Log($"<color=cyan>[MapGenerator]</color> Placed 4 enemy camps at corners");
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates a container GameObject under the generated root.
        /// </summary>
        private Transform CreateContainer(string name)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(generatedRoot.transform);
            container.transform.localPosition = Vector3.zero;
            return container.transform;
        }

        /// <summary>
        /// Converts grid coordinates to world position.
        /// </summary>
        private Vector3 GridToWorld(Vector2Int gridPos)
        {
            // Center the grid around origin
            float xOffset = (gridSize.x - 1) * cellSize / 2f;
            float zOffset = (gridSize.y - 1) * cellSize / 2f;

            return new Vector3(
                gridPos.x * cellSize - xOffset,
                0f,
                gridPos.y * cellSize - zOffset
            );
        }

        /// <summary>
        /// Checks if a cell is within the core radius.
        /// </summary>
        private bool IsWithinCoreRadius(Vector2Int cellPos, Vector2Int center)
        {
            int dx = Mathf.Abs(cellPos.x - center.x);
            int dz = Mathf.Abs(cellPos.y - center.y);
            return dx <= coreRadius && dz <= coreRadius;
        }

        /// <summary>
        /// Gets a random item from a list using seeded RNG.
        /// </summary>
        private T GetRandomFromList<T>(List<T> list) where T : class
        {
            if (list == null || list.Count == 0) return null;
            return list[rng.Next(0, list.Count)];
        }

        /// <summary>
        /// Instantiates a prefab with proper editor/runtime handling.
        /// </summary>
        private GameObject InstantiatePrefab(GameObject prefab, Vector3 position, Transform parent)
        {
            GameObject instance;

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.transform.position = position;
                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Stamp");
            }
            else
            {
                instance = Instantiate(prefab, position, Quaternion.identity, parent);
            }
            #else
            instance = Instantiate(prefab, position, Quaternion.identity, parent);
            #endif

            return instance;
        }

        /// <summary>
        /// Creates a MapMarker at the specified position.
        /// </summary>
        private void CreateMarker(Transform parent, string name, MapMarkerType type, Vector3 position, float radius)
        {
            GameObject markerObj = new GameObject(name);
            markerObj.transform.SetParent(parent);
            markerObj.transform.position = position;

            MapMarker marker = markerObj.AddComponent<MapMarker>();
            marker.type = type;
            marker.radius = radius;

            #if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(markerObj, $"Create Marker {name}");
            #endif
        }

        /// <summary>
        /// Creates a MapZone at the specified position.
        /// </summary>
        private void CreateZone(Transform parent, string name, MapZoneType type, Vector3 position, Vector3 size)
        {
            GameObject zoneObj = new GameObject(name);
            zoneObj.transform.SetParent(parent);
            zoneObj.transform.position = position;

            BoxCollider collider = zoneObj.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = true;

            MapZone zone = zoneObj.AddComponent<MapZone>();
            zone.type = type;

            #if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(zoneObj, $"Create Zone {name}");
            #endif
        }

        /// <summary>
        /// Bakes the NavMesh after generation.
        /// </summary>
        private void BakeNavMesh()
        {
            #if UNITY_EDITOR
            #pragma warning disable CS0618 // Legacy API
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            #pragma warning restore CS0618
            Debug.Log("<color=cyan>[MapGenerator]</color> NavMesh baked.");
            #endif
        }

        // ═══════════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            DrawGridGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            DrawGridGizmos();
            DrawCoreAreaGizmo();
        }

        /// <summary>
        /// Draws the generation grid in the scene view.
        /// </summary>
        private void DrawGridGizmos()
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);

            float halfWidth = (gridSize.x * cellSize) / 2f;
            float halfHeight = (gridSize.y * cellSize) / 2f;

            // Draw grid lines
            for (int x = 0; x <= gridSize.x; x++)
            {
                float xPos = x * cellSize - halfWidth;
                Gizmos.DrawLine(
                    new Vector3(xPos, 0f, -halfHeight),
                    new Vector3(xPos, 0f, halfHeight)
                );
            }

            for (int z = 0; z <= gridSize.y; z++)
            {
                float zPos = z * cellSize - halfHeight;
                Gizmos.DrawLine(
                    new Vector3(-halfWidth, 0f, zPos),
                    new Vector3(halfWidth, 0f, zPos)
                );
            }

            // Draw outer boundary
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(gridSize.x * cellSize, 1f, gridSize.y * cellSize));
        }

        /// <summary>
        /// Draws the core area boundary.
        /// </summary>
        private void DrawCoreAreaGizmo()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            float coreSize = (coreRadius * 2 + 1) * cellSize;
            Gizmos.DrawCube(Vector3.up * 0.5f, new Vector3(coreSize, 1f, coreSize));

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.up * 0.5f, new Vector3(coreSize, 1f, coreSize));
        }

        // ═══════════════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Debug")]
        [Button("📊 Print Stats", ButtonSizes.Medium)]
        private void PrintStats()
        {
            int totalCells = gridSize.x * gridSize.y;
            int coreCells = (coreRadius * 2 + 1) * (coreRadius * 2 + 1);
            int stampCells = totalCells - coreCells;

            Debug.Log($"=== Map Generator Stats ===\n" +
                $"Grid: {gridSize.x}x{gridSize.y} = {totalCells} cells\n" +
                $"Cell Size: {cellSize}m\n" +
                $"Map Size: {gridSize.x * cellSize}m x {gridSize.y * cellSize}m\n" +
                $"Core Cells: {coreCells}\n" +
                $"Stamp Cells: {stampCells}\n" +
                $"Available Stamps:\n" +
                $"  Core: {coreStamps.Count}\n" +
                $"  Forest: {forestStamps.Count}\n" +
                $"  Rock: {rockStamps.Count}\n" +
                $"  Water: {waterStamps.Count}\n" +
                $"  OpenField: {openFieldStamps.Count}");
        }
    }
}
