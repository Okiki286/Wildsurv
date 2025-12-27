using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace WildernessSurvival.Tools.Editor
{
    /// <summary>
    /// Custom Editor for the ForestGenerator component.
    /// Provides buttons for auto-loading assets, generating, and clearing the forest.
    /// </summary>
    [CustomEditor(typeof(ForestGenerator))]
    public class ForestGeneratorEditor : UnityEditor.Editor
    {
        // Asset paths from the Poly Universal Pack
        private const string TreesPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Nature/Trees";
        private const string RocksPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Nature/Rocks";

        private SerializedProperty radiusProp;
        private SerializedProperty treeDensityProp;
        private SerializedProperty rockDensityProp;
        private SerializedProperty minScaleProp;
        private SerializedProperty maxScaleProp;
        private SerializedProperty treePrefabsProp;
        private SerializedProperty rockPrefabsProp;
        private SerializedProperty groundLayerProp;
        private SerializedProperty raycastHeightProp;
        private SerializedProperty spawnHeightOffsetProp;

        private bool showAdvancedSettings = false;

        private void OnEnable()
        {
            radiusProp = serializedObject.FindProperty("radius");
            treeDensityProp = serializedObject.FindProperty("treeDensity");
            rockDensityProp = serializedObject.FindProperty("rockDensity");
            minScaleProp = serializedObject.FindProperty("minScale");
            maxScaleProp = serializedObject.FindProperty("maxScale");
            treePrefabsProp = serializedObject.FindProperty("treePrefabs");
            rockPrefabsProp = serializedObject.FindProperty("rockPrefabs");
            groundLayerProp = serializedObject.FindProperty("groundLayer");
            raycastHeightProp = serializedObject.FindProperty("raycastHeight");
            spawnHeightOffsetProp = serializedObject.FindProperty("spawnHeightOffset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ForestGenerator generator = (ForestGenerator)target;

            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🌲 Forest Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Auto-Load Button
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("🔄 Auto-Load Assets", GUILayout.Height(30)))
            {
                AutoLoadAssets(generator);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Generation Area
            EditorGUILayout.LabelField("Generation Area", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(radiusProp);

            EditorGUILayout.Space(5);

            // Density Settings
            EditorGUILayout.LabelField("Density Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(treeDensityProp);
            EditorGUILayout.PropertyField(rockDensityProp);

            EditorGUILayout.Space(5);

            // Scale Randomization
            EditorGUILayout.LabelField("Scale Randomization", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(minScaleProp, GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 20));
            EditorGUILayout.PropertyField(maxScaleProp, GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 20));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Ground Layer
            EditorGUILayout.LabelField("Raycast Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(groundLayerProp, new GUIContent("Ground Layer", "Select only the layers that represent ground/terrain."));

            // Advanced Settings Foldout
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Raycast Settings", true);
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(raycastHeightProp);
                EditorGUILayout.PropertyField(spawnHeightOffsetProp);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Prefab Lists
            EditorGUILayout.LabelField("Prefab Lists", EditorStyles.boldLabel);
            
            // Tree count info
            EditorGUILayout.LabelField($"Trees Loaded: {generator.treePrefabs.Count}", EditorStyles.helpBox);
            EditorGUILayout.PropertyField(treePrefabsProp, new GUIContent("Tree Prefabs"), true);

            EditorGUILayout.Space(5);

            // Rock count info
            EditorGUILayout.LabelField($"Rocks Loaded: {generator.rockPrefabs.Count}", EditorStyles.helpBox);
            EditorGUILayout.PropertyField(rockPrefabsProp, new GUIContent("Rock Prefabs"), true);

            EditorGUILayout.Space(15);

            // Action Buttons
            EditorGUILayout.BeginHorizontal();
            
            // Generate Button
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("🌳 Generate Forest", GUILayout.Height(40)))
            {
                generator.Generate();
                EditorUtility.SetDirty(generator);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Clear Button
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑️ CLEAR ALL", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Clear Forest", 
                    $"This will delete all {generator.transform.childCount} child objects. Are you sure?", 
                    "Yes, Clear", "Cancel"))
                {
                    generator.ClearForest();
                    EditorUtility.SetDirty(generator);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // Info box
            EditorGUILayout.HelpBox(
                "Layer Mask Setup:\n" +
                "1. Go to Edit > Project Settings > Tags and Layers\n" +
                "2. Create a 'Ground' layer (e.g., Layer 8)\n" +
                "3. Assign your terrain/ground objects to this layer\n" +
                "4. Set 'Ground Layer' above to only include this layer\n\n" +
                "This prevents trees from spawning on top of other trees!",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Automatically loads tree and rock prefabs from the Poly Universal Pack.
        /// </summary>
        private void AutoLoadAssets(ForestGenerator generator)
        {
            Undo.RecordObject(generator, "Auto-Load Forest Assets");

            int treesLoaded = 0;
            int rocksLoaded = 0;

            // Load Trees
            generator.treePrefabs.Clear();
            string[] treeGuids = AssetDatabase.FindAssets("t:Prefab", new[] { TreesPath });
            foreach (string guid in treeGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Filter: Only include prefabs that start with "Tree_" and contain "Mature" or "Young"
                // This avoids dead trees, stumps, branches, etc.
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (fileName.StartsWith("Tree_") && 
                    (fileName.Contains("Mature") || fileName.Contains("Young")) &&
                    !fileName.Contains("Dead") && 
                    !fileName.Contains("Broken") &&
                    !fileName.Contains("Stump") &&
                    !fileName.Contains("Fallen"))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        generator.treePrefabs.Add(prefab);
                        treesLoaded++;
                    }
                }
            }

            // Load Rocks
            generator.rockPrefabs.Clear();
            string[] rockGuids = AssetDatabase.FindAssets("t:Prefab", new[] { RocksPath });
            foreach (string guid in rockGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                
                // Filter: Include Rock_ and Stone_ prefabs
                if (fileName.StartsWith("Rock_") || fileName.StartsWith("Stone_"))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        generator.rockPrefabs.Add(prefab);
                        rocksLoaded++;
                    }
                }
            }

            EditorUtility.SetDirty(generator);
            Debug.Log($"[ForestGenerator] Auto-loaded {treesLoaded} trees and {rocksLoaded} rocks from Poly Universal Pack.");

            if (treesLoaded == 0 && rocksLoaded == 0)
            {
                EditorUtility.DisplayDialog("No Assets Found",
                    $"Could not find any prefabs at:\n\n" +
                    $"Trees: {TreesPath}\n" +
                    $"Rocks: {RocksPath}\n\n" +
                    "Please verify the Poly Universal Pack is installed correctly.",
                    "OK");
            }
        }
    }
}
