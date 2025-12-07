using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using WildernessSurvival.Gameplay.Map;
using WildernessSurvival.World;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor tool for automatically populating MapZones with prefabs from a BiomeDefinition.
    /// Scatters trees, rocks, and foliage based on zone type.
    /// </summary>
    public class ZonePopulatorTool : OdinEditorWindow
    {
        // ═══════════════════════════════════════════════════════════════════
        // WINDOW MENU
        // ═══════════════════════════════════════════════════════════════════

        [MenuItem("Tools/Wilderness/🌲 Zone Populator")]
        private static void OpenWindow()
        {
            var window = GetWindow<ZonePopulatorTool>();
            window.titleContent = new GUIContent("🌲 Zone Populator");
            window.minSize = new Vector2(450, 550);
            window.Show();
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════

        private const string POPULATED_CONTAINER_NAME = "_PopulatedContent";

        // ═══════════════════════════════════════════════════════════════════
        // SETTINGS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Configuration")]
        [BoxGroup("Configuration/Required")]
        [Required("A BiomeDefinition is required to populate zones")]
        [Tooltip("The biome definition containing prefab lists")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        public BiomeDefinition biome;

        [BoxGroup("Configuration/Required")]
        [Required("An environment parent is needed for spawned objects")]
        [Tooltip("Parent transform for spawned objects (e.g., World/Environment)")]
        public Transform environmentParent;

        [BoxGroup("Configuration/Density")]
        [Range(0.1f, 3f)]
        [Tooltip("Multiplier for spawn density (1.0 = normal)")]
        public float densityMultiplier = 1.0f;

        [BoxGroup("Configuration/Density")]
        [Range(5f, 50f)]
        [Tooltip("Approximate area per object (lower = more dense)")]
        public float areaPerObject = 15f;

        [BoxGroup("Configuration/Raycast")]
        [Tooltip("Layer mask for ground detection")]
        public LayerMask groundLayer = 1; // Default layer

        [BoxGroup("Configuration/Raycast")]
        [Tooltip("Height from which to raycast down")]
        public float raycastHeight = 50f;

        [BoxGroup("Configuration/Randomization")]
        [MinMaxSlider(0.5f, 2f, true)]
        [Tooltip("Scale variation range")]
        public Vector2 scaleRange = new Vector2(0.85f, 1.2f);

        [BoxGroup("Configuration/Randomization")]
        [Tooltip("Enable random Y rotation")]
        public bool randomRotation = true;

        [BoxGroup("Configuration/Options")]
        [Tooltip("Set spawned objects as static")]
        public bool makeStatic = true;

        [BoxGroup("Configuration/Options")]
        [Tooltip("Parent objects to zone (vs environment parent)")]
        public bool parentToZone = true;

        // ═══════════════════════════════════════════════════════════════════
        // SELECTION INFO
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Selection")]
        [ShowInInspector, ReadOnly]
        [InfoBox("Select MapZone GameObjects in the Scene, then click Populate")]
        private int SelectedZonesCount
        {
            get
            {
                int count = 0;
                foreach (var obj in Selection.gameObjects)
                {
                    if (obj.GetComponent<MapZone>() != null)
                        count++;
                }
                return count;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // MAIN ACTIONS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Actions")]
        [Button("🌲 Populate Selected Zones", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.9f, 0.5f)]
        private void PopulateSelectedZones()
        {
            if (biome == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a BiomeDefinition.", "OK");
                return;
            }

            if (!parentToZone && environmentParent == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an Environment Parent.", "OK");
                return;
            }

            var selectedZones = GetSelectedZones();
            if (selectedZones.Count == 0)
            {
                EditorUtility.DisplayDialog("No Zones Selected", 
                    "Select GameObjects with MapZone components in the Scene view.", "OK");
                return;
            }

            int totalSpawned = 0;

            foreach (var zone in selectedZones)
            {
                int spawned = PopulateZone(zone);
                totalSpawned += spawned;
            }

            Debug.Log($"<color=green>[ZonePopulator]</color> ✅ Populated {selectedZones.Count} zones with {totalSpawned} objects.");
        }

        [Button("🗑️ Clear Selected Zones", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.6f)]
        private void ClearSelectedZones()
        {
            var selectedZones = GetSelectedZones();
            if (selectedZones.Count == 0)
            {
                EditorUtility.DisplayDialog("No Zones Selected", 
                    "Select GameObjects with MapZone components to clear.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Confirm Clear", 
                $"Clear populated content from {selectedZones.Count} zone(s)?", 
                "Clear", "Cancel"))
            {
                return;
            }

            int cleared = 0;
            foreach (var zone in selectedZones)
            {
                cleared += ClearZone(zone);
            }

            Debug.Log($"<color=orange>[ZonePopulator]</color> Cleared {cleared} objects from {selectedZones.Count} zones.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // POPULATION LOGIC
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Populates a single zone with appropriate prefabs.
        /// </summary>
        private int PopulateZone(MapZone zone)
        {
            Collider col = zone.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"[ZonePopulator] Zone '{zone.name}' has no collider, skipping.");
                return 0;
            }

            // Get prefab list based on zone type
            List<GameObject> prefabs = GetPrefabsForZoneType(zone.type);
            if (prefabs == null || prefabs.Count == 0)
            {
                Debug.Log($"[ZonePopulator] No prefabs for zone type '{zone.type}', skipping.");
                return 0;
            }

            // Calculate spawn count based on area
            float area = CalculateArea(col);
            int baseCount = Mathf.RoundToInt(area / areaPerObject);
            int spawnCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * densityMultiplier));

            // Get or create content container
            Transform container = GetOrCreateContainer(zone);

            // Spawn objects
            int spawned = 0;
            Bounds bounds = col.bounds;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPos;
                if (TryGetSpawnPosition(bounds, out spawnPos))
                {
                    GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
                    SpawnObject(prefab, spawnPos, container);
                    spawned++;
                }
            }

            Debug.Log($"<color=cyan>[ZonePopulator]</color> Zone '{zone.name}' ({zone.type}): Spawned {spawned}/{spawnCount} objects.");
            return spawned;
        }

        /// <summary>
        /// Gets the appropriate prefab list for a zone type.
        /// </summary>
        private List<GameObject> GetPrefabsForZoneType(MapZoneType type)
        {
            switch (type)
            {
                case MapZoneType.Resource_Wood:
                    // Combine trees and foliage
                    var woodList = new List<GameObject>();
                    if (biome.treePrefabs != null) woodList.AddRange(biome.treePrefabs);
                    if (biome.foliagePrefabs != null) woodList.AddRange(biome.foliagePrefabs);
                    return woodList;

                case MapZoneType.Resource_Stone:
                    return biome.rockPrefabs;

                case MapZoneType.Resource_Food:
                    // Use foliage and decor for farms
                    var foodList = new List<GameObject>();
                    if (biome.foliagePrefabs != null) foodList.AddRange(biome.foliagePrefabs);
                    if (biome.decorPrefabs != null) foodList.AddRange(biome.decorPrefabs);
                    return foodList;

                case MapZoneType.EnemyCamp:
                    // Use rocks and decor for enemy camps
                    var campList = new List<GameObject>();
                    if (biome.rockPrefabs != null) campList.AddRange(biome.rockPrefabs);
                    if (biome.decorPrefabs != null) campList.AddRange(biome.decorPrefabs);
                    return campList;

                case MapZoneType.Path_Main:
                    // Sparse decor along paths
                    return biome.decorPrefabs;

                case MapZoneType.NoBuild:
                case MapZoneType.BuildAllowed:
                default:
                    // Skip logical zones
                    return null;
            }
        }

        /// <summary>
        /// Calculates the approximate area of a collider.
        /// </summary>
        private float CalculateArea(Collider col)
        {
            if (col is BoxCollider box)
            {
                Vector3 size = Vector3.Scale(box.size, col.transform.lossyScale);
                return size.x * size.z;
            }
            else if (col is SphereCollider sphere)
            {
                float radius = sphere.radius * Mathf.Max(col.transform.lossyScale.x, col.transform.lossyScale.z);
                return Mathf.PI * radius * radius;
            }
            else
            {
                // Fallback to bounds
                Bounds bounds = col.bounds;
                return bounds.size.x * bounds.size.z;
            }
        }

        /// <summary>
        /// Tries to find a valid spawn position using raycast.
        /// </summary>
        private bool TryGetSpawnPosition(Bounds bounds, out Vector3 position)
        {
            // Try several times to find a valid position
            for (int attempt = 0; attempt < 5; attempt++)
            {
                Vector3 randomPoint = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    raycastHeight,
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                Ray ray = new Ray(randomPoint, Vector3.down);
                
                if (Physics.Raycast(ray, out RaycastHit hit, raycastHeight * 2f, groundLayer))
                {
                    position = hit.point;
                    return true;
                }
            }

            // Fallback to bounds center at y=0
            position = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                0f,
                Random.Range(bounds.min.z, bounds.max.z)
            );
            return true;
        }

        /// <summary>
        /// Spawns a single object with randomization.
        /// </summary>
        private void SpawnObject(GameObject prefab, Vector3 position, Transform parent)
        {
            // Random rotation
            Quaternion rotation = Quaternion.identity;
            if (randomRotation)
            {
                rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            // Random scale
            float scale = Random.Range(scaleRange.x, scaleRange.y);

            // Instantiate
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = Vector3.one * scale;

            // Static flags
            if (makeStatic)
            {
                instance.isStatic = true;
                GameObjectUtility.SetStaticEditorFlags(instance, 
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Object");
        }

        /// <summary>
        /// Gets or creates the populated content container for a zone.
        /// </summary>
        private Transform GetOrCreateContainer(MapZone zone)
        {
            if (parentToZone)
            {
                // Look for existing container
                Transform existing = zone.transform.Find(POPULATED_CONTAINER_NAME);
                if (existing != null)
                {
                    return existing;
                }

                // Create new container
                GameObject container = new GameObject(POPULATED_CONTAINER_NAME);
                container.transform.SetParent(zone.transform);
                container.transform.localPosition = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(container, "Create Container");
                return container.transform;
            }
            else
            {
                return environmentParent;
            }
        }

        /// <summary>
        /// Clears populated content from a zone.
        /// </summary>
        private int ClearZone(MapZone zone)
        {
            int cleared = 0;

            // Find the populated container
            Transform container = zone.transform.Find(POPULATED_CONTAINER_NAME);
            if (container != null)
            {
                cleared = container.childCount;
                Undo.DestroyObjectImmediate(container.gameObject);
            }

            return cleared;
        }

        // ═══════════════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gets all selected MapZone components.
        /// </summary>
        private List<MapZone> GetSelectedZones()
        {
            List<MapZone> zones = new List<MapZone>();

            foreach (var obj in Selection.gameObjects)
            {
                if (obj.scene.IsValid()) // Only scene objects
                {
                    MapZone zone = obj.GetComponent<MapZone>();
                    if (zone != null)
                    {
                        zones.Add(zone);
                    }
                }
            }

            return zones;
        }

        // ═══════════════════════════════════════════════════════════════════
        // QUICK ACTIONS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Quick Actions")]
        [HorizontalGroup("Quick Actions/Row")]
        [Button("🔍 Find All Zones", ButtonSizes.Medium)]
        private void FindAllZones()
        {
            var zones = FindObjectsByType<MapZone>(FindObjectsSortMode.None);
            
            if (zones.Length == 0)
            {
                EditorUtility.DisplayDialog("No Zones", "No MapZone components found in scene.", "OK");
                return;
            }

            GameObject[] zoneObjects = new GameObject[zones.Length];
            for (int i = 0; i < zones.Length; i++)
            {
                zoneObjects[i] = zones[i].gameObject;
            }

            Selection.objects = zoneObjects;
            Debug.Log($"<color=cyan>[ZonePopulator]</color> Selected {zones.Length} zones.");
        }

        [HorizontalGroup("Quick Actions/Row")]
        [Button("📊 Zone Summary", ButtonSizes.Medium)]
        private void ShowZoneSummary()
        {
            var zones = FindObjectsByType<MapZone>(FindObjectsSortMode.None);

            Dictionary<MapZoneType, int> counts = new Dictionary<MapZoneType, int>();
            foreach (var zone in zones)
            {
                if (!counts.ContainsKey(zone.type))
                    counts[zone.type] = 0;
                counts[zone.type]++;
            }

            string summary = "=== Zone Summary ===\n";
            foreach (var kvp in counts)
            {
                summary += $"  {kvp.Key}: {kvp.Value}\n";
            }
            summary += $"\nTotal: {zones.Length} zones";

            Debug.Log(summary);
        }

        [TitleGroup("Biome Info")]
        [Button("📋 Show Biome Prefabs", ButtonSizes.Medium)]
        [ShowIf("@biome != null")]
        private void ShowBiomePrefabs()
        {
            if (biome == null) return;

            Debug.Log($"=== {biome.biomeId} Prefab Counts ===\n" +
                $"  Trees: {biome.treePrefabs?.Count ?? 0}\n" +
                $"  Rocks: {biome.rockPrefabs?.Count ?? 0}\n" +
                $"  Foliage: {biome.foliagePrefabs?.Count ?? 0}\n" +
                $"  Decor: {biome.decorPrefabs?.Count ?? 0}");
        }
    }
}
