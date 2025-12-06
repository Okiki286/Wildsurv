using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
namespace WildernessEditor
{
    /// <summary>
    /// Environment Setup Tool for preparing the project for Stylized Nature Pack integration.
    /// Handles Layer/Tag setup, folder structure creation, and asset processing.
    /// </summary>
    public class EnvironmentSetupTool : OdinEditorWindow
    {
        // ═══════════════════════════════════════════════════════════════════
        // WINDOW MENU
        // ═══════════════════════════════════════════════════════════════════
        
        [MenuItem("Tools/Wilderness/🌿 Environment Setup")]
        private static void OpenWindow()
        {
            var window = GetWindow<EnvironmentSetupTool>();
            window.titleContent = new GUIContent("🌿 Environment Setup");
            window.minSize = new Vector2(500, 550);
            window.Show();
        }

        // ═══════════════════════════════════════════════════════════════════
        // REQUIRED TAGS & LAYERS
        // ═══════════════════════════════════════════════════════════════════
        
        private static readonly string[] REQUIRED_TAGS = new[]
        {
            "Env_Tree",
            "Env_Rock",
            "Env_Decor",
            "Env_Water"
        };

        private static readonly string[] REQUIRED_LAYERS = new[]
        {
            "Environment_Blocking",
            "Environment_Decor",
            "Water"
        };

        private static readonly string[] REQUIRED_FOLDERS = new[]
        {
            "Assets/_Art/Environment/Trees/Gameplay",
            "Assets/_Art/Environment/Rocks/Gameplay",
            "Assets/_Art/Environment/Foliage/Gameplay",
            "Assets/_Art/Environment/Water/Gameplay",
            "Assets/_Art/Environment/Decor/Gameplay"
        };

        // ═══════════════════════════════════════════════════════════════════
        // TAB 1: SETUP
        // ═══════════════════════════════════════════════════════════════════
        
