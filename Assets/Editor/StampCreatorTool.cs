using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using WildernessSurvival.World;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor tool for creating Map Stamp prefabs from selected scene objects.
    /// Automates the process of grouping, organizing, and saving reusable level chunks.
    /// </summary>
    public class StampCreatorTool : OdinEditorWindow
    {
        // ═══════════════════════════════════════════════════════════════════
        // WINDOW MENU
        // ═══════════════════════════════════════════════════════════════════

        [MenuItem("Tools/Wilderness/📦 Stamp Creator")]
        private static void OpenWindow()
        {
            var window = GetWindow<StampCreatorTool>();
            window.titleContent = new GUIContent("📦 Stamp Creator");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════

        private const string STAMPS_BASE_PATH = "Assets/_Art/Environment/Stamps";

        // ═══════════════════════════════════════════════════════════════════
        // INPUT FIELDS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Stamp Configuration")]
        [BoxGroup("Stamp Configuration/Type")]
        [EnumToggleButtons]
        [Tooltip("Category of the stamp")]
        public StampType stampType = StampType.Forest;

        [BoxGroup("Stamp Configuration/Name")]
        [Tooltip("Name for the stamp (will be prefixed with ENV_Stamp_[Type]_)")]
        [ValidateInput("ValidateStampName", "Name cannot be empty or contain invalid characters")]
        public string stampName = "Dense_01";

        [BoxGroup("Stamp Configuration/Options")]
        [Tooltip("Set all objects to static for batching")]
        public bool makeStatic = true;

        [BoxGroup("Stamp Configuration/Options")]
        [Tooltip("Keep the instance in scene after creating prefab")]
        public bool keepInstance = false;

        [BoxGroup("Stamp Configuration/Options")]
        [Tooltip("Center objects at origin (0,0,0) or use geometric center")]
        public bool centerAtOrigin = false;

        // ═══════════════════════════════════════════════════════════════════
        // SELECTION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Selected Objects")]
        [InfoBox("Select objects in the Scene view, then click 'Refresh Selection'", InfoMessageType.Info)]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false)]
        [ShowInInspector]
        private List<GameObject> selectedObjects = new List<GameObject>();

        [TitleGroup("Selected Objects")]
        [ShowInInspector, ReadOnly]
        private int SelectionCount => selectedObjects.Count;

        [TitleGroup("Selected Objects")]
        [Button("🔄 Refresh Selection", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void RefreshSelection()
        {
            selectedObjects.Clear();

            // Get all selected GameObjects (excluding prefabs in project)
            foreach (var obj in Selection.gameObjects)
            {
                // Only include scene objects (not project assets)
                if (obj.scene.IsValid())
                {
                    selectedObjects.Add(obj);
                }
            }

            if (selectedObjects.Count == 0)
            {
                Debug.LogWarning("[StampCreator] No scene objects selected. Select objects in the Scene view.");
            }
            else
            {
                Debug.Log($"<color=cyan>[StampCreator]</color> Selected {selectedObjects.Count} objects.");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // MAIN ACTION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Create")]
        [Button("📦 Create Stamp Prefab", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.9f, 0.5f)]
        [EnableIf("CanCreateStamp")]
        private void CreateStampPrefab()
        {
            if (selectedObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No objects selected. Click 'Refresh Selection' first.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(stampName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a stamp name.", "OK");
                return;
            }

            // Build the prefab name
            string fullName = $"ENV_Stamp_{stampType}_{stampName}";
            
            Debug.Log($"<color=cyan>[StampCreator]</color> Creating stamp: {fullName}");

            // 1. Calculate geometric center of selected objects
            Vector3 center = CalculateGeometricCenter();
            if (centerAtOrigin)
            {
                center = Vector3.zero;
            }

            // 2. Create parent GameObject
            GameObject stampParent = new GameObject(fullName);
            stampParent.transform.position = center;
            Undo.RegisterCreatedObjectUndo(stampParent, "Create Stamp Parent");

            // 3. Parent all selected objects
            foreach (var obj in selectedObjects)
            {
                if (obj == null) continue;

                // Record for undo
                Undo.SetTransformParent(obj.transform, stampParent.transform, "Parent to Stamp");

                // Apply static flags if enabled
                if (makeStatic)
                {
                    SetStaticRecursive(obj);
                }
            }

            // 4. Add MapStamp component
            MapStamp stamp = stampParent.AddComponent<MapStamp>();
            stamp.stampType = stampType;
            stamp.UpdateObjectCount();
            stamp.AutoCalculateSize();

            // 5. Ensure save directory exists
            string typeFolderPath = Path.Combine(STAMPS_BASE_PATH, stampType.ToString());
            EnsureDirectoryExists(typeFolderPath);

            // 6. Save as prefab
            string prefabPath = Path.Combine(typeFolderPath, $"{fullName}.prefab");
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(stampParent, prefabPath, out bool success);

            if (success)
            {
                Debug.Log($"<color=green>[StampCreator]</color> ✅ Prefab saved: {prefabPath}");

                // 7. Handle instance
                if (keepInstance)
                {
                    // Replace with prefab instance
                    PrefabUtility.InstantiatePrefab(prefab);
                    Undo.DestroyObjectImmediate(stampParent);
                }
                else
                {
                    // Destroy scene instance
                    Undo.DestroyObjectImmediate(stampParent);
                }

                // Ping the created prefab
                EditorGUIUtility.PingObject(prefab);
                Selection.activeObject = prefab;

                // Clear selection list
                selectedObjects.Clear();

                EditorUtility.DisplayDialog("Success", 
                    $"Stamp prefab created!\n\n{prefabPath}", 
                    "OK");
            }
            else
            {
                Debug.LogError($"[StampCreator] Failed to save prefab at: {prefabPath}");
                EditorUtility.DisplayDialog("Error", "Failed to save prefab. Check console for details.", "OK");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calculates the geometric center of all selected objects.
        /// </summary>
        private Vector3 CalculateGeometricCenter()
        {
            if (selectedObjects.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var obj in selectedObjects)
            {
                if (obj != null)
                {
                    sum += obj.transform.position;
                    count++;
                }
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        /// <summary>
        /// Sets static flags recursively on a GameObject and all children.
        /// </summary>
        private void SetStaticRecursive(GameObject obj)
        {
            obj.isStatic = true;
            
            // Set specific editor flags for batching
            GameObjectUtility.SetStaticEditorFlags(obj, 
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);

            foreach (Transform child in obj.transform)
            {
                SetStaticRecursive(child.gameObject);
            }
        }

        /// <summary>
        /// Ensures a directory exists, creating it if necessary.
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                // Split path and create each segment
                string[] segments = path.Split('/');
                string currentPath = segments[0];

                for (int i = 1; i < segments.Length; i++)
                {
                    string nextPath = Path.Combine(currentPath, segments[i]);
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, segments[i]);
                        Debug.Log($"<color=cyan>[StampCreator]</color> Created folder: {nextPath}");
                    }
                    currentPath = nextPath;
                }
            }
        }

        /// <summary>
        /// Validates the stamp name input.
        /// </summary>
        private bool ValidateStampName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            
            // Check for invalid path characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return !name.Any(c => invalidChars.Contains(c));
        }

        /// <summary>
        /// Checks if we can create a stamp.
        /// </summary>
        private bool CanCreateStamp()
        {
            return selectedObjects.Count > 0 && !string.IsNullOrWhiteSpace(stampName);
        }

        // ═══════════════════════════════════════════════════════════════════
        // UTILITY BUTTONS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Utilities")]
        [HorizontalGroup("Utilities/Row")]
        [Button("🗂️ Open Stamps Folder", ButtonSizes.Medium)]
        private void OpenStampsFolder()
        {
            string typePath = Path.Combine(STAMPS_BASE_PATH, stampType.ToString());
            
            if (AssetDatabase.IsValidFolder(typePath))
            {
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(typePath);
                EditorGUIUtility.PingObject(folder);
                Selection.activeObject = folder;
            }
            else if (AssetDatabase.IsValidFolder(STAMPS_BASE_PATH))
            {
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(STAMPS_BASE_PATH);
                EditorGUIUtility.PingObject(folder);
                Selection.activeObject = folder;
            }
            else
            {
                EditorUtility.DisplayDialog("Folder Not Found", 
                    "Stamps folder doesn't exist yet. Create a stamp first.", "OK");
            }
        }

        [HorizontalGroup("Utilities/Row")]
        [Button("📋 List Existing Stamps", ButtonSizes.Medium)]
        private void ListExistingStamps()
        {
            string typePath = Path.Combine(STAMPS_BASE_PATH, stampType.ToString());
            
            if (!AssetDatabase.IsValidFolder(typePath))
            {
                Debug.Log($"[StampCreator] No stamps folder for type: {stampType}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { typePath });
            
            Debug.Log($"<color=cyan>[StampCreator]</color> === {stampType} Stamps ({guids.Length}) ===");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"  • {Path.GetFileNameWithoutExtension(path)}");
            }
        }

        [TitleGroup("Preview")]
        [Button("👁️ Preview Selection Bounds", ButtonSizes.Medium)]
        private void PreviewSelectionBounds()
        {
            if (selectedObjects.Count == 0)
            {
                Debug.LogWarning("[StampCreator] No objects selected.");
                return;
            }

            Vector3 center = CalculateGeometricCenter();
            Bounds bounds = new Bounds(center, Vector3.zero);

            foreach (var obj in selectedObjects)
            {
                if (obj == null) continue;

                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds.Encapsulate(obj.transform.position);
                }
            }

            Debug.Log($"<color=cyan>[StampCreator]</color> Selection Bounds:\n" +
                $"  Center: {center}\n" +
                $"  Size: {bounds.size}\n" +
                $"  Objects: {selectedObjects.Count}");

            // Focus scene view on the bounds
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Frame(bounds, false);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Auto-refresh selection when window opens
            RefreshSelection();
        }

        private void OnSelectionChange()
        {
            // Auto-refresh when selection changes
            RefreshSelection();
            Repaint();
        }
    }
}
