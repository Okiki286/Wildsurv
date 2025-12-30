#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using WildernessSurvival.UI.LoadGame;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Odin Editor Window to programmatically generate the Load Game Menu UI.
    /// Menu: Tools/Wilderness/Load Menu Generator
    /// </summary>
    public class LoadMenuGenerator : OdinEditorWindow
    {
        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness/Load Menu Generator")]
        private static void OpenWindow()
        {
            var window = GetWindow<LoadMenuGenerator>();
            window.titleContent = new GUIContent("Load Menu Generator");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        // ============================================
        // REFERENCES
        // ============================================

        [BoxGroup("References")]
        [InfoBox("The generator will auto-find the Canvas if not assigned.", InfoMessageType.Info)]
        [SerializeField] private Canvas parentCanvas;

        [BoxGroup("References")]
        [InfoBox("Assign the pixel/retro TMP font asset.", InfoMessageType.Warning)]
        [SerializeField] private TMP_FontAsset mainFont;

        // ============================================
        // STYLE
        // ============================================

        [BoxGroup("Style")]
        [LabelText("Panel Color (Cream)")]
        [SerializeField] private Color panelColor = new Color(240f / 255f, 230f / 255f, 210f / 255f, 1f); // #F0E6D2

        [BoxGroup("Style")]
        [LabelText("Slot Color (Darker Cream)")]
        [SerializeField] private Color slotColor = new Color(230f / 255f, 216f / 255f, 192f / 255f, 1f); // #E6D8C0

        [BoxGroup("Style")]
        [LabelText("Outline Color (Dark Brown)")]
        [SerializeField] private Color outlineColor = new Color(60f / 255f, 40f / 255f, 13f / 255f, 1f); // #3C280D

        [BoxGroup("Style")]
        [LabelText("Text Color (Dark Brown)")]
        [SerializeField] private Color textColor = new Color(60f / 255f, 40f / 255f, 13f / 255f, 1f); // #3C280D

        [BoxGroup("Style")]
        [LabelText("Load Button Color (Green)")]
        [SerializeField] private Color loadButtonColor = new Color(0.4f, 0.8f, 0.4f, 1f);

        [BoxGroup("Style")]
        [LabelText("Delete Button Color (Red)")]
        [SerializeField] private Color deleteButtonColor = new Color(0.9f, 0.4f, 0.4f, 1f);

        // ============================================
        // DEBUG
        // ============================================

        [BoxGroup("Debug")]
        [SerializeField] private bool verboseLogging = true;

        // ============================================
        // GENERATION RESULTS
        // ============================================

        [TitleGroup("Generation Results")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Generated Panel")]
        private GameObject generatedPanel;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("LoadGameUI Script")]
        private LoadGameUI generatedLoadGameUI;

        // ============================================
        // MAIN ACTION
        // ============================================

        [TitleGroup("Actions")]
        [Button("🏗️ Generate Load Menu", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        [PropertyTooltip("Create the entire Load Game UI hierarchy")]
        private void GenerateLoadMenu()
        {
            Log("=== Starting Load Menu Generation ===", "lime");

            // Step 1: Auto-find Canvas if not assigned
            if (parentCanvas == null)
            {
                parentCanvas = FindFirstObjectByType<Canvas>();
                if (parentCanvas != null)
                {
                    Log($"✅ Auto-found Canvas: {parentCanvas.gameObject.name}", "cyan");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error",
                        "No Canvas found in the scene!\n\n" +
                        "Please create a Canvas first or assign one manually in the 'References' section.",
                        "OK");
                    return;
                }
            }

            // Step 2: Validate font
            if (mainFont == null)
            {
                LogWarning("⚠️ No TMP_FontAsset assigned. Using default font.");
            }

            // Step 3: Check if Load Menu already exists
            LoadGameUI existingUI = FindFirstObjectByType<LoadGameUI>();
            if (existingUI != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Load Menu Already Exists",
                    $"A LoadGameUI already exists on '{existingUI.gameObject.name}'.\n\n" +
                    "Do you want to delete it and create a new one?",
                    "Yes, Replace",
                    "Cancel"
                );

                if (!overwrite)
                {
                    Log("Generation cancelled by user", "yellow");
                    return;
                }

                Log($"Deleting existing Load Menu: {existingUI.gameObject.name}", "yellow");
                DestroyImmediate(existingUI.gameObject);
            }

            // Step 4: Create the UI hierarchy
            generatedPanel = CreatePanelLoadGame();
            generatedLoadGameUI = generatedPanel.GetComponent<LoadGameUI>();

            // Step 5: Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Log("=== Load Menu Generation Complete ===", "lime");

            // Success dialog
            EditorUtility.DisplayDialog("Success!",
                "✅ Load Game Menu Generated Successfully!\n\n" +
                $"Panel: {generatedPanel.name}\n\n" +
                "The menu is ready to use. Save the scene to keep changes.\n\n" +
                "Next Steps:\n" +
                "1. Assign icons to Load/Delete buttons (optional)\n" +
                "2. Test in Play Mode\n" +
                "3. Wire 'Show()' to a main menu button",
                "OK");

            // Select the generated panel
            Selection.activeGameObject = generatedPanel;
        }

        // ============================================
        // UI CREATION METHODS
        // ============================================

        private GameObject CreatePanelLoadGame()
        {
            Log("Creating Panel_LoadGame...", "cyan");

            // Create root panel
            GameObject panel = new GameObject("Panel_LoadGame");
            panel.transform.SetParent(parentCanvas.transform, false);

            // RectTransform (800x600, Center/Middle)
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(800, 600);
            panelRect.anchoredPosition = Vector2.zero;

            // Image (Cream background)
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = panelColor;

            // Outline (Dark Brown)
            Outline panelOutline = panel.AddComponent<Outline>();
            panelOutline.effectColor = outlineColor;
            panelOutline.effectDistance = new Vector2(3, -3);

            // LoadGameUI script
            LoadGameUI loadGameUI = panel.AddComponent<LoadGameUI>();

            // Create children
            GameObject titleText = CreateTitleText(panel.transform);
            GameObject closeButton = CreateCloseButton(panel.transform);
            GameObject scrollView = CreateScrollView(panel.transform);
            GameObject emptyStatePanel = CreateEmptyStatePanel(panel.transform);
            GameObject confirmDeletePanel = CreateConfirmDeletePanel(panel.transform);

            // Create slot prefab (as disabled child)
            GameObject slotPrefab = CreateSlotPrefab(panel.transform);

            // Auto-wire LoadGameUI
            WireLoadGameUI(loadGameUI, panel, closeButton, scrollView, emptyStatePanel, confirmDeletePanel, slotPrefab);

            Log("✅ Panel_LoadGame created", "green");

            return panel;
        }

        private GameObject CreateTitleText(Transform parent)
        {
            Log("Creating Text_Title...", "cyan");

            GameObject titleObj = new GameObject("Text_Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(700, 80);
            rect.anchoredPosition = new Vector2(0, -10);

            TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
            text.text = "CARICA PARTITA";
            text.font = mainFont;
            text.fontSize = 48;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            // Outline for title
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color(0, 0, 0, 0.5f);

            Log("✅ Text_Title created", "green");
            return titleObj;
        }

        private GameObject CreateCloseButton(Transform parent)
        {
            Log("Creating Btn_Close...", "cyan");

            GameObject btnObj = new GameObject("Btn_Close");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(60, 60);
            rect.anchoredPosition = new Vector2(-10, -10);

            Image img = btnObj.AddComponent<Image>();
            img.color = deleteButtonColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Button text ("X")
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "X";
            text.font = mainFont;
            text.fontSize = 36;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            Log("✅ Btn_Close created", "green");
            return btnObj;
        }

        private GameObject CreateScrollView(Transform parent)
        {
            Log("Creating Scroll_View...", "cyan");

            // Scroll View root
            GameObject scrollViewObj = new GameObject("Scroll_View");
            scrollViewObj.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.offsetMin = new Vector2(20, 120); // Left, Bottom padding
            scrollRect.offsetMax = new Vector2(-20, -100); // Right, Top padding

            ScrollRect scrollComponent = scrollViewObj.AddComponent<ScrollRect>();

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.1f); // Transparent background

            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0, 0); // Height will auto-expand

            // Vertical Layout Group - FIXED FOR PROPER WIDTH CONTROL
            VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10); // L, R, T, B - prevents outline cutoff
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;    // Control child width
            layoutGroup.childControlHeight = false;  // Don't control height
            layoutGroup.childForceExpandWidth = true; // Force slots to stretch horizontally
            layoutGroup.childForceExpandHeight = false;

            // Content Size Fitter (expand vertically)
            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Wire ScrollRect
            scrollComponent.content = contentRect;
            scrollComponent.viewport = viewportRect;
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            scrollComponent.scrollSensitivity = 25;

            Log("✅ Scroll_View created", "green");
            return scrollViewObj;
        }

        private GameObject CreateSlotPrefab(Transform parent)
        {
            Log("Creating Template_SaveSlot...", "cyan");

            GameObject slotObj = new GameObject("Template_SaveSlot");
            slotObj.transform.SetParent(parent, false);
            slotObj.SetActive(false); // Disabled by default (it's a prefab template)

            RectTransform rect = slotObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); // Stretch Top
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0, 80);
            rect.anchoredPosition = Vector2.zero;

            // Background Image
            Image img = slotObj.AddComponent<Image>();
            img.color = slotColor;

            // Outline
            Outline outline = slotObj.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2, -2);

            // SaveSlotItem script
            SaveSlotItem slotScript = slotObj.AddComponent<SaveSlotItem>();

            // LayoutElement - CRITICAL FIX FOR WIDTH STRETCHING
            LayoutElement slotLayoutElement = slotObj.AddComponent<LayoutElement>();
            slotLayoutElement.minHeight = 80;          // Fixed minimum height
            slotLayoutElement.preferredWidth = 0;      // Don't fight parent layout
            slotLayoutElement.flexibleWidth = 1;       // Stretch to fill available width

            // Horizontal Layout Group
            HorizontalLayoutGroup layoutGroup = slotObj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(15, 15, 10, 10);
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = true;     // Controlled by layout
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;

            // Children: Text_Filename, Text_Day, Text_Timestamp, Spacer, Btn_Load, Btn_Delete
            GameObject filenameText = CreateSlotText(slotObj.transform, "Text_Filename", "AutoSave 0", 200); // 300 -> 200 to fit
            GameObject dayText = CreateSlotText(slotObj.transform, "Text_Day", "Day 5", 80);           // 100 -> 80
            GameObject timestampText = CreateSlotText(slotObj.transform, "Text_Timestamp", "2025-01-15 14:32", 180);

            // Spacer (flexible)
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(slotObj.transform, false);
            RectTransform spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(0, 0);
            LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            GameObject loadButton = CreateSlotButton(slotObj.transform, "Btn_Load", "LOAD", loadButtonColor);
            GameObject deleteButton = CreateSlotButton(slotObj.transform, "Btn_Delete", "DEL", deleteButtonColor);

            // Auto-wire SaveSlotItem
            WireSaveSlotItem(slotScript, filenameText, timestampText, dayText, loadButton, deleteButton);

            Log("✅ Template_SaveSlot created", "green");
            return slotObj;
        }

        private GameObject CreateSlotText(Transform parent, string name, string content, float width)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 60);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = mainFont;
            text.fontSize = 24;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Left;

            LayoutElement layout = textObj.AddComponent<LayoutElement>();
            layout.preferredWidth = width;

            return textObj;
        }

        private GameObject CreateSlotButton(Transform parent, string name, string label, Color color)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 50);

            Image img = btnObj.AddComponent<Image>();
            img.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.font = mainFont;
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            LayoutElement layout = btnObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 80;

            return btnObj;
        }

        private GameObject CreateEmptyStatePanel(Transform parent)
        {
            Log("Creating Panel_EmptyState...", "cyan");

            GameObject emptyObj = new GameObject("Panel_EmptyState");
            emptyObj.transform.SetParent(parent, false);
            emptyObj.SetActive(false); // Hidden by default

            RectTransform rect = emptyObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(20, 120);
            rect.offsetMax = new Vector2(-20, -100);

            // Text
            GameObject textObj = new GameObject("Text_Empty");
            textObj.transform.SetParent(emptyObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "No save files found.\n\nStart a new game to create a save.";
            text.font = mainFont;
            text.fontSize = 28;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;

            Log("✅ Panel_EmptyState created", "green");
            return emptyObj;
        }

        private GameObject CreateConfirmDeletePanel(Transform parent)
        {
            Log("Creating Panel_ConfirmDelete...", "cyan");

            GameObject confirmObj = new GameObject("Panel_ConfirmDelete");
            confirmObj.transform.SetParent(parent, false);
            confirmObj.SetActive(false); // Hidden by default

            RectTransform rect = confirmObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 250);
            rect.anchoredPosition = Vector2.zero;

            // Background
            Image img = confirmObj.AddComponent<Image>();
            img.color = panelColor;

            Outline outline = confirmObj.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(4, -4);

            // Confirm text
            GameObject textObj = new GameObject("Text_Confirm");
            textObj.transform.SetParent(confirmObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-20, -20);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Delete save file?\n\nThis cannot be undone.";
            text.font = mainFont;
            text.fontSize = 24;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;

            // Buttons container
            GameObject buttonsObj = new GameObject("Buttons");
            buttonsObj.transform.SetParent(confirmObj.transform, false);

            RectTransform buttonsRect = buttonsObj.AddComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0f, 0f);
            buttonsRect.anchorMax = new Vector2(1f, 0.5f);
            buttonsRect.offsetMin = new Vector2(20, 20);
            buttonsRect.offsetMax = new Vector2(-20, 0);

            HorizontalLayoutGroup layout = buttonsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Yes button
            GameObject yesBtn = CreateConfirmButton(buttonsObj.transform, "Btn_Yes", "YES", deleteButtonColor);

            // No button
            GameObject noBtn = CreateConfirmButton(buttonsObj.transform, "Btn_No", "NO", new Color(0.6f, 0.6f, 0.6f, 1f));

            Log("✅ Panel_ConfirmDelete created", "green");
            return confirmObj;
        }

        private GameObject CreateConfirmButton(Transform parent, string name, string label, Color color)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.font = mainFont;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            return btnObj;
        }

        // ============================================
        // AUTO-WIRING
        // ============================================

        private void WireLoadGameUI(LoadGameUI script, GameObject panel, GameObject closeButton, GameObject scrollView, GameObject emptyStatePanel, GameObject confirmDeletePanel, GameObject slotPrefab)
        {
            Log("Auto-wiring LoadGameUI...", "cyan");

            // Use reflection to set private serialized fields
            SerializedObject so = new SerializedObject(script);

            so.FindProperty("windowPanel").objectReferenceValue = panel;
            so.FindProperty("closeButton").objectReferenceValue = closeButton.GetComponent<Button>();

            // Scroll view -> Content
            Transform content = scrollView.transform.Find("Viewport/Content");
            so.FindProperty("slotContainer").objectReferenceValue = content;
            so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab.GetComponent<SaveSlotItem>();

            // Empty state
            so.FindProperty("emptyStatePanel").objectReferenceValue = emptyStatePanel;
            so.FindProperty("emptyStateText").objectReferenceValue = emptyStatePanel.transform.Find("Text_Empty").GetComponent<TextMeshProUGUI>();

            // Confirm delete panel
            so.FindProperty("confirmDeletePanel").objectReferenceValue = confirmDeletePanel;
            so.FindProperty("confirmDeleteText").objectReferenceValue = confirmDeletePanel.transform.Find("Text_Confirm").GetComponent<TextMeshProUGUI>();
            so.FindProperty("confirmDeleteYesButton").objectReferenceValue = confirmDeletePanel.transform.Find("Buttons/Btn_Yes").GetComponent<Button>();
            so.FindProperty("confirmDeleteNoButton").objectReferenceValue = confirmDeletePanel.transform.Find("Buttons/Btn_No").GetComponent<Button>();

            so.ApplyModifiedProperties();

            Log("✅ LoadGameUI auto-wired", "green");
        }

        private void WireSaveSlotItem(SaveSlotItem script, GameObject filenameText, GameObject timestampText, GameObject dayText, GameObject loadButton, GameObject deleteButton)
        {
            Log("Auto-wiring SaveSlotItem...", "cyan");

            SerializedObject so = new SerializedObject(script);

            so.FindProperty("filenameText").objectReferenceValue = filenameText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("timestampText").objectReferenceValue = timestampText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("dayText").objectReferenceValue = dayText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("loadButton").objectReferenceValue = loadButton.GetComponent<Button>();
            so.FindProperty("deleteButton").objectReferenceValue = deleteButton.GetComponent<Button>();

            so.ApplyModifiedProperties();

            Log("✅ SaveSlotItem auto-wired", "green");
        }

        // ============================================
        // QUICK ACTIONS
        // ============================================

        [TitleGroup("Quick Actions")]
        [Button("🔍 Find Existing Load Menu", ButtonSizes.Medium)]
        [GUIColor(0.6f, 0.8f, 1f)]
        private void FindExistingLoadMenu()
        {
            LoadGameUI existing = FindFirstObjectByType<LoadGameUI>();
            if (existing != null)
            {
                generatedPanel = existing.gameObject;
                generatedLoadGameUI = existing;

                Selection.activeGameObject = generatedPanel;

                Log($"✅ Found existing Load Menu: {generatedPanel.name}", "green");
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found",
                    "No LoadGameUI found in the scene.\n\n" +
                    "Click 'Generate Load Menu' to create one.",
                    "OK");
            }
        }

        [TitleGroup("Quick Actions")]
        [Button("🗑️ Delete Load Menu", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DeleteLoadMenu()
        {
            LoadGameUI existing = FindFirstObjectByType<LoadGameUI>();
            if (existing != null)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Confirm Delete",
                    $"Delete Load Menu: {existing.gameObject.name}?\n\n" +
                    "This cannot be undone.",
                    "Yes, Delete",
                    "Cancel"
                );

                if (confirm)
                {
                    DestroyImmediate(existing.gameObject);
                    generatedPanel = null;
                    generatedLoadGameUI = null;

                    Log("✅ Load Menu deleted", "yellow");

                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                    );
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found",
                    "No LoadGameUI found in the scene.",
                    "OK");
            }
        }

        // ============================================
        // LOGGING
        // ============================================

        private void Log(string message, string color)
        {
            if (verboseLogging)
            {
                Debug.Log($"<color={color}>[LoadMenuGenerator]</color> {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"<color=yellow>[LoadMenuGenerator]</color> {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"<color=red>[LoadMenuGenerator]</color> {message}");
        }

        // ============================================
        // INSTRUCTIONS
        // ============================================

        [TitleGroup("Instructions")]
        [InfoBox(
            "This tool generates the complete Load Game Menu UI hierarchy.\n\n" +
            "Steps:\n" +
            "1. Assign a TMP_FontAsset (pixel/retro font)\n" +
            "2. (Optional) Adjust colors in 'Style' section\n" +
            "3. Click 'Generate Load Menu'\n" +
            "4. Save the scene\n\n" +
            "The generated UI will be fully wired and ready to use.\n\n" +
            "To open the menu, call: LoadGameUI.Instance.Show()\n" +
            "To wire to a button: Assign the Show() method to a main menu button's onClick event.",
            InfoMessageType.Info)]
        [Button("Open Load Game UI Script")]
        [GUIColor(0.6f, 0.8f, 1f)]
        private void OpenLoadGameUIScript()
        {
            // Find the script asset
            string[] guids = AssetDatabase.FindAssets("LoadGameUI t:Script");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, 1);
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found",
                    "Could not find LoadGameUI.cs script.",
                    "OK");
            }
        }
    }
}
#endif
