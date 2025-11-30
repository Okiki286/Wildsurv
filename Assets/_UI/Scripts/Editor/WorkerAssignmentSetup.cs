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

        [TitleGroup("Prefab Generation")]
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
            RectTransform idealRT = idealObj.GetComponent<RectTransform>();
            idealRT.anchorMin = Vector2.zero;
            idealRT.anchorMax = Vector2.one;
            idealRT.sizeDelta = Vector2.zero;

            Image idealBorder = idealObj.GetComponent<Image>(); // Just reusing panel as border holder
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
            // Root Panel - Fullscreen with anchors
            GameObject root = new GameObject("WorkerAssignmentPanel");
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.sizeDelta = Vector2.zero; // Stretch to fill
            
            // Component
            WorkerAssignmentUI ui = root.AddComponent<WorkerAssignmentUI>();

            // Main Panel (Visual) - Centered 900x700
            GameObject panel = CreateChildPanel(root, "Panel", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(900, 700);
            panelRT.anchoredPosition = Vector2.zero;

            // Header
            GameObject headerObj = CreateChildText(panel, "Header", "Worker Assignment", 28);
            RectTransform headerRT = headerObj.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.5f, 1);
            headerRT.anchorMax = new Vector2(0.5f, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.anchoredPosition = new Vector2(0, -20);
            headerRT.sizeDelta = new Vector2(800, 40);

            // Structure Info
            GameObject structInfo = CreateChildPanel(panel, "StructureInfo", Color.clear);
            RectTransform structInfoRT = structInfo.GetComponent<RectTransform>();
            structInfoRT.anchorMin = new Vector2(0.5f, 1);
            structInfoRT.anchorMax = new Vector2(0.5f, 1);
            structInfoRT.pivot = new Vector2(0.5f, 1);
            structInfoRT.anchoredPosition = new Vector2(0, -70);
            structInfoRT.sizeDelta = new Vector2(800, 60);

            GameObject sIcon = CreateChildImage(structInfo, "Icon", Color.white);
            sIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
            GameObject sName = CreateChildText(structInfo, "Name", "Structure Name", 20);
            GameObject sStats = CreateChildText(structInfo, "Stats", "HP: 100/100", 14);

            // Slots Header
            GameObject slotsHeader = CreateChildText(panel, "SlotsHeader", "Assigned Workers", 18);
            RectTransform slotsHeaderRT = slotsHeader.GetComponent<RectTransform>();
            slotsHeaderRT.anchorMin = new Vector2(0, 1);
            slotsHeaderRT.anchorMax = new Vector2(0, 1);
            slotsHeaderRT.pivot = new Vector2(0, 1);
            slotsHeaderRT.anchoredPosition = new Vector2(50, -150);
            slotsHeaderRT.sizeDelta = new Vector2(400, 30);
            slotsHeader.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

            // Slots Container
            GameObject slotsCont = CreateChildPanel(panel, "SlotsContainer", Color.clear);
            RectTransform slotsRT = slotsCont.GetComponent<RectTransform>();
            slotsRT.anchorMin = new Vector2(0, 1);
            slotsRT.anchorMax = new Vector2(0, 1);
            slotsRT.pivot = new Vector2(0, 1);
            slotsRT.anchoredPosition = new Vector2(50, -190);
            slotsRT.sizeDelta = new Vector2(800, 150);
            
            HorizontalLayoutGroup slotsLayout = slotsCont.AddComponent<HorizontalLayoutGroup>();
            slotsLayout.childControlWidth = false;
            slotsLayout.childControlHeight = false;
            slotsLayout.spacing = 10;
            slotsLayout.childAlignment = TextAnchor.MiddleLeft;

            // Available Header
            GameObject availCount = CreateChildText(panel, "AvailableCount", "Available Workers (0)", 18);
            RectTransform availCountRT = availCount.GetComponent<RectTransform>();
            availCountRT.anchorMin = new Vector2(0, 1);
            availCountRT.anchorMax = new Vector2(0, 1);
            availCountRT.pivot = new Vector2(0, 1);
            availCountRT.anchoredPosition = new Vector2(50, -360);
            availCountRT.sizeDelta = new Vector2(400, 30);
            availCount.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

            // Available Container
            GameObject availCont = CreateChildPanel(panel, "AvailableContainer", Color.clear);
            RectTransform availRT = availCont.GetComponent<RectTransform>();
            availRT.anchorMin = new Vector2(0, 0);
            availRT.anchorMax = new Vector2(1, 1);
            availRT.pivot = new Vector2(0.5f, 1);
            availRT.anchoredPosition = new Vector2(0, -400);
            availRT.sizeDelta = new Vector2(-100, -450);
            
            GridLayoutGroup grid = availCont.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(380, 70);
            grid.spacing = new Vector2(15, 15);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            // Production Info
            GameObject prodPanel = CreateChildPanel(panel, "ProductionInfo", new Color(0,0,0,0.4f));
            RectTransform prodRT = prodPanel.GetComponent<RectTransform>();
            prodRT.anchorMin = new Vector2(0.5f, 0);
            prodRT.anchorMax = new Vector2(0.5f, 0);
            prodRT.pivot = new Vector2(0.5f, 0);
            prodRT.anchoredPosition = new Vector2(0, 60);
            prodRT.sizeDelta = new Vector2(800, 50);

            GameObject baseProd = CreateChildText(prodPanel, "Base", "Base: 10", 14);
            baseProd.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 0);
            GameObject bonusProd = CreateChildText(prodPanel, "Bonus", "Bonus: +0%", 14);
            bonusProd.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            GameObject totalProd = CreateChildText(prodPanel, "Total", "Total: 10", 16);
            totalProd.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, 0);

            // Close Button
            GameObject closeBtn = CreateChildButton(panel, "CloseBtn", "Close");
            RectTransform closeBtnRT = closeBtn.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(0.5f, 0);
            closeBtnRT.anchorMax = new Vector2(0.5f, 0);
            closeBtnRT.pivot = new Vector2(0.5f, 0);
            closeBtnRT.anchoredPosition = new Vector2(0, 10);
            closeBtnRT.sizeDelta = new Vector2(200, 40);
            
            // IMPORTANT: Panel starts hidden
            panel.SetActive(false);
            
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

        [TitleGroup("Data Generation")]
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

        [TitleGroup("Scene Setup")]
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

        [TitleGroup("System Setup")]
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
        // 5. SETUP STRUCTURE CLICK HANDLERS
        // ============================================

        [TitleGroup("Structure Setup")]
        [Button("Add Click Handlers to Structures", ButtonSizes.Large), GUIColor(0.6f, 0.8f, 0.4f)]
        [InfoBox("Finds all StructureController objects in the scene and adds BoxCollider + StructureClickHandler if missing.")]
        public void SetupStructureClickHandlers()
        {
            StructureController[] structures = FindObjectsByType<StructureController>(FindObjectsSortMode.None);
            
            if (structures.Length == 0)
            {
                Debug.LogWarning("[WorkerAssignmentSetup] No StructureController objects found in scene!");
                return;
            }

            int addedColliders = 0;
            int addedHandlers = 0;

            foreach (StructureController structure in structures)
            {
                GameObject obj = structure.gameObject;

                // Check if has any collider
                Collider existingCollider = obj.GetComponent<Collider>();
                if (existingCollider == null)
                {
                    // Add BoxCollider
                    BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                    
                    // Try to auto-size based on renderer
                    Renderer renderer = obj.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        boxCollider.center = renderer.bounds.center - obj.transform.position;
                        boxCollider.size = renderer.bounds.size;
                    }
                    else
                    {
                        // Default size
                        boxCollider.size = new Vector3(2, 2, 2);
                    }

                    addedColliders++;
                    Debug.Log($"<color=yellow>[WorkerAssignmentSetup]</color> Added BoxCollider to {obj.name}");
                }

                // Check if has StructureClickHandler
                StructureClickHandler existingHandler = obj.GetComponent<StructureClickHandler>();
                if (existingHandler == null)
                {
                    obj.AddComponent<StructureClickHandler>();
                    addedHandlers++;
                    Debug.Log($"<color=yellow>[WorkerAssignmentSetup]</color> Added StructureClickHandler to {obj.name}");
                }
            }

            Debug.Log($"<color=green>[WorkerAssignmentSetup]</color> Setup complete! Found {structures.Length} structures. Added {addedColliders} colliders and {addedHandlers} click handlers.");
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
