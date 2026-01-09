using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unity Editor Automation Tool for Worker Asset Migration
///
/// Migrates all gameplay components from old Worker_Synty_Base to new Worker_Rogue model
/// while preserving all logic, stats, and configuration values.
/// </summary>
public class WorkerMigrator : EditorWindow
{
    [Header("Prefab References")]
    [SerializeField] private GameObject sourcePrefab;
    [SerializeField] private GameObject targetPrefab;

    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;
    private bool adjustCollider = true;
    private float capsuleHeight = 1.8f;
    private float capsuleRadius = 0.5f;
    private float capsuleCenter = 0.9f;

    private List<string> migrationLog = new List<string>();

    // Components that should NOT be copied (rendering/transform/animator)
    private static readonly System.Type[] EXCLUDED_COMPONENTS = new System.Type[]
    {
        typeof(Transform),
        typeof(Animator),
        typeof(MeshFilter),
        typeof(MeshRenderer),
        typeof(SkinnedMeshRenderer),
        typeof(LODGroup)
    };

    [MenuItem("Tools/Worker Migrator")]
    public static void ShowWindow()
    {
        var window = GetWindow<WorkerMigrator>("Worker Migrator");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }

    private void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space(10);

        DrawPrefabSelection();

        EditorGUILayout.Space(10);

        DrawAdvancedOptions();

        EditorGUILayout.Space(10);

        DrawMigrationButton();

        EditorGUILayout.Space(10);

