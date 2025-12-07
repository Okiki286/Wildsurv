using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// All-in-one tool that copies game systems from SampleScene to the current scene.
    /// Automatically handles hierarchy organization and component setup.
    /// </summary>
    public class SceneSystemClonerTool : OdinEditorWindow
    {
        // ═══════════════════════════════════════════════════════════════════
        // WINDOW MENU
        // ═══════════════════════════════════════════════════════════════════

        [MenuItem("Tools/Wilderness/📋 Scene System Cloner")]
        private static void OpenWindow()
        {
            var window = GetWindow<SceneSystemClonerTool>();
            window.titleContent = new GUIContent("📋 Scene Cloner");
            window.minSize = new Vector2(500, 700);
            window.Show();
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Source Scene")]
        [InfoBox("This tool copies game systems from SampleScene.unity to your current scene.")]
        [ShowInInspector, ReadOnly]
        private string sourceScenePath = "Assets/Scenes/SampleScene.unity";

        [TitleGroup("Source Scene")]
        [ShowInInspector, ReadOnly]
        private string CurrentScene => EditorSceneManager.GetActiveScene().name;

        // ═══════════════════════════════════════════════════════════════════
        // SYSTEM SELECTION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Systems to Copy")]
        [BoxGroup("Systems to Copy/Core")]
        [ToggleLeft] public bool copyGameManager = true;

        [BoxGroup("Systems to Copy/Core")]
        [ToggleLeft] public bool copyWorkerSystem = true;

        [BoxGroup("Systems to Copy/Core")]
        [ToggleLeft] public bool copyStructureSystem = true;

        [BoxGroup("Systems to Copy/Core")]
        [ToggleLeft] public bool copyResourceSystem = true;

        [BoxGroup("Systems to Copy/Core")]
        [ToggleLeft] public bool copyBuildModeController = true;

        [BoxGroup("Systems to Copy/Atmosphere")]
        [ToggleLeft] public bool copyDayNightSystem = true;

        [BoxGroup("Systems to Copy/Atmosphere")]
        [ToggleLeft] public bool copyLightingManager = true;

        [BoxGroup("Systems to Copy/Atmosphere")]
        [ToggleLeft] public bool copyGlobalVolume = false;

        [TitleGroup("Camera & Lights")]
        [BoxGroup("Camera & Lights/Options")]
        [ToggleLeft] public bool copyMainCamera = true;

        [BoxGroup("Camera & Lights/Options")]
        [ToggleLeft] public bool copyDirectionalLight = true;

        [TitleGroup("UI")]
        [BoxGroup("UI/Options")]
        [ToggleLeft] public bool copyCanvas = true;

        [BoxGroup("UI/Options")]
        [ToggleLeft] public bool copyEventSystem = true;

        // ═══════════════════════════════════════════════════════════════════
        // MAIN ACTION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Execute")]
        [Button("📋 COPY SYSTEMS TO CURRENT SCENE", ButtonSizes.Large)]
        [GUIColor(0.3f, 0.9f, 0.4f)]
        private void CopySystemsToCurrentScene()
        {
            // Validate source scene exists
            if (!System.IO.File.Exists(sourceScenePath))
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Source scene not found:\n{sourceScenePath}", "OK");
                return;
            }

            // Confirm action
            if (!EditorUtility.DisplayDialog("Confirm Copy", 
                $"This will copy selected systems from SampleScene to '{CurrentScene}'.\n\n" +
                "Existing objects with the same name will be REPLACED.\n\n" +
                "Continue?", "Copy", "Cancel"))
            {
                return;
            }

            // Save current scene first
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=cyan>[SceneCloner]</color> Starting system copy...");

            // Build list of objects to copy
            List<string> objectsToCopy = BuildCopyList();
            
            if (objectsToCopy.Count == 0)
            {
                EditorUtility.DisplayDialog("Nothing to Copy", 
                    "No systems selected for copying.", "OK");
                return;
            }

            // Open source scene additively
            var currentScene = EditorSceneManager.GetActiveScene();
            var sourceScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Additive);

            int copiedCount = 0;
            List<string> copiedNames = new List<string>();

            try
            {
                // Find and copy each object
                foreach (string objName in objectsToCopy)
                {
                    GameObject sourceObj = FindObjectInScene(sourceScene, objName);
                    
                    if (sourceObj != null)
                    {
                        // Check if already exists in current scene
                        GameObject existingObj = FindObjectInScene(currentScene, objName);
                        if (existingObj != null)
                        {
                            Undo.DestroyObjectImmediate(existingObj);
                            Debug.Log($"<color=yellow>[SceneCloner]</color> Replaced existing: {objName}");
                        }

                        // Duplicate and move to current scene
                        GameObject copy = Object.Instantiate(sourceObj);
                        copy.name = objName; // Remove "(Clone)" suffix
                        
                        // Move to current scene
                        EditorSceneManager.MoveGameObjectToScene(copy, currentScene);
                        
                        // Organize in hierarchy
                        OrganizeInHierarchy(copy, objName);
                        
                        Undo.RegisterCreatedObjectUndo(copy, $"Copy {objName}");
                        
                        copiedCount++;
                        copiedNames.Add(objName);
                        Debug.Log($"<color=green>[SceneCloner]</color> Copied: {objName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SceneCloner] Object not found in source: {objName}");
                    }
                }
            }
            finally
            {
                // Close source scene
                EditorSceneManager.CloseScene(sourceScene, true);
            }

            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(currentScene);

            Debug.Log($"<color=green>[SceneCloner]</color> ✅ Copied {copiedCount} objects!");
            
            string summary = string.Join("\n• ", copiedNames);
            EditorUtility.DisplayDialog("Copy Complete", 
                $"Copied {copiedCount} systems:\n\n• {summary}\n\nRemember to save the scene!", "OK");
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the list of object names to copy based on selections.
        /// </summary>
        private List<string> BuildCopyList()
        {
            List<string> list = new List<string>();

            // Core systems
            if (copyGameManager) list.Add("GameManager");
            if (copyWorkerSystem) list.Add("WorkerSystem");
            if (copyStructureSystem) list.Add("StructureSystem");
            if (copyResourceSystem) list.Add("ResourceSystem");
            if (copyBuildModeController) list.Add("BuildModeController");

            // Atmosphere
            if (copyDayNightSystem) list.Add("DayNightSystem");
            if (copyLightingManager) list.Add("LightingManager");
            if (copyGlobalVolume) list.Add("Global Volume");

            // Camera & Lights
            if (copyMainCamera) list.Add("Main Camera");
            if (copyDirectionalLight) list.Add("Directional Light");

            // UI
            if (copyCanvas) list.Add("Canvas");
            if (copyEventSystem) list.Add("EventSystem");

            return list;
        }

        /// <summary>
        /// Finds a GameObject by name in a specific scene.
        /// </summary>
        private GameObject FindObjectInScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                if (rootObj.name == name)
                    return rootObj;

                // Also check children (for nested objects)
                Transform found = FindChildRecursive(rootObj.transform, name);
                if (found != null)
                    return found.gameObject;
            }
            return null;
        }

        /// <summary>
        /// Recursively finds a child by name.
        /// </summary>
        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Organizes copied object in proper hierarchy.
        /// </summary>
        private void OrganizeInHierarchy(GameObject obj, string name)
        {
            // Determine appropriate parent based on object type
            string parentName = GetParentCategoryForObject(name);
            
            if (!string.IsNullOrEmpty(parentName))
            {
                GameObject parent = GameObject.Find(parentName);
                if (parent == null)
                {
                    // Create parent container
                    parent = new GameObject(parentName);
                    Undo.RegisterCreatedObjectUndo(parent, $"Create {parentName}");
                }
                obj.transform.SetParent(parent.transform);
            }
        }

        /// <summary>
        /// Gets the parent category for an object.
        /// </summary>
        private string GetParentCategoryForObject(string objName)
        {
            switch (objName)
            {
                case "GameManager":
                case "WorkerSystem":
                case "StructureSystem":
                case "ResourceSystem":
                case "BuildModeController":
                case "DayNightSystem":
                case "LightingManager":
                    return "--- GAMEPLAY ---";

                case "Canvas":
                case "EventSystem":
                    return null; // UI stays at root or use "--- UI ---"

                default:
                    return null; // Stay at root
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // QUICK ACTIONS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Quick Select")]
        [HorizontalGroup("Quick Select/Row")]
        [Button("✅ Select All", ButtonSizes.Medium)]
        private void SelectAll()
        {
            copyGameManager = true;
            copyWorkerSystem = true;
            copyStructureSystem = true;
            copyResourceSystem = true;
            copyBuildModeController = true;
            copyDayNightSystem = true;
            copyLightingManager = true;
            copyGlobalVolume = true;
            copyMainCamera = true;
            copyDirectionalLight = true;
            copyCanvas = true;
            copyEventSystem = true;
        }

        [HorizontalGroup("Quick Select/Row")]
        [Button("❌ Deselect All", ButtonSizes.Medium)]
        private void DeselectAll()
        {
            copyGameManager = false;
            copyWorkerSystem = false;
            copyStructureSystem = false;
            copyResourceSystem = false;
            copyBuildModeController = false;
            copyDayNightSystem = false;
            copyLightingManager = false;
            copyGlobalVolume = false;
            copyMainCamera = false;
            copyDirectionalLight = false;
            copyCanvas = false;
            copyEventSystem = false;
        }

        [HorizontalGroup("Quick Select/Row")]
        [Button("🎮 Core Only", ButtonSizes.Medium)]
        private void SelectCoreOnly()
        {
            DeselectAll();
            copyGameManager = true;
            copyWorkerSystem = true;
            copyStructureSystem = true;
            copyResourceSystem = true;
            copyBuildModeController = true;
        }

        [HorizontalGroup("Quick Select/Row")]
        [Button("🖼️ UI Only", ButtonSizes.Medium)]
        private void SelectUIOnly()
        {
            DeselectAll();
            copyCanvas = true;
            copyEventSystem = true;
        }

        // ═══════════════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Validation")]
        [Button("🔍 Check Current Scene", ButtonSizes.Medium)]
        private void CheckCurrentScene()
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            
            List<string> allSystems = new List<string>
            {
                "GameManager", "WorkerSystem", "StructureSystem", "ResourceSystem",
                "BuildModeController", "DayNightSystem", "LightingManager", "Global Volume",
                "Main Camera", "Directional Light", "Canvas", "EventSystem"
            };

            string report = $"=== {currentScene.name} System Check ===\n\n";
            int found = 0;
            int missing = 0;

            foreach (string name in allSystems)
            {
                GameObject obj = FindObjectInScene(currentScene, name);
                if (obj != null)
                {
                    report += $"✅ {name}\n";
                    found++;
                }
                else
                {
                    report += $"❌ {name}\n";
                    missing++;
                }
            }

            report += $"\n=== Found: {found} | Missing: {missing} ===";
            Debug.Log(report);

            EditorUtility.DisplayDialog("Scene Check", 
                $"Found: {found} systems\nMissing: {missing} systems\n\nSee console for details.", "OK");
        }

        [Button("📂 Open SampleScene", ButtonSizes.Medium)]
        private void OpenSampleScene()
        {
            if (System.IO.File.Exists(sourceScenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(sourceScenePath);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "SampleScene.unity not found!", "OK");
            }
        }
    }
}
