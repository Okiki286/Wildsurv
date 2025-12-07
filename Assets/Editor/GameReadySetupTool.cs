using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using WildernessSurvival.Gameplay.Map;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.Core.Systems;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor tool for instantly configuring a map scene for Play Mode.
    /// Sets up core systems, camera, UI, and gameplay elements.
    /// </summary>
    public class GameReadySetupTool : OdinEditorWindow
    {
        // ═══════════════════════════════════════════════════════════════════
        // WINDOW MENU
        // ═══════════════════════════════════════════════════════════════════

        [MenuItem("Tools/Wilderness/🚀 Game Ready Setup")]
        private static void OpenWindow()
        {
            var window = GetWindow<GameReadySetupTool>();
            window.titleContent = new GUIContent("🚀 Game Ready Setup");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════

        private const string ROOT_GAMEPLAY = "--- GAMEPLAY ---";
        private const string ROOT_UI = "--- UI ---";

        // ═══════════════════════════════════════════════════════════════════
        // PREFAB REFERENCES
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Prefabs")]
        [BoxGroup("Prefabs/Core")]
        [Required("GameManager prefab is required")]
        [AssetsOnly]
        [Tooltip("Prefab containing core game systems")]
        public GameObject gameManagerPrefab;

        [BoxGroup("Prefabs/Core")]
        [Required("UI HUD prefab is required")]
        [AssetsOnly]
        [Tooltip("Main game UI canvas prefab")]
        public GameObject uiHudPrefab;

        [BoxGroup("Prefabs/World")]
        [Required("Camera rig prefab is required")]
        [AssetsOnly]
        [Tooltip("Isometric camera rig prefab")]
        public GameObject cameraRigPrefab;

        [BoxGroup("Prefabs/World")]
        [Required("Physical bonfire prefab is required")]
        [AssetsOnly]
        [Tooltip("The actual bonfire structure prefab (not the marker)")]
        public GameObject physicalBonfirePrefab;

        // ═══════════════════════════════════════════════════════════════════
        // OPTIONS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Options")]
        [BoxGroup("Options/Workers")]
        [Range(1, 10)]
        [Tooltip("Number of workers to spawn on game start")]
        public int startingWorkerCount = 3;

        [BoxGroup("Options/Workers")]
        [Tooltip("Auto-find and assign WorkerData assets")]
        public bool autoFindWorkerData = true;

        [BoxGroup("Options/Camera")]
        [Tooltip("Camera height offset from bonfire")]
        public Vector3 cameraOffset = new Vector3(0f, 15f, -15f);

        [BoxGroup("Options/NavMesh")]
        [Tooltip("Rebuild NavMesh after setup")]
        public bool rebuildNavMesh = true;

        // ═══════════════════════════════════════════════════════════════════
        // STATUS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Scene Status")]
        [ShowInInspector, ReadOnly]
        private bool HasBonfireMarker => FindMarker(MapMarkerType.Bonfire) != null;

        [ShowInInspector, ReadOnly]
        private bool HasPlayerSpawnMarker => FindMarker(MapMarkerType.PlayerSpawn) != null;

        [ShowInInspector, ReadOnly]
        private bool HasGameManager => FindFirstObjectByType<MonoBehaviour>()?.GetType().Name == "GameManager" || 
                                        GameObject.Find("GameManager") != null;

        [ShowInInspector, ReadOnly]
        private bool HasWorkerSystem => FindFirstObjectByType<WorkerSystem>() != null;

        [ShowInInspector, ReadOnly]
        private bool HasMainCamera => Camera.main != null;

        // ═══════════════════════════════════════════════════════════════════
        // MAIN ACTION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Execute")]
        [Button("🚀 MAKE SCENE PLAYABLE", ButtonSizes.Large)]
        [GUIColor(0.3f, 0.9f, 0.4f)]
        private void MakeScenePlayable()
        {
            Debug.Log("<color=cyan>[GameReadySetup]</color> Starting scene setup...");

            // Step 1: Check markers
            MapMarker bonfireMarker = FindMarker(MapMarkerType.Bonfire);
            MapMarker playerSpawnMarker = FindMarker(MapMarkerType.PlayerSpawn);

            if (bonfireMarker == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "No Marker_Bonfire found in scene.\n\nUse Map Architect Tool to create markers first.", "OK");
                return;
            }

            if (playerSpawnMarker == null)
            {
                Debug.LogWarning("[GameReadySetup] No PlayerSpawn marker found. Players will spawn at bonfire.");
            }

            Vector3 bonfirePosition = bonfireMarker.Position;
            Debug.Log($"<color=cyan>[GameReadySetup]</color> Bonfire position: {bonfirePosition}");

            // Step 2: Setup root hierarchy
            GameObject gameplayRoot = FindOrCreateRoot(ROOT_GAMEPLAY);
            GameObject uiRoot = FindOrCreateRoot(ROOT_UI);

            // Step 3: Core Systems
            SetupGameManager(gameplayRoot);
            SetupWorkerSystem(gameplayRoot);
            SetupGameMapManager(gameplayRoot);
            SetupStructureSystem(gameplayRoot);

            // Step 4: Bonfire
            GameObject bonfire = SetupBonfire(bonfirePosition, gameplayRoot);

            // Step 5: Camera
            SetupCamera(bonfirePosition, bonfire);

            // Step 6: UI
            SetupUI(uiRoot);

            // Step 7: NavMesh
            if (rebuildNavMesh)
            {
                BakeNavMesh();
            }

            Debug.Log("<color=green>[GameReadySetup]</color> ✅ Scene setup complete! Ready to play.");
            EditorUtility.DisplayDialog("Setup Complete", 
                "Scene is now playable!\n\n" +
                "✅ Core systems configured\n" +
                "✅ Bonfire placed\n" +
                "✅ Camera positioned\n" +
                "✅ UI ready\n\n" +
                "Press Play to start!", "OK");
        }

        // ═══════════════════════════════════════════════════════════════════
        // SETUP METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Sets up the GameManager system.
        /// </summary>
        private void SetupGameManager(GameObject parent)
        {
            // Check if already exists
            GameObject existing = GameObject.Find("GameManager");
            if (existing != null)
            {
                Debug.Log("<color=yellow>[GameReadySetup]</color> GameManager already exists.");
                return;
            }

            if (gameManagerPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(gameManagerPrefab, parent.transform);
                instance.name = "GameManager";
                Undo.RegisterCreatedObjectUndo(instance, "Create GameManager");
                Debug.Log("<color=green>[GameReadySetup]</color> GameManager instantiated.");
            }
            else
            {
                // Create empty GameManager
                GameObject gm = new GameObject("GameManager");
                gm.transform.SetParent(parent.transform);
                Undo.RegisterCreatedObjectUndo(gm, "Create GameManager");
                Debug.Log("<color=yellow>[GameReadySetup]</color> Created empty GameManager (no prefab assigned).");
            }
        }

        /// <summary>
        /// Sets up and configures the WorkerSystem.
        /// </summary>
        private void SetupWorkerSystem(GameObject parent)
        {
            WorkerSystem workerSystem = FindFirstObjectByType<WorkerSystem>();

            if (workerSystem == null)
            {
                // Create WorkerSystem
                GameObject wsObj = new GameObject("WorkerSystem");
                wsObj.transform.SetParent(parent.transform);
                workerSystem = wsObj.AddComponent<WorkerSystem>();
                Undo.RegisterCreatedObjectUndo(wsObj, "Create WorkerSystem");
                Debug.Log("<color=green>[GameReadySetup]</color> WorkerSystem created.");
            }

            // Auto-configure WorkerSystem
            if (autoFindWorkerData)
            {
                ConfigureWorkerSystem(workerSystem);
            }
        }

        /// <summary>
        /// Finds and assigns WorkerData assets to the WorkerSystem.
        /// </summary>
        private void ConfigureWorkerSystem(WorkerSystem workerSystem)
        {
            // Find all WorkerData assets
            string[] guids = AssetDatabase.FindAssets("t:WorkerData");
            List<WorkerData> workerDatas = new List<WorkerData>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WorkerData data = AssetDatabase.LoadAssetAtPath<WorkerData>(path);
                if (data != null)
                {
                    workerDatas.Add(data);
                }
            }

            if (workerDatas.Count > 0)
            {
                // Use SerializedObject to modify the worker system
                SerializedObject so = new SerializedObject(workerSystem);
                
                // Find the availableWorkerTypes field
                SerializedProperty workerTypesProp = so.FindProperty("availableWorkerTypes");
                if (workerTypesProp != null)
                {
                    workerTypesProp.ClearArray();
                    foreach (var data in workerDatas)
                    {
                        workerTypesProp.InsertArrayElementAtIndex(workerTypesProp.arraySize);
                        workerTypesProp.GetArrayElementAtIndex(workerTypesProp.arraySize - 1).objectReferenceValue = data;
                    }
                }

                // Set starting worker count
                SerializedProperty countProp = so.FindProperty("startingWorkerCount");
                if (countProp != null)
                {
                    countProp.intValue = startingWorkerCount;
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(workerSystem);

                Debug.Log($"<color=green>[GameReadySetup]</color> WorkerSystem configured with {workerDatas.Count} worker types.");
            }
            else
            {
                Debug.LogWarning("[GameReadySetup] No WorkerData assets found in project!");
            }
        }

        /// <summary>
        /// Sets up the GameMapManager.
        /// </summary>
        private void SetupGameMapManager(GameObject parent)
        {
            GameMapManager existing = FindFirstObjectByType<GameMapManager>();
            if (existing != null)
            {
                Debug.Log("<color=yellow>[GameReadySetup]</color> GameMapManager already exists.");
                return;
            }

            GameObject obj = new GameObject("GameMapManager");
            obj.transform.SetParent(parent.transform);
            obj.AddComponent<GameMapManager>();
            Undo.RegisterCreatedObjectUndo(obj, "Create GameMapManager");
            Debug.Log("<color=green>[GameReadySetup]</color> GameMapManager created.");
        }

        /// <summary>
        /// Sets up the StructureSystem if needed.
        /// </summary>
        private void SetupStructureSystem(GameObject parent)
        {
            StructureSystem existing = FindFirstObjectByType<StructureSystem>();
            if (existing != null)
            {
                Debug.Log("<color=yellow>[GameReadySetup]</color> StructureSystem already exists.");
                return;
            }

            GameObject obj = new GameObject("StructureSystem");
            obj.transform.SetParent(parent.transform);
            obj.AddComponent<StructureSystem>();
            Undo.RegisterCreatedObjectUndo(obj, "Create StructureSystem");
            Debug.Log("<color=green>[GameReadySetup]</color> StructureSystem created.");
        }

        /// <summary>
        /// Sets up the physical bonfire structure.
        /// </summary>
        private GameObject SetupBonfire(Vector3 position, GameObject parent)
        {
            // Check if bonfire structure already exists
            StructureController[] structures = FindObjectsByType<StructureController>(FindObjectsSortMode.None);
            foreach (var structure in structures)
            {
                if (structure.Data != null && 
                    structure.Data.StructureId.ToLower().Contains("bonfire"))
                {
                    Debug.Log("<color=yellow>[GameReadySetup]</color> Bonfire structure already exists.");
                    return structure.gameObject;
                }
            }

            if (physicalBonfirePrefab == null)
            {
                Debug.LogWarning("[GameReadySetup] No physical bonfire prefab assigned!");
                return null;
            }

            // Instantiate bonfire
            GameObject bonfire = (GameObject)PrefabUtility.InstantiatePrefab(physicalBonfirePrefab);
            bonfire.transform.position = position;
            bonfire.name = "Bonfire";
            Undo.RegisterCreatedObjectUndo(bonfire, "Create Bonfire");

            Debug.Log($"<color=green>[GameReadySetup]</color> Bonfire placed at {position}");
            return bonfire;
        }

        /// <summary>
        /// Sets up the camera rig.
        /// </summary>
        private void SetupCamera(Vector3 targetPosition, GameObject followTarget)
        {
            // Find and remove old main camera (if not part of a rig)
            Camera oldCamera = Camera.main;
            if (oldCamera != null && oldCamera.transform.parent == null)
            {
                // It's a standalone camera, replace it
                Undo.DestroyObjectImmediate(oldCamera.gameObject);
                Debug.Log("<color=yellow>[GameReadySetup]</color> Removed old Main Camera.");
            }

            if (cameraRigPrefab == null)
            {
                Debug.LogWarning("[GameReadySetup] No camera rig prefab assigned!");
                return;
            }

            // Instantiate camera rig
            GameObject cameraRig = (GameObject)PrefabUtility.InstantiatePrefab(cameraRigPrefab);
            cameraRig.transform.position = targetPosition + cameraOffset;
            cameraRig.name = "CameraRig";
            Undo.RegisterCreatedObjectUndo(cameraRig, "Create Camera Rig");

            // Try to configure follow target
            var cameraController = cameraRig.GetComponentInChildren<IsometricCameraController>();
            if (cameraController != null && followTarget != null)
            {
                // Try to set follow target via SerializedObject
                SerializedObject so = new SerializedObject(cameraController);
                SerializedProperty targetProp = so.FindProperty("followTarget");
                if (targetProp != null)
                {
                    targetProp.objectReferenceValue = followTarget.transform;
                    so.ApplyModifiedProperties();
                }
            }

            Debug.Log($"<color=green>[GameReadySetup]</color> Camera rig positioned at {cameraRig.transform.position}");
        }

        /// <summary>
        /// Sets up the UI systems.
        /// </summary>
        private void SetupUI(GameObject parent)
        {
            // Check if UI already exists
            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            if (existingCanvas != null && existingCanvas.gameObject.name.Contains("HUD"))
            {
                Debug.Log("<color=yellow>[GameReadySetup]</color> UI HUD already exists.");
                return;
            }

            if (uiHudPrefab == null)
            {
                Debug.LogWarning("[GameReadySetup] No UI HUD prefab assigned!");
                return;
            }

            GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiHudPrefab, parent.transform);
            ui.name = "GameHUD";
            Undo.RegisterCreatedObjectUndo(ui, "Create UI HUD");

            Debug.Log("<color=green>[GameReadySetup]</color> UI HUD instantiated.");
        }

        /// <summary>
        /// Bakes the NavMesh.
        /// </summary>
        private void BakeNavMesh()
        {
            #pragma warning disable CS0618
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            #pragma warning restore CS0618
            Debug.Log("<color=cyan>[GameReadySetup]</color> NavMesh baked.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // UTILITY METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Finds a MapMarker of a specific type.
        /// </summary>
        private MapMarker FindMarker(MapMarkerType type)
        {
            MapMarker[] markers = FindObjectsByType<MapMarker>(FindObjectsSortMode.None);
            foreach (var marker in markers)
            {
                if (marker.type == type)
                    return marker;
            }
            return null;
        }

        /// <summary>
        /// Finds or creates a root GameObject.
        /// </summary>
        private GameObject FindOrCreateRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root != null) return root;

            root = new GameObject(name);
            root.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, $"Create {name}");
            return root;
        }

        // ═══════════════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Validation")]
        [Button("🔍 Check Scene Status", ButtonSizes.Medium)]
        private void CheckSceneStatus()
        {
            string status = "=== Scene Status ===\n\n";

            status += HasBonfireMarker ? "✅ Bonfire Marker\n" : "❌ Bonfire Marker (REQUIRED)\n";
            status += HasPlayerSpawnMarker ? "✅ Player Spawn Marker\n" : "⚠️ Player Spawn Marker (optional)\n";
            status += HasGameManager ? "✅ GameManager\n" : "❌ GameManager\n";
            status += HasWorkerSystem ? "✅ WorkerSystem\n" : "❌ WorkerSystem\n";
            status += HasMainCamera ? "✅ Main Camera\n" : "❌ Main Camera\n";

            // Check structures
            var structures = FindObjectsByType<StructureController>(FindObjectsSortMode.None);
            status += $"\n📦 Structures: {structures.Length}";

            // Check zones
            var zones = FindObjectsByType<MapZone>(FindObjectsSortMode.None);
            status += $"\n📍 Map Zones: {zones.Length}";

            Debug.Log(status);
        }

        [Button("🔄 Refresh Status", ButtonSizes.Small)]
        private void RefreshStatus()
        {
            Repaint();
        }
    }
}
