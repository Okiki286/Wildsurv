using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
namespace WildernessEditor
{
    /// <summary>
    /// Procedural World Generator Tool with Odin Inspector.
    /// Scans for environmental prefabs and generates test maps using Perlin Noise.
    /// </summary>
    public class WorldGeneratorTool : OdinEditorWindow
    {
        // ═══════════════════════════════════════════════════════════════════
        // WINDOW MENU
        // ═══════════════════════════════════════════════════════════════════
        
        [MenuItem("Tools/Wilderness/🌍 World Generator")]
        private static void OpenWindow()
        {
            var window = GetWindow<WorldGeneratorTool>();
            window.titleContent = new GUIContent("🌍 World Generator");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        // ═══════════════════════════════════════════════════════════════════
        // TAB 1: ASSET DISCOVERY
        // ═══════════════════════════════════════════════════════════════════
        
        [TabGroup("Tabs", "🔍 Asset Discovery")]
        [BoxGroup("Tabs/🔍 Asset Discovery/Search Settings")]
        [FolderPath(AbsolutePath = false)]
        [Tooltip("Folder path to search for prefabs (e.g., 'Assets/')")]
        public string searchPath = "Assets";

        [TabGroup("Tabs", "🔍 Asset Discovery")]
        [BoxGroup("Tabs/🔍 Asset Discovery/Ground Prefabs")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = true)]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        public List<GameObject> groundPrefabs = new List<GameObject>();

        [TabGroup("Tabs", "🔍 Asset Discovery")]
        [BoxGroup("Tabs/🔍 Asset Discovery/Tree Prefabs")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = true)]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        public List<GameObject> treePrefabs = new List<GameObject>();

