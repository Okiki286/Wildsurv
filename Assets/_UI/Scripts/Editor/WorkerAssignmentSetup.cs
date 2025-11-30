using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Collections.Generic;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.UI;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Tool di automazione per il setup del Worker Assignment UI.
    /// 
    /// ORDINE DI ESECUZIONE CONSIGLIATO:
    /// 1. CreatePrefabs() -> Genera i prefab UI necessari
    /// 2. CreateExampleWorkerData() -> Genera dati di esempio
    /// 3. SetupScene() -> Aggiunge la UI alla scena corrente
    /// 4. SetupWorkerSystem() -> Configura il WorkerSystem nella scena
    /// </summary>
    public class WorkerAssignmentSetup : OdinEditorWindow
    {
        [MenuItem("Tools/Wilderness Survival/Worker Assignment Setup")]
        private static void OpenWindow()
        {
            GetWindow<WorkerAssignmentSetup>().Show();
        }

        // ============================================
        // CONFIGURAZIONE PATHS
        // ============================================

        [TitleGroup("Configuration")]
        [FolderPath]
        public string prefabPath = "Assets/_UI/WorkerAssignment/Prefabs";

        [TitleGroup("Configuration")]
        [FolderPath]
        public string dataPath = "Assets/_Gameplay/Workers/Data";

        // ============================================
        // 1. CREATE PREFABS
        // ============================================

        [TitleGroup("1. Prefab Generation")]
        [Button("Create UI Prefabs", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void CreatePrefabs()
        {
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
                AssetDatabase.Refresh();
            }

            // 1. Create WorkerSlot Prefab
            GameObject slotPrefab = CreateWorkerSlotPrefab();
            
            // 2. Create AvailableWorker Prefab
            GameObject availablePrefab = CreateAvailableWorkerPrefab();

            // 3. Create Main Panel Prefab
            CreateWorkerAssignmentPanelPrefab(slotPrefab, availablePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("<color=green>[WorkerAssignmentSetup]</color> All prefabs created successfully!");
        }

        private GameObject CreateWorkerSlotPrefab()
        {
            // Root
            GameObject root = new GameObject("WorkerSlot");
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(100, 140);
            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Component
            WorkerSlotUI ui = root.AddComponent<WorkerSlotUI>();

            // Filled State
            GameObject filledState = CreateChildPanel(root, "FilledState", Color.clear);
            
            // Icon
            GameObject iconObj = CreateChildImage(filledState, "Icon", Color.white);
            RectTransform iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.sizeDelta = new Vector2(60, 60);
            iconRT.anchoredPosition = new Vector2(0, 20);

            // Texts
            GameObject nameObj = CreateChildText(filledState, "Name", "Worker Name", 14);
            nameObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
            
            GameObject roleObj = CreateChildText(filledState, "Role", "Role", 12);
            roleObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -35);

            GameObject bonusObj = CreateChildText(filledState, "Bonus", "+10%", 12);
            bonusObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);

            // Remove Button
            GameObject btnObj = CreateChildButton(filledState, "RemoveBtn", "X");
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(1, 1);
            btnRT.anchorMax = new Vector2(1, 1);
            btnRT.anchoredPosition = new Vector2(-15, -15);
            btnRT.sizeDelta = new Vector2(20, 20);

            // Empty State
            GameObject emptyState = CreateChildPanel(root, "EmptyState", new Color(0,0,0,0.5f));
            CreateChildText(emptyState, "EmptyText", "Empty Slot", 12);

            // Assign References using SerializedObject to avoid dirty scene issues
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("backgroundImage").objectReferenceValue = rootBg;
            so.FindProperty("workerIconImage").objectReferenceValue = iconObj.GetComponent<Image>();
            so.FindProperty("workerNameText").objectReferenceValue = nameObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("workerRoleText").objectReferenceValue = roleObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("bonusText").objectReferenceValue = bonusObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("removeButton").objectReferenceValue = btnObj.GetComponent<Button>();
            so.FindProperty("removeButtonText").objectReferenceValue = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            so.FindProperty("filledStateObject").objectReferenceValue = filledState;
            so.FindProperty("emptyStateObject").objectReferenceValue = emptyState;
            so.ApplyModifiedProperties();

            // Save Prefab
            string path = $"{prefabPath}/WorkerSlot.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private GameObject CreateAvailableWorkerPrefab()
        {
            // Root
            GameObject root = new GameObject("AvailableWorker");
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(200, 60);
            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            
            // Component
            AvailableWorkerUI ui = root.AddComponent<AvailableWorkerUI>();

            // Icon
            GameObject iconObj = CreateChildImage(root, "Icon", Color.white);
            RectTransform iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0.5f);
            iconRT.anchorMax = new Vector2(0, 0.5f);
            iconRT.pivot = new Vector2(0, 0.5f);
            iconRT.sizeDelta = new Vector2(50, 50);
            iconRT.anchoredPosition = new Vector2(5, 0);

            // Texts
            GameObject nameObj = CreateChildText(root, "Name", "Worker Name", 14);
            RectTransform nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.5f);
            nameRT.anchorMax = new Vector2(0, 0.5f);
            nameRT.pivot = new Vector2(0, 0.5f);
            nameRT.anchoredPosition = new Vector2(65, 10);
            nameRT.sizeDelta = new Vector2(100, 20);
            nameObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

            GameObject roleObj = CreateChildText(root, "Role", "Role", 12);
            RectTransform roleRT = roleObj.GetComponent<RectTransform>();
            roleRT.anchorMin = new Vector2(0, 0.5f);
            roleRT.anchorMax = new Vector2(0, 0.5f);
            roleRT.pivot = new Vector2(0, 0.5f);
            roleRT.anchoredPosition = new Vector2(65, -10);
            roleRT.sizeDelta = new Vector2(100, 20);
            roleObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

            // Bonus
            GameObject bonusObj = CreateChildText(root, "Bonus", "+15%", 14);
            RectTransform bonusRT = bonusObj.GetComponent<RectTransform>();
            bonusRT.anchorMin = new Vector2(1, 0.5f);
            bonusRT.anchorMax = new Vector2(1, 0.5f);
            bonusRT.pivot = new Vector2(1, 0.5f);
            bonusRT.anchoredPosition = new Vector2(-60, 0);

            // Assign Button
            GameObject btnObj = CreateChildButton(root, "AssignBtn", "+");
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(1, 0.5f);
            btnRT.anchorMax = new Vector2(1, 0.5f);
            btnRT.pivot = new Vector2(1, 0.5f);
            btnRT.anchoredPosition = new Vector2(-5, 0);
            btnRT.sizeDelta = new Vector2(40, 40);

            // Ideal Match
            GameObject idealObj = CreateChildPanel(root, "IdealMatch", Color.clear);
            Image idealBorder = idealObj.AddComponent<Image>(); // Just reusing panel as border holder
            idealBorder.type = Image.Type.Sliced;
            idealBorder.color = Color.green;
            idealObj.SetActive(false);

            // Assign References
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("backgroundImage").objectReferenceValue = rootBg;
            so.FindProperty("iconImage").objectReferenceValue = iconObj.GetComponent<Image>();
            so.FindProperty("nameText").objectReferenceValue = nameObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("roleText").objectReferenceValue = roleObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("bonusPreviewText").objectReferenceValue = bonusObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("assignButton").objectReferenceValue = btnObj.GetComponent<Button>();
            so.FindProperty("assignButtonText").objectReferenceValue = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            so.FindProperty("idealMatchIndicator").objectReferenceValue = idealObj;
            so.FindProperty("idealMatchBorder").objectReferenceValue = idealBorder;
            so.ApplyModifiedProperties();

            // Save Prefab
            string path = $"{prefabPath}/AvailableWorker.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private void CreateWorkerAssignmentPanelPrefab(GameObject slotPrefab, GameObject availablePrefab)
        {
            // Root Panel
            GameObject root = new GameObject("WorkerAssignmentPanel");
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(800, 600);
            
            // Main Panel (Visual)
            GameObject panel = CreateChildPanel(root, "Panel", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            
            // Component
            WorkerAssignmentUI ui = root.AddComponent<WorkerAssignmentUI>();

            // Header
            GameObject headerObj = CreateChildText(panel, "Header", "Worker Assignment", 24);
            headerObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 270);

            // Structure Info
            GameObject structInfo = CreateChildPanel(panel, "StructureInfo", Color.clear);
            GameObject sIcon = CreateChildImage(structInfo, "Icon", Color.white);
            GameObject sName = CreateChildText(structInfo, "Name", "Structure Name", 20);
            GameObject sStats = CreateChildText(structInfo, "Stats", "HP: 100/100", 14);

            // Slots Container
            GameObject slotsCont = CreateChildPanel(panel, "SlotsContainer", Color.clear);
            HorizontalLayoutGroup slotsLayout = slotsCont.AddComponent<HorizontalLayoutGroup>();
            slotsLayout.childControlWidth = false;
            slotsLayout.childControlHeight = false;
            slotsLayout.spacing = 10;
            GameObject slotsHeader = CreateChildText(panel, "SlotsHeader", "Assigned Workers", 18);

            // Available Container
            GameObject availCont = CreateChildPanel(panel, "AvailableContainer", Color.clear);
            GridLayoutGroup grid = availCont.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200, 60);
            grid.spacing = new Vector2(10, 10);
            GameObject availCount = CreateChildText(panel, "AvailableCount", "Available Workers (0)", 18);

            // Production Info
            GameObject prodPanel = CreateChildPanel(panel, "ProductionInfo", new Color(0,0,0,0.3f));
            GameObject baseProd = CreateChildText(prodPanel, "Base", "Base: 10", 14);
            GameObject bonusProd = CreateChildText(prodPanel, "Bonus", "Bonus: +0%", 14);
            GameObject totalProd = CreateChildText(prodPanel, "Total", "Total: 10", 16);

            // Close Button
            GameObject closeBtn = CreateChildButton(panel, "CloseBtn", "Close");
            
            // Assign References
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("assignmentPanel").objectReferenceValue = panel;
            so.FindProperty("structureNameText").objectReferenceValue = sName.GetComponent<TextMeshProUGUI>();
            so.FindProperty("structureStatsText").objectReferenceValue = sStats.GetComponent<TextMeshProUGUI>();
            so.FindProperty("structureIconImage").objectReferenceValue = sIcon.GetComponent<Image>();
            so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
            
            so.FindProperty("workerSlotsContainer").objectReferenceValue = slotsCont.transform;
            so.FindProperty("workerSlotPrefab").objectReferenceValue = slotPrefab;
            so.FindProperty("slotsHeaderText").objectReferenceValue = slotsHeader.GetComponent<TextMeshProUGUI>();
            
            so.FindProperty("availableWorkersContainer").objectReferenceValue = availCont.transform;
            so.FindProperty("availableWorkerPrefab").objectReferenceValue = availablePrefab;
            so.FindProperty("availableCountText").objectReferenceValue = availCount.GetComponent<TextMeshProUGUI>();
            
            so.FindProperty("productionPanel").objectReferenceValue = prodPanel;
            so.FindProperty("baseProductionText").objectReferenceValue = baseProd.GetComponent<TextMeshProUGUI>();
            so.FindProperty("bonusProductionText").objectReferenceValue = bonusProd.GetComponent<TextMeshProUGUI>();
            so.FindProperty("totalProductionText").objectReferenceValue = totalProd.GetComponent<TextMeshProUGUI>();
            
            so.ApplyModifiedProperties();

            // Save Prefab
            string path = $"{prefabPath}/WorkerAssignmentPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
        }

        // ============================================
        // 2. CREATE DATA
        // ============================================

        [TitleGroup("2. Data Generation")]
        [Button("Create Example Worker Data", ButtonSizes.Large), GUIColor(0.4f, 0.6f, 0.8f)]
        public void CreateExampleWorkerData()
        {
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
                AssetDatabase.Refresh();
            }

            CreateWorkerDataAsset("Gatherer", WorkerRole.Gatherer, 1.25f, 1f, Color.green);
            CreateWorkerDataAsset("Builder", WorkerRole.Builder, 1f, 1.5f, Color.yellow);
            CreateWorkerDataAsset("Guard", WorkerRole.Guard, 1f, 1f, Color.red);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("<color=green>[WorkerAssignmentSetup]</color> Example WorkerData created!");
        }

        private void CreateWorkerDataAsset(string name, WorkerRole role, float prodMult, float buildMult, Color color)
        {
            WorkerData data = ScriptableObject.CreateInstance<WorkerData>();
            
            // Use SerializedObject to set private fields
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("workerId").stringValue = name.ToLower();
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("workerType").enumValueIndex = (int)WorkerType.Villager;
            so.FindProperty("defaultRole").intValue = (int)role; // Flags enum, careful
            so.FindProperty("roleColor").colorValue = color;
            so.FindProperty("productivityMultiplier").floatValue = prodMult;
            so.FindProperty("buildSpeedMultiplier").floatValue = buildMult;
            so.ApplyModifiedProperties();

            string path = $"{dataPath}/{name}.asset";
            AssetDatabase.CreateAsset(data, path);
        }

        // ============================================
        // 3. SETUP SCENE
        // ============================================

        [TitleGroup("3. Scene Setup")]
        [Button("Setup Scene UI", ButtonSizes.Large), GUIColor(0.8f, 0.6f, 0.4f)]
        public void SetupScene()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("[WorkerAssignmentSetup] Created new Canvas");
            }

            // Check if panel already exists
            WorkerAssignmentUI existingUI = FindFirstObjectByType<WorkerAssignmentUI>();
            if (existingUI != null)
            {
                Debug.LogWarning("[WorkerAssignmentSetup] WorkerAssignmentUI already exists in scene.");
                return;
            }

            // Load Prefab
            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{prefabPath}/WorkerAssignmentPanel.prefab");
            if (panelPrefab == null)
            {
                Debug.LogError($"[WorkerAssignmentSetup] Prefab not found at {prefabPath}/WorkerAssignmentPanel.prefab. Run Step 1 first!");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvas.transform);
            instance.name = "WorkerAssignmentPanel";
            
            // Ensure EventSystem
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            Debug.Log("<color=green>[WorkerAssignmentSetup]</color> Scene UI setup complete!");
        }

        // ============================================
        // 4. SETUP WORKER SYSTEM
        // ============================================

        [TitleGroup("4. System Setup")]
        [Button("Setup Worker System", ButtonSizes.Large), GUIColor(0.8f, 0.4f, 0.6f)]
        public void SetupWorkerSystem()
        {
            WorkerSystem system = FindFirstObjectByType<WorkerSystem>();
            if (system == null)
            {
                GameObject sysObj = new GameObject("WorkerSystem");
                system = sysObj.AddComponent<WorkerSystem>();
                Debug.Log("[WorkerAssignmentSetup] Created WorkerSystem GameObject");
            }

            // Load all WorkerData
            string[] guids = AssetDatabase.FindAssets("t:WorkerData", new[] { dataPath });
            List<WorkerData> dataList = new List<WorkerData>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WorkerData d = AssetDatabase.LoadAssetAtPath<WorkerData>(path);
                if (d != null) dataList.Add(d);
            }

            // Assign to system
            SerializedObject so = new SerializedObject(system);
            SerializedProperty workersProp = so.FindProperty("availableWorkers");
            workersProp.ClearArray();
            workersProp.arraySize = dataList.Count;
            for (int i = 0; i < dataList.Count; i++)
            {
                workersProp.GetArrayElementAtIndex(i).objectReferenceValue = dataList[i];
            }
            
            // Spawn Point
            if (so.FindProperty("spawnPoint").objectReferenceValue == null)
            {
                GameObject spawn = new GameObject("WorkerSpawnPoint");
                so.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
            }

            so.ApplyModifiedProperties();
            
            Debug.Log($"<color=green>[WorkerAssignmentSetup]</color> WorkerSystem configured with {dataList.Count} worker types!");
        }

        // ============================================
        // HELPERS
        // ============================================

        private GameObject CreateChildPanel(GameObject parent, string name, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private GameObject CreateChildImage(GameObject parent, string name, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private GameObject CreateChildText(GameObject parent, string name, string content, int fontSize)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            TextMeshProUGUI txt = obj.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            return obj;
        }

        private GameObject CreateChildButton(GameObject parent, string name, string text)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            Image img = obj.AddComponent<Image>();
            img.color = Color.white;
            Button btn = obj.AddComponent<Button>();
            
            GameObject txtObj = CreateChildText(obj, "Text", text, 14);
            txtObj.GetComponent<TextMeshProUGUI>().color = Color.black;
            
            return obj;
        }
    }
}
