using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.UI;

namespace WildernessSurvival.Editor.Structures
{
    /// <summary>
    /// Tool Odin per automatizzare la creazione di prefab di strutture dal modello Synty.
    /// Genera automaticamente prefab gameplay + preview con naming convention e setup completo.
    /// </summary>
    public class StructurePrefabWizard : OdinEditorWindow
    {
        // ============================================
        // ENUMS
        // ============================================

        public enum StructureType
        {
            Farm,
            Tower,
            Sawmill,
            Quarry,
            Barracks,
            House,
            Storage,
            Workshop,
            Custom
        }

        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness Survival/Structure Prefab Wizard")]
        private static void OpenWindow()
        {
            var window = GetWindow<StructurePrefabWizard>();
            window.titleContent = new GUIContent("Structure Prefab Wizard");
            window.Show();
        }

        // ============================================
        // CONFIGURATION FIELDS
        // ============================================

        [TitleGroup("Input")]
        [BoxGroup("Input/Source")]
        [Required("Trascina qui il prefab Synty di partenza")]
        [AssetsOnly]
        [LabelText("Source Prefab (Synty Model)")]
        [InfoBox("Il modello 3D originale da cui partire (es. SM_Building_Tower_01)", InfoMessageType.None)]
        public GameObject sourcePrefab;

        [BoxGroup("Input/Source")]
        [LabelText("Structure Type")]
        [EnumToggleButtons]
        public StructureType structureType = StructureType.Custom;

        [BoxGroup("Input/Source")]
        [LabelText("Base Name")]
        [InfoBox("Nome base della struttura (es. 'Tower', 'Farm'). Se vuoto, deriva dal nome del prefab.", InfoMessageType.None)]
        public string baseName;

        [BoxGroup("Input/Source")]
        [LabelText("Structure Data (Optional)")]
        [AssetsOnly]
        [InfoBox("Se null, verrà creato un nuovo StructureData automaticamente.", InfoMessageType.None)]
        public StructureData structureData;

        // ============================================
        // VISUAL SETTINGS
        // ============================================

        [TitleGroup("Visual Settings")]
        [BoxGroup("Visual Settings/Scale")]
        [LabelText("Visual Scale")]
        [MinValue(0.1f)]
        [InfoBox("Scala del VisualRoot. I modelli Synty sono spesso troppo grandi, usa valori < 1 per ridurli (es. 0.5)", InfoMessageType.None)]
        public float visualScale = 1f;

        [BoxGroup("Visual Settings/Scale")]
        [LabelText("Default Work Radius")]
        [MinValue(0.1f)]
        [InfoBox("Distanza dal centro della struttura a cui i worker si fermano per lavorare", InfoMessageType.None)]
        public float defaultWorkRadius = 1.5f;

        [TitleGroup("Paths")]
        [BoxGroup("Paths/Output")]
        [FolderPath]
        [LabelText("Gameplay Prefabs Path")]
        public string gameplayPrefabsPath = "Assets/_Gameplay/Structures/Prefabs";

        [BoxGroup("Paths/Output")]
        [FolderPath]
        [LabelText("Preview Prefabs Path")]
        public string previewPrefabsPath = "Assets/_Gameplay/Structures/Preview";

        [BoxGroup("Paths/Output")]
        [FolderPath]
        [LabelText("Structure Data Path")]
        public string structureDataPath = "Assets/_Gameplay/Structures/Data";

        [TitleGroup("Options")]
        [BoxGroup("Options/Settings")]
        [LabelText("Add Box Collider")]
        [Tooltip("Aggiunge automaticamente un BoxCollider al prefab gameplay")]
        public bool addBoxCollider = true;

        [BoxGroup("Options/Settings")]
        [LabelText("Auto-Open Prefabs")]
        [Tooltip("Apre automaticamente i prefab creati nell'editor")]
        public bool autoOpenPrefabs = false;

        // ============================================
        // GENERATION BUTTON
        // ============================================

