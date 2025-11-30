using UnityEngine;
using UnityEditor;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Tool per generare automaticamente prefab worker con setup completo
    /// </summary>
    public static class WorkerPrefabGenerator
    {
        [MenuItem("Wilderness/Setup/1. Generate Worker Prefab")]
        public static void GenerateWorkerPrefab()
        {
            // Crea cartella se non esiste
            string folderPath = "Assets/_Content/Prefabs/Workers";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/_Content/Prefabs", "Workers");
            }

            // Crea GameObject
            GameObject workerObj = new GameObject("Worker_Villager");
            
            // Aggiungi Capsule per visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(workerObj.transform);
            visual.transform.localPosition = Vector3.up; // Alza di 1 unità
            visual.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            
            // Colora il worker
            var renderer = visual.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.3f, 0.6f, 0.9f); // Blu
            renderer.material = mat;
            
            // Aggiungi collider al root
            CapsuleCollider collider = workerObj.AddComponent<CapsuleCollider>();
            collider.center = Vector3.up;
            collider.height = 2f;
            collider.radius = 0.5f;
            
            // Aggiungi Rigidbody
            Rigidbody rb = workerObj.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.linearDamping = 2f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Aggiungi WorkerController
            WorkerController controller = workerObj.AddComponent<WorkerController>();
            
            // Salva come prefab
            string prefabPath = $"{folderPath}/Worker_Villager.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(workerObj, prefabPath);
            
            // Cleanup
            GameObject.DestroyImmediate(workerObj);
            
            // Seleziona il prefab
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            
            Debug.Log($"<color=green>[WorkerPrefabGenerator]</color> Prefab creato: {prefabPath}");
            Debug.Log($"<color=yellow>⚠️ ORA:</color> Assegna questo prefab al VillagerData.asset nel campo 'Prefab'");
        }

        [MenuItem("Wilderness/Setup/2. Create Missing Tags")]
        public static void CreateMissingTags()
        {
            CreateTag("Bonfire");
            CreateTag("Resource");
            CreateTag("Structure");
            CreateTag("Enemy");
            CreateTag("Worker");
            
            Debug.Log("<color=green>[Setup]</color> Tag creati: Bonfire, Resource, Structure, Enemy, Worker");
        }

        [MenuItem("Wilderness/Setup/3. Create Missing Layers")]
        public static void CreateMissingLayers()
        {
            CreateLayer("Ground", 8);
            CreateLayer("Structure", 9);
            CreateLayer("Worker", 10);
            CreateLayer("Enemy", 11);
            CreateLayer("Resource", 12);
            
            Debug.Log("<color=green>[Setup]</color> Layer creati: Ground(8), Structure(9), Worker(10), Enemy(11), Resource(12)");
        }

        [MenuItem("Wilderness/Setup/🚀 FULL AUTO SETUP (All-in-One)")]
        public static void FullAutoSetup()
        {
            Debug.Log("<color=cyan>========================================</color>");
            Debug.Log("<color=cyan>[AUTO SETUP] Inizializzazione completa...</color>");
            Debug.Log("<color=cyan>========================================</color>");
            
            // Step 1: Tags
            CreateMissingTags();
            
            // Step 2: Layers
            CreateMissingLayers();
            
            // Step 3: Worker Prefab
            GenerateWorkerPrefab();
            
            // Step 4: Trova VillagerData e assegna prefab automaticamente
            AutoAssignPrefabToWorkerData();
            
            Debug.Log("<color=green>========================================</color>");
            Debug.Log("<color=green>[AUTO SETUP] ✅ COMPLETATO!</color>");
            Debug.Log("<color=green>========================================</color>");
            Debug.Log("\n<color=yellow>🎮 Premi PLAY per testare il WorkerSystem!</color>\n");
        }

        private static void AutoAssignPrefabToWorkerData()
        {
            // Cerca il prefab appena creato
            string prefabPath = "Assets/_Content/Prefabs/Workers/Worker_Villager.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogWarning("[Setup] Prefab non trovato, salta auto-assign");
                return;
            }

            // Cerca tutti i WorkerData nel progetto
            string[] guids = AssetDatabase.FindAssets("t:WorkerData");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WorkerData workerData = AssetDatabase.LoadAssetAtPath<WorkerData>(path);
                
                if (workerData != null)
                {
                    // Usa reflection per assegnare il prefab (campo privato)
                    var serializedObject = new SerializedObject(workerData);
                    var prefabProperty = serializedObject.FindProperty("prefab");
                    
                    if (prefabProperty != null)
                    {
                        prefabProperty.objectReferenceValue = prefab;
                        serializedObject.ApplyModifiedProperties();
                        
                        EditorUtility.SetDirty(workerData);
                        AssetDatabase.SaveAssets();
                        
                        Debug.Log($"<color=green>[Setup]</color> ✅ Prefab assegnato automaticamente a: {workerData.name}");
                    }
                }
            }
        }

        // Helper: Crea tag se non esiste
        private static void CreateTag(string tagName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Verifica se esiste già
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
                if (tag.stringValue == tagName)
                {
                    return; // Già esistente
                }
            }

            // Aggiungi nuovo tag
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(0);
            newTag.stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            
            Debug.Log($"  ✅ Tag creato: {tagName}");
        }

        // Helper: Crea layer se non esiste
        private static void CreateLayer(string layerName, int layerIndex)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            if (layerIndex < 0 || layerIndex >= layersProp.arraySize)
            {
                Debug.LogError($"Layer index {layerIndex} fuori range!");
                return;
            }

            SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(layerIndex);
            
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"  ✅ Layer creato: {layerName} (Index: {layerIndex})");
            }
            else
            {
                Debug.Log($"  ⚠️ Layer {layerIndex} già occupato da: {layerSP.stringValue}");
            }
        }
    }
}
