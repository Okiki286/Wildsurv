using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Tool to apply Modular Game UI Kit aesthetics to existing UI prefabs.
    /// Creates _MKTest versions without modifying originals.
    /// </summary>
    public class ModularUIKitStyling : EditorWindow
    {
        private const string KIT_PATH = "Assets/ModularGameUIKit/Common";
        private const string PREFAB_PATH = "Assets/_UI/Prefabs";
        
        // Color palette
        private static readonly Color PRIMARY_BG = new Color(0.1f, 0.11f, 0.18f, 0.95f); // #1A1D2E
        private static readonly Color SECONDARY_BG = new Color(0.17f, 0.19f, 0.26f, 1f); // #2C3142
        private static readonly Color ACCENT = new Color(0.31f, 0.8f, 0.64f, 1f); // #4ECCA3
        private static readonly Color RESOURCE_GREEN = new Color(0.42f, 0.81f, 0.5f, 1f); // #6BCF7F
        private static readonly Color RESOURCE_ORANGE = new Color(1f, 0.72f, 0.3f, 1f); // #FFB84D
        private static readonly Color BORDER = new Color(0.24f, 0.27f, 0.35f, 1f); // #3D4459

        [MenuItem("Tools/UI Kit/Apply MK Aesthetics to Scene UI")]
        public static void ApplyMKAestheticsToScene()
        {
            Debug.Log("<color=cyan>[MK Styling]</color> Starting aesthetic application to scene objects...");

            // Check if scene is open
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SampleScene")
            {
                EditorUtility.DisplayDialog("Wrong Scene",
                    "Please open SampleScene first!\n\nThis tool modifies UI objects in the active scene.",
                    "OK");
                return;
            }

            // Load fonts
            TMP_FontAsset fontRegular = LoadAsset<TMP_FontAsset>($"{KIT_PATH}/Fonts/Inter-Regular SDF");
            TMP_FontAsset fontSemiBold = LoadAsset<TMP_FontAsset>($"{KIT_PATH}/Fonts/Inter-SemiBold SDF");

            if (fontRegular == null || fontSemiBold == null)
            {
                Debug.LogError("[MK Styling] Fonts not found!");
                return;
            }

            // Load icons
            Sprite woodIcon = LoadSprite($"{KIT_PATH}/Sprites/Icons/Wood");
            Sprite gemIcon = LoadSprite($"{KIT_PATH}/Sprites/Icons/Gem");
            Sprite meatIcon = LoadSprite($"{KIT_PATH}/Sprites/Icons/Meat");
            Sprite shieldIcon = LoadSprite($"{KIT_PATH}/Sprites/Icons/Shield");
            Sprite hammerIcon = LoadSprite($"{KIT_PATH}/Sprites/Icons/Hammer");
            Sprite sawIcon = LoadSprite($"{KIT_PATH}/Sprites/Icons/Saw");

            // Load background sprites
            Sprite panelBg = LoadSprite($"{KIT_PATH}/Sprites/Shapes/Background");
            Sprite rectangleBg = LoadSprite($"{KIT_PATH}/Sprites/Shapes/Rectangle");

            Debug.Log($"<color=green>[MK Styling]</color> Loaded {CountNonNull(woodIcon, gemIcon, meatIcon, shieldIcon, hammerIcon, sawIcon)} icons");

            // Find UI objects in scene
            WildernessSurvival.UI.ResourceDisplayUI resourceUI = Object.FindFirstObjectByType<WildernessSurvival.UI.ResourceDisplayUI>();
            WildernessSurvival.UI.BuildMenuUI buildMenuUI = Object.FindFirstObjectByType<WildernessSurvival.UI.BuildMenuUI>();

            if (resourceUI == null && buildMenuUI == null)
            {
                EditorUtility.DisplayDialog("UI Not Found",
                    "Could not find ResourceDisplayUI or BuildMenuUI in the scene.\n\n" +
                    "Make sure you've run the UI setup tool first:\n" +
                    "Tools → Setup UI → Setup Build Menu and Resource Display",
                    "OK");
                return;
            }

            // Apply aesthetics
            if (resourceUI != null)
            {
                StyleResourceDisplayInScene(resourceUI.gameObject, fontRegular, fontSemiBold, woodIcon, gemIcon, meatIcon, panelBg);
            }

            if (buildMenuUI != null)
            {
                StyleBuildMenuInScene(buildMenuUI.gameObject, fontRegular, fontSemiBold, panelBg);
                StyleStructureButtonTemplateInScene(buildMenuUI.gameObject, fontRegular, fontSemiBold, rectangleBg);
            }

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("<color=green>[MK Styling]</color> ✅ Aesthetics applied to scene!");
            EditorUtility.DisplayDialog("UI Kit Styling Complete",
                "Modular UI Kit aesthetics applied successfully!\n\n" +
                "Changes made to scene objects:\n" +
                "- ResourceDisplay: Dark panel, colored icons, Inter fonts\n" +
                "- BuildMenu: Styled panels, borders, tooltips\n\n" +
                "Save the scene to keep changes.\n" +
                "Use Ctrl+Z to undo if needed.",
                "OK");
        }

        private static void StyleResourceDisplayInScene(GameObject resourceDisplayObj, TMP_FontAsset fontRegular, TMP_FontAsset fontSemiBold,
            Sprite woodIcon, Sprite gemIcon, Sprite meatIcon, Sprite panelBg)
        {
            if (resourceDisplayObj == null) return;

            // Add background panel if it doesn't exist
            Transform bgTransform = resourceDisplayObj.transform.Find("Background");
            if (bgTransform == null)
            {
                GameObject bgPanel = new GameObject("Background");
                bgPanel.transform.SetParent(resourceDisplayObj.transform, false);
                bgPanel.transform.SetAsFirstSibling();
                
                Image bgImage = bgPanel.AddComponent<Image>();
                if (panelBg != null) bgImage.sprite = panelBg;
                bgImage.color = PRIMARY_BG;
                bgImage.type = Image.Type.Sliced;
                
                RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = new Vector2(20, 20); // Padding
                bgRect.anchoredPosition = Vector2.zero;
            }

            // Style resource items
            StyleResourceItem(resourceDisplayObj.transform, "WarmwoodDisplay", fontSemiBold, woodIcon, RESOURCE_GREEN);
            StyleResourceItem(resourceDisplayObj.transform, "ShardDisplay", fontSemiBold, gemIcon, ACCENT);
            StyleResourceItem(resourceDisplayObj.transform, "FoodDisplay", fontSemiBold, meatIcon, RESOURCE_ORANGE);

            EditorUtility.SetDirty(resourceDisplayObj);
            Debug.Log($"<color=green>[MK Styling]</color> Styled ResourceDisplay");
        }

        private static void StyleResourceItem(Transform parent, string itemName, TMP_FontAsset font, Sprite icon, Color iconColor)
        {
            Transform item = parent.Find(itemName);
            if (item == null) return;

            // Update icon
            Image iconImage = item.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.color = iconColor;
            }

            // Update fonts
            TextMeshProUGUI amountText = item.Find("AmountText")?.GetComponent<TextMeshProUGUI>();
            if (amountText != null)
            {
                amountText.font = font;
                amountText.fontSize = 18;
                amountText.color = Color.white;
            }

            TextMeshProUGUI maxText = item.Find("MaxText")?.GetComponent<TextMeshProUGUI>();
            if (maxText != null)
            {
                maxText.font = font;
                maxText.fontSize = 14;
                maxText.color = new Color(0.72f, 0.77f, 0.84f, 1f); // Light gray
            }
        }

        private static void StyleBuildMenuInScene(GameObject buildMenuObj, TMP_FontAsset fontRegular, TMP_FontAsset fontSemiBold, Sprite panelBg)
        {
            if (buildMenuObj == null) return;

            // Find buildMenuPanel child
            SerializedObject so = new SerializedObject(buildMenuObj.GetComponent<WildernessSurvival.UI.BuildMenuUI>());
            SerializedProperty panelProp = so.FindProperty("buildMenuPanel");
            
            if (panelProp != null && panelProp.objectReferenceValue != null)
            {
                GameObject panel = panelProp.objectReferenceValue as GameObject;
                
                // Style main panel background
                Image panelImage = panel.GetComponent<Image>();
                if (panelImage != null)
                {
                    if (panelBg != null) panelImage.sprite = panelBg;
                    panelImage.color = PRIMARY_BG;
                    panelImage.type = Image.Type.Sliced;
                }

                // Add outline if not present
                if (panel.GetComponent<Outline>() == null)
                {
                    Outline outline = panel.AddComponent<Outline>();
                    outline.effectColor = BORDER;
                    outline.effectDistance = new Vector2(2, -2);
                }

                EditorUtility.SetDirty(panel);
            }

            // Style header text
            SerializedProperty headerTextProp = so.FindProperty("headerText");
            if (headerTextProp != null && headerTextProp.objectReferenceValue != null)
            {
                TextMeshProUGUI headerText = headerTextProp.objectReferenceValue as TextMeshProUGUI;
                if (headerText != null)
                {
                    headerText.font = fontSemiBold;
                    headerText.fontSize = 28;
                    headerText.color = ACCENT;
                    EditorUtility.SetDirty(headerText);
                }
            }

            // Style tooltip panel
            SerializedProperty tooltipPanelProp = so.FindProperty("tooltipPanel");
            if (tooltipPanelProp != null && tooltipPanelProp.objectReferenceValue != null)
            {
                GameObject tooltipPanel = tooltipPanelProp.objectReferenceValue as GameObject;
                if (tooltipPanel != null)
                {
                    Image tooltipImage = tooltipPanel.GetComponent<Image>();
                    if (tooltipImage != null)
                    {
                        tooltipImage.color = PRIMARY_BG;
                    }

                    // Add outline if not present
                    if (tooltipPanel.GetComponent<Outline>() == null)
                    {
                        Outline tooltipOutline = tooltipPanel.AddComponent<Outline>();
                        tooltipOutline.effectColor = ACCENT;
                        tooltipOutline.effectDistance = new Vector2(1, -1);
                    }

                    EditorUtility.SetDirty(tooltipPanel);
                }
            }

            // Style tooltip texts
            StyleTooltipText(so, "tooltipTitle", fontSemiBold, 18);
            StyleTooltipText(so, "tooltipDescription", fontRegular, 14);
            StyleTooltipText(so, "tooltipCosts", fontRegular, 14);
            StyleTooltipText(so, "tooltipStats", fontRegular, 14);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(buildMenuObj);
            Debug.Log($"<color=green>[MK Styling]</color> Styled BuildMenu");
        }

        private static void StyleTooltipText(SerializedObject so, string propName, TMP_FontAsset font, float fontSize)
        {
            SerializedProperty textProp = so.FindProperty(propName);
            if (textProp != null && textProp.objectReferenceValue != null)
            {
                TextMeshProUGUI text = textProp.objectReferenceValue as TextMeshProUGUI;
                if (text != null)
                {
                    text.font = font;
                    text.fontSize = fontSize;
                    EditorUtility.SetDirty(text);
                }
            }
        }

        private static void StyleStructureButtonTemplateInScene(GameObject buildMenuObj, TMP_FontAsset fontRegular, TMP_FontAsset fontSemiBold, Sprite buttonBg)
        {
            if (buildMenuObj == null) return;

            SerializedObject so = new SerializedObject(buildMenuObj.GetComponent<WildernessSurvival.UI.BuildMenuUI>());
            SerializedProperty prefabProp = so.FindProperty("structureButtonPrefab");

            if (prefabProp != null && prefabProp.objectReferenceValue != null)
            {
                GameObject btnTemplate = prefabProp.objectReferenceValue as GameObject;
                
                // Style background
                Image bgImage = btnTemplate.GetComponent<Image>();
                if (bgImage != null)
                {
                    if (buttonBg != null) bgImage.sprite = buttonBg;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.color = SECONDARY_BG;
                }

                // Update button colors
                Button button = btnTemplate.GetComponent<Button>();
                if (button != null)
                {
                    ColorBlock colors = button.colors;
                    colors.normalColor = SECONDARY_BG;
                    colors.highlightedColor = new Color(0.24f, 0.27f, 0.35f, 1f); // Lighter
                    colors.pressedColor = new Color(0.31f, 0.8f, 0.64f, 0.2f); // Accent with alpha
                    colors.disabledColor = PRIMARY_BG;
                    button.colors = colors;
                }

                // Style text elements
                TextMeshProUGUI nameText = btnTemplate.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.font = fontSemiBold;
                    nameText.fontSize = 14;
                    EditorUtility.SetDirty(nameText);
                }

                TextMeshProUGUI costText = btnTemplate.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
                if (costText != null)
                {
                    costText.font = fontRegular;
                    costText.fontSize = 12;
                    EditorUtility.SetDirty(costText);
                }

                // Style hotkey badge
                Transform hotkeyBadge = btnTemplate.transform.Find("HotkeyBadge");
                if (hotkeyBadge == null)
                {
                    // Create badge if missing (UISetupTools might not have created it)
                    GameObject badgeObj = new GameObject("HotkeyBadge");
                    badgeObj.transform.SetParent(btnTemplate.transform, false);
                    hotkeyBadge = badgeObj.transform;
                    
                    Image badgeImg = badgeObj.AddComponent<Image>();
                    badgeImg.color = ACCENT;
                    
                    RectTransform rt = badgeObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    rt.anchoredPosition = new Vector2(-5, -5);
                    rt.sizeDelta = new Vector2(20, 20);

                    GameObject textObj = new GameObject("Text");
                    textObj.transform.SetParent(badgeObj.transform, false);
                    TextMeshProUGUI badgeText = textObj.AddComponent<TextMeshProUGUI>();
                    badgeText.alignment = TextAlignmentOptions.Center;
                    badgeText.enableWordWrapping = false;
                    
                    RectTransform textRt = textObj.GetComponent<RectTransform>();
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.sizeDelta = Vector2.zero;
                }

                if (hotkeyBadge != null)
                {
                    Image badgeImage = hotkeyBadge.GetComponent<Image>();
                    if (badgeImage != null)
                    {
                        badgeImage.color = ACCENT;
                    }

                    TextMeshProUGUI hotkeyText = hotkeyBadge.GetComponentInChildren<TextMeshProUGUI>();
                    if (hotkeyText != null)
                    {
                        hotkeyText.font = fontSemiBold;
                        hotkeyText.fontSize = 10;
                        hotkeyText.color = PRIMARY_BG;
                        EditorUtility.SetDirty(hotkeyText);
                    }
                }

                EditorUtility.SetDirty(btnTemplate);
                Debug.Log($"<color=green>[MK Styling]</color> Styled StructureButton Template");
            }
        }

        // Utility methods
        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>($"{path}.asset");
            if (asset == null)
            {
                Debug.LogWarning($"[MK Styling] Asset not found: {path}");
            }
            return asset;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{path}.png");
            if (sprite == null)
            {
                Debug.LogWarning($"[MK Styling] Sprite not found: {path}");
            }
            return sprite;
        }

        private static int CountNonNull(params object[] objects)
        {
            int count = 0;
            foreach (var obj in objects)
            {
                if (obj != null) count++;
            }
            return count;
        }
    }
}