        [TitleGroup("Actions")]
        [Button(ButtonSizes.Large, Name = "Generate Prefabs")]
        [GUIColor(0.4f, 0.8f, 1f)]
        [PropertySpace(SpaceBefore = 10)]
        private void GeneratePrefabs()
        {
            if (!ValidateInput())
            {
                return;
            }

            try
            {
                // 1. Normalizza base name
                string normalizedName = NormalizeBaseName();

                // 2. Crea/assegna StructureData
                StructureData targetData = EnsureStructureData(normalizedName);

                // 3. Genera prefab gameplay
                GameObject gameplayPrefab = CreateGameplayPrefab(normalizedName, targetData);

                // 4. Genera prefab preview
                GameObject previewPrefab = CreatePreviewPrefab(normalizedName, gameplayPrefab);

                // 5. Save assets
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 6. Log risultati
                Debug.Log($"<color=green>[StructurePrefabWizard]</color> ✅ Prefabs generated successfully!\n" +
                    $"Gameplay: {AssetDatabase.GetAssetPath(gameplayPrefab)}\n" +
                    $"Preview: {AssetDatabase.GetAssetPath(previewPrefab)}\n" +
                    $"Data: {AssetDatabase.GetAssetPath(targetData)}");

                // 7. Auto-open se richiesto
                if (autoOpenPrefabs)
                {
                    Selection.activeObject = gameplayPrefab;
                    AssetDatabase.OpenAsset(gameplayPrefab);
                }

                EditorUtility.DisplayDialog("Success",
                    $"Structure prefabs created successfully!\n\n" +
                    $"Gameplay: {gameplayPrefab.name}\n" +
                    $"Preview: {previewPrefab.name}", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StructurePrefabWizard] ❌ Error generating prefabs: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate prefabs:\n{e.Message}", "OK");
            }
        }

        [Button(ButtonSizes.Medium, Name = "Clear Fields")]
        [PropertySpace(SpaceBefore = 5)]
        private void ClearFields()
        {
            sourcePrefab = null;
            structureData = null;
            baseName = "";
            structureType = StructureType.Custom;
            visualScale = 1f;
            defaultWorkRadius = 1.5f;
        }

        // ============================================
        // VALIDATION
        // ============================================

        private bool ValidateInput()
        {
            if (sourcePrefab == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Source Prefab is required!", "OK");
                return false;
            }

            // Verifica che i path esistano, altrimenti creali
            EnsureDirectoryExists(gameplayPrefabsPath);
            EnsureDirectoryExists(previewPrefabsPath);
            EnsureDirectoryExists(structureDataPath);

            return true;
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
                Debug.Log($"[StructurePrefabWizard] Created directory: {path}");
            }
        }

        // ============================================
        // NAMING
        // ============================================

        private string NormalizeBaseName()
        {
            // Se baseName è vuoto, deriva dal prefab sorgente
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = DeriveNameFromPrefab(sourcePrefab.name);
            }

            // Rimuovi spazi e caratteri invalidi
            string normalized = baseName.Trim().Replace(" ", "");

            // Se ancora vuoto, fallback a tipo di struttura
            if (string.IsNullOrEmpty(normalized))
            {
                normalized = structureType.ToString();
            }