        DrawLogSection();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Worker Asset Migration Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool migrates all gameplay components from the old Synty worker to the new Rogue worker model.\n\n" +
            "Process:\n" +
            "1. Component Cloning - Copies all logic scripts from source to target\n" +
            "2. Collider Adjustment - Adapts collider to new model proportions\n" +
            "3. Scene Replacement - Updates all instances in the current scene",
            MessageType.Info
        );
    }

    private void DrawPrefabSelection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Prefab Configuration", EditorStyles.boldLabel);

        sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Source Prefab (Synty)",
            sourcePrefab,
            typeof(GameObject),
            false
        );

        targetPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Target Prefab (Rogue)",
            targetPrefab,
            typeof(GameObject),
            false
        );

        // Visual feedback
        if (sourcePrefab != null)
        {
            EditorGUILayout.HelpBox($"Source: {sourcePrefab.name}", MessageType.None);
        }

        if (targetPrefab != null)
        {
            EditorGUILayout.HelpBox($"Target: {targetPrefab.name}", MessageType.None);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAdvancedOptions()
    {
        EditorGUILayout.BeginVertical("box");
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options", true);

        if (showAdvancedOptions)
        {
            EditorGUI.indentLevel++;

            adjustCollider = EditorGUILayout.Toggle("Auto-Adjust Collider", adjustCollider);

            if (adjustCollider)
            {
                EditorGUI.indentLevel++;
                capsuleHeight = EditorGUILayout.FloatField("Capsule Height", capsuleHeight);
                capsuleRadius = EditorGUILayout.FloatField("Capsule Radius", capsuleRadius);
                capsuleCenter = EditorGUILayout.FloatField("Capsule Center Y", capsuleCenter);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawMigrationButton()
    {
        EditorGUI.BeginDisabledGroup(sourcePrefab == null || targetPrefab == null);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Perform Migration", GUILayout.Height(40)))
        {
            PerformMigration();
        }

        EditorGUI.EndDisabledGroup();

        if (sourcePrefab == null || targetPrefab == null)
        {
            EditorGUILayout.HelpBox("Please assign both Source and Target prefabs to proceed.", MessageType.Warning);
        }
    }

    private void DrawLogSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Migration Log", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        foreach (var logEntry in migrationLog)
        {
            EditorGUILayout.LabelField(logEntry, EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Clear Log"))
        {
            migrationLog.Clear();
        }

        EditorGUILayout.EndVertical();
    }

    private void PerformMigration()
    {
        migrationLog.Clear();
        Log("=== Starting Worker Migration ===");
        Log($"Source: {sourcePrefab.name}");
        Log($"Target: {targetPrefab.name}");

        try
        {
            // Step 1: Component Cloning
            Log("\n[STEP 1] Component Cloning...");
            CloneComponents();

            // Step 2: Collider Adjustment
            if (adjustCollider)
            {
                Log("\n[STEP 2] Collider Adjustment...");
                AdjustCollider();
            }
            else
            {
                Log("\n[STEP 2] Collider Adjustment... SKIPPED");
            }

            // Step 3: Scene Replacement
            Log("\n[STEP 3] Scene Replacement...");
            ReplaceSceneInstances();

            Log("\n=== Migration Complete! ===");
            EditorUtility.DisplayDialog("Success", "Worker migration completed successfully!", "OK");
        }
        catch (System.Exception e)
        {
            Log($"\n[ERROR] Migration failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Migration failed:\n{e.Message}", "OK");
        }
    }

    private void CloneComponents()
    {
        // Get the prefab asset path to modify it
        string targetPath = AssetDatabase.GetAssetPath(targetPrefab);

        if (string.IsNullOrEmpty(targetPath))
        {
            throw new System.Exception("Target prefab must be a prefab asset, not a scene instance.");
        }

        // Load prefab for editing
        GameObject targetPrefabInstance = PrefabUtility.LoadPrefabContents(targetPath);

        try
        {
            // Get all components from source
            Component[] sourceComponents = sourcePrefab.GetComponents<Component>();

            int copiedCount = 0;
            int skippedCount = 0;

            foreach (Component sourceComp in sourceComponents)
            {
                if (sourceComp == null) continue;

                System.Type compType = sourceComp.GetType();

                // Skip excluded components
                if (EXCLUDED_COMPONENTS.Contains(compType))
                {
                    Log($"  SKIP: {compType.Name} (excluded type)");
                    skippedCount++;
                    continue;
                }

                // Check if target already has this component
                Component existingComp = targetPrefabInstance.GetComponent(compType);

                if (existingComp != null)
                {
                    Log($"  REPLACE: {compType.Name} (already exists, will overwrite)");
                    DestroyImmediate(existingComp);
                }

                // Copy component using Unity's ComponentUtility
                if (UnityEditorInternal.ComponentUtility.CopyComponent(sourceComp))
                {
                    if (UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targetPrefabInstance))
                    {
                        Log($"  COPY: {compType.Name} ✓");
                        copiedCount++;

                        // Special handling for Animator references
                        if (compType.Name == "WorkerAnimatorController")
                        {
                            UpdateAnimatorReferences(targetPrefabInstance);
                        }
                    }
                    else
                    {
                        Log($"  FAILED: Could not paste {compType.Name}");
                    }
                }
                else
                {
                    Log($"  FAILED: Could not copy {compType.Name}");
                }
            }

            // Save the modified prefab
            PrefabUtility.SaveAsPrefabAsset(targetPrefabInstance, targetPath);

            Log($"\nComponent Cloning Summary:");
            Log($"  ✓ Copied: {copiedCount}");
            Log($"  - Skipped: {skippedCount}");
        }
        finally
        {
            // Always unload the prefab contents
            PrefabUtility.UnloadPrefabContents(targetPrefabInstance);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void UpdateAnimatorReferences(GameObject target)
    {
        // Find the WorkerAnimatorController component (if exists)
        var animController = target.GetComponent<MonoBehaviour>();
        if (animController == null) return;

        var animatorField = animController.GetType().GetField("animator",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (animatorField != null)
        {
            Animator newAnimator = target.GetComponent<Animator>();
            if (newAnimator != null)
            {
                animatorField.SetValue(animController, newAnimator);
                Log($"    → Updated Animator reference in {animController.GetType().Name}");
            }
        }
    }

    private void AdjustCollider()
    {
        string targetPath = AssetDatabase.GetAssetPath(targetPrefab);
        GameObject targetPrefabInstance = PrefabUtility.LoadPrefabContents(targetPath);

        try
        {
            CapsuleCollider capsule = targetPrefabInstance.GetComponent<CapsuleCollider>();

            if (capsule != null)
            {
                capsule.height = capsuleHeight;
                capsule.radius = capsuleRadius;
                capsule.center = new Vector3(0, capsuleCenter, 0);

                Log($"  ✓ Adjusted CapsuleCollider:");
                Log($"    Height: {capsuleHeight}");
                Log($"    Radius: {capsuleRadius}");
                Log($"    Center: (0, {capsuleCenter}, 0)");
            }
            else
            {
                Log("  ! No CapsuleCollider found on target prefab");
            }

            PrefabUtility.SaveAsPrefabAsset(targetPrefabInstance, targetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(targetPrefabInstance);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void ReplaceSceneInstances()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid())
        {
            Log("  ! No active scene found");
            return;
        }

        // Find all instances of the source prefab in the scene
        GameObject[] allObjects = activeScene.GetRootGameObjects();
        List<GameObject> sourceInstances = new List<GameObject>();

        foreach (GameObject root in allObjects)
        {
            // Get all transforms in hierarchy (including the root)
            Transform[] children = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform childTransform in children)
            {
                GameObject obj = childTransform.gameObject;

                if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                {
                    GameObject prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(obj);

                    if (prefabParent == sourcePrefab || obj.name.Contains(sourcePrefab.name))
                    {
                        sourceInstances.Add(obj);
                    }
                }
            }
        }

        if (sourceInstances.Count == 0)
        {
            Log($"  ! No instances of {sourcePrefab.name} found in scene");
            return;
        }

        Log($"  Found {sourceInstances.Count} instances to replace:");

        int replacedCount = 0;

        foreach (GameObject oldWorker in sourceInstances)
        {
            // Store transform data
            Vector3 position = oldWorker.transform.position;
            Quaternion rotation = oldWorker.transform.rotation;
            Vector3 scale = oldWorker.transform.localScale;
            Transform parent = oldWorker.transform.parent;
            int siblingIndex = oldWorker.transform.GetSiblingIndex();
            string oldName = oldWorker.name;

            // Instantiate new worker
            GameObject newWorker = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);

            // Apply transform data
            newWorker.transform.position = position;
            newWorker.transform.rotation = rotation;
            newWorker.transform.localScale = scale;
            newWorker.transform.SetParent(parent);
            newWorker.transform.SetSiblingIndex(siblingIndex);
            newWorker.name = oldName.Replace(sourcePrefab.name, targetPrefab.name);

            // Destroy old worker
            DestroyImmediate(oldWorker);

            replacedCount++;
            Log($"    [{replacedCount}] {oldName} → {newWorker.name} at {position}");
        }

        EditorSceneManager.MarkSceneDirty(activeScene);

        Log($"\n  ✓ Replaced {replacedCount} instances in scene");
        Log($"  ! Remember to save the scene!");
    }

    private void Log(string message)
    {
        migrationLog.Add(message);
        Debug.Log($"[WorkerMigrator] {message}");
        Repaint();
    }
}