        [TabGroup("Tabs", "🔍 Asset Discovery")]
        [BoxGroup("Tabs/🔍 Asset Discovery/Rock Prefabs")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = true)]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        public List<GameObject> rockPrefabs = new List<GameObject>();

        [TabGroup("Tabs", "🔍 Asset Discovery")]
        [BoxGroup("Tabs/🔍 Asset Discovery/Mountain Prefabs")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = true)]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        public List<GameObject> mountainPrefabs = new List<GameObject>();

        [TabGroup("Tabs", "🔍 Asset Discovery")]
        [BoxGroup("Tabs/🔍 Asset Discovery/Actions")]
        [Button("🔍 Auto-Discover Assets", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void AutoDiscoverAssets()
        {
            // Clear existing lists
            groundPrefabs.Clear();
            treePrefabs.Clear();
            rockPrefabs.Clear();
            mountainPrefabs.Clear();

            // Validate search path
            string validPath = string.IsNullOrEmpty(searchPath) ? "Assets" : searchPath;
            if (!AssetDatabase.IsValidFolder(validPath))
            {
                Debug.LogError($"[WorldGenerator] Invalid search path: {validPath}");
                return;
            }

            // Find all prefabs
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { validPath });
            int totalFound = 0;

            EditorUtility.DisplayProgressBar("Scanning Prefabs", "Searching...", 0f);

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab == null) continue;

                    string nameLower = prefab.name.ToLowerInvariant();

                    // Categorize by keywords (case insensitive)
                    if (ContainsAny(nameLower, "ground", "tile", "terrain", "floor"))
                    {
                        groundPrefabs.Add(prefab);
                        totalFound++;
                    }
                    else if (ContainsAny(nameLower, "tree", "pine", "palm", "oak", "birch"))
                    {
                        treePrefabs.Add(prefab);
                        totalFound++;
                    }
                    else if (ContainsAny(nameLower, "rock", "stone", "boulder"))
                    {
                        rockPrefabs.Add(prefab);
                        totalFound++;
                    }
                    else if (ContainsAny(nameLower, "mountain", "cliff", "hill", "peak"))
                    {
                        mountainPrefabs.Add(prefab);
                        totalFound++;
                    }

                    // Update progress
                    if (i % 50 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Scanning Prefabs", 
                            $"Processing: {path}", 
                            (float)i / guids.Length);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"<color=cyan>[WorldGenerator]</color> Asset Discovery Complete!\n" +
                     $"• Ground: {groundPrefabs.Count}\n" +
                     $"• Trees: {treePrefabs.Count}\n" +
                     $"• Rocks: {rockPrefabs.Count}\n" +
                     $"• Mountains: {mountainPrefabs.Count}\n" +
                     $"• Total: {totalFound} prefabs found");

            // Show warning if lists are empty
            if (totalFound == 0)
            {
                EditorUtility.DisplayDialog("No Assets Found", 
                    "No matching prefabs were found in the specified path.\n\n" +
                    "Ensure your prefabs contain keywords like:\n" +
                    "• Ground: 'Ground', 'Tile', 'Terrain', 'Floor'\n" +
                    "• Trees: 'Tree', 'Pine', 'Palm', 'Oak'\n" +
                    "• Rocks: 'Rock', 'Stone', 'Boulder'\n" +
                    "• Mountains: 'Mountain', 'Cliff', 'Hill'", 
                    "OK");
            }
        }

        [TabGroup("Tabs", "🔍 Asset Discovery")]
        [BoxGroup("Tabs/🔍 Asset Discovery/Actions")]
        [Button("🗑️ Clear All Lists")]
        [GUIColor(1f, 0.6f, 0.6f)]
        private void ClearAllLists()
        {
            groundPrefabs.Clear();
            treePrefabs.Clear();
            rockPrefabs.Clear();
            mountainPrefabs.Clear();
            Debug.Log("<color=yellow>[WorldGenerator]</color> All prefab lists cleared.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // TAB 2: GENERATION
        // ═══════════════════════════════════════════════════════════════════
        
        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Map Settings")]
        [Tooltip("Size of the map in tiles (X, Z)")]
        public Vector2Int mapSize = new Vector2Int(50, 50);

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Map Settings")]
        [Tooltip("Size of each tile in world units")]
        [Range(0.5f, 10f)]
        public float tileSize = 2.0f;

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Noise Settings")]
        [Tooltip("Scale of the Perlin noise (lower = larger features)")]
        [Range(0.01f, 0.5f)]
        public float noiseScale = 0.1f;

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Noise Settings")]
        [Tooltip("Random seed offset for variation")]
        public Vector2 noiseSeed = new Vector2(0f, 0f);

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Spawn Settings")]
        [Tooltip("Probability of spawning objects (0-1)")]
        [Range(0f, 1f)]
        public float density = 0.3f;

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Spawn Settings")]
        [Tooltip("Threshold for mountain spawning (noise value)")]
        [Range(0.5f, 1f)]
        public float mountainThreshold = 0.75f;

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Spawn Settings")]
        [Tooltip("Threshold for tree spawning (noise value)")]
        [Range(0.3f, 0.8f)]
        public float treeThreshold = 0.5f;

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Spawn Settings")]
        [Tooltip("Threshold for rock spawning (noise value)")]
        [Range(0.1f, 0.5f)]
        public float rockThreshold = 0.3f;

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Randomization")]
        [Tooltip("Enable random Y rotation")]
        public bool randomRotation = true;

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Randomization")]
        [Tooltip("Scale variation range")]
        [MinMaxSlider(0.5f, 1.5f, true)]
        public Vector2 scaleVariation = new Vector2(0.9f, 1.1f);

        // Statistics
        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Statistics")]
        [ReadOnly, ShowInInspector]
        private int lastGroundCount;
        
        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Statistics")]
        [ReadOnly, ShowInInspector]
        private int lastTreeCount;
        
        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Statistics")]
        [ReadOnly, ShowInInspector]
        private int lastRockCount;
        
        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Statistics")]
        [ReadOnly, ShowInInspector]
        private int lastMountainCount;

        // ═══════════════════════════════════════════════════════════════════
        // GENERATION ACTIONS
        // ═══════════════════════════════════════════════════════════════════

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Actions")]
        [Button("🌱 Generate World", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void GenerateWorld()
        {
            // Safety checks
            bool hasAnyPrefabs = groundPrefabs.Count > 0 || treePrefabs.Count > 0 || 
                                rockPrefabs.Count > 0 || mountainPrefabs.Count > 0;

            if (!hasAnyPrefabs)
            {
                if (!EditorUtility.DisplayDialog("No Prefabs Assigned",
                    "No prefabs are assigned. A default ground plane will be created.\n\n" +
                    "Would you like to continue?",
                    "Continue", "Cancel"))
                {
                    return;
                }
            }

            // Reset statistics
            lastGroundCount = 0;
            lastTreeCount = 0;
            lastRockCount = 0;
            lastMountainCount = 0;

            // Cleanup existing world
            CleanupExistingWorld();

            // Create parent container
            GameObject worldParent = new GameObject("GeneratedWorld");
            Undo.RegisterCreatedObjectUndo(worldParent, "Generate World");

            // Create sub-containers for organization
            GameObject groundContainer = CreateContainer("Ground", worldParent);
            GameObject treesContainer = CreateContainer("Trees", worldParent);
            GameObject rocksContainer = CreateContainer("Rocks", worldParent);
            GameObject mountainsContainer = CreateContainer("Mountains", worldParent);

            EditorUtility.DisplayProgressBar("Generating World", "Creating ground...", 0.1f);

            try
            {
                // Generate ground
                GenerateGround(groundContainer);

                EditorUtility.DisplayProgressBar("Generating World", "Scattering objects...", 0.3f);

                // Generate objects using Perlin Noise
                for (int x = 0; x < mapSize.x; x++)
                {
                    for (int z = 0; z < mapSize.y; z++)
                    {
                        float worldX = x * tileSize;
                        float worldZ = z * tileSize;

                        // Calculate noise value
                        float noiseX = (x + noiseSeed.x) * noiseScale;
                        float noiseZ = (z + noiseSeed.y) * noiseScale;
                        float noiseValue = Mathf.PerlinNoise(noiseX, noiseZ);

                        // Density check
                        if (Random.value > density) continue;

                        Vector3 position = new Vector3(worldX, 0, worldZ);
                        
                        // Determine what to spawn based on noise value
                        if (noiseValue > mountainThreshold && mountainPrefabs.Count > 0)
                        {
                            // Mountains on high noise values (edges/clusters)
                            SpawnPrefab(mountainPrefabs, position, mountainsContainer);
                            lastMountainCount++;
                        }
                        else if (noiseValue > treeThreshold && noiseValue <= mountainThreshold && treePrefabs.Count > 0)
                        {
                            // Trees on medium-high noise values
                            SpawnPrefab(treePrefabs, position, treesContainer);
                            lastTreeCount++;
                        }
                        else if (noiseValue > rockThreshold && noiseValue <= treeThreshold && rockPrefabs.Count > 0)
                        {
                            // Rocks on medium-low noise values
                            SpawnPrefab(rockPrefabs, position, rocksContainer);
                            lastRockCount++;
                        }
                    }

                    // Update progress
                    if (x % 5 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Generating World", 
                            $"Scattering objects ({x}/{mapSize.x})...", 
                            0.3f + (0.6f * ((float)x / mapSize.x)));
                    }
                }

                EditorUtility.DisplayProgressBar("Generating World", "Setting static flags...", 0.95f);

                // Set static flags for all children
                SetStaticFlagsRecursive(worldParent);

                // Select the new world object
                Selection.activeGameObject = worldParent;

                Debug.Log($"<color=green>[WorldGenerator]</color> World Generated Successfully!\n" +
                         $"• Ground tiles: {lastGroundCount}\n" +
                         $"• Trees: {lastTreeCount}\n" +
                         $"• Rocks: {lastRockCount}\n" +
                         $"• Mountains: {lastMountainCount}\n" +
                         $"• Total objects: {lastGroundCount + lastTreeCount + lastRockCount + lastMountainCount}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Actions")]
        [Button("🎲 Randomize Seed")]
        [GUIColor(0.9f, 0.7f, 1f)]
        private void RandomizeSeed()
        {
            noiseSeed = new Vector2(Random.Range(0f, 10000f), Random.Range(0f, 10000f));
            Debug.Log($"<color=magenta>[WorldGenerator]</color> New seed: {noiseSeed}");
        }

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Actions")]
        [Button("🗺️ Bake NavMesh", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.6f, 1f)]
        private void BakeNavMesh()
        {
            // Check if GeneratedWorld exists
            GameObject world = GameObject.Find("GeneratedWorld");
            if (world == null)
            {
                EditorUtility.DisplayDialog("No World Found",
                    "Please generate a world first using 'Generate World' button.",
                    "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Baking NavMesh", "Building navigation mesh...", 0.5f);

            try
            {
                // Use legacy NavMesh API (suppress deprecation warning)
                #pragma warning disable CS0618
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                #pragma warning restore CS0618
                Debug.Log("<color=blue>[WorldGenerator]</color> NavMesh baked successfully!");
                
                EditorUtility.DisplayDialog("NavMesh Baked",
                    "NavMesh has been baked successfully.\n\n" +
                    "Ensure your characters have NavMeshAgent components.",
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WorldGenerator] NavMesh baking failed: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [TabGroup("Tabs", "🌍 Generation")]
        [BoxGroup("Tabs/🌍 Generation/Actions")]
        [Button("🗑️ Delete Generated World")]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DeleteGeneratedWorld()
        {
            CleanupExistingWorld();
            Debug.Log("<color=orange>[WorldGenerator]</color> Generated world deleted.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════

        private bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(k => text.Contains(k));
        }

        private void CleanupExistingWorld()
        {
            GameObject existing = GameObject.Find("GeneratedWorld");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
        }

        private GameObject CreateContainer(string name, GameObject parent)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent.transform);
            container.transform.localPosition = Vector3.zero;
            return container;
        }

        private void GenerateGround(GameObject container)
        {
            if (groundPrefabs.Count > 0)
            {
                // Use ground prefabs in a grid pattern
                for (int x = 0; x < mapSize.x; x++)
                {
                    for (int z = 0; z < mapSize.y; z++)
                    {
                        Vector3 position = new Vector3(x * tileSize, 0, z * tileSize);
                        GameObject prefab = groundPrefabs[Random.Range(0, groundPrefabs.Count)];
                        
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        if (instance != null)
                        {
                            instance.transform.position = position;
                            instance.transform.SetParent(container.transform);
                            instance.layer = LayerMask.NameToLayer("Default");
                            lastGroundCount++;
                        }
                    }
                }
            }
            else
            {
                // Create a default scaled plane
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                plane.name = "DefaultGround";
                plane.transform.SetParent(container.transform);
                
                // Position and scale the plane
                float totalWidth = mapSize.x * tileSize;
                float totalHeight = mapSize.y * tileSize;
                
                plane.transform.position = new Vector3(totalWidth / 2f - tileSize / 2f, 0, totalHeight / 2f - tileSize / 2f);
                plane.transform.rotation = Quaternion.Euler(90, 0, 0);
                plane.transform.localScale = new Vector3(totalWidth, totalHeight, 1);
                
                // Add a simple material
                Renderer renderer = plane.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.3f, 0.5f, 0.2f); // Green-ish ground
                    renderer.material = mat;
                }
                
                plane.layer = LayerMask.NameToLayer("Default");
                lastGroundCount = 1;
                
                Debug.Log("<color=yellow>[WorldGenerator]</color> No ground prefabs found. Created default plane.");
            }
        }

        private void SpawnPrefab(List<GameObject> prefabList, Vector3 position, GameObject parent)
        {
            if (prefabList == null || prefabList.Count == 0) return;

            // Select random prefab
            GameObject prefab = prefabList[Random.Range(0, prefabList.Count)];
            if (prefab == null) return;

            // Instantiate as prefab instance (maintains prefab link)
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                // Fallback to regular instantiate
                instance = Instantiate(prefab);
            }

            // Set position
            instance.transform.position = position;
            instance.transform.SetParent(parent.transform);

            // Apply random rotation (Y axis only)
            if (randomRotation)
            {
                instance.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }

            // Apply random scale variation
            float scale = Random.Range(scaleVariation.x, scaleVariation.y);
            instance.transform.localScale = Vector3.one * scale;
        }

        private void SetStaticFlagsRecursive(GameObject root)
        {
            // Set BatchingStatic flag for mobile optimization
            // Note: NavigationStatic is deprecated - use NavMeshSurface components for modern NavMesh workflow
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI;
            
            SetStaticFlagsForObject(root, flags);

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                SetStaticFlagsForObject(child.gameObject, flags);
            }
        }

        private void SetStaticFlagsForObject(GameObject obj, StaticEditorFlags flags)
        {
            GameObjectUtility.SetStaticEditorFlags(obj, flags);
        }
    }
}
#endif