            return normalized;
        }

        private string DeriveNameFromPrefab(string prefabName)
        {
            // Rimuovi prefissi comuni tipo "SM_", "Building_", ecc.
            string cleaned = prefabName;

            string[] prefixesToRemove = { "SM_", "SM_Building_", "Building_", "Prop_", "ENV_" };
            foreach (string prefix in prefixesToRemove)
            {
                if (cleaned.StartsWith(prefix))
                {
                    cleaned = cleaned.Substring(prefix.Length);
                }
            }

            // Rimuovi suffissi numerici tipo "_01"
            if (cleaned.Contains("_"))
            {
                int lastUnderscore = cleaned.LastIndexOf('_');
                string afterUnderscore = cleaned.Substring(lastUnderscore + 1);

                // Se dopo l'underscore c'è solo un numero, rimuovilo
                if (int.TryParse(afterUnderscore, out _))
                {
                    cleaned = cleaned.Substring(0, lastUnderscore);
                }
            }

            return cleaned;
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        /// <summary>
        /// Ottiene un componente se esiste, altrimenti lo aggiunge.
        /// </summary>
        private T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }
            return component;
        }

        // ============================================
        // STRUCTURE DATA
        // ============================================

        private StructureData EnsureStructureData(string normalizedName)
        {
            // Se già fornito dall'utente, usalo
            if (structureData != null)
            {
                Debug.Log($"[StructurePrefabWizard] Using existing StructureData: {structureData.name}");
                return structureData;
            }

            // Altrimenti crea nuovo
            string dataName = $"SD_{normalizedName}_Lv1";
            string dataPath = Path.Combine(structureDataPath, $"{dataName}.asset");

            // Controlla se esiste già
            StructureData existing = AssetDatabase.LoadAssetAtPath<StructureData>(dataPath);
            if (existing != null)
            {
                Debug.Log($"[StructurePrefabWizard] Found existing StructureData: {dataPath}");
                return existing;
            }

            // Crea nuovo StructureData
            StructureData newData = ScriptableObject.CreateInstance<StructureData>();

            // Setup base
            newData.name = dataName;
            // Usa reflection per impostare StructureId se è un campo serializzato
            var idField = typeof(StructureData).GetField("structureId",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            if (idField != null)
            {
                idField.SetValue(newData, normalizedName.ToLower());
            }

            // Salva asset
            AssetDatabase.CreateAsset(newData, dataPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[StructurePrefabWizard] Created new StructureData at: {dataPath}");
            return newData;
        }

        // ============================================
        // GAMEPLAY PREFAB
        // ============================================

        private GameObject CreateGameplayPrefab(string normalizedName, StructureData targetData)
        {
            string prefabName = $"STR_{normalizedName}_Lv1";
            string prefabPath = Path.Combine(gameplayPrefabsPath, $"{prefabName}.prefab");

            // 1. Crea root GameObject
            GameObject root = new GameObject(prefabName);

            // 2. Crea VisualRoot child
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            // ═══════════════════════════════════════════════════════════
            // NUOVO: Applica visual scale al VisualRoot
            // ═══════════════════════════════════════════════════════════
            visualRoot.transform.localScale = Vector3.one * visualScale;

            // 3. Istanzia source prefab come child di VisualRoot
            GameObject visualChild = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (visualChild == null)
            {
                visualChild = Instantiate(sourcePrefab);
            }
            visualChild.transform.SetParent(visualRoot.transform);
            visualChild.transform.localPosition = Vector3.zero;
            visualChild.transform.localRotation = Quaternion.identity;
            visualChild.transform.localScale = Vector3.one;

            // 4. Aggiungi componenti gameplay al root
            StructureController controller = GetOrAddComponent<StructureController>(root);
            // StructureClickHandler rimosso - usare StructureSelectionManager
            StructureStatusUI statusUI = GetOrAddComponent<StructureStatusUI>(root);

            Debug.Log($"[StructurePrefabWizard] Added gameplay components: StructureController, StructureStatusUI");

            // 5. Setup StructureController usando reflection per campi privati
            var dataField = typeof(StructureController).GetField("structureData",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            var visualRootField = typeof(StructureController).GetField("visualRoot",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            // ═══════════════════════════════════════════════════════════
            // NUOVO: Setup work radius
            // ═══════════════════════════════════════════════════════════
            var workRadiusField = typeof(StructureController).GetField("workRadius",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            if (dataField != null)
            {
                dataField.SetValue(controller, targetData);
            }

            if (visualRootField != null)
            {
                visualRootField.SetValue(controller, visualRoot.transform);
            }

            if (workRadiusField != null)
            {
                workRadiusField.SetValue(controller, defaultWorkRadius);
                Debug.Log($"[StructurePrefabWizard] Set work radius to {defaultWorkRadius}");
            }

            // 6. Aggiungi collider se richiesto
            if (addBoxCollider)
            {
                BoxCollider collider = root.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = root.AddComponent<BoxCollider>();
                    // Dimensioni basate sulla scala visuale
                    float scaledSize = 2f * visualScale;
                    collider.size = new Vector3(scaledSize, scaledSize, scaledSize);
                    collider.center = new Vector3(0f, scaledSize * 0.5f, 0f);
                }
            }

            // 7. Salva come prefab
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            // 8. Cleanup scena
            DestroyImmediate(root);

            // 9. Aggiorna riferimento in StructureData
            var prefabField = typeof(StructureData).GetField("prefab",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            if (prefabField != null && targetData != null)
            {
                prefabField.SetValue(targetData, prefabAsset);
                EditorUtility.SetDirty(targetData);
            }

            Debug.Log($"[StructurePrefabWizard] Created gameplay prefab at: {prefabPath}\n" +
                $"  Visual Scale: {visualScale}\n" +
                $"  Work Radius: {defaultWorkRadius}");
            return prefabAsset;
        }

        // ============================================
        // PREVIEW PREFAB
        // ============================================

        private GameObject CreatePreviewPrefab(string normalizedName, GameObject gameplayPrefab)
        {
            string previewName = $"STR_{normalizedName}_Lv1_Preview";
            string previewPath = Path.Combine(previewPrefabsPath, $"{previewName}.prefab");

            // 1. Istanzia gameplay prefab in scena
            GameObject previewRoot = PrefabUtility.InstantiatePrefab(gameplayPrefab) as GameObject;
            if (previewRoot == null)
            {
                previewRoot = Instantiate(gameplayPrefab);
            }
            previewRoot.name = previewName;

            // 2. Rimuovi componenti di gameplay
            StructureController controller = previewRoot.GetComponent<StructureController>();
            if (controller != null)
            {
                DestroyImmediate(controller);
            }

            // StructureClickHandler non più usato - rimosso

            StructureStatusUI statusUI = previewRoot.GetComponent<StructureStatusUI>();
            if (statusUI != null)
            {
                DestroyImmediate(statusUI);
            }

            Debug.Log($"[StructurePrefabWizard] Removed gameplay components from preview: StructureController, StructureStatusUI");

            // Rimuovi altri componenti non necessari (UI, ecc.)
            var uiComponents = previewRoot.GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in uiComponents)
            {
                DestroyImmediate(canvas.gameObject);
            }

            // 3. Disabilita collider (il preview non deve avere collision fisica)
            var colliders = previewRoot.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // 4. Salva come prefab
            GameObject previewAsset = PrefabUtility.SaveAsPrefabAsset(previewRoot, previewPath);

            // 5. Cleanup scena
            DestroyImmediate(previewRoot);

            Debug.Log($"[StructurePrefabWizard] Created preview prefab at: {previewPath}");
            return previewAsset;
        }

        // ============================================
        // HELPERS
        // ============================================

        [TitleGroup("Debug")]
        [Button("Log Current Settings")]
        [PropertySpace(SpaceBefore = 10)]
        private void LogSettings()
        {
            Debug.Log($"<color=cyan>[StructurePrefabWizard] Current Settings:</color>\n" +
                $"Source Prefab: {(sourcePrefab != null ? sourcePrefab.name : "null")}\n" +
                $"Structure Type: {structureType}\n" +
                $"Base Name: '{baseName}'\n" +
                $"Visual Scale: {visualScale}\n" +
                $"Work Radius: {defaultWorkRadius}\n" +
                $"Structure Data: {(structureData != null ? structureData.name : "null")}\n" +
                $"Gameplay Path: {gameplayPrefabsPath}\n" +
                $"Preview Path: {previewPrefabsPath}\n" +
                $"Data Path: {structureDataPath}");
        }

        [Button("Open Gameplay Prefabs Folder")]
        [PropertySpace(SpaceBefore = 5)]
        private void OpenGameplayFolder()
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(gameplayPrefabsPath);
            if (folder != null)
            {
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
            }
        }

        [Button("Open Preview Prefabs Folder")]
        private void OpenPreviewFolder()
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(previewPrefabsPath);
            if (folder != null)
            {
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
            }
        }
    }
}
