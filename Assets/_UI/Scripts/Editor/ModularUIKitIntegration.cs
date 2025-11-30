using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.UI;

namespace WildernessSurvival.EditorTools
{
    public class ModularUIKitIntegration : EditorWindow
    {
        [MenuItem("Tools/UI Kit/Apply Modular UI Kit Aesthetics")]
        public static void ApplyUIKitAesthetics()
        {
            Debug.Log("<color=cyan>[UI Kit Integration]</color> Starting aesthetic application...");

            // Paths to UI Kit assets
            string fontPath = "Assets/ModularGameUIKit/Common/Fonts/Inter-SemiBold SDF";
            string fontBoldPath = "Assets/ModularGameUIKit/Common/Fonts/Inter-Regular SDF";
            
            // Icon paths
            string woodIconPath = "Assets/ModularGameUIKit/Common/Sprites/Icons/Wood";
            string gemIconPath = "Assets/ModularGameUIKit/Common/Sprites/Icons/Gem";
            string meatIconPath = "Assets/ModularGameUIKit/Common/Sprites/Icons/Meat";
            string shieldIconPath = "Assets/ModularGameUIKit/Common/Sprites/Icons/Shield";
            string sawIconPath = "Assets/ModularGameUIKit/Common/Sprites/Icons/Saw";
            string hammerIconPath = "Assets/ModularGameUIKit/Common/Sprites/Icons/Hammer";
            
            // Background shapes
            string panelBgPath = "Assets/ModularGameUIKit/Common/Sprites/Shapes/Background";
            string buttonBgPath = "Assets/ModularGameUIKit/Common/Sprites/Shapes/Rectangle";

            // Load assets
            TMP_FontAsset fontSemiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath + ".asset");
            TMP_FontAsset fontRegular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontBoldPath + ".asset");
            
            Sprite woodIcon = LoadSprite(woodIconPath);
            Sprite gemIcon = LoadSprite(gemIconPath);
            Sprite meatIcon = LoadSprite(meatIconPath);
            Sprite shieldIcon = LoadSprite(shieldIconPath);
            Sprite sawIcon = LoadSprite(sawIconPath);
            Sprite hammerIcon = LoadSprite(hammerIconPath);
            
            Sprite panelBg = LoadSprite(panelBgPath);
            Sprite buttonBg = LoadSprite(buttonBgPath);

            if (fontSemiBold == null || fontRegular == null)
            {
                Debug.LogError("[UI Kit] Fonts not found! Check paths.");
                return;
            }

            Debug.Log($"<color=green>[UI Kit]</color> Loaded fonts and {CountNonNull(woodIcon, gemIcon, meatIcon)} icons");

            // Apply to scene objects
            ApplyToResourceDisplay(fontRegular, woodIcon, gemIcon, meatIcon);
            ApplyToBuildMenu(fontSemiBold, fontRegular, panelBg, buttonBg);

