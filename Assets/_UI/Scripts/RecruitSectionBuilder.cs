using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Editor helper to create and setup the RecruitSection UI hierarchy.
    /// Add this to the WorkerAssignmentPanel and click the button to auto-generate.
    /// Remove after setup is complete.
    /// </summary>
    public class RecruitSectionBuilder : MonoBehaviour
    {
#if UNITY_EDITOR
        [TitleGroup("Recruit Section Builder")]
        [InfoBox("This helper creates the complete RecruitSection UI hierarchy.\n" +
                 "Click the button below, then REMOVE this component after setup.", InfoMessageType.Info)]
        
        [SerializeField] private RectTransform parentContainer;
        
        [TitleGroup("Style Settings")]
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Color backgroundColor = new Color(0.15f, 0.2f, 0.15f, 0.95f);
        [SerializeField] private Color buttonColor = new Color(0.2f, 0.5f, 0.3f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private int iconSize = 24;
        [SerializeField] private int fontSize = 18;

        [TitleGroup("Actions")]
        [Button("🔧 Create Recruit Section", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        private void CreateRecruitSection()
        {
            if (parentContainer == null)
            {
                parentContainer = GetComponent<RectTransform>();
            }

            // Check if already exists
            Transform existing = parentContainer.Find("RecruitSection");
            if (existing != null)
            {
                Debug.LogWarning("[RecruitSectionBuilder] RecruitSection already exists! Delete it first if you want to recreate.");
                UnityEditor.Selection.activeGameObject = existing.gameObject;
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // CREATE RECRUIT SECTION CONTAINER
            // ═══════════════════════════════════════════════════════════

            GameObject recruitSection = CreateUIElement("RecruitSection", parentContainer);
            RectTransform sectionRect = recruitSection.GetComponent<RectTransform>();
            
            // Position at bottom of panel
            sectionRect.anchorMin = new Vector2(0, 0);
            sectionRect.anchorMax = new Vector2(1, 0);
            sectionRect.pivot = new Vector2(0.5f, 0);
            sectionRect.anchoredPosition = new Vector2(0, 20);
            sectionRect.sizeDelta = new Vector2(-40, 80); // padding from edges

            // Background
            Image sectionBg = recruitSection.AddComponent<Image>();
            sectionBg.color = backgroundColor;

            // Horizontal layout
            HorizontalLayoutGroup layout = recruitSection.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // ═══════════════════════════════════════════════════════════
            // CREATE RECRUIT BUTTON
            // ═══════════════════════════════════════════════════════════

            GameObject buttonObj = CreateUIElement("RecruitButton", sectionRect);
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(180, 50);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = buttonColor;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Button text
            GameObject buttonTextObj = CreateUIElement("ButtonText", buttonRect);
            RectTransform textRect = buttonTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Recruit Worker";
            buttonText.fontSize = fontSize;
            buttonText.color = textColor;
            buttonText.alignment = TextAlignmentOptions.Center;
            if (font != null) buttonText.font = font;

            // ═══════════════════════════════════════════════════════════
            // CREATE COST DISPLAY (Icon + Number)
            // ═══════════════════════════════════════════════════════════

            GameObject costContainer = CreateUIElement("CostContainer", sectionRect);
            RectTransform costRect = costContainer.GetComponent<RectTransform>();
            costRect.sizeDelta = new Vector2(80, 50);

            HorizontalLayoutGroup costLayout = costContainer.AddComponent<HorizontalLayoutGroup>();
            costLayout.spacing = 5;
            costLayout.childAlignment = TextAnchor.MiddleCenter;
            costLayout.childControlWidth = false;
            costLayout.childControlHeight = false;
            costLayout.childForceExpandWidth = false;
            costLayout.childForceExpandHeight = false;

            // ═══════════════════════════════════════════════════════════
            // CREATE COST ICON (the placeholder Image you need!)
            // ═══════════════════════════════════════════════════════════

            GameObject iconObj = CreateUIElement("CostIcon", costRect);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            Image costIcon = iconObj.AddComponent<Image>();
            costIcon.color = textColor;
            costIcon.preserveAspect = true;
            // Sprite will be set dynamically by RecruitUI

            // ═══════════════════════════════════════════════════════════
            // CREATE COST TEXT
            // ═══════════════════════════════════════════════════════════

            GameObject costTextObj = CreateUIElement("CostText", costRect);
            RectTransform costTextRect = costTextObj.GetComponent<RectTransform>();
            costTextRect.sizeDelta = new Vector2(50, 30);

            TextMeshProUGUI costText = costTextObj.AddComponent<TextMeshProUGUI>();
            costText.text = "70";
            costText.fontSize = fontSize + 2;
            costText.fontStyle = FontStyles.Bold;
            costText.color = textColor;
            costText.alignment = TextAlignmentOptions.Left;
            if (font != null) costText.font = font;

            // ═══════════════════════════════════════════════════════════
            // ADD RECRUIT UI COMPONENT
            // ═══════════════════════════════════════════════════════════

            RecruitUI recruitUI = recruitSection.AddComponent<RecruitUI>();

            // Use SerializedObject to set private serialized fields
            var so = new UnityEditor.SerializedObject(recruitUI);
            so.FindProperty("recruitButton").objectReferenceValue = button;
            so.FindProperty("buttonText").objectReferenceValue = buttonText;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.FindProperty("costIconImage").objectReferenceValue = costIcon;
            so.ApplyModifiedProperties();

            // ═══════════════════════════════════════════════════════════
            // WIRE UP WORKER ASSIGNMENT UI
            // ═══════════════════════════════════════════════════════════

            WorkerAssignmentUI workerUI = GetComponent<WorkerAssignmentUI>();
            if (workerUI != null)
            {
                var workerSO = new UnityEditor.SerializedObject(workerUI);
                workerSO.FindProperty("recruitSection").objectReferenceValue = recruitSection;
                workerSO.FindProperty("recruitUIComponent").objectReferenceValue = recruitUI;
                workerSO.ApplyModifiedProperties();

                Debug.Log("<color=green>[RecruitSectionBuilder]</color> ✓ WorkerAssignmentUI wired up!");
            }

            // Mark prefab dirty
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);

            Debug.Log("<color=green>[RecruitSectionBuilder]</color> ✓ RecruitSection created successfully!\n" +
                      "Structure:\n" +
                      "  └─ RecruitSection\n" +
                      "      ├─ RecruitButton\n" +
                      "      │   └─ ButtonText\n" +
                      "      └─ CostContainer\n" +
                      "          ├─ CostIcon (Image - dynamic sprite)\n" +
                      "          └─ CostText\n\n" +
                      "You can now REMOVE this RecruitSectionBuilder component.");

            UnityEditor.Selection.activeGameObject = recruitSection;
        }

        private GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        [Button("🗑️ Delete Existing RecruitSection", ButtonSizes.Medium), GUIColor(0.8f, 0.4f, 0.4f)]
        private void DeleteExisting()
        {
            if (parentContainer == null)
            {
                parentContainer = GetComponent<RectTransform>();
            }

            Transform existing = parentContainer.Find("RecruitSection");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
                Debug.Log("[RecruitSectionBuilder] RecruitSection deleted.");
            }
            else
            {
                Debug.Log("[RecruitSectionBuilder] No RecruitSection found.");
            }
        }
#endif
    }
}
