using UnityEngine;
using UnityEditor;
using WildernessSurvival.Gameplay.Enemies;

namespace WildernessSurvival.Core.Editor.Enemies
{
    /// <summary>
    /// Tool Editor per creare prefab enemy + EnemyData in un click.
    /// Menu: Tools > Wilderness Survival > Enemies > Create Enemy Prefab + Data
    /// </summary>
    public static class EnemyPrefabCreator
    {
        // ============================================
        // PATHS
        // ============================================

        private const string DATA_FOLDER = "Assets/_Gameplay/Enemies/Data";
        private const string PREFAB_FOLDER = "Assets/_Gameplay/Enemies/Prefabs";

        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness Survival/Enemies/Create Enemy Prefab + Data")]
        public static void CreateEnemyPrefabAndData()
        {
            // Assicura cartelle esistano
            EnsureFoldersExist();

            // Chiedi nome nemico
            string enemyName = ShowNameInputDialog();
            if (string.IsNullOrEmpty(enemyName))
            {
                Debug.LogWarning("[EnemyPrefabCreator] Operazione annullata.");
                return;
            }

            // Crea EnemyData
            EnemyData enemyData = CreateEnemyData(enemyName);
            if (enemyData == null) return;

            // Crea Prefab
            GameObject prefab = CreateEnemyPrefab(enemyName, enemyData);
            if (prefab == null) return;

            // Assegna prefab a EnemyData
            AssignPrefabToData(enemyData, prefab);

            // Seleziona il prefab nella Project window
            Selection.activeObject = prefab;

            Debug.Log($"<color=green>[EnemyPrefabCreator]</color> Creato:\n" +
                $"  - EnemyData: {DATA_FOLDER}/{enemyName}.asset\n" +
                $"  - Prefab: {PREFAB_FOLDER}/{enemyName}.prefab");

            EditorUtility.DisplayDialog(
                "Enemy Creato",
                $"Creato con successo:\n\n" +
                $"EnemyData: {enemyName}.asset\n" +
                $"Prefab: {enemyName}.prefab\n\n" +
                $"Il prefab ha EnemyInstance + NavMeshAgent + Collider pronti.",
                "OK");
        }

        // ============================================
        // FOLDER MANAGEMENT
        // ============================================