        [TabGroup("Tabs", "⚙️ Setup")]
        [BoxGroup("Tabs/⚙️ Setup/Tags & Layers")]
        [Title("Required Tags")]
        [DisplayAsString, ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
        private List<string> RequiredTagsDisplay => REQUIRED_TAGS.ToList();

        [TabGroup("Tabs", "⚙️ Setup")]
        [BoxGroup("Tabs/⚙️ Setup/Tags & Layers")]
        [Title("Required Layers")]
        [DisplayAsString, ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
        private List<string> RequiredLayersDisplay => REQUIRED_LAYERS.ToList();

        [TabGroup("Tabs", "⚙️ Setup")]
        [BoxGroup("Tabs/⚙️ Setup/Tags & Layers")]
        [Button("🏷️ Setup Tags & Layers", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void SetupTagsAndLayers()
        {
            int tagsAdded = 0;
            int layersAdded = 0;

            // Get TagManager asset
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            // ─────────────────────────────────────────────────────────────────
            // SETUP TAGS
            // ─────────────────────────────────────────────────────────────────
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            foreach (string requiredTag in REQUIRED_TAGS)
            {
                bool tagExists = false;
                
                // Check existing tags
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == requiredTag)
                    {
                        tagExists = true;
                        break;
                    }
                }

                // Also check built-in tags
                if (!tagExists)
                {
                    try
                    {
                        // This will throw if tag doesn't exist, but we catch it
                        foreach (string tag in UnityEditorInternal.InternalEditorUtility.tags)
                        {
                            if (tag == requiredTag)
                            {
                                tagExists = true;
                                break;
                            }
                        }
                    }
                    catch { }
                }

                if (!tagExists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = requiredTag;
                    tagsAdded++;
                    Debug.Log($"<color=green>[EnvSetup]</color> Added tag: {requiredTag}");
                }
                else
                {
                    Debug.Log($"<color=yellow>[EnvSetup]</color> Tag already exists: {requiredTag}");
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // SETUP LAYERS
            // ─────────────────────────────────────────────────────────────────
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            foreach (string requiredLayer in REQUIRED_LAYERS)
            {
                bool layerExists = false;
                int firstEmptySlot = -1;

                // Check existing layers (start from layer 6, after UI layer 5)
                for (int i = 6; i < layersProp.arraySize; i++)
                {
                    string layerName = layersProp.GetArrayElementAtIndex(i).stringValue;
                    
                    if (layerName == requiredLayer)
                    {
                        layerExists = true;
                        break;
                    }
                    
                    if (string.IsNullOrEmpty(layerName) && firstEmptySlot == -1)
                    {
                        firstEmptySlot = i;
                    }
                }

                if (!layerExists && firstEmptySlot != -1)
                {
                    layersProp.GetArrayElementAtIndex(firstEmptySlot).stringValue = requiredLayer;
                    layersAdded++;
                    Debug.Log($"<color=green>[EnvSetup]</color> Added layer '{requiredLayer}' at slot {firstEmptySlot}");
                }
                else if (layerExists)
                {
                    Debug.Log($"<color=yellow>[EnvSetup]</color> Layer already exists: {requiredLayer}");
                }
                else
                {
                    Debug.LogWarning($"<color=red>[EnvSetup]</color> No empty layer slot available for: {requiredLayer}");
                }
            }

            tagManager.ApplyModifiedProperties();

            // Summary
            string message = $"Tags & Layers Setup Complete!\n\n" +
                           $"Tags added: {tagsAdded}\n" +
                           $"Layers added: {layersAdded}";
            
            EditorUtility.DisplayDialog("Setup Complete", message, "OK");
            Debug.Log($"<color=cyan>[EnvSetup]</color> {message.Replace("\n", " ")}");
        }

        // ═══════════════════════════════════════════════════════════════════
        // TAB 1: FOLDERS
        // ═══════════════════════════════════════════════════════════════════

        [TabGroup("Tabs", "⚙️ Setup")]
        [BoxGroup("Tabs/⚙️ Setup/Folder Structure")]
        [Title("Required Folders")]
        [DisplayAsString, ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
        private List<string> RequiredFoldersDisplay => REQUIRED_FOLDERS.ToList();

        [TabGroup("Tabs", "⚙️ Setup")]
        [BoxGroup("Tabs/⚙️ Setup/Folder Structure")]
        [Button("📁 Create Folder Structure", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.6f)]
        private void CreateFolderStructure()
        {
            int foldersCreated = 0;
            int foldersExisting = 0;

            foreach (string folderPath in REQUIRED_FOLDERS)
            {
                // Convert to system path
                string fullPath = Path.Combine(Application.dataPath, folderPath.Replace("Assets/", ""));
                
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    foldersCreated++;
                    Debug.Log($"<color=green>[EnvSetup]</color> Created folder: {folderPath}");
                }
                else
                {
                    foldersExisting++;
                    Debug.Log($"<color=yellow>[EnvSetup]</color> Folder exists: {folderPath}");
                }
            }

            AssetDatabase.Refresh();

            string message = $"Folder Structure Setup Complete!\n\n" +
                           $"Folders created: {foldersCreated}\n" +
                           $"Already existing: {foldersExisting}";

            EditorUtility.DisplayDialog("Folders Created", message, "OK");
            Debug.Log($"<color=cyan>[EnvSetup]</color> {message.Replace("\n", " ")}");
        }

        [TabGroup("Tabs", "⚙️ Setup")]
        [BoxGroup("Tabs/⚙️ Setup/Quick Actions")]
        [Button("🚀 Run Full Setup (Tags + Layers + Folders)", ButtonSizes.Large)]
        [GUIColor(0.9f, 0.7f, 0.3f)]
        private void RunFullSetup()
        {
            SetupTagsAndLayers();
            CreateFolderStructure();
            Debug.Log("<color=magenta>[EnvSetup]</color> ✅ Full setup complete!");
        }

        // ═══════════════════════════════════════════════════════════════════
        // TAB 2: ASSET PROCESSING
        // ═══════════════════════════════════════════════════════════════════

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Source")]
        [FolderPath(AbsolutePath = false)]
        [Tooltip("Source folder to scan for prefabs (e.g., 'Assets/SoStylized')")]
        public string sourcePath = "Assets/SoStylized";

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Filters")]
        [Tooltip("Only include prefabs containing these keywords (case insensitive)")]
        public List<string> includeKeywords = new List<string> { "LOD0", "Prefab" };

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Filters")]
        [Tooltip("Exclude prefabs containing these keywords (case insensitive)")]
        public List<string> excludeKeywords = new List<string> { "LOD1", "LOD2", "LOD3", "Collider" };

        // Results
        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Candidates")]
        [ReadOnly, ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = true)]
        private List<CandidateInfo> treeCandidates = new List<CandidateInfo>();

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Candidates")]
        [ReadOnly, ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = true)]
        private List<CandidateInfo> rockCandidates = new List<CandidateInfo>();

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Candidates")]
        [ReadOnly, ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = true)]
        private List<CandidateInfo> foliageCandidates = new List<CandidateInfo>();

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Candidates")]
        [ReadOnly, ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = true)]
        private List<CandidateInfo> waterCandidates = new List<CandidateInfo>();

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Candidates")]
        [ReadOnly, ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = true)]
        private List<CandidateInfo> decorCandidates = new List<CandidateInfo>();

