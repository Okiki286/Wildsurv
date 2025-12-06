using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using WildernessSurvival.Gameplay.Map;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor tool for setting up handcrafted RTS scenes with proper hierarchy,
    /// gameplay systems, and default map elements.
    /// </summary>
    public class MapArchitectTool : OdinEditorWindow
    {
        // ═══════════════════════════════════════════════════════════════════
        // WINDOW MENU
        // ═══════════════════════════════════════════════════════════════════

        [MenuItem("Tools/Wilderness/🗺️ Map Architect")]
        private static void OpenWindow()
        {
            var window = GetWindow<MapArchitectTool>();
            window.titleContent = new GUIContent("🗺️ Map Architect");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════

        private const string ROOT_GAMEPLAY = "--- GAMEPLAY ---";
        private const string ROOT_WORLD = "--- WORLD ---";

        // ═══════════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Scene Setup")]
        [BoxGroup("Scene Setup/Ground Settings")]
        [Tooltip("Scale of the default ground plane")]
        public Vector3 groundScale = new Vector3(10f, 1f, 10f);

        [BoxGroup("Scene Setup/Ground Settings")]
        [Tooltip("Material for the ground (optional)")]
        public Material groundMaterial;

        [BoxGroup("Scene Setup/Marker Defaults")]
        [Tooltip("Offset for PlayerSpawn from Bonfire")]
        public Vector3 playerSpawnOffset = new Vector3(3f, 0f, 0f);

        [BoxGroup("Scene Setup/Zone Defaults")]
        [Tooltip("Default size for example zones")]
        public Vector3 defaultZoneSize = new Vector3(10f, 5f, 10f);

        // ═══════════════════════════════════════════════════════════════════
        // MAIN SETUP BUTTON
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Actions")]
        [Button("🏗️ Setup Handcrafted Scene", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.9f, 0.5f)]
        private void SetupHandcraftedScene()
        {
            Debug.Log("<color=cyan>[MapArchitect]</color> Starting handcrafted scene setup...");

            // 1. Create root hierarchy
            GameObject gameplayRoot = FindOrCreateRoot(ROOT_GAMEPLAY);
            GameObject worldRoot = FindOrCreateRoot(ROOT_WORLD);

            // 2. Setup Gameplay systems
            SetupGameplaySystems(gameplayRoot);

            // 3. Setup World containers
            SetupWorldContainers(worldRoot);

            // 4. Create default content
            CreateDefaultContent(worldRoot);

            // 5. Select the world root for convenience
            Selection.activeGameObject = worldRoot;

            Debug.Log("<color=green>[MapArchitect]</color> ✅ Handcrafted scene setup complete!");
            EditorUtility.DisplayDialog("Setup Complete", 
                "Handcrafted scene hierarchy created!\n\n" +
                "• Gameplay systems added\n" +
                "• World containers created\n" +
                "• Default markers and zones placed\n\n" +
                "You can now place additional markers and zones.", 
                "OK");
        }

        // ═══════════════════════════════════════════════════════════════════
        // ROOT HIERARCHY
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Finds or creates a root GameObject in the scene.
        /// </summary>
        private GameObject FindOrCreateRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            
            if (root != null)
            {
                Debug.Log($"<color=yellow>[MapArchitect]</color> Root '{name}' already exists.");
                return root;
            }

            root = new GameObject(name);
            root.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, $"Create {name}");
            Debug.Log($"<color=green>[MapArchitect]</color> Created root: {name}");
            
            return root;
        }

        // ═══════════════════════════════════════════════════════════════════
        // GAMEPLAY SYSTEMS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Sets up all gameplay system GameObjects under the GAMEPLAY root.
        /// </summary>
        private void SetupGameplaySystems(GameObject gameplayRoot)
        {
            // GameManager
            FindOrCreateSystem<MonoBehaviour>(gameplayRoot, "GameManager", null);

            // WorkerSystem
            FindOrCreateSystem<WorkerSystem>(gameplayRoot, "WorkerSystem", typeof(WorkerSystem));

            // GameMapManager (CRUCIAL)
            FindOrCreateSystem<GameMapManager>(gameplayRoot, "GameMapManager", typeof(GameMapManager));

            Debug.Log("<color=green>[MapArchitect]</color> Gameplay systems configured.");
        }

        /// <summary>
        /// Finds or creates a system GameObject with a specific component.
        /// </summary>
        private T FindOrCreateSystem<T>(GameObject parent, string name, System.Type componentType) where T : MonoBehaviour
        {
            // Check if already exists as child
            Transform existing = parent.transform.Find(name);
            if (existing != null)
            {
                Debug.Log($"<color=yellow>[MapArchitect]</color> System '{name}' already exists.");
                T existingComponent = existing.GetComponent<T>();
                if (existingComponent == null && componentType != null)
                {
                    existingComponent = existing.gameObject.AddComponent(componentType) as T;
                }
                return existingComponent;
            }

            // Also check for existing in scene root
            GameObject existingInScene = GameObject.Find(name);
            if (existingInScene != null)
            {
                // Move it under parent
                existingInScene.transform.SetParent(parent.transform);
                Debug.Log($"<color=yellow>[MapArchitect]</color> Moved existing '{name}' under {parent.name}.");
                return existingInScene.GetComponent<T>();
            }

            // Create new
            GameObject systemObj = new GameObject(name);
            systemObj.transform.SetParent(parent.transform);
            systemObj.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(systemObj, $"Create {name}");

            T component = null;
            if (componentType != null)
            {
                component = systemObj.AddComponent(componentType) as T;
            }

            Debug.Log($"<color=green>[MapArchitect]</color> Created system: {name}");
            return component;
        }

        // ═══════════════════════════════════════════════════════════════════
        // WORLD CONTAINERS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Sets up world container GameObjects under the WORLD root.
        /// </summary>
        private void SetupWorldContainers(GameObject worldRoot)
        {
            // Create containers
            FindOrCreateContainer(worldRoot, "Markers");
            FindOrCreateContainer(worldRoot, "Zones");
            FindOrCreateContainer(worldRoot, "Environment");
            Transform groundContainer = FindOrCreateContainer(worldRoot, "Ground");

            // Create default ground plane if none exists
            if (groundContainer.childCount == 0)
            {
                CreateGroundPlane(groundContainer);
            }
            else
            {
                Debug.Log("<color=yellow>[MapArchitect]</color> Ground already has children, skipping plane creation.");
            }

            Debug.Log("<color=green>[MapArchitect]</color> World containers configured.");
        }

        /// <summary>
        /// Finds or creates a container GameObject.
        /// </summary>
        private Transform FindOrCreateContainer(GameObject parent, string name)
        {
            Transform existing = parent.transform.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject container = new GameObject(name);
            container.transform.SetParent(parent.transform);
            container.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(container, $"Create {name} container");
            
            return container.transform;
        }

        /// <summary>
        /// Creates a default ground plane.
        /// </summary>
        private void CreateGroundPlane(Transform parent)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground_Plane";
            plane.transform.SetParent(parent);
            plane.transform.localPosition = Vector3.zero;
            plane.transform.localScale = groundScale;

            // Apply material if assigned
            if (groundMaterial != null)
            {
                plane.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
            }

            // Set as NavigationStatic
            GameObjectUtility.SetStaticEditorFlags(plane, StaticEditorFlags.BatchingStatic);

            Undo.RegisterCreatedObjectUndo(plane, "Create Ground Plane");
            Debug.Log("<color=green>[MapArchitect]</color> Created ground plane.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // DEFAULT CONTENT
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates default markers and zones as examples.
        /// </summary>
        private void CreateDefaultContent(GameObject worldRoot)
        {
            Transform markersContainer = worldRoot.transform.Find("Markers");
            Transform zonesContainer = worldRoot.transform.Find("Zones");

            if (markersContainer == null || zonesContainer == null)
            {
                Debug.LogError("[MapArchitect] Markers or Zones container not found!");
                return;
            }

            // Create Bonfire marker at origin
            CreateMarker(markersContainer, "Marker_Bonfire", MapMarkerType.Bonfire, Vector3.zero);

            // Create PlayerSpawn near bonfire
            CreateMarker(markersContainer, "Marker_PlayerSpawn", MapMarkerType.PlayerSpawn, playerSpawnOffset);

            // Create example resource zone
            CreateZone(zonesContainer, "Zone_Forest_Example", MapZoneType.Resource_Wood, 
                new Vector3(15f, 0f, 0f), defaultZoneSize);

            Debug.Log("<color=green>[MapArchitect]</color> Default content created.");
        }

        /// <summary>
        /// Creates a MapMarker if it doesn't exist.
        /// </summary>
        private void CreateMarker(Transform parent, string name, MapMarkerType type, Vector3 position)
        {
            // Check if already exists
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Debug.Log($"<color=yellow>[MapArchitect]</color> Marker '{name}' already exists.");
                return;
            }

            GameObject markerObj = new GameObject(name);
            markerObj.transform.SetParent(parent);
            markerObj.transform.position = position;

            MapMarker marker = markerObj.AddComponent<MapMarker>();
            marker.type = type;
            marker.radius = 2f;

            Undo.RegisterCreatedObjectUndo(markerObj, $"Create Marker {name}");
            Debug.Log($"<color=green>[MapArchitect]</color> Created marker: {name} ({type})");
        }

        /// <summary>
        /// Creates a MapZone if it doesn't exist.
        /// </summary>
        private void CreateZone(Transform parent, string name, MapZoneType type, Vector3 position, Vector3 size)
        {
            // Check if already exists
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Debug.Log($"<color=yellow>[MapArchitect]</color> Zone '{name}' already exists.");
                return;
            }

            GameObject zoneObj = new GameObject(name);
            zoneObj.transform.SetParent(parent);
            zoneObj.transform.position = position;

            // Add BoxCollider as trigger
            BoxCollider collider = zoneObj.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = true;

            // Add MapZone component
            MapZone zone = zoneObj.AddComponent<MapZone>();
            zone.type = type;

            Undo.RegisterCreatedObjectUndo(zoneObj, $"Create Zone {name}");
            Debug.Log($"<color=green>[MapArchitect]</color> Created zone: {name} ({type})");
        }

        // ═══════════════════════════════════════════════════════════════════
        // UTILITY BUTTONS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Quick Add")]
        [HorizontalGroup("Quick Add/Markers")]
        [Button("➕ Add Marker", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void AddNewMarker()
        {
            GameObject worldRoot = GameObject.Find(ROOT_WORLD);
            if (worldRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Run 'Setup Handcrafted Scene' first.", "OK");
                return;
            }

            Transform markersContainer = worldRoot.transform.Find("Markers");
            if (markersContainer == null) return;

            int count = markersContainer.childCount + 1;
            CreateMarker(markersContainer, $"Marker_New_{count}", MapMarkerType.PlayerSpawn, 
                SceneView.lastActiveSceneView.pivot);
        }

        [HorizontalGroup("Quick Add/Markers")]
        [Button("➕ Add Zone", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.8f)]
        private void AddNewZone()
        {
            GameObject worldRoot = GameObject.Find(ROOT_WORLD);
            if (worldRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Run 'Setup Handcrafted Scene' first.", "OK");
                return;
            }

            Transform zonesContainer = worldRoot.transform.Find("Zones");
            if (zonesContainer == null) return;

            int count = zonesContainer.childCount + 1;
            CreateZone(zonesContainer, $"Zone_New_{count}", MapZoneType.BuildAllowed, 
                SceneView.lastActiveSceneView.pivot, defaultZoneSize);
        }

        [TitleGroup("Validation")]
        [Button("✅ Validate Scene Setup", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 0.5f)]
        private void ValidateSceneSetup()
        {
            int issues = 0;
            string report = "=== Scene Validation Report ===\n\n";

            // Check roots
            if (GameObject.Find(ROOT_GAMEPLAY) == null)
            {
                report += "❌ Missing: " + ROOT_GAMEPLAY + "\n";
                issues++;
            }
            else
            {
                report += "✅ Found: " + ROOT_GAMEPLAY + "\n";
            }

            if (GameObject.Find(ROOT_WORLD) == null)
            {
                report += "❌ Missing: " + ROOT_WORLD + "\n";
                issues++;
            }
            else
            {
                report += "✅ Found: " + ROOT_WORLD + "\n";
            }

            // Check GameMapManager
            var mapManager = FindFirstObjectByType<GameMapManager>();
            if (mapManager == null)
            {
                report += "❌ Missing: GameMapManager component\n";
                issues++;
            }
            else
            {
                report += "✅ Found: GameMapManager\n";
            }

            // Check markers
            var markers = FindObjectsByType<MapMarker>(FindObjectsSortMode.None);
            report += $"\n📍 Markers found: {markers.Length}\n";

            // Check zones
            var zones = FindObjectsByType<MapZone>(FindObjectsSortMode.None);
            report += $"📦 Zones found: {zones.Length}\n";

            // Check for bonfire
            bool hasBonfire = false;
            foreach (var marker in markers)
            {
                if (marker.type == MapMarkerType.Bonfire)
                {
                    hasBonfire = true;
                    break;
                }
            }

            if (!hasBonfire)
            {
                report += "⚠️ Warning: No Bonfire marker found\n";
            }

            report += $"\n=== {(issues == 0 ? "All checks passed!" : $"{issues} issue(s) found")} ===";

            Debug.Log(report);
            EditorUtility.DisplayDialog("Validation Result", 
                issues == 0 ? "Scene setup is valid!" : $"Found {issues} issue(s). Check console for details.", 
                "OK");
        }

        [TitleGroup("Cleanup")]
        [Button("🗑️ Clear All Markers & Zones", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.6f)]
        private void ClearMarkersAndZones()
        {
            if (!EditorUtility.DisplayDialog("Confirm Clear", 
                "This will delete ALL MapMarkers and MapZones in the scene.\n\nAre you sure?", 
                "Clear", "Cancel"))
            {
                return;
            }

            var markers = FindObjectsByType<MapMarker>(FindObjectsSortMode.None);
            var zones = FindObjectsByType<MapZone>(FindObjectsSortMode.None);

            int count = 0;
            foreach (var marker in markers)
            {
                Undo.DestroyObjectImmediate(marker.gameObject);
                count++;
            }
            foreach (var zone in zones)
            {
                Undo.DestroyObjectImmediate(zone.gameObject);
                count++;
            }

            Debug.Log($"<color=orange>[MapArchitect]</color> Cleared {count} markers/zones.");
        }
    }
}