            Debug.Log("<color=green>[UI Kit Integration]</color> ✅ Aesthetic application complete!");
            EditorUtility.DisplayDialog("UI Kit Integration", 
                "Modular UI Kit aesthetics applied successfully!\n\n" +
                "Check the scene for updated visuals.\n" +
                "Original logic preserved.", "OK");
        }

        private static void ApplyToResourceDisplay(TMP_FontAsset font, Sprite woodIcon, Sprite gemIcon, Sprite meatIcon)
        {
            ResourceDisplayUI resourceUI = Object.FindFirstObjectByType<ResourceDisplayUI>();
            if (resourceUI == null)
            {
                Debug.LogWarning("[UI Kit] ResourceDisplayUI not found in scene");
                return;
            }

            // Use SerializedObject to access private fields
            SerializedObject so = new SerializedObject(resourceUI);
            
            // Update Warmwood display
            ApplyResourceItemAesthetics(so.FindProperty("warmwoodDisplay"), font, woodIcon, new Color(0.4f, 0.8f, 0.4f));
            
            // Update Shard display
            ApplyResourceItemAesthetics(so.FindProperty("shardDisplay"), font, gemIcon, new Color(0.4f, 0.8f, 1f));
            
            // Update Food display
            ApplyResourceItemAesthetics(so.FindProperty("foodDisplay"), font, meatIcon, new Color(1f, 0.6f, 0.4f));

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(resourceUI);
            
            Debug.Log("<color=green>[UI Kit]</color> ResourceDisplay aesthetics updated");
        }

        private static void ApplyResourceItemAesthetics(SerializedProperty itemProp, TMP_FontAsset font, Sprite icon, Color iconColor)
        {
            if (itemProp == null) return;

            // Update icon
            SerializedProperty iconImageProp = itemProp.FindPropertyRelative("iconImage");
            if (iconImageProp != null && iconImageProp.objectReferenceValue != null)
            {
                Image iconImage = iconImageProp.objectReferenceValue as Image;
                if (iconImage != null && icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.color = iconColor;
                    EditorUtility.SetDirty(iconImage);
                }
            }

            // Update fonts
            UpdateTextFont(itemProp.FindPropertyRelative("amountText"), font);
            UpdateTextFont(itemProp.FindPropertyRelative("maxText"), font);
        }

        private static void ApplyToBuildMenu(TMP_FontAsset fontSemiBold, TMP_FontAsset fontRegular, Sprite panelBg, Sprite buttonBg)
        {
            BuildMenuUI buildMenuUI = Object.FindFirstObjectByType<BuildMenuUI>();
            if (buildMenuUI == null)
            {
                Debug.LogWarning("[UI Kit] BuildMenuUI not found in scene");
                return;
            }

            SerializedObject so = new SerializedObject(buildMenuUI);
            
            // Update header text
            SerializedProperty headerTextProp = so.FindProperty("headerText");
            UpdateTextFont(headerTextProp, fontSemiBold);
            
            // Update panel background
            SerializedProperty panelProp = so.FindProperty("buildMenuPanel");
            if (panelProp != null && panelProp.objectReferenceValue != null && panelBg != null)
            {
                GameObject panelObj = panelProp.objectReferenceValue as GameObject;
                Image panelImage = panelObj?.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.sprite = panelBg;
                    panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f); // Dark blue-gray
                    EditorUtility.SetDirty(panelImage);
                }
            }

            // Update tooltip fonts
            UpdateTextFont(so.FindProperty("tooltipTitle"), fontSemiBold);
            UpdateTextFont(so.FindProperty("tooltipDescription"), fontRegular);
            UpdateTextFont(so.FindProperty("tooltipCosts"), fontRegular);
            UpdateTextFont(so.FindProperty("tooltipStats"), fontRegular);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(buildMenuUI);
            
            Debug.Log("<color=green>[UI Kit]</color> BuildMenu aesthetics updated");
        }

        private static void UpdateTextFont(SerializedProperty textProp, TMP_FontAsset font)
        {
            if (textProp == null || textProp.objectReferenceValue == null || font == null) return;
            
            TextMeshProUGUI text = textProp.objectReferenceValue as TextMeshProUGUI;
            if (text != null)
            {
                text.font = font;
                EditorUtility.SetDirty(text);
            }
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path + ".png");
            if (sprite == null)
            {
                Debug.LogWarning($"[UI Kit] Sprite not found: {path}");
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

        [MenuItem("Tools/UI Kit/Revert to Original UI")]
        public static void RevertToOriginal()
        {
            if (EditorUtility.DisplayDialog("Revert UI", 
                "This will revert all UI changes back to the original.\n\nContinue?", 
                "Yes", "Cancel"))
            {
                Debug.Log("<color=yellow>[UI Kit]</color> Revert functionality not yet implemented.");
                Debug.Log("<color=yellow>[UI Kit]</color> To revert: Reload the scene or use Undo (Ctrl+Z)");
            }
        }
    }
}