        [System.Serializable]
        public class CandidateInfo
        {
            public string Name;
            public string Path;
            public string SuggestedDestination;

            public override string ToString() => $"{Name} → {SuggestedDestination}";
        }

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Actions")]
        [Button("🔍 Scan for Asset Candidates", ButtonSizes.Large)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void ScanAssetCandidates()
        {
            // Clear previous results
            treeCandidates.Clear();
            rockCandidates.Clear();
            foliageCandidates.Clear();
            waterCandidates.Clear();
            decorCandidates.Clear();

            if (string.IsNullOrEmpty(sourcePath) || !AssetDatabase.IsValidFolder(sourcePath))
            {
                EditorUtility.DisplayDialog("Invalid Path", 
                    $"Source path is invalid: {sourcePath}\n\nPlease select a valid folder.", "OK");
                return;
            }

            // Find all prefabs
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { sourcePath });
            int totalScanned = 0;
            int totalCandidates = 0;

            EditorUtility.DisplayProgressBar("Scanning Assets", "Processing prefabs...", 0f);

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    string fileNameLower = fileName.ToLowerInvariant();

                    totalScanned++;

                    // Apply include filter (if any keywords specified, at least one must match)
                    if (includeKeywords.Count > 0)
                    {
                        bool hasInclude = includeKeywords.Any(k => 
                            fileNameLower.Contains(k.ToLowerInvariant()));
                        
                        // If no include keyword matches and it's not a "main" prefab (no LOD suffix), skip
                        if (!hasInclude && !IsMainPrefab(fileNameLower))
                        {
                            continue;
                        }
                    }

                    // Apply exclude filter
                    if (excludeKeywords.Any(k => fileNameLower.Contains(k.ToLowerInvariant())))
                    {
                        continue;
                    }

                    // Categorize by name keywords
                    CandidateInfo candidate = new CandidateInfo
                    {
                        Name = fileName,
                        Path = path
                    };

                    if (ContainsAny(fileNameLower, "tree", "pine", "oak", "birch", "palm", "trunk"))
                    {
                        candidate.SuggestedDestination = "Trees/Gameplay";
                        treeCandidates.Add(candidate);
                        totalCandidates++;
                    }
                    else if (ContainsAny(fileNameLower, "rock", "stone", "boulder", "cliff", "mountain"))
                    {
                        candidate.SuggestedDestination = "Rocks/Gameplay";
                        rockCandidates.Add(candidate);
                        totalCandidates++;
                    }
                    else if (ContainsAny(fileNameLower, "grass", "bush", "fern", "flower", "mushroom", "plant", "leaf", "foliage"))
                    {
                        candidate.SuggestedDestination = "Foliage/Gameplay";
                        foliageCandidates.Add(candidate);
                        totalCandidates++;
                    }
                    else if (ContainsAny(fileNameLower, "water", "pond", "lake", "river", "stream"))
                    {
                        candidate.SuggestedDestination = "Water/Gameplay";
                        waterCandidates.Add(candidate);
                        totalCandidates++;
                    }
                    else if (ContainsAny(fileNameLower, "stump", "log", "branch", "decor", "prop"))
                    {
                        candidate.SuggestedDestination = "Decor/Gameplay";
                        decorCandidates.Add(candidate);
                        totalCandidates++;
                    }

                    // Update progress
                    if (i % 20 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Scanning Assets", 
                            $"Processing: {fileName}", (float)i / guids.Length);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Log results
            Debug.Log($"<color=cyan>[EnvSetup]</color> Asset Scan Complete!\n" +
                     $"• Scanned: {totalScanned} prefabs\n" +
                     $"• Candidates found: {totalCandidates}\n" +
                     $"  - Trees: {treeCandidates.Count}\n" +
                     $"  - Rocks: {rockCandidates.Count}\n" +
                     $"  - Foliage: {foliageCandidates.Count}\n" +
                     $"  - Water: {waterCandidates.Count}\n" +
                     $"  - Decor: {decorCandidates.Count}");

            if (totalCandidates == 0)
            {
                EditorUtility.DisplayDialog("No Candidates Found",
                    "No matching prefabs were found.\n\n" +
                    "Try adjusting the include/exclude keywords or check the source path.", "OK");
            }
        }

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Actions")]
        [Button("📋 Copy Candidates to Console", ButtonSizes.Medium)]
        [GUIColor(0.7f, 0.7f, 1f)]
        private void LogCandidatesToConsole()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("═══ ASSET CANDIDATES REPORT ═══\n");