        private static void EnsureFoldersExist()
        {
            // Data folder
            if (!AssetDatabase.IsValidFolder(DATA_FOLDER))
            {
                string parent = System.IO.Path.GetDirectoryName(DATA_FOLDER).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(DATA_FOLDER);

                if (!AssetDatabase.IsValidFolder(parent))
                {
                    string grandParent = System.IO.Path.GetDirectoryName(parent).Replace("\\", "/");
                    string parentFolder = System.IO.Path.GetFileName(parent);
                    AssetDatabase.CreateFolder(grandParent, parentFolder);
                }

                AssetDatabase.CreateFolder(parent, folder);
            }

            // Prefab folder
            if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
            {
                string parent = System.IO.Path.GetDirectoryName(PREFAB_FOLDER).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(PREFAB_FOLDER);

                if (!AssetDatabase.IsValidFolder(parent))
                {
                    string grandParent = System.IO.Path.GetDirectoryName(parent).Replace("\\", "/");
                    string parentFolder = System.IO.Path.GetFileName(parent);
                    AssetDatabase.CreateFolder(grandParent, parentFolder);
                }

                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        // ============================================
        // NAME INPUT
        // ============================================

        private static string ShowNameInputDialog()
        {
            // Usa EditorInputDialog o fallback a default
            string defaultName = "Enemy_Default";

            // Trova un nome unico
            int counter = 1;
            while (AssetDatabase.LoadAssetAtPath<EnemyData>($"{DATA_FOLDER}/{defaultName}.asset") != null)
            {
                counter++;
                defaultName = $"Enemy_Default_{counter}";
            }

            return defaultName;
        }

        // ============================================
        // ENEMY DATA CREATION
        // ============================================

        private static EnemyData CreateEnemyData(string enemyName)
        {
            string assetPath = $"{DATA_FOLDER}/{enemyName}.asset";

            // Verifica se esiste già
            EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
            if (existing != null)
            {
                Debug.LogWarning($"[EnemyPrefabCreator] EnemyData '{enemyName}' esiste già. Uso quello esistente.");
                return existing;
            }

            // Crea ScriptableObject
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();

            // Configura valori default usando SerializedObject
            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();

            // Ora configura i campi via SerializedObject
            SerializedObject so = new SerializedObject(data);

            // Identificazione
            so.FindProperty("enemyId").stringValue = enemyName.ToLower().Replace(" ", "_");
            so.FindProperty("displayName").stringValue = enemyName.Replace("_", " ");
            so.FindProperty("description").stringValue = "Un nemico base.";

            // Classificazione
            so.FindProperty("enemyType").enumValueIndex = 0; // Rusher
            so.FindProperty("tier").intValue = 1;
            so.FindProperty("rarity").enumValueIndex = 0; // Common
            so.FindProperty("isBoss").boolValue = false;

            // Stats Base - valori safe
            so.FindProperty("baseHealth").intValue = 50;
            so.FindProperty("armor").intValue = 0;
            so.FindProperty("moveSpeed").floatValue = 3.5f;
            so.FindProperty("attackDamage").floatValue = 8f;
            so.FindProperty("attackInterval").floatValue = 1.2f;
            so.FindProperty("attackRange").floatValue = 1.8f;

            // Comportamento AI
            so.FindProperty("targetPriority").enumValueIndex = 0; // Nearest
            so.FindProperty("aggroRange").floatValue = 8f;

            // Rewards
            so.FindProperty("baseShardDrop").intValue = 5;
            so.FindProperty("baseXP").intValue = 10;

            // Scaling
            so.FindProperty("healthScaling").floatValue = 1.1f;
            so.FindProperty("damageScaling").floatValue = 1.05f;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            return data;
        }

        // ============================================
        // PREFAB CREATION
        // ============================================

        private static GameObject CreateEnemyPrefab(string enemyName, EnemyData data)
        {
            string prefabPath = $"{PREFAB_FOLDER}/{enemyName}.prefab";

            // Verifica se esiste già
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                Debug.LogWarning($"[EnemyPrefabCreator] Prefab '{enemyName}' esiste già. Uso quello esistente.");
                return existingPrefab;
            }

            // Crea root GameObject
            GameObject root = new GameObject(enemyName);

            // ═══════════════════════════════════════════════════════════
            // COMPONENTI OBBLIGATORI (basati su EnemyInstance/EnemySpawner)
            // ═══════════════════════════════════════════════════════════

            // 1. EnemyInstance (contiene AI, combat, pathfinding)
            //    [RequireComponent(typeof(NavMeshAgent))] aggiunge automaticamente NavMeshAgent
            EnemyInstance enemyInstance = root.AddComponent<EnemyInstance>();

            // 2. NavMeshAgent (aggiunto automaticamente da RequireComponent, ma configuriamolo)
            UnityEngine.AI.NavMeshAgent agent = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null)
            {
                agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            }

            // Configura NavMeshAgent con valori da EnemyData
            agent.speed = data.MoveSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = data.AttackRange * 0.9f;
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.baseOffset = 0f;
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            agent.avoidancePriority = 50;

            // 3. Collider (per trigger detection da WaystoneDebuffAura)
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 0.9f, 0);
            capsule.radius = 0.4f;
            capsule.height = 1.8f;
            capsule.direction = 1; // Y-axis

            // 4. Rigidbody (KINEMATIC per trigger reliability)
            //    WaystoneDebuffAura.cs (linea 16): "Serve un Rigidbody (isKinematic=true) su questo GO o sul nemico"
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // ═══════════════════════════════════════════════════════════
            // LAYER SETUP (Enemy = layer 11, come da WaystoneDebuffAura)
            // ═══════════════════════════════════════════════════════════

            // Assicura che il layer "Enemy" esista (layer 11)
            int enemyLayer = 11;
            if (LayerMask.LayerToName(enemyLayer) == "Enemy" || string.IsNullOrEmpty(LayerMask.LayerToName(enemyLayer)))
            {
                root.layer = enemyLayer;
            }
            else
            {
                Debug.LogWarning("[EnemyPrefabCreator] Layer 11 non è 'Enemy'. Imposta manualmente il layer.");
            }

            // ═══════════════════════════════════════════════════════════
            // VISUAL PLACEHOLDER
            // ═══════════════════════════════════════════════════════════

