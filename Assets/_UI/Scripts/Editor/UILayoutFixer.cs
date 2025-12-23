using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.UI;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor tool to fix Build Menu and Top HUD layouts with Cream & Wood theme.
    /// Creates proper containers with HorizontalLayoutGroup for organization.
    /// </summary>
    public class UILayoutFixer : EditorWindow
    {
        private const string KIT_PATH = "Assets/ModularGameUIKit/Common";
        
        // =============================================
        // WILDERNESS PALETTE (CREAM & WOOD)
        // =============================================
        private static readonly Color CREAM_BG = new Color(0.949f, 0.902f, 0.847f, 1f);           // #F2E6D8
        private static readonly Color CREAM_DARKER = new Color(0.902f, 0.847f, 0.784f, 1f);       // #E6D8C8 - Slightly darker cream
        private static readonly Color DARK_WOOD = new Color(0.365f, 0.251f, 0.216f, 1f);          // #5D4037
        private static readonly Color DEEP_BROWN = new Color(0.243f, 0.153f, 0.137f, 1f);         // #3E2723
        private static readonly Color WHITE_TEXT = Color.white;

        [MenuItem("Tools/UI Kit/Fix Build Menu & HUD Layout")]
        public static void FixLayouts()
        {
            Debug.Log("<color=orange>[UI Layout Fix]</color> Starting layout fixes...");

            // Load kit assets
            Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KIT_PATH}/Sprites/Shapes/Background.png");
            TMP_FontAsset fontSemiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{KIT_PATH}/Fonts/Inter-SemiBold SDF.asset");

            if (backgroundSprite == null)
            {
                EditorUtility.DisplayDialog("Assets Missing",
                    "Could not load Background.png from:\n" +
                    $"{KIT_PATH}/Sprites/Shapes/\n\n" +
                    "Check if the Modular Game UI Kit is imported.",
                    "OK");
                return;
            }

            // TASK 1: Fix Build Menu
            FixBuildMenuLayout(backgroundSprite, fontSemiBold);

            // TASK 2: Fix Top HUD
            FixTopHUDLayout(backgroundSprite, fontSemiBold);

            Debug.Log("<color=green>[UI Layout Fix]</color> ✅ Layout fixes complete!");
            EditorUtility.DisplayDialog("Layout Fixes Applied",
                "UI Layout fixes applied:\n\n" +
                "✅ Build Menu: Panel container + HorizontalLayoutGroup\n" +
                "✅ Top HUD: Cream background + Brown text\n\n" +
                "Save the scene to keep changes.",
                "OK");
        }

        // ========================================
        // TASK 1: FIX BUILD MENU
        // ========================================
        private static void FixBuildMenuLayout(Sprite bgSprite, TMP_FontAsset font)
        {
            BuildMenuUI buildMenu = Object.FindFirstObjectByType<BuildMenuUI>();
            if (buildMenu == null)
            {
                Debug.LogWarning("[UI Layout Fix] BuildMenuUI not found in scene");
                return;
            }

            SerializedObject so = new SerializedObject(buildMenu);
            SerializedProperty containerProp = so.FindProperty("structureButtonsContainer");
            SerializedProperty panelProp = so.FindProperty("buildMenuPanel");

            if (containerProp?.objectReferenceValue == null)
            {
                Debug.LogWarning("[UI Layout Fix] structureButtonsContainer not assigned in BuildMenuUI");
                return;
            }

            // Handle both Transform and GameObject types
            Transform buttonsContainer = null;
            Object containerRef = containerProp.objectReferenceValue;
            if (containerRef is Transform t)
            {
                buttonsContainer = t;
            }
            else if (containerRef is GameObject go)
            {
                buttonsContainer = go.transform;
            }
            else if (containerRef is Component c)
            {
                buttonsContainer = c.transform;
            }

            if (buttonsContainer == null)
            {
                Debug.LogWarning($"[UI Layout Fix] Could not get Transform from structureButtonsContainer (type: {containerRef?.GetType()?.Name})");
                return;
            }

            // Handle main panel (could be GameObject)
            GameObject mainPanel = null;
            if (panelProp?.objectReferenceValue is GameObject panelGo)
            {
                mainPanel = panelGo;
            }
            else if (panelProp?.objectReferenceValue is Transform panelT)
            {
                mainPanel = panelT.gameObject;
            }
            else if (panelProp?.objectReferenceValue is Component panelC)
            {
                mainPanel = panelC.gameObject;
            }

            // 1. STYLE THE MAIN BUILD MENU PANEL
            if (mainPanel != null)
            {
                Image panelImg = mainPanel.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.sprite = bgSprite;
                    panelImg.color = CREAM_BG;
                    panelImg.type = Image.Type.Sliced;
                    EditorUtility.SetDirty(panelImg);
                    Debug.Log("<color=orange>[UI Layout Fix]</color> BuildMenu main panel: Cream background");
                }
            }

            // 2. ADD HORIZONTAL LAYOUT GROUP TO BUTTONS CONTAINER
            // Remove any existing LayoutGroup (e.g. GridLayoutGroup) to avoid conflicts
            LayoutGroup existingLayout = buttonsContainer.GetComponent<LayoutGroup>();
            if (existingLayout != null && !(existingLayout is HorizontalLayoutGroup))
            {
                Object.DestroyImmediate(existingLayout);
            }

            HorizontalLayoutGroup hlg = buttonsContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = buttonsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            if (hlg != null)
            {
                hlg.spacing = 10f;
                hlg.padding = new RectOffset(10, 10, 10, 10);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                EditorUtility.SetDirty(hlg);
            }

            // 3. ADD CONTENT SIZE FITTER
            ContentSizeFitter csf = buttonsContainer.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = buttonsContainer.gameObject.AddComponent<ContentSizeFitter>();
            }
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EditorUtility.SetDirty(csf);

            // 4. STYLE CONTAINER WITH BACKGROUND
            Image containerBg = buttonsContainer.GetComponent<Image>();
            if (containerBg == null)
            {
                containerBg = buttonsContainer.gameObject.AddComponent<Image>();
            }
            containerBg.sprite = bgSprite;
            containerBg.color = CREAM_BG;
            containerBg.type = Image.Type.Sliced;
            EditorUtility.SetDirty(containerBg);

            // 5. ADD OUTLINE TO CONTAINER
            Outline containerOutline = buttonsContainer.GetComponent<Outline>();
            if (containerOutline == null)
            {
                containerOutline = buttonsContainer.gameObject.AddComponent<Outline>();
            }
            containerOutline.effectColor = DARK_WOOD;
            containerOutline.effectDistance = new Vector2(2, -2);
            EditorUtility.SetDirty(containerOutline);

            Debug.Log("<color=orange>[UI Layout Fix]</color> BuildMenu container: HorizontalLayoutGroup + ContentSizeFitter added");

            // 6. STYLE ALL EXISTING BUTTONS
            int buttonCount = 0;
            foreach (Transform child in buttonsContainer)
            {
                Button btn = child.GetComponent<Button>();
                if (btn == null) continue;

                // Button background - transparent or lighter cream
                Image btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = new Color(1f, 1f, 1f, 0f); // Transparent
                    EditorUtility.SetDirty(btnImg);
                }

                // Add outline to each button
                Outline btnOutline = btn.GetComponent<Outline>();
                if (btnOutline == null)
                {
                    btnOutline = btn.gameObject.AddComponent<Outline>();
                }
                btnOutline.effectColor = DARK_WOOD;
                btnOutline.effectDistance = new Vector2(2, -2);
                EditorUtility.SetDirty(btnOutline);

                // Color block for button states
                ColorBlock colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.95f, 0.92f, 0.88f, 1f);
                colors.pressedColor = new Color(0.9f, 0.85f, 0.78f, 1f);
                btn.colors = colors;
                EditorUtility.SetDirty(btn);

                // Style all text children - Deep Brown
                TextMeshProUGUI[] texts = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var txt in texts)
                {
                    if (font != null) txt.font = font;
                    txt.color = DEEP_BROWN;
                    EditorUtility.SetDirty(txt);
                }

                buttonCount++;
            }
            Debug.Log($"<color=orange>[UI Layout Fix]</color> BuildMenu: Styled {buttonCount} buttons with Wood outline + Brown text");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(buttonsContainer.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        // ========================================
        // TASK 2: FIX TOP HUD (RESOURCES)
        // ========================================
        private static void FixTopHUDLayout(Sprite bgSprite, TMP_FontAsset font)
        {
            ResourceDisplayUI resourceUI = Object.FindFirstObjectByType<ResourceDisplayUI>();
            if (resourceUI == null)
            {
                Debug.LogWarning("[UI Layout Fix] ResourceDisplayUI not found in scene");
                return;
            }

            GameObject resourceObj = resourceUI.gameObject;

            // 1. CREATE OR UPDATE BACKGROUND PANEL
            Transform existingBg = resourceObj.transform.Find("Resource_Background_Panel");
            GameObject bgPanel;
            
            if (existingBg == null)
            {
                // Also check for old "Background" name
                existingBg = resourceObj.transform.Find("Background");
            }

            if (existingBg == null)
            {
                bgPanel = new GameObject("Resource_Background_Panel");
                bgPanel.transform.SetParent(resourceObj.transform, false);
                bgPanel.transform.SetAsFirstSibling();
                
                RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0, 0);
                bgRect.anchorMax = new Vector2(1, 1);
                bgRect.sizeDelta = new Vector2(20, 10); // Extra padding
                bgRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                bgPanel = existingBg.gameObject;
                bgPanel.name = "Resource_Background_Panel"; // Rename to standard
            }

            // 2. STYLE BACKGROUND - Cream, Opaque
            Image bgImage = bgPanel.GetComponent<Image>();
            if (bgImage == null) bgImage = bgPanel.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.color = new Color(CREAM_BG.r, CREAM_BG.g, CREAM_BG.b, 1f); // Full opacity
            bgImage.type = Image.Type.Sliced;
            bgImage.raycastTarget = false; // Don't block clicks
            EditorUtility.SetDirty(bgImage);

            // 3. ADD OUTLINE
            Outline bgOutline = bgPanel.GetComponent<Outline>();
            if (bgOutline == null) bgOutline = bgPanel.AddComponent<Outline>();
            bgOutline.effectColor = DARK_WOOD;
            bgOutline.effectDistance = new Vector2(2, -2);
            EditorUtility.SetDirty(bgOutline);

            // 4. ADD LAYOUT GROUP TO HUD PANEL
            HorizontalLayoutGroup hudLayout = bgPanel.GetComponent<HorizontalLayoutGroup>();
            if (hudLayout == null) hudLayout = bgPanel.AddComponent<HorizontalLayoutGroup>();
            hudLayout.spacing = 20f;
            hudLayout.padding = new RectOffset(15, 15, 5, 5);
            hudLayout.childAlignment = TextAnchor.MiddleCenter;
            hudLayout.childControlWidth = false;
            hudLayout.childControlHeight = false;
            hudLayout.childForceExpandWidth = false;
            hudLayout.childForceExpandHeight = false;
            EditorUtility.SetDirty(hudLayout);

            ContentSizeFitter hudFitter = bgPanel.GetComponent<ContentSizeFitter>();
            if (hudFitter == null) hudFitter = bgPanel.AddComponent<ContentSizeFitter>();
            hudFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            hudFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EditorUtility.SetDirty(hudFitter);

            Debug.Log("<color=orange>[UI Layout Fix]</color> HUD: Resource_Background_Panel styled with HorizontalLayoutGroup");

            // 5. PARENT RESOURCE DISPLAYS TO THE NEW PANEL
            SerializedObject so = new SerializedObject(resourceUI);
            string[] displayNames = { "warmwoodDisplay", "shardDisplay", "foodDisplay" };

            foreach (string displayName in displayNames)
            {
                SerializedProperty displayProp = so.FindProperty(displayName);
                if (displayProp == null) continue;

                SerializedProperty containerProp = displayProp.FindPropertyRelative("container");
                if (containerProp?.objectReferenceValue is RectTransform container)
                {
                    if (container.parent != bgPanel.transform)
                    {
                        container.SetParent(bgPanel.transform, false);
                        EditorUtility.SetDirty(container);
                    }
                }

                // Update text colors to brown
                SerializedProperty amountTextProp = displayProp.FindPropertyRelative("amountText");
                SerializedProperty maxTextProp = displayProp.FindPropertyRelative("maxText");

                if (amountTextProp?.objectReferenceValue is TextMeshProUGUI amountText)
                {
                    if (font != null) amountText.font = font;
                    amountText.color = DEEP_BROWN;
                    EditorUtility.SetDirty(amountText);
                }

                if (maxTextProp?.objectReferenceValue is TextMeshProUGUI maxText)
                {
                    if (font != null) maxText.font = font;
                    maxText.color = DEEP_BROWN;
                    EditorUtility.SetDirty(maxText);
                }
            }

            // 6. UPDATE ANY OTHER TEXT CHILDREN - Deep Brown
            TextMeshProUGUI[] allTexts = resourceObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            int extraTextCount = 0;
            foreach (var txt in allTexts)
            {
                if (txt.color != DEEP_BROWN)
                {
                    if (font != null) txt.font = font;
                    txt.color = DEEP_BROWN;
                    EditorUtility.SetDirty(txt);
                    extraTextCount++;
                }
            }

            so.ApplyModifiedProperties();
            Debug.Log($"<color=orange>[UI Layout Fix]</color> HUD: Resource items parented and text updated.");

            EditorUtility.SetDirty(resourceObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
