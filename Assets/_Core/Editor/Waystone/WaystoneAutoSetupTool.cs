/*
 * ============================================================================
 * WAYSTONE AUTO SETUP TOOL
 * ============================================================================
 *
 * HOW TO USE:
 * -----------
 * 1. Open Unity Editor
 * 2. Go to menu: Tools → Wilderness Survival → Waystone → Auto Setup Waystone Beacon
 * 3. The tool will automatically:
 *    - Find and configure WaystoneBeacon.prefab
 *    - Add all required components (StructureController, Light, SphereCollider, WaystoneDebuffAura)
 *    - Create VisualRoot child if missing
 *    - Set layer to Structures (9) on all objects
 *    - Configure associated StructureData (isBaseCenter=true, footprint=2x2)
 *    - Add BaseCenterSystem to scene (on GameManager or new _Systems object)
 *
 * PREREQUISITES:
 * --------------
 * - WaystoneBeacon.prefab must exist somewhere in Assets/
 * - Layer 9 should be named "Structures" (tool will warn if not)
 * - Scripts must compile (StructureController, WaystoneDebuffAura, BaseCenterSystem)
 *
 * IDEMPOTENT:
 * -----------
 * This tool can be run multiple times safely - it won't duplicate components.
 *
 * ============================================================================
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace WildernessSurvival.Editor.Waystone
{
    // ============================================
    // DIAGNOSTICA: Conferma che l'assembly è caricato
    // ============================================
    [InitializeOnLoad]
    public static class WaystoneEditorDiagnostics
    {
        private const string SESSION_KEY = "WaystoneAutoSetupTool_Loaded_v1";
        
        static WaystoneEditorDiagnostics()
        {
            // Log una sola volta per sessione Editor
            if (!SessionState.GetBool(SESSION_KEY, false))
            {
                SessionState.SetBool(SESSION_KEY, true);
                
                var assembly = typeof(WaystoneEditorDiagnostics).Assembly;
                var scriptPath = GetScriptPath();
                
                Debug.Log($"<color=lime>[WaystoneAutoSetup]</color> ✓ Assembly caricato!\n" +
                    $"  Assembly: {assembly.GetName().Name}\n" +
                    $"  Path: {scriptPath}");
            }
        }
        
        private static string GetScriptPath()
        {
            var guids = AssetDatabase.FindAssets($"t:MonoScript WaystoneAutoSetupTool");
            if (guids.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            return "(non trovato)";
        }
        
        // ============================================
        // MENU DIAGNOSTICO
        // ============================================
        
        [MenuItem("Tools/Wilderness Survival/Diagnostics/Print Tool Status", false, 1000)]
        public static void PrintToolStatus()
        {
            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
            Debug.Log("<color=cyan>[WaystoneAutoSetup] DIAGNOSTIC REPORT</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
            
            // 1. Unity Version
            Debug.Log($"<color=white>Unity Version:</color> {Application.unityVersion}");
            
            // 2. Assembly info
            var assembly = typeof(WaystoneEditorDiagnostics).Assembly;
            Debug.Log($"<color=white>Assembly:</color> {assembly.FullName}");
            Debug.Log($"<color=white>Assembly Location:</color> {assembly.Location}");
            
            // 3. Script path
            var guids = AssetDatabase.FindAssets($"t:MonoScript WaystoneAutoSetupTool");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Debug.Log($"<color=white>Script Path:</color> {path}");
                
                // Check if in Editor folder
                bool inEditor = path.Contains("/Editor/") || path.Contains("\\Editor\\");
                Debug.Log($"<color=white>In Editor Folder:</color> {(inEditor ? "<color=green>✓ YES</color>" : "<color=red>✗ NO</color>")}");
            }
            else
            {
                Debug.LogWarning("Script WaystoneAutoSetupTool non trovato!");
            }
            
            // 4. Check for asmdef
            CheckAsmdef();
            
            // 5. List all "Tools/Wilderness Survival" menu items registered
            Debug.Log("<color=white>Menu Items con 'Wilderness Survival':</color>");
            var menuMethods = FindMenuItemMethods();
            foreach (var method in menuMethods)
            {
                Debug.Log($"  • {method}");
            }
            
            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
        }
        
        [MenuItem("Tools/Wilderness Survival/Diagnostics/Force Reimport Editor Scripts", false, 1001)]
        public static void ForceReimportEditorScripts()
        {
            Debug.Log("<color=yellow>[Diagnostics] Forcing reimport of all Editor scripts...</color>");
            
            string[] folders = new[]
            {
                "Assets/_Core/Editor",
                "Assets/Editor",
                "Assets/_UI/Scripts/Editor",
                "Assets/_Gameplay/Core/Editor",
                "Assets/_Gameplay/Workers/Editor"
            };

            int reimported = 0;
            foreach (string folder in folders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { folder });
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        reimported++;
                    }
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>[Diagnostics] ✓ Reimported {reimported} editor scripts. Restart Unity if menus still don't appear.</color>");
        }
        
        private static void CheckAsmdef()
        {
            // Search for asmdef files in relevant directories
            string[] searchPaths = new[]
            {
                "Assets/_Core",
                "Assets/_Gameplay",
                "Assets/_UI",
                "Assets/Editor"
            };
            
            bool foundAny = false;
            foreach (string searchPath in searchPaths)
            {
                if (!AssetDatabase.IsValidFolder(searchPath)) continue;
                
                string[] guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { searchPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Debug.Log($"<color=yellow>⚠ Found .asmdef:</color> {path}");
                    foundAny = true;
                    
                    // Read and check if Editor-only
                    var asmdef = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(path);
                    if (asmdef != null)
                    {
                        string json = asmdef.text;
                        bool isEditorOnly = json.Contains("\"Editor\"") && json.Contains("\"includePlatforms\"");
                        Debug.Log($"  Editor-only: {(isEditorOnly ? "YES" : "NO (potenziale problema!)")}");
                    }
                }
            }
            
            if (!foundAny)
            {
                Debug.Log("<color=green>✓ No custom .asmdef found - using default Assembly-CSharp-Editor</color>");
            }
        }
        
        private static List<string> FindMenuItemMethods()
        {
            var results = new List<string>();
            
            // Search for files containing "Tools/Wilderness Survival"
            string[] guids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Editor/") && !path.Contains("\\Editor\\")) continue;
                
                try
                {
                    string content = System.IO.File.ReadAllText(path);
                    if (content.Contains("Tools/Wilderness Survival"))
                    {
                        // Extract method names with MenuItem
                        var lines = content.Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].Contains("[MenuItem") && lines[i].Contains("Tools/Wilderness Survival"))
                            {
                                // Get next line for method name
                                if (i + 1 < lines.Length)
                                {
                                    string methodLine = lines[i + 1].Trim();
                                    string menuPath = ExtractMenuPath(lines[i]);
                                    results.Add($"{menuPath} → {System.IO.Path.GetFileName(path)}");
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            
            return results;
        }
        
        private static string ExtractMenuPath(string line)
        {
            int start = line.IndexOf("\"");
            if (start >= 0)
            {
                int end = line.IndexOf("\"", start + 1);
                if (end > start)
                {
                    return line.Substring(start + 1, end - start - 1);
                }
            }
            return line;
        }
    }

    public static class WaystoneAutoSetupTool
    {
        // ============================================
        // CONSTANTS
        // ============================================

        private const string PREFAB_NAME = "WaystoneBeacon";
        private const string VISUAL_ROOT_NAME = "VisualRoot";
        private const string AURA_LIGHT_NAME = "AuraLight";
        private const int STRUCTURES_LAYER = 9;
        private const float DEFAULT_AURA_RADIUS = 6f;
        private const float DEFAULT_LIGHT_INTENSITY = 1.5f;

        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness Survival/Waystone/Auto Setup Waystone Beacon", false, 100)]
        public static void AutoSetupWaystoneBeacon()
        {
            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
            Debug.Log("<color=cyan>[WaystoneAutoSetup] Starting Waystone Beacon Auto Setup...</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");

            var report = new SetupReport();

            try
            {
                // Step 1: Validate layer
                ValidateLayer(report);

                // Step 2: Find prefab
                string prefabPath = FindPrefab(PREFAB_NAME, report);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    report.AddError($"Prefab '{PREFAB_NAME}.prefab' not found in project!");
                    PrintReport(report);
                    return;
                }

                // Step 3: Setup prefab
                SetupPrefab(prefabPath, report);

                // Step 4: Setup scene
                SetupScene(report);

                // Step 5: Final save
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                report.success = true;
            }
            catch (System.Exception ex)
            {
                report.AddError($"Exception: {ex.Message}\n{ex.StackTrace}");
            }

            PrintReport(report);
        }

        [MenuItem("Tools/Wilderness Survival/Waystone/Validate Waystone Setup", false, 101)]
        public static void ValidateWaystoneSetup()
        {
            Debug.Log("<color=cyan>[WaystoneValidation] Checking Waystone setup...</color>");

            int issues = 0;

            // Check prefab
            string[] guids = AssetDatabase.FindAssets($"{PREFAB_NAME} t:Prefab");
            if (guids.Length == 0)
            {
                Debug.LogError("[Validation] WaystoneBeacon.prefab NOT FOUND!");
                issues++;
            }
            else
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    // Check components by name to avoid compile errors
                    if (prefab.GetComponent("StructureController") == null)
                    {
                        Debug.LogWarning("[Validation] Prefab missing StructureController");
                        issues++;
                    }

                    if (prefab.GetComponent("WaystoneDebuffAura") == null)
                    {
                        Debug.LogWarning("[Validation] Prefab missing WaystoneDebuffAura");
                        issues++;
                    }

                    if (prefab.GetComponentInChildren<Light>() == null)
                    {
                        Debug.LogWarning("[Validation] Prefab missing Light");
                        issues++;
                    }

                    SphereCollider trigger = null;
                    foreach (var col in prefab.GetComponentsInChildren<SphereCollider>())
                    {
                        if (col.isTrigger) { trigger = col; break; }
                    }
                    if (trigger == null)
                    {
                        Debug.LogWarning("[Validation] Prefab missing SphereCollider (trigger)");
                        issues++;
                    }

                    if (prefab.layer != STRUCTURES_LAYER)
                    {
                        Debug.LogWarning($"[Validation] Prefab root layer is {prefab.layer}, expected {STRUCTURES_LAYER}");
                        issues++;
                    }
                }
            }

            // Check scene for BaseCenterSystem
            var bcs = Object.FindFirstObjectByType<MonoBehaviour>();
            bool foundBCS = false;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb.GetType().Name == "BaseCenterSystem")
                {
                    foundBCS = true;
                    break;
                }
            }
            if (!foundBCS)
            {
                Debug.LogWarning("[Validation] Scene missing BaseCenterSystem");
                issues++;
            }

            if (issues == 0)
            {
                Debug.Log("<color=green>[Validation] All checks passed! ✓</color>");
            }
            else
            {
                Debug.Log($"<color=yellow>[Validation] Found {issues} issue(s). Run Auto Setup to fix.</color>");
            }
        }

        // ============================================
        // VALIDATION
        // ============================================

        private static void ValidateLayer(SetupReport report)
        {
            string layerName = LayerMask.LayerToName(STRUCTURES_LAYER);
            if (string.IsNullOrEmpty(layerName))
            {
                report.AddWarning($"Layer {STRUCTURES_LAYER} is not named. Consider naming it 'Structures'.");
            }
            else if (layerName != "Structures" && layerName != "Structure")
            {
                report.AddWarning($"Layer {STRUCTURES_LAYER} is named '{layerName}', expected 'Structures'.");
            }
            else
            {
                report.AddInfo($"Layer {STRUCTURES_LAYER} = '{layerName}' ✓");
            }
        }

        // ============================================
        // PREFAB FINDING
        // ============================================

        private static string FindPrefab(string prefabName, SetupReport report)
        {
            string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                if (fileName == prefabName)
                {
                    report.AddInfo($"Found prefab: {path}");
                    return path;
                }
            }

            // Try broader search
            guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(prefabName))
                {
                    report.AddInfo($"Found prefab (broad search): {path}");
                    return path;
                }
            }

            return null;
        }

        // ============================================
        // PREFAB SETUP
        // ============================================

        private static void SetupPrefab(string prefabPath, SetupReport report)
        {
            report.AddInfo($"Setting up prefab: {prefabPath}");

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                report.AddError($"Failed to load prefab at {prefabPath}");
                return;
            }

            // Open prefab for editing
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                // 1. Ensure StructureController (by type name)
                EnsureComponentByTypeName(prefabRoot, "StructureController",
                    "WildernessSurvival.Gameplay.Structures.StructureController, Assembly-CSharp", report);

                // 2. Ensure VisualRoot child
                EnsureChildObject(prefabRoot.transform, VISUAL_ROOT_NAME, report);

                // 3. Ensure Light
                Light auraLight = EnsureAuraLight(prefabRoot.transform, report);

                // 4. Ensure SphereCollider (trigger)
                SphereCollider auraTrigger = EnsureAuraTrigger(prefabRoot, auraLight, report);

                // 5. Ensure WaystoneDebuffAura
                EnsureDebuffAura(prefabRoot, auraLight, auraTrigger, report);

                // 6. Set layers recursively
                SetLayerRecursively(prefabRoot, STRUCTURES_LAYER, report);

                // 7. Save prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                report.AddInfo($"Prefab saved: {prefabPath}");

                // 8. Setup associated StructureData
                SetupStructureData(prefabRoot, report);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void EnsureComponentByTypeName(GameObject target, string displayName, string fullTypeName, SetupReport report)
        {
            // Check if already exists
            Component existing = target.GetComponent(displayName);
            if (existing != null)
            {
                report.AddFound($"{displayName} already exists on {target.name}");
                return;
            }

            // Try to add by type
            System.Type type = System.Type.GetType(fullTypeName);
            if (type != null)
            {
                target.AddComponent(type);
                report.AddCreated($"Added {displayName} to {target.name}");
            }
            else
            {
                report.AddWarning($"Could not find type '{displayName}' - add manually or ensure script compiles");
            }
        }

        private static Transform EnsureChildObject(Transform parent, string childName, SetupReport report)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject childObj = new GameObject(childName);
                childObj.transform.SetParent(parent);
                childObj.transform.localPosition = Vector3.zero;
                childObj.transform.localRotation = Quaternion.identity;
                childObj.transform.localScale = Vector3.one;
                child = childObj.transform;
                report.AddCreated($"Created child '{childName}'");
            }
            else
            {
                report.AddFound($"Child '{childName}' already exists");
            }
            return child;
        }

        private static Light EnsureAuraLight(Transform prefabRoot, SetupReport report)
        {
            Light existingLight = prefabRoot.GetComponentInChildren<Light>();
            if (existingLight != null)
            {
                report.AddFound($"Light found: {existingLight.gameObject.name}");
                if (existingLight.range < 1f)
                {
                    existingLight.range = DEFAULT_AURA_RADIUS;
                    report.AddInfo($"Set light range to {DEFAULT_AURA_RADIUS}");
                }
                return existingLight;
            }

            // Create AuraLight child
            GameObject lightObj = new GameObject(AURA_LIGHT_NAME);
            lightObj.transform.SetParent(prefabRoot);
            lightObj.transform.localPosition = new Vector3(0, 1f, 0);
            lightObj.transform.localRotation = Quaternion.identity;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = DEFAULT_AURA_RADIUS;
            light.intensity = DEFAULT_LIGHT_INTENSITY;
            light.color = new Color(0.5f, 0.8f, 1f);

            report.AddCreated($"Created '{AURA_LIGHT_NAME}' with Point light (range={DEFAULT_AURA_RADIUS})");
            return light;
        }

        private static SphereCollider EnsureAuraTrigger(GameObject prefabRoot, Light auraLight, SetupReport report)
        {
            SphereCollider[] colliders = prefabRoot.GetComponentsInChildren<SphereCollider>();
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                {
                    report.AddFound($"SphereCollider (trigger) found: {col.gameObject.name}");
                    if (auraLight != null)
                    {
                        col.radius = auraLight.range;
                        report.AddInfo($"Synced trigger radius to light range: {col.radius}");
                    }
                    return col;
                }
            }

            SphereCollider rootCollider = prefabRoot.GetComponent<SphereCollider>();
            if (rootCollider != null)
            {
                rootCollider.isTrigger = true;
                rootCollider.radius = auraLight != null ? auraLight.range : DEFAULT_AURA_RADIUS;
                report.AddInfo("Converted existing SphereCollider to trigger");
                return rootCollider;
            }

            SphereCollider trigger = prefabRoot.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = auraLight != null ? auraLight.range : DEFAULT_AURA_RADIUS;
            trigger.center = Vector3.zero;

            report.AddCreated($"Created SphereCollider (trigger) on root (radius={trigger.radius})");
            return trigger;
        }

        private static void EnsureDebuffAura(GameObject prefabRoot, Light light, SphereCollider trigger, SetupReport report)
        {
            // Check if already exists by name
            Component existing = prefabRoot.GetComponent("WaystoneDebuffAura");
            if (existing != null)
            {
                report.AddFound("WaystoneDebuffAura already exists");
                ConfigureDebuffAura(existing, light, trigger, report);
                return;
            }

            // Try to add by type
            System.Type auraType = System.Type.GetType("WildernessSurvival.Gameplay.Structures.WaystoneDebuffAura, Assembly-CSharp");
            if (auraType != null)
            {
                Component aura = prefabRoot.AddComponent(auraType);
                report.AddCreated("Added WaystoneDebuffAura component");
                ConfigureDebuffAura(aura, light, trigger, report);
            }
            else
            {
                report.AddWarning("Could not find WaystoneDebuffAura type - add manually or ensure script compiles");
            }
        }

        private static void ConfigureDebuffAura(Component aura, Light light, SphereCollider trigger, SetupReport report)
        {
            SerializedObject so = new SerializedObject(aura);

            SerializedProperty lightProp = so.FindProperty("auraLight");
            if (lightProp != null) lightProp.objectReferenceValue = light;

            SerializedProperty triggerProp = so.FindProperty("auraTrigger");
            if (triggerProp != null) triggerProp.objectReferenceValue = trigger;

            SetSerializedFloat(so, "moveMultiplier", 0.75f);
            SetSerializedFloat(so, "attackMultiplier", 0.85f);
            SetSerializedFloat(so, "tickInterval", 0.25f);

            so.ApplyModifiedProperties();
            report.AddInfo("Configured WaystoneDebuffAura references and defaults");
        }

        private static void SetSerializedFloat(SerializedObject so, string propName, float value)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Float)
            {
                prop.floatValue = value;
            }
        }

        private static void SetLayerRecursively(GameObject obj, int layer, SetupReport report)
        {
            int changedCount = 0;
            SetLayerRecursiveInternal(obj, layer, ref changedCount);
            if (changedCount > 0)
            {
                report.AddInfo($"Set layer {layer} on {changedCount} objects");
            }
        }

        private static void SetLayerRecursiveInternal(GameObject obj, int layer, ref int count)
        {
            if (obj.layer != layer)
            {
                obj.layer = layer;
                count++;
            }
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursiveInternal(child.gameObject, layer, ref count);
            }
        }

        // ============================================
        // STRUCTURE DATA SETUP
        // ============================================

        private static void SetupStructureData(GameObject prefabInstance, SetupReport report)
        {
            Component controller = prefabInstance.GetComponent("StructureController");
            if (controller == null)
            {
                report.AddWarning("StructureController not found - cannot setup StructureData");
                return;
            }

            SerializedObject controllerSO = new SerializedObject(controller);
            SerializedProperty dataProp = controllerSO.FindProperty("structureData");

            if (dataProp == null || dataProp.objectReferenceValue == null)
            {
                report.AddWarning("StructureData not assigned on StructureController. Searching for Waystone StructureData...");

                ScriptableObject waystoneData = FindWaystoneStructureData(report);
                if (waystoneData != null)
                {
                    dataProp.objectReferenceValue = waystoneData;
                    controllerSO.ApplyModifiedProperties();
                    report.AddInfo($"Assigned StructureData: {waystoneData.name}");
                    ConfigureStructureData(waystoneData, report);
                }
                else
                {
                    report.AddWarning("No Waystone StructureData found. Create one and assign manually.");
                }
                return;
            }

            ConfigureStructureData(dataProp.objectReferenceValue as ScriptableObject, report);
        }

        private static ScriptableObject FindWaystoneStructureData(SetupReport report)
        {
            string[] guids = AssetDatabase.FindAssets("t:StructureData");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (data != null)
                {
                    string name = data.name.ToLower();
                    if (name.Contains("waystone") || name.Contains("beacon") || name.Contains("core"))
                    {
                        report.AddInfo($"Found potential Waystone StructureData: {path}");
                        return data;
                    }
                }
            }

            return null;
        }

        private static void ConfigureStructureData(ScriptableObject structureData, SetupReport report)
        {
            if (structureData == null) return;

            SerializedObject so = new SerializedObject(structureData);
            bool modified = false;

            // Set isBaseCenter = true
            SerializedProperty baseCenterProp = so.FindProperty("isBaseCenter");
            if (baseCenterProp != null)
            {
                if (!baseCenterProp.boolValue)
                {
                    baseCenterProp.boolValue = true;
                    modified = true;
                    report.AddInfo("Set StructureData.isBaseCenter = true");
                }
                else
                {
                    report.AddFound("StructureData.isBaseCenter already true");
                }
            }

            // Set isUnique = true
            SerializedProperty uniqueProp = so.FindProperty("isUnique");
            if (uniqueProp != null && !uniqueProp.boolValue)
            {
                uniqueProp.boolValue = true;
                modified = true;
                report.AddInfo("Set StructureData.isUnique = true");
            }

            // Set gridSize = 2x2
            SerializedProperty gridSizeProp = so.FindProperty("gridSize");
            if (gridSizeProp != null)
            {
                Vector2Int currentSize = gridSizeProp.vector2IntValue;
                if (currentSize.x < 2 || currentSize.y < 2)
                {
                    gridSizeProp.vector2IntValue = new Vector2Int(2, 2);
                    modified = true;
                    report.AddInfo("Set StructureData.gridSize = 2x2");
                }
            }

            if (modified)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(structureData);
                report.AddInfo($"StructureData '{structureData.name}' saved");
            }
        }

        // ============================================
        // SCENE SETUP
        // ============================================

        private static void SetupScene(SetupReport report)
        {
            report.AddInfo("Setting up active scene...");

            // Check if BaseCenterSystem already exists
            bool foundBCS = false;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb.GetType().Name == "BaseCenterSystem")
                {
                    report.AddFound($"BaseCenterSystem already exists on '{mb.gameObject.name}'");
                    foundBCS = true;
                    break;
                }
            }

            if (foundBCS) return;

            // Try to find BaseCenterSystem type
            System.Type bcsType = System.Type.GetType("WildernessSurvival.Core.Systems.BaseCenterSystem, Assembly-CSharp");
            if (bcsType == null)
            {
                report.AddWarning("BaseCenterSystem type not found - ensure script compiles and add manually");
                return;
            }

            // Try to find GameManager
            GameObject gameManager = GameObject.Find("GameManager");
            if (gameManager != null)
            {
                if (gameManager.GetComponent(bcsType) == null)
                {
                    gameManager.AddComponent(bcsType);
                    report.AddCreated("Added BaseCenterSystem to GameManager");
                }
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                return;
            }

            // Create _Systems object
            GameObject systemsObj = GameObject.Find("_Systems");
            if (systemsObj == null)
            {
                systemsObj = new GameObject("_Systems");
                report.AddCreated("Created '_Systems' GameObject");
            }

            if (systemsObj.GetComponent(bcsType) == null)
            {
                systemsObj.AddComponent(bcsType);
                report.AddCreated("Added BaseCenterSystem to _Systems");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // ============================================
        // REPORT
        // ============================================

        private class SetupReport
        {
            public bool success = false;
            public List<string> created = new List<string>();
            public List<string> found = new List<string>();
            public List<string> info = new List<string>();
            public List<string> warnings = new List<string>();
            public List<string> errors = new List<string>();

            public void AddCreated(string msg) => created.Add(msg);
            public void AddFound(string msg) => found.Add(msg);
            public void AddInfo(string msg) => info.Add(msg);
            public void AddWarning(string msg) => warnings.Add(msg);
            public void AddError(string msg) => errors.Add(msg);
        }

        private static void PrintReport(SetupReport report)
        {
            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
            Debug.Log("<color=cyan>[WaystoneAutoSetup] SETUP REPORT</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");

            if (report.created.Count > 0)
            {
                Debug.Log("<color=green>✓ CREATED:</color>");
                foreach (var item in report.created)
                    Debug.Log($"  <color=green>+ {item}</color>");
            }

            if (report.found.Count > 0)
            {
                Debug.Log("<color=white>○ FOUND (already exists):</color>");
                foreach (var item in report.found)
                    Debug.Log($"  <color=white>○ {item}</color>");
            }

            if (report.info.Count > 0)
            {
                Debug.Log("<color=gray>ℹ INFO:</color>");
                foreach (var item in report.info)
                    Debug.Log($"  <color=gray>ℹ {item}</color>");
            }

            if (report.warnings.Count > 0)
            {
                Debug.Log("<color=yellow>⚠ WARNINGS:</color>");
                foreach (var item in report.warnings)
                    Debug.LogWarning($"  ⚠ {item}");
            }

            if (report.errors.Count > 0)
            {
                Debug.Log("<color=red>✗ ERRORS:</color>");
                foreach (var item in report.errors)
                    Debug.LogError($"  ✗ {item}");
            }

            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");

            if (report.success)
                Debug.Log("<color=green>✓ SETUP COMPLETED SUCCESSFULLY!</color>");
            else if (report.errors.Count > 0)
                Debug.Log("<color=red>✗ SETUP FAILED - See errors above</color>");
            else
                Debug.Log("<color=yellow>⚠ SETUP COMPLETED WITH WARNINGS</color>");

            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
        }
    }
}
#endif
