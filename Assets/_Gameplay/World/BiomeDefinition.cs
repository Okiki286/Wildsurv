using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace WildernessSurvival.World
{
    /// <summary>
    /// ScriptableObject defining a biome's environmental prefabs, densities, and placement rules.
    /// Used by MapGenerator for data-driven procedural world generation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBiome", menuName = "Wilderness/World/Biome Definition")]
    public class BiomeDefinition : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════════
        // BIOME IDENTITY
        // ═══════════════════════════════════════════════════════════════════

        [BoxGroup("Identity")]
        [Tooltip("Unique identifier for this biome")]
        public string biomeId = "forest_temperate";

        [BoxGroup("Identity")]
        [TextArea(2, 4)]
        [Tooltip("Description of this biome")]
        public string description = "A temperate forest with mixed vegetation.";

        // ═══════════════════════════════════════════════════════════════════
        // PREFAB LISTS
        // ═══════════════════════════════════════════════════════════════════

        [FoldoutGroup("Prefabs")]
        [BoxGroup("Prefabs/Trees")]
        [AssetList(Path = "_Art/Environment/Trees")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        [Tooltip("Tree prefabs for this biome")]
        public List<GameObject> treePrefabs = new List<GameObject>();

        [FoldoutGroup("Prefabs")]
        [BoxGroup("Prefabs/Rocks")]
        [AssetList(Path = "_Art/Environment/Rocks")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        [Tooltip("Rock prefabs for this biome (blocking navigation)")]
        public List<GameObject> rockPrefabs = new List<GameObject>();

        [FoldoutGroup("Prefabs")]
        [BoxGroup("Prefabs/Foliage")]
        [AssetList(Path = "_Art/Environment/Foliage")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        [Tooltip("Foliage prefabs (grass, bushes, flowers - decorative)")]
        public List<GameObject> foliagePrefabs = new List<GameObject>();

        [FoldoutGroup("Prefabs")]
        [BoxGroup("Prefabs/Decor")]
        [AssetList(Path = "_Art/Environment/Decor")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        [Tooltip("Decorative prefabs (stumps, logs, branches)")]
        public List<GameObject> decorPrefabs = new List<GameObject>();

        // ═══════════════════════════════════════════════════════════════════
        // DENSITY SETTINGS (Per Chunk)
        // ═══════════════════════════════════════════════════════════════════

        [FoldoutGroup("Density")]
        [BoxGroup("Density/Count Ranges")]
        [MinMaxSlider(0, 100, true)]
        [Tooltip("Min/Max trees per chunk")]
        public Vector2Int treesPerChunk = new Vector2Int(5, 15);

        [FoldoutGroup("Density")]
        [BoxGroup("Density/Count Ranges")]
        [MinMaxSlider(0, 50, true)]
        [Tooltip("Min/Max rocks per chunk")]
        public Vector2Int rocksPerChunk = new Vector2Int(3, 8);

        [FoldoutGroup("Density")]
        [BoxGroup("Density/Count Ranges")]
        [MinMaxSlider(0, 200, true)]
        [Tooltip("Min/Max foliage per chunk")]
        public Vector2Int foliagePerChunk = new Vector2Int(20, 50);

        [FoldoutGroup("Density")]
        [BoxGroup("Density/Count Ranges")]
        [MinMaxSlider(0, 30, true)]
        [Tooltip("Min/Max decor per chunk")]
        public Vector2Int decorPerChunk = new Vector2Int(2, 8);

        // ═══════════════════════════════════════════════════════════════════
        // PLACEMENT RULES
        // ═══════════════════════════════════════════════════════════════════

        [FoldoutGroup("Rules")]
        [BoxGroup("Rules/Spacing")]
        [Range(1f, 10f)]
        [Tooltip("Minimum distance between trees")]
        public float minDistanceBetweenTrees = 3f;

        [FoldoutGroup("Rules")]
        [BoxGroup("Rules/Spacing")]
        [Range(0.5f, 5f)]
        [Tooltip("Minimum distance between rocks")]
        public float minDistanceBetweenRocks = 1.5f;

        [FoldoutGroup("Rules")]
        [BoxGroup("Rules/Terrain")]
        [Range(0f, 60f)]
        [Tooltip("Maximum terrain slope angle for tree placement (degrees)")]
        public float maxSlopeForTrees = 30f;

        [FoldoutGroup("Rules")]
        [BoxGroup("Rules/Terrain")]
        [Range(0f, 90f)]
        [Tooltip("Maximum terrain slope angle for foliage placement (degrees)")]
        public float maxSlopeForFoliage = 45f;

        [FoldoutGroup("Rules")]
        [BoxGroup("Rules/Safety")]
        [Range(3f, 20f)]
        [Tooltip("Radius around bonfire where nothing spawns")]
        public float bonfireSafeRadius = 8f;

        [FoldoutGroup("Rules")]
        [BoxGroup("Rules/Safety")]
        [Range(1f, 10f)]
        [Tooltip("Minimum distance from map edges")]
        public float edgeMargin = 2f;

        // ═══════════════════════════════════════════════════════════════════
        // RANDOMIZATION
        // ═══════════════════════════════════════════════════════════════════

        [FoldoutGroup("Randomization")]
        [MinMaxSlider(0.5f, 1.5f, true)]
        [Tooltip("Scale variation range for all objects")]
        public Vector2 scaleVariation = new Vector2(0.85f, 1.15f);

        [FoldoutGroup("Randomization")]
        [Tooltip("Enable random Y-axis rotation")]
        public bool randomRotation = true;

        // ═══════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gets a random tree prefab from the list.
        /// </summary>
        public GameObject GetRandomTree()
        {
            if (treePrefabs == null || treePrefabs.Count == 0) return null;
            return treePrefabs[Random.Range(0, treePrefabs.Count)];
        }

        /// <summary>
        /// Gets a random rock prefab from the list.
        /// </summary>
        public GameObject GetRandomRock()
        {
            if (rockPrefabs == null || rockPrefabs.Count == 0) return null;
            return rockPrefabs[Random.Range(0, rockPrefabs.Count)];
        }

        /// <summary>
        /// Gets a random foliage prefab from the list.
        /// </summary>
        public GameObject GetRandomFoliage()
        {
            if (foliagePrefabs == null || foliagePrefabs.Count == 0) return null;
            return foliagePrefabs[Random.Range(0, foliagePrefabs.Count)];
        }

        /// <summary>
        /// Gets a random decor prefab from the list.
        /// </summary>
        public GameObject GetRandomDecor()
        {
            if (decorPrefabs == null || decorPrefabs.Count == 0) return null;
            return decorPrefabs[Random.Range(0, decorPrefabs.Count)];
        }

        /// <summary>
        /// Gets a random count within the specified range.
        /// </summary>
        public int GetRandomTreeCount() => Random.Range(treesPerChunk.x, treesPerChunk.y + 1);
        public int GetRandomRockCount() => Random.Range(rocksPerChunk.x, rocksPerChunk.y + 1);
        public int GetRandomFoliageCount() => Random.Range(foliagePerChunk.x, foliagePerChunk.y + 1);
        public int GetRandomDecorCount() => Random.Range(decorPerChunk.x, decorPerChunk.y + 1);

        /// <summary>
        /// Gets a random scale based on scale variation settings.
        /// </summary>
        public float GetRandomScale() => Random.Range(scaleVariation.x, scaleVariation.y);

        /// <summary>
        /// Gets a random Y rotation if enabled.
        /// </summary>
        public Quaternion GetRandomRotation()
        {
            if (!randomRotation) return Quaternion.identity;
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        // ═══════════════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════════════

        [Button("🔍 Validate Biome", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void ValidateBiome()
        {
            int issues = 0;

            if (string.IsNullOrEmpty(biomeId))
            {
                Debug.LogWarning("[BiomeDefinition] Biome ID is empty!");
                issues++;
            }

            if (treePrefabs.Count == 0) Debug.LogWarning("[BiomeDefinition] No tree prefabs assigned.");
            if (rockPrefabs.Count == 0) Debug.LogWarning("[BiomeDefinition] No rock prefabs assigned.");
            if (foliagePrefabs.Count == 0) Debug.LogWarning("[BiomeDefinition] No foliage prefabs assigned.");

            // Check for null entries
            issues += ValidatePrefabList(treePrefabs, "Trees");
            issues += ValidatePrefabList(rockPrefabs, "Rocks");
            issues += ValidatePrefabList(foliagePrefabs, "Foliage");
            issues += ValidatePrefabList(decorPrefabs, "Decor");

            if (issues == 0)
            {
                Debug.Log($"<color=green>[BiomeDefinition]</color> '{biomeId}' validation passed!");
            }
            else
            {
                Debug.LogWarning($"<color=orange>[BiomeDefinition]</color> '{biomeId}' has {issues} issue(s).");
            }
        }

        private int ValidatePrefabList(List<GameObject> list, string name)
        {
            int nullCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    Debug.LogWarning($"[BiomeDefinition] {name} list has null entry at index {i}");
                    nullCount++;
                }
            }
            return nullCount;
        }
    }
}
