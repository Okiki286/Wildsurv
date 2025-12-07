using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Tool to auto-wire missing references in copied scene systems.
    /// Fixes broken references after using SceneSystemClonerTool.
    /// </summary>
    public class SceneReferenceFixerTool : OdinEditorWindow
    {
        [MenuItem("Tools/Wilderness/🔧 Fix Scene References")]
        private static void OpenWindow()
        {
            var window = GetWindow<SceneReferenceFixerTool>();
            window.titleContent = new GUIContent("🔧 Reference Fixer");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        // ═══════════════════════════════════════════════════════════════════
        // MAIN ACTION
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Auto-Fix")]
        [InfoBox("This tool fixes missing references in BuildModeController and other systems after copying from SampleScene.")]
        [Button("🔧 FIX ALL REFERENCES", ButtonSizes.Large)]
        [GUIColor(0.3f, 0.9f, 0.4f)]
        private void FixAllReferences()
        {
            int fixed_count = 0;

            fixed_count += FixBuildModeController();
            fixed_count += FixStructureSystem();
            fixed_count += FixIsometricCamera();

            Debug.Log($"<color=green>[ReferenceFixer]</color> ✅ Fixed {fixed_count} references!");
            EditorUtility.DisplayDialog("References Fixed", 
                $"Fixed {fixed_count} missing references.\n\nRemember to save the scene!", "OK");
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD MODE CONTROLLER
        // ═══════════════════════════════════════════════════════════════════

        private int FixBuildModeController()
        {
            int fixed_count = 0;
            
            BuildModeController bmc = FindFirstObjectByType<BuildModeController>();
            if (bmc == null)
            {
                Debug.LogWarning("[ReferenceFixer] BuildModeController not found!");
                return 0;
            }

            SerializedObject so = new SerializedObject(bmc);

            // Fix Main Camera
            SerializedProperty cameraProp = so.FindProperty("mainCamera");
            if (cameraProp != null && cameraProp.objectReferenceValue == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraProp.objectReferenceValue = mainCam;
                    fixed_count++;
                    Debug.Log("<color=cyan>[ReferenceFixer]</color> Fixed: BuildModeController.mainCamera");
                }
            }

            // Fix Ground Layer
            SerializedProperty groundLayerProp = so.FindProperty("groundLayer");
            if (groundLayerProp != null)
            {
                int groundLayerMask = LayerMask.GetMask("Ground");
                if (groundLayerMask == 0)
                {
                    // Fallback to Default layer
                    groundLayerMask = LayerMask.GetMask("Default");
                }
                if (groundLayerProp.intValue == 0)
                {
                    groundLayerProp.intValue = groundLayerMask;
                    fixed_count++;
                    Debug.Log("<color=cyan>[ReferenceFixer]</color> Fixed: BuildModeController.groundLayer");
                }
            }

            // Fix Valid Placement Material
            SerializedProperty validMatProp = so.FindProperty("validPlacementMaterial");
            if (validMatProp != null && validMatProp.objectReferenceValue == null)
            {
                Material validMat = FindOrCreatePlacementMaterial("ValidPlacement", new Color(0.2f, 0.9f, 0.2f, 0.5f));
                if (validMat != null)
                {
                    validMatProp.objectReferenceValue = validMat;
                    fixed_count++;
                    Debug.Log("<color=cyan>[ReferenceFixer]</color> Fixed: BuildModeController.validPlacementMaterial");
                }
            }

            // Fix Invalid Placement Material
            SerializedProperty invalidMatProp = so.FindProperty("invalidPlacementMaterial");
            if (invalidMatProp != null && invalidMatProp.objectReferenceValue == null)
            {
                Material invalidMat = FindOrCreatePlacementMaterial("InvalidPlacement", new Color(0.9f, 0.2f, 0.2f, 0.5f));
                if (invalidMat != null)
                {
                    invalidMatProp.objectReferenceValue = invalidMat;
                    fixed_count++;
                    Debug.Log("<color=cyan>[ReferenceFixer]</color> Fixed: BuildModeController.invalidPlacementMaterial");
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bmc);

            return fixed_count;
        }

        // ═══════════════════════════════════════════════════════════════════
        // STRUCTURE SYSTEM
        // ═══════════════════════════════════════════════════════════════════

        private int FixStructureSystem()
        {
            int fixed_count = 0;

            StructureSystem ss = FindFirstObjectByType<StructureSystem>();
            if (ss == null)
            {
                Debug.LogWarning("[ReferenceFixer] StructureSystem not found!");
                return 0;
            }

            SerializedObject so = new SerializedObject(ss);

            // Fix Ground Layer
            SerializedProperty groundLayerProp = so.FindProperty("groundLayer");
            if (groundLayerProp != null && groundLayerProp.intValue == 0)
            {
                int groundLayerMask = LayerMask.GetMask("Ground");
                if (groundLayerMask == 0)
                {
                    groundLayerMask = LayerMask.GetMask("Default");
                }
                groundLayerProp.intValue = groundLayerMask;
                fixed_count++;
                Debug.Log("<color=cyan>[ReferenceFixer]</color> Fixed: StructureSystem.groundLayer");
            }

            // Auto-populate availableStructures if empty
            SerializedProperty structuresProp = so.FindProperty("availableStructures");
            if (structuresProp != null && structuresProp.arraySize == 0)
            {
                string[] guids = AssetDatabase.FindAssets("t:StructureData");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    StructureData data = AssetDatabase.LoadAssetAtPath<StructureData>(path);
                    if (data != null)
                    {
                        structuresProp.InsertArrayElementAtIndex(structuresProp.arraySize);
                        structuresProp.GetArrayElementAtIndex(structuresProp.arraySize - 1).objectReferenceValue = data;
                        fixed_count++;
                    }
                }
                if (structuresProp.arraySize > 0)
                {
                    Debug.Log($"<color=cyan>[ReferenceFixer]</color> Fixed: StructureSystem.availableStructures ({structuresProp.arraySize} items)");
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ss);

            return fixed_count;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ISOMETRIC CAMERA
        // ═══════════════════════════════════════════════════════════════════

        private int FixIsometricCamera()
        {
            int fixed_count = 0;

            var camera = FindFirstObjectByType<WildernessSurvival.Core.Systems.IsometricCameraController>();
            if (camera == null)
            {
                // Try to add it to Main Camera
                Camera mainCam = Camera.main;
                if (mainCam != null && mainCam.GetComponent<WildernessSurvival.Core.Systems.IsometricCameraController>() == null)
                {
                    mainCam.gameObject.AddComponent<WildernessSurvival.Core.Systems.IsometricCameraController>();
                    fixed_count++;
                    Debug.Log("<color=cyan>[ReferenceFixer]</color> Added: IsometricCameraController to Main Camera");
                }
            }

            return fixed_count;
        }

        // ═══════════════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════════════

        private Material FindOrCreatePlacementMaterial(string name, Color color)
        {
            // Try to find existing material
            string[] guids = AssetDatabase.FindAssets($"{name} t:Material");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }

            // Create new material
            string matPath = $"Assets/_Core/Materials/{name}.mat";
            
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/_Core/Materials"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Core"))
                {
                    AssetDatabase.CreateFolder("Assets", "_Core");
                }
                AssetDatabase.CreateFolder("Assets/_Core", "Materials");
            }

            // Create transparent material
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            
            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            
            // Make transparent
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"<color=green>[ReferenceFixer]</color> Created material: {matPath}");
            return mat;
        }

        // ═══════════════════════════════════════════════════════════════════
        // DIAGNOSTICS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Diagnostics")]
        [Button("🔍 Check Missing References", ButtonSizes.Medium)]
        private void CheckMissingReferences()
        {
            string report = "=== Missing References Check ===\n\n";
            int issues = 0;

            // BuildModeController
            BuildModeController bmc = FindFirstObjectByType<BuildModeController>();
            if (bmc != null)
            {
                SerializedObject so = new SerializedObject(bmc);
                
                if (so.FindProperty("mainCamera")?.objectReferenceValue == null)
                {
                    report += "❌ BuildModeController.mainCamera\n";
                    issues++;
                }
                if (so.FindProperty("validPlacementMaterial")?.objectReferenceValue == null)
                {
                    report += "❌ BuildModeController.validPlacementMaterial\n";
                    issues++;
                }
                if (so.FindProperty("invalidPlacementMaterial")?.objectReferenceValue == null)
                {
                    report += "❌ BuildModeController.invalidPlacementMaterial\n";
                    issues++;
                }
                if (so.FindProperty("groundLayer")?.intValue == 0)
                {
                    report += "⚠️ BuildModeController.groundLayer (is 0)\n";
                    issues++;
                }
            }
            else
            {
                report += "❌ BuildModeController not found!\n";
                issues++;
            }

            // StructureSystem
            StructureSystem ss = FindFirstObjectByType<StructureSystem>();
            if (ss != null)
            {
                SerializedObject so = new SerializedObject(ss);
                if (so.FindProperty("groundLayer")?.intValue == 0)
                {
                    report += "⚠️ StructureSystem.groundLayer (is 0)\n";
                    issues++;
                }
            }
            else
            {
                report += "❌ StructureSystem not found!\n";
                issues++;
            }

            // Camera
            if (Camera.main == null)
            {
                report += "❌ No Main Camera found!\n";
                issues++;
            }

            report += $"\n=== {issues} issue(s) found ===";
            Debug.Log(report);

            if (issues > 0)
            {
                EditorUtility.DisplayDialog("Issues Found", 
                    $"Found {issues} missing reference(s).\n\nClick 'FIX ALL REFERENCES' to auto-fix.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("All Good!", "No missing references found.", "OK");
            }
        }
    }
}
