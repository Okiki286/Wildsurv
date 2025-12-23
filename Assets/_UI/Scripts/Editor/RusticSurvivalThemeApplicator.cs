using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.UI;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor tool to apply the "Wilderness Palette" (Cream & Wood) theme to UI elements.
    /// Uses Modular Game UI Kit assets with cream backgrounds and wood accents.
    /// </summary>
    public class RusticSurvivalThemeApplicator : EditorWindow
    {
        private const string KIT_PATH = "Assets/ModularGameUIKit/Common";
        
        // =============================================
        // WILDERNESS PALETTE (CREAM & WOOD)
        // =============================================
        private static readonly Color CREAM_BG = new Color(0.949f, 0.902f, 0.847f, 1f);           // #F2E6D8 - Main backgrounds
        private static readonly Color DARK_WOOD = new Color(0.365f, 0.251f, 0.216f, 1f);          // #5D4037 - Action buttons
        private static readonly Color DEEP_BROWN = new Color(0.243f, 0.153f, 0.137f, 1f);         // #3E2723 - Primary text
        private static readonly Color WHITE_TEXT = Color.white;                                    // Button text alt
        private static readonly Color WOOD_HIGHLIGHT = new Color(0.45f, 0.32f, 0.26f, 1f);        // Lighter wood hover
        private static readonly Color WOOD_PRESSED = new Color(0.3f, 0.2f, 0.15f, 1f);            // Darker wood pressed

        [MenuItem("Tools/UI Kit/Apply Wilderness Theme (Cream & Wood)")]
        public static void ApplyWildernessTheme()
        {
            Debug.Log("<color=orange>[Wilderness Theme]</color> Starting Cream & Wood theme application...");

            // Load kit assets
            Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KIT_PATH}/Sprites/Shapes/Background.png");
            Sprite rectangleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KIT_PATH}/Sprites/Shapes/Rectangle.png");
            TMP_FontAsset fontSemiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{KIT_PATH}/Fonts/Inter-SemiBold SDF.asset");
            TMP_FontAsset fontRegular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{KIT_PATH}/Fonts/Inter-Regular SDF.asset");

            if (backgroundSprite == null || rectangleSprite == null)
            {
                EditorUtility.DisplayDialog("Assets Missing",
                    "Could not load required sprites from:\n" +
                    $"{KIT_PATH}/Sprites/Shapes/\n\n" +
                    "Check if the Modular Game UI Kit is imported correctly.",
                    "OK");
                return;
            }

            if (fontSemiBold == null || fontRegular == null)
            {
                Debug.LogWarning("[Wilderness Theme] Fonts not found, text styling will be skipped.");
            }

            // Apply to all three targets
            ApplyToWorkerAssignmentUI(backgroundSprite, rectangleSprite, fontSemiBold, fontRegular);
            ApplyToBuildMenuUI(backgroundSprite, rectangleSprite, fontSemiBold);
            ApplyToResourceDisplay(rectangleSprite, fontSemiBold);

            Debug.Log("<color=green>[Wilderness Theme]</color> ✅ Cream & Wood theme applied!");
            EditorUtility.DisplayDialog("Wilderness Theme Applied",
                "UI elements have been reskinned with Cream & Wood:\n\n" +
                "• WorkerAssignmentUI: Cream panels + Wood buttons\n" +
                "• BuildMenuUI: Cream structure buttons + Wood outlines\n" +
                "• ResourceDisplay HUD: Cream background\n" +
                "• All text: Deep Brown (#3E2723)\n" +
                "• Fonts: Inter SemiBold/Regular\n\n" +
                "Save the scene to keep changes.",
                "OK");
        }

        // ========================================
        // TARGET 1: WORKER ASSIGNMENT PANEL
        // ========================================
        private static void ApplyToWorkerAssignmentUI(Sprite bgSprite, Sprite btnSprite, TMP_FontAsset fontBold, TMP_FontAsset fontRegular)
        {
            WorkerAssignmentUI workerUI = Object.FindFirstObjectByType<WorkerAssignmentUI>();
            if (workerUI == null)
            {
                Debug.LogWarning("[Wilderness Theme] WorkerAssignmentUI not found in scene");
                return;
            }

            SerializedObject so = new SerializedObject(workerUI);
            SerializedProperty panelProp = so.FindProperty("assignmentPanel");

            if (panelProp != null && panelProp.objectReferenceValue != null)
            {
                GameObject panel = panelProp.objectReferenceValue as GameObject;
                
                // 1. MAIN PANEL BACKGROUND - Cream
                Image panelImage = panel.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.sprite = bgSprite;
                    panelImage.color = CREAM_BG;
                    panelImage.type = Image.Type.Sliced;
                    
                    // Add wood outline
                    Outline outline = panel.GetComponent<Outline>();
                    if (outline == null) outline = panel.AddComponent<Outline>();
                    outline.effectColor = DARK_WOOD;
                    outline.effectDistance = new Vector2(3, -3);
                    
                    EditorUtility.SetDirty(panelImage);
                    EditorUtility.SetDirty(outline);
                    Debug.Log("<color=orange>[Wilderness Theme]</color> Main panel: Cream background + Wood outline");
                }

                // 2. BUTTONS - Dark Wood with white text
                Button[] buttons = panel.GetComponentsInChildren<Button>(true);
                foreach (Button btn in buttons)
                {
                    Image btnImage = btn.GetComponent<Image>();
                    if (btnImage != null)
                    {
                        btnImage.sprite = btnSprite;
                        btnImage.color = DARK_WOOD;
                        btnImage.type = Image.Type.Sliced;
                        
                        ColorBlock colors = btn.colors;
                        colors.normalColor = DARK_WOOD;
                        colors.highlightedColor = WOOD_HIGHLIGHT;
                        colors.pressedColor = WOOD_PRESSED;
                        colors.selectedColor = WOOD_HIGHLIGHT;
                        colors.disabledColor = new Color(0.5f, 0.4f, 0.35f, 0.5f);
                        btn.colors = colors;
                        
                        EditorUtility.SetDirty(btnImage);
                        EditorUtility.SetDirty(btn);
                    }

                    // Button text: White
                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null && fontBold != null)
                    {
                        btnText.font = fontBold;
                        btnText.color = WHITE_TEXT;
                        EditorUtility.SetDirty(btnText);
                    }
                }
                Debug.Log($"<color=orange>[Wilderness Theme]</color> Styled {buttons.Length} buttons: Dark Wood + White text");

                // 3. ALL TEXT ELEMENTS (non-button) - Deep Brown
                if (fontBold != null || fontRegular != null)
                {
                    TextMeshProUGUI[] allTexts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var txt in allTexts)
                    {
                        // Skip button texts (already styled white)
                        if (txt.transform.parent != null && txt.transform.parent.GetComponent<Button>() != null)
                            continue;
                        
                        bool isHeader = txt.gameObject.name.ToLower().Contains("header") ||
                                       txt.gameObject.name.ToLower().Contains("title") ||
                                       txt.gameObject.name.ToLower().Contains("name");
                        
                        txt.font = isHeader && fontBold != null ? fontBold : (fontRegular ?? fontBold);
                        txt.color = DEEP_BROWN;
                        EditorUtility.SetDirty(txt);
                    }
                    Debug.Log($"<color=orange>[Wilderness Theme]</color> Updated {allTexts.Length} text elements: Deep Brown");
                }

                EditorUtility.SetDirty(panel);
            }

            so.ApplyModifiedProperties();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        // ========================================
        // TARGET 2: BUILD MENU UI
        // ========================================
        private static void ApplyToBuildMenuUI(Sprite bgSprite, Sprite btnSprite, TMP_FontAsset font)
        {
            BuildMenuUI buildMenu = Object.FindFirstObjectByType<BuildMenuUI>();
            if (buildMenu == null)
            {
                Debug.LogWarning("[Wilderness Theme] BuildMenuUI not found in scene");
                return;
            }

            SerializedObject so = new SerializedObject(buildMenu);
            
            // Style the main panel
            SerializedProperty panelProp = so.FindProperty("buildMenuPanel");
            if (panelProp != null && panelProp.objectReferenceValue != null)
            {
                GameObject panel = panelProp.objectReferenceValue as GameObject;
                Image panelImage = panel.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.sprite = bgSprite;
                    panelImage.color = new Color(CREAM_BG.r, CREAM_BG.g, CREAM_BG.b, 0.95f); // Slight transparency
                    panelImage.type = Image.Type.Sliced;
                    EditorUtility.SetDirty(panelImage);
                }
            }

            // Style the structure buttons container
            SerializedProperty containerProp = so.FindProperty("structureButtonsContainer");
            if (containerProp != null && containerProp.objectReferenceValue != null)
            {
                Transform container = containerProp.objectReferenceValue as Transform;
                
                // Style each structure button
                Button[] structButtons = container.GetComponentsInChildren<Button>(true);
                foreach (Button btn in structButtons)
                {
                    Image btnImage = btn.GetComponent<Image>();
                    if (btnImage != null)
                    {
                        btnImage.sprite = bgSprite;
                        btnImage.color = CREAM_BG;
                        btnImage.type = Image.Type.Sliced;
                        
                        // Add wood outline
                        Outline outline = btn.GetComponent<Outline>();
                        if (outline == null) outline = btn.gameObject.AddComponent<Outline>();
                        outline.effectColor = DARK_WOOD;
                        outline.effectDistance = new Vector2(2, -2);
                        
                        ColorBlock colors = btn.colors;
                        colors.normalColor = CREAM_BG;
                        colors.highlightedColor = new Color(1f, 0.97f, 0.93f, 1f); // Light cream hover
                        colors.pressedColor = new Color(0.9f, 0.85f, 0.78f, 1f);   // Darker cream pressed
                        colors.selectedColor = colors.highlightedColor;
                        btn.colors = colors;
                        
                        EditorUtility.SetDirty(btnImage);
                        EditorUtility.SetDirty(btn);
                        EditorUtility.SetDirty(outline);
                    }

                    // Style button texts - Deep Brown
                    TextMeshProUGUI[] btnTexts = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var txt in btnTexts)
                    {
                        if (font != null) txt.font = font;
                        txt.color = DEEP_BROWN;
                        EditorUtility.SetDirty(txt);
                    }
                }
                Debug.Log($"<color=orange>[Wilderness Theme]</color> BuildMenu: Styled {structButtons.Length} structure buttons");
            }

            // Style header text
            SerializedProperty headerProp = so.FindProperty("headerText");
            if (headerProp != null && headerProp.objectReferenceValue != null)
            {
                TextMeshProUGUI header = headerProp.objectReferenceValue as TextMeshProUGUI;
                if (header != null && font != null)
                {
                    header.font = font;
                    header.color = DEEP_BROWN;
                    EditorUtility.SetDirty(header);
                }
            }

            so.ApplyModifiedProperties();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        // ========================================
        // TARGET 3: TOP HUD (RESOURCES)
        // ========================================
        private static void ApplyToResourceDisplay(Sprite bgSprite, TMP_FontAsset font)
        {
            ResourceDisplayUI resourceUI = Object.FindFirstObjectByType<ResourceDisplayUI>();
            if (resourceUI == null)
            {
                Debug.LogWarning("[Wilderness Theme] ResourceDisplayUI not found in scene");
                return;
            }

            GameObject resourceObj = resourceUI.gameObject;
            
            // Add or update background - Cream
            Transform bgTransform = resourceObj.transform.Find("Background");
            Image bgImage;
            
            if (bgTransform == null)
            {
                GameObject bgPanel = new GameObject("Background");
                bgPanel.transform.SetParent(resourceObj.transform, false);
                bgPanel.transform.SetAsFirstSibling();
                bgImage = bgPanel.AddComponent<Image>();
                
                RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = new Vector2(20, 10);
                bgRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                bgImage = bgTransform.GetComponent<Image>();
                if (bgImage == null) bgImage = bgTransform.gameObject.AddComponent<Image>();
            }

            bgImage.sprite = bgSprite;
            bgImage.color = CREAM_BG;
            bgImage.type = Image.Type.Sliced;
            
            // Add wood outline
            Outline bgOutline = bgImage.GetComponent<Outline>();
            if (bgOutline == null) bgOutline = bgImage.gameObject.AddComponent<Outline>();
            bgOutline.effectColor = DARK_WOOD;
            bgOutline.effectDistance = new Vector2(2, -2);
            
            EditorUtility.SetDirty(bgImage);
            EditorUtility.SetDirty(bgOutline);

            // Style resource text elements - Deep Brown
            if (font != null)
            {
                TextMeshProUGUI[] texts = resourceObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var txt in texts)
                {
                    txt.font = font;
                    txt.color = DEEP_BROWN;
                    EditorUtility.SetDirty(txt);
                }
                Debug.Log($"<color=orange>[Wilderness Theme]</color> HUD: {texts.Length} texts -> Deep Brown");
            }

            EditorUtility.SetDirty(resourceObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("<color=orange>[Wilderness Theme]</color> HUD: Cream background + Wood outline");
        }
    }
}