            void LogCategory(string name, List<CandidateInfo> list)
            {
                sb.AppendLine($"【 {name} 】 ({list.Count} items)");
                foreach (var item in list)
                {
                    sb.AppendLine($"  • {item.Name}");
                    sb.AppendLine($"    From: {item.Path}");
                    sb.AppendLine($"    To:   Assets/_Art/Environment/{item.SuggestedDestination}");
                }
                sb.AppendLine();
            }

            LogCategory("TREES", treeCandidates);
            LogCategory("ROCKS", rockCandidates);
            LogCategory("FOLIAGE", foliageCandidates);
            LogCategory("WATER", waterCandidates);
            LogCategory("DECOR", decorCandidates);

            Debug.Log(sb.ToString());
        }

        [TabGroup("Tabs", "📦 Asset Processing")]
        [BoxGroup("Tabs/📦 Asset Processing/Actions")]
        [Button("📁 Move All Candidates to Gameplay Folders", ButtonSizes.Large)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void MoveAllCandidates()
        {
            int totalMoved = 0;

            if (!EditorUtility.DisplayDialog("Confirm Move",
                $"This will move {GetTotalCandidates()} prefabs to their respective Gameplay folders.\n\n" +
                "This action can be undone with Ctrl+Z.\n\nContinue?",
                "Move", "Cancel"))
            {
                return;
            }

            // Ensure folders exist
            CreateFolderStructure();

            totalMoved += MoveCandidates(treeCandidates, "Assets/_Art/Environment/Trees/Gameplay");
            totalMoved += MoveCandidates(rockCandidates, "Assets/_Art/Environment/Rocks/Gameplay");
            totalMoved += MoveCandidates(foliageCandidates, "Assets/_Art/Environment/Foliage/Gameplay");
            totalMoved += MoveCandidates(waterCandidates, "Assets/_Art/Environment/Water/Gameplay");
            totalMoved += MoveCandidates(decorCandidates, "Assets/_Art/Environment/Decor/Gameplay");

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Move Complete",
                $"Successfully moved {totalMoved} assets to Gameplay folders.", "OK");

            // Clear lists after move
            treeCandidates.Clear();
            rockCandidates.Clear();
            foliageCandidates.Clear();
            waterCandidates.Clear();
            decorCandidates.Clear();
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════

        private bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(k => text.Contains(k));
        }

        private bool IsMainPrefab(string fileName)
        {
            // A "main" prefab typically doesn't have LOD suffixes
            return !fileName.Contains("lod1") && 
                   !fileName.Contains("lod2") && 
                   !fileName.Contains("lod3") &&
                   !fileName.Contains("_lod");
        }

        private int GetTotalCandidates()
        {
            return treeCandidates.Count + rockCandidates.Count + 
                   foliageCandidates.Count + waterCandidates.Count + decorCandidates.Count;
        }

        private int MoveCandidates(List<CandidateInfo> candidates, string destinationFolder)
        {
            int moved = 0;

            foreach (var candidate in candidates)
            {
                string fileName = Path.GetFileName(candidate.Path);
                string newPath = Path.Combine(destinationFolder, fileName);

                string result = AssetDatabase.MoveAsset(candidate.Path, newPath);
                
                if (string.IsNullOrEmpty(result))
                {
                    moved++;
                    Debug.Log($"<color=green>[EnvSetup]</color> Moved: {fileName} → {destinationFolder}");
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[EnvSetup]</color> Failed to move {fileName}: {result}");
                }
            }

            return moved;
        }
    }
}
#endif