            // Crea child VisualRoot con mesh placeholder
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
            visualRoot.transform.localScale = Vector3.one;
            visualRoot.layer = root.layer;

            // Placeholder: Capsule mesh
            GameObject capsuleMesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsuleMesh.name = "Model";
            capsuleMesh.transform.SetParent(visualRoot.transform);
            capsuleMesh.transform.localPosition = new Vector3(0, 0.9f, 0);
            capsuleMesh.transform.localRotation = Quaternion.identity;
            capsuleMesh.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            capsuleMesh.layer = root.layer;

            // Rimuovi collider dal mesh placeholder (usiamo quello su root)
            Collider meshCollider = capsuleMesh.GetComponent<Collider>();
            if (meshCollider != null)
            {
                Object.DestroyImmediate(meshCollider);
            }

            // Colora il placeholder
            Renderer renderer = capsuleMesh.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Crea materiale rosso per visibilità
                Material enemyMat = new Material(Shader.Find("Standard"));
                enemyMat.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                enemyMat.name = "EnemyPlaceholder_Mat";
                renderer.sharedMaterial = enemyMat;
            }

            // ═══════════════════════════════════════════════════════════
            // SALVA COME PREFAB
            // ═══════════════════════════════════════════════════════════

            // Salva prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            // Distruggi l'istanza temporanea
            Object.DestroyImmediate(root);

            return prefab;
        }

        // ============================================
        // ASSIGN PREFAB TO DATA
        // ============================================

        private static void AssignPrefabToData(EnemyData data, GameObject prefab)
        {
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("prefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        // ============================================
        // VARIANT CREATION (per clonare da prefab base)
        // ============================================

        [MenuItem("Tools/Wilderness Survival/Enemies/Create Enemy Variant from Selection")]
        public static void CreateEnemyVariantFromSelection()
        {
            // Verifica selezione
            GameObject selectedPrefab = Selection.activeGameObject;
            if (selectedPrefab == null)
            {
                EditorUtility.DisplayDialog("Nessuna Selezione",
                    "Seleziona un prefab enemy esistente nella Project window.",
                    "OK");
                return;
            }

            // Verifica che sia un prefab
            string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab"))
            {
                EditorUtility.DisplayDialog("Non è un Prefab",
                    "Seleziona un file .prefab nella Project window.",
                    "OK");
                return;
            }

            // Verifica che abbia EnemyInstance
            EnemyInstance enemyInstance = selectedPrefab.GetComponent<EnemyInstance>();
            if (enemyInstance == null)
            {
                EditorUtility.DisplayDialog("Non è un Enemy Prefab",
                    "Il prefab selezionato non ha il componente EnemyInstance.",
                    "OK");
                return;
            }

            // Genera nome variante
            string baseName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
            string variantName = baseName + "_Variant";
            int counter = 1;
            while (AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_FOLDER}/{variantName}.prefab") != null)
            {
                counter++;
                variantName = $"{baseName}_Variant_{counter}";
            }

            EnsureFoldersExist();

            // Crea nuovo EnemyData
            EnemyData newData = CreateEnemyData(variantName);

            // Duplica prefab
            string newPrefabPath = $"{PREFAB_FOLDER}/{variantName}.prefab";
            AssetDatabase.CopyAsset(prefabPath, newPrefabPath);

            GameObject newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);

            // Assegna nuovo EnemyData
            AssignPrefabToData(newData, newPrefab);

            Selection.activeObject = newPrefab;

            Debug.Log($"<color=green>[EnemyPrefabCreator]</color> Creata variante: {variantName}");

            EditorUtility.DisplayDialog(
                "Variante Creata",
                $"Creata variante da '{baseName}':\n\n" +
                $"EnemyData: {variantName}.asset\n" +
                $"Prefab: {variantName}.prefab",
                "OK");
        }

        // ============================================
        // VALIDATION
        // ============================================

        [MenuItem("Tools/Wilderness Survival/Enemies/Create Enemy Variant from Selection", true)]
        public static bool ValidateCreateVariant()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null) return false;

            string path = AssetDatabase.GetAssetPath(selected);
            return !string.IsNullOrEmpty(path) && path.EndsWith(".prefab");
        }
    }
}
