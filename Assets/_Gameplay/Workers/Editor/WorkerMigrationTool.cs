using UnityEngine;
using UnityEditor;
using UnityEditor.AI;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.AI;
using System.Collections.Generic;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Tool per la migrazione dei Worker da Rigidbody a NavMeshAgent
    /// e per il setup automatico dell'environment per NavMesh.
    /// </summary>
    public class WorkerMigrationTool : OdinEditorWindow
    {
        [MenuItem("Tools/Wilderness Survival/Worker Migration Tool")]
        private static void OpenWindow()
        {
            GetWindow<WorkerMigrationTool>("Worker & NavMesh Setup").Show();
        }

        // ============================================
        // 1. WORKER SETUP
        // ============================================

        [TitleGroup("Worker Setup")]
        [InfoBox("Migra i prefab Worker da Rigidbody a NavMeshAgent per mobile performance.", InfoMessageType.Info)]
        [BoxGroup("Worker Setup/Migration")]
        [FolderPath(RequireExistingPath = true)]
        [LabelText("Worker Prefabs Folder")]
        [Tooltip("Path della cartella contenente i prefab dei worker")]
        public string workerPrefabsPath = "Assets/_Gameplay/Workers/Prefabs";

        [BoxGroup("Worker Setup/Migration")]
        [Button("Migrate Worker Prefabs", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void MigrateWorkerPrefabs()
        {
            if (string.IsNullOrEmpty(workerPrefabsPath))
            {
                Debug.LogError("[WorkerMigrationTool] Worker Prefabs Path is empty!");
                return;
            }

            // Find all prefabs in the specified folder
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { workerPrefabsPath });
            
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[WorkerMigrationTool] No prefabs found in {workerPrefabsPath}");
                return;
            }

            int migratedCount = 0;
            int skippedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                // Check if it has WorkerController
                var workerController = prefab.GetComponent<Gameplay.Workers.WorkerController>();
                if (workerController == null)
                {
                    skippedCount++;
                    continue;
                }

                // Check if already has NavMeshAgent
                NavMeshAgent existingAgent = prefab.GetComponent<NavMeshAgent>();
                if (existingAgent != null)
                {
                    Debug.Log($"[WorkerMigrationTool] {prefab.name} already has NavMeshAgent, skipping.");
                    skippedCount++;
                    continue;
                }

                // Instantiate to modify
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

                // Remove Rigidbody if present
                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    DestroyImmediate(rb);
                    Debug.Log($"<color=yellow>[WorkerMigrationTool]</color> Removed Rigidbody from {instance.name}");
                }

                // Add NavMeshAgent
                NavMeshAgent agent = instance.AddComponent<NavMeshAgent>();
                
                // Configure NavMeshAgent for mobile
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.autoBraking = true;
                agent.speed = 3.5f;
                agent.acceleration = 8f;
                agent.angularSpeed = 120f;

                Debug.Log($"<color=green>[WorkerMigrationTool]</color> Added NavMeshAgent to {instance.name}");

                // Save changes back to prefab
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                DestroyImmediate(instance);

                migratedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=cyan>[WorkerMigrationTool]</color> Migration complete! Migrated: {migratedCount}, Skipped: {skippedCount}");
        }

        [BoxGroup("Worker Setup/Migration")]
        [Button("Verify Worker Prefabs", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 0.4f)]
        private void VerifyWorkerPrefabs()
        {
            if (string.IsNullOrEmpty(workerPrefabsPath))
            {
                Debug.LogError("[WorkerMigrationTool] Worker Prefabs Path is empty!");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { workerPrefabsPath });
            
            int withNavMesh = 0;
            int withRigidbody = 0;
            int total = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                var workerController = prefab.GetComponent<Gameplay.Workers.WorkerController>();
                if (workerController == null) continue;

                total++;

                if (prefab.GetComponent<NavMeshAgent>() != null) withNavMesh++;
                if (prefab.GetComponent<Rigidbody>() != null) withRigidbody++;
            }

            Debug.Log($"<color=cyan>[WorkerMigrationTool]</color> Verification Results:\n" +
                $"Total Worker Prefabs: {total}\n" +
                $"With NavMeshAgent: {withNavMesh}\n" +
                $"With Rigidbody: {withRigidbody}");
        }

        // ============================================
        // 1.5. WORKER PREFAB CREATOR
        // ============================================

        [TitleGroup("Worker Prefab Creator")]
        [InfoBox("Genera un prefab Worker mobile-optimized da zero con gerarchia corretta e componenti configurati.", InfoMessageType.Info)]
        
        [BoxGroup("Worker Prefab Creator/Settings")]
        [LabelText("Prefab Name")]
        [Tooltip("Nome del prefab da creare")]
        public string newWorkerPrefabName = "Worker_Base_Mobile";

        [BoxGroup("Worker Prefab Creator/Settings")]
        [FolderPath(RequireExistingPath = false)]
        [LabelText("Output Folder")]
        [Tooltip("Cartella in cui salvare il prefab")]
        public string prefabOutputPath = "Assets/_Gameplay/Workers/Prefabs";

        [BoxGroup("Worker Prefab Creator/Actions")]
        [Button("Create Mobile Worker Prefab", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.8f)]
        private void CreateMobileWorkerPrefab()
        {
            // Step 1: Verifica Percorso
            if (!System.IO.Directory.Exists(prefabOutputPath))
            {
                System.IO.Directory.CreateDirectory(prefabOutputPath);
                AssetDatabase.Refresh();
                Debug.Log($"<color=yellow>[WorkerMigrationTool]</color> Created directory: {prefabOutputPath}");
            }

            // Step 2: Costruzione Gerarchia con Pivot Fix
            GameObject parent = new GameObject(newWorkerPrefabName);
            
            // Crea figlio Visuals usando primitiva Capsule
            GameObject visuals = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visuals.name = "Visuals";
            visuals.transform.SetParent(parent.transform);
            
            // Pivot Trick: Sposta il figlio a Y=1 per mantenere il pivot del padre ai piedi
            // La capsule è alta 2m con centro a 0, spostandola a Y=1 il pivot del padre resta a Y=0
            visuals.transform.localPosition = new Vector3(0, 1, 0);

            // Step 3: Setup Componenti - NavMeshAgent sul Padre
            NavMeshAgent agent = parent.AddComponent<NavMeshAgent>();
            
            // Configurazione Mobile Optimized
            agent.speed = 3.5f;
            agent.acceleration = 80f; // Movimento reattivo
            agent.angularSpeed = 120f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Risparmio CPU su Android
            agent.radius = 0.35f;
            agent.height = 2f; // Altezza della capsule
            agent.baseOffset = 0f; // Offset dal pivot

            // Aggiungi WorkerController
            var controller = parent.AddComponent<Gameplay.Workers.WorkerController>();
            Debug.Log($"<color=green>[WorkerMigrationTool]</color> Added NavMeshAgent and WorkerController to parent");

            // Step 4: Setup Figlio Visuals - CapsuleCollider come Trigger
            CapsuleCollider capsuleCollider = visuals.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                capsuleCollider.isTrigger = true; // Trigger per click senza collisioni fisiche
                Debug.Log($"<color=green>[WorkerMigrationTool]</color> Set CapsuleCollider as trigger");
            }

            // Rimuovi Rigidbody se Unity l'ha aggiunto automaticamente
            Rigidbody rb = visuals.GetComponent<Rigidbody>();
            if (rb != null)
            {
                DestroyImmediate(rb);
                Debug.Log($"<color=yellow>[WorkerMigrationTool]</color> Removed auto-generated Rigidbody from Visuals");
            }

            // Step 5: Salvataggio Prefab
            string prefabPath = $"{prefabOutputPath}/{newWorkerPrefabName}.prefab";
            
            // Verifica se esiste già
            if (System.IO.File.Exists(prefabPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Prefab Already Exists",
                    $"Il prefab '{newWorkerPrefabName}' esiste già. Sovrascrivere?",
                    "Sovrascrivi",
                    "Annulla"
                );

                if (!overwrite)
                {
                    DestroyImmediate(parent);
                    Debug.Log("<color=yellow>[WorkerMigrationTool]</color> Operazione annullata dall'utente");
                    return;
                }
            }

            // Salva come prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(parent, prefabPath);
            
            // Step 6: Cleanup - Distruggi istanza temporanea
            DestroyImmediate(parent);

            // Log successo
            Debug.Log($"<color=green>✅ [WorkerMigrationTool]</color> Mobile Worker Prefab created successfully at: <color=cyan>{prefabPath}</color>");
            
            // Ping nel Project per evidenziarlo
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;
        }

        // ============================================
        // 2. ENVIRONMENT SETUP
        // ============================================

        [TitleGroup("Environment Setup")]
        [InfoBox("Configura automaticamente gli oggetti della scena per il NavMesh e effettua il bake.", InfoMessageType.Info)]
        
        [BoxGroup("Environment Setup/Configuration")]
        [LabelText("Ground Layers")]
        [Tooltip("Seleziona i layer considerati 'terreno calpestabile'")]
        public LayerMask groundLayers = ~0; // Default: tutti i layer

        [BoxGroup("Environment Setup/Configuration")]
        [ReadOnly]
        [ShowInInspector]
        [LabelText("Objects Found")]
        private int objectsFoundCount = 0;

        [BoxGroup("Environment Setup/Configuration")]
        [ReadOnly]
        [ShowInInspector]
        [LabelText("Navigation Static Set")]
        private int navigationStaticSetCount = 0;

        [BoxGroup("Environment Setup/Actions")]
        [Button("Setup Static Flags & Bake NavMesh", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void SetupStaticFlagsAndBake()
        {
            objectsFoundCount = 0;
            navigationStaticSetCount = 0;

            // Step A: Pulizia e Ricerca
            List<GameObject> targetObjects = FindGroundObjects();

            if (targetObjects.Count == 0)
            {
                Debug.LogWarning("[WorkerMigrationTool] No valid ground objects found in scene!");
                return;
            }

            objectsFoundCount = targetObjects.Count;

            // Step B: Applicazione Flag (Bitwise Operation)
            ApplyNavigationStaticFlags(targetObjects);

            // Step C: Bake NavMesh
            BakeNavMesh();

            Debug.Log($"<color=green>[WorkerMigrationTool]</color> Setup complete!\n" +
                $"Objects processed: {objectsFoundCount}\n" +
                $"Navigation Static flags set: {navigationStaticSetCount}");
        }

        /// <summary>
        /// Step A: Trova tutti i GameObject che appartengono ai layer selezionati
        /// e hanno MeshRenderer o Terrain
        /// </summary>
        private List<GameObject> FindGroundObjects()
        {
            List<GameObject> validObjects = new List<GameObject>();

            // Trova tutti i GameObject attivi nella scena
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                // Verifica se appartiene ai layer selezionati
                if (!IsInLayerMask(obj, groundLayers))
                    continue;

                // Verifica se ha MeshRenderer o Terrain
                bool hasMeshRenderer = obj.GetComponent<MeshRenderer>() != null;
                bool hasTerrain = obj.GetComponent<Terrain>() != null;

                if (hasMeshRenderer || hasTerrain)
                {
                    validObjects.Add(obj);
                }
            }

            Debug.Log($"<color=cyan>[WorkerMigrationTool]</color> Found {validObjects.Count} ground objects in selected layers.");
            return validObjects;
        }

        /// <summary>
        /// Step B: Applica il flag NavigationStatic usando bitwise OR
        /// per preservare i flag esistenti
        /// </summary>
        private void ApplyNavigationStaticFlags(List<GameObject> objects)
        {
#pragma warning disable CS0618 // NavigationStatic is deprecated but still functional for this use case
            foreach (GameObject obj in objects)
            {
                // Leggi i flag attuali
                StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(obj);

                // Aggiungi NavigationStatic usando OR bit-a-bit
                StaticEditorFlags newFlags = currentFlags | StaticEditorFlags.NavigationStatic;

                // Applica i nuovi flag
                GameObjectUtility.SetStaticEditorFlags(obj, newFlags);

                navigationStaticSetCount++;
            }
#pragma warning restore CS0618

            Debug.Log($"<color=green>[WorkerMigrationTool]</color> Set Navigation Static flag on {navigationStaticSetCount} objects.");
        }

        /// <summary>
        /// Step C: Effettua il bake del NavMesh
        /// </summary>
        private void BakeNavMesh()
        {
            Debug.Log("<color=yellow>[WorkerMigrationTool]</color> Starting NavMesh bake...");

#pragma warning disable CS0618 // Using deprecated API as replacement doesn't provide equivalent functionality for editor baking
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore CS0618

            Debug.Log("<color=green>[WorkerMigrationTool]</color> NavMesh Baked Successfully!");
        }

        /// <summary>
        /// Verifica se un GameObject appartiene a un LayerMask
        /// </summary>
        private bool IsInLayerMask(GameObject obj, LayerMask mask)
        {
            return ((1 << obj.layer) & mask) != 0;
        }

        // ============================================
        // UTILITY BUTTONS
        // ============================================

        [TitleGroup("Utilities")]
        [BoxGroup("Utilities/NavMesh Info")]
        [Button("Show NavMesh Stats", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        private void ShowNavMeshStats()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            
            Debug.Log($"<color=cyan>[NavMesh Stats]</color>\n" +
                $"Vertices: {triangulation.vertices.Length}\n" +
                $"Triangles: {triangulation.indices.Length / 3}\n" +
                $"Areas: {triangulation.areas.Length}");
        }

        [BoxGroup("Utilities/NavMesh Info")]
        [Button("Clear NavMesh", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void ClearNavMesh()
        {
#pragma warning disable CS0618 // Using deprecated API as replacement doesn't provide equivalent functionality for editor operations
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
#pragma warning restore CS0618
            Debug.Log("<color=yellow>[WorkerMigrationTool]</color> NavMesh cleared.");
        }

        [BoxGroup("Utilities/Scene Info")]
        [Button("List Objects by Layer", ButtonSizes.Medium)]
        [GUIColor(0.6f, 0.8f, 1f)]
        private void ListObjectsByLayer()
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            Dictionary<int, int> layerCounts = new Dictionary<int, int>();

            foreach (GameObject obj in allObjects)
            {
                int layer = obj.layer;
                if (!layerCounts.ContainsKey(layer))
                {
                    layerCounts[layer] = 0;
                }
                layerCounts[layer]++;
            }

            string output = "<color=cyan>[Scene Layer Distribution]</color>\n";
            foreach (var kvp in layerCounts)
            {
                string layerName = LayerMask.LayerToName(kvp.Key);
                output += $"Layer {kvp.Key} ({layerName}): {kvp.Value} objects\n";
            }

            Debug.Log(output);
        }
    }
}
