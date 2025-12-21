using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace WildernessSurvival.Core.Editor
{
    /// <summary>
    /// Editor tool per aggiungere la sezione Recruit all'interno del WorkerAssignmentUI panel.
    /// </summary>
    public static class WorkerAssignmentRecruitSetup
    {
        [MenuItem("Tools/Wilderness/Population/Add Recruit Section to WorkerAssignmentUI")]
        public static void AddRecruitSectionToWorkerAssignmentUI()
        {
            // 1. Trova il WorkerAssignmentUI nella scena o chiedi di aprire il prefab
            var workerAssignmentUI = Object.FindFirstObjectByType<UI.WorkerAssignmentUI>();
            
            if (workerAssignmentUI == null)
            {
                Debug.LogError("[RecruitSetup] WorkerAssignmentUI not found in scene! " +
                    "Open the prefab or ensure it's in the scene.");
                return;
            }

            // 2. Trova il pannello principale (assignmentPanel)
            Transform panelTransform = workerAssignmentUI.transform.Find("AssignmentPanel");
            if (panelTransform == null)
            {
                // Fallback: usa il primo figlio
                if (workerAssignmentUI.transform.childCount > 0)
                {
                    panelTransform = workerAssignmentUI.transform.GetChild(0);
                }
                else
                {
                    Debug.LogError("[RecruitSetup] No AssignmentPanel found!");
                    return;
                }
            }

            // 3. Verifica se esiste già
            var existingSection = panelTransform.Find("RecruitSection");
            if (existingSection != null)
            {
                Debug.Log("[RecruitSetup] RecruitSection already exists! Selecting it.");
                Selection.activeGameObject = existingSection.gameObject;
                return;
            }

            // 4. Crea RecruitSection
            GameObject recruitSection = new GameObject("RecruitSection");
            recruitSection.transform.SetParent(panelTransform, false);
            
            RectTransform sectionRect = recruitSection.AddComponent<RectTransform>();
            sectionRect.anchorMin = new Vector2(0, 1);
            sectionRect.anchorMax = new Vector2(1, 1);
            sectionRect.pivot = new Vector2(0.5f, 1);
            sectionRect.anchoredPosition = new Vector2(0, -560); // Sotto AvailableWorkersContainer
            sectionRect.sizeDelta = new Vector2(-100, 70);
            
            // Background
            Image sectionBg = recruitSection.AddComponent<Image>();
            sectionBg.color = new Color(0.15f, 0.35f, 0.25f, 0.9f); // Verde scuro
            
            // Horizontal Layout
            HorizontalLayoutGroup layout = recruitSection.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // 5. Crea Button
            GameObject buttonObj = new GameObject("RecruitButton");
            buttonObj.transform.SetParent(recruitSection.transform, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 50);
            
            Image buttonImg = buttonObj.AddComponent<Image>();
            buttonImg.color = new Color(0.2f, 0.6f, 0.3f, 1f);
            
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImg;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.3f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.4f);
            colors.pressedColor = new Color(0.15f, 0.5f, 0.25f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
            button.colors = colors;

            // Button Text
            GameObject buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Recruit Worker";
            buttonText.fontSize = 20;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            // 6. Crea Cost Section
            GameObject costSection = new GameObject("CostSection");
            costSection.transform.SetParent(recruitSection.transform, false);
            
            RectTransform costRect = costSection.AddComponent<RectTransform>();
            costRect.sizeDelta = new Vector2(100, 50);
            
            HorizontalLayoutGroup costLayout = costSection.AddComponent<HorizontalLayoutGroup>();
            costLayout.spacing = 5;
            costLayout.childAlignment = TextAnchor.MiddleCenter;
            costLayout.childForceExpandWidth = false;
            costLayout.childForceExpandHeight = false;

            // Food Icon placeholder
            GameObject iconObj = new GameObject("FoodIcon");
            iconObj.transform.SetParent(costSection.transform, false);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(32, 32);
            
            Image foodIcon = iconObj.AddComponent<Image>();
            foodIcon.color = new Color(1f, 0.8f, 0.2f); // Giallo placeholder

            // Cost Text
            GameObject costTextObj = new GameObject("CostText");
            costTextObj.transform.SetParent(costSection.transform, false);
            
            RectTransform costTextRect = costTextObj.AddComponent<RectTransform>();
            costTextRect.sizeDelta = new Vector2(60, 40);
            
            TextMeshProUGUI costText = costTextObj.AddComponent<TextMeshProUGUI>();
            costText.text = "40";
            costText.fontSize = 24;
            costText.alignment = TextAlignmentOptions.Left;
            costText.color = new Color(1f, 0.9f, 0.4f); // Giallo oro

            // 7. Aggiungi RecruitUI component
            var recruitUI = recruitSection.AddComponent<UI.RecruitUI>();

            // 8. Assegna riferimenti via SerializedObject
            SerializedObject so = new SerializedObject(recruitUI);
            so.FindProperty("recruitButton").objectReferenceValue = button;
            so.FindProperty("buttonText").objectReferenceValue = buttonText;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.FindProperty("foodIcon").objectReferenceValue = foodIcon;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 9. Aggiorna WorkerAssignmentUI references
            SerializedObject uiSo = new SerializedObject(workerAssignmentUI);
            uiSo.FindProperty("recruitSection").objectReferenceValue = recruitSection;
            uiSo.FindProperty("recruitUIComponent").objectReferenceValue = recruitUI;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            // 10. Seleziona e ping
            Selection.activeGameObject = recruitSection;
            EditorGUIUtility.PingObject(recruitSection);

            Debug.Log("<color=green>[RecruitSetup]</color> ✅ RecruitSection added to WorkerAssignmentUI!\n" +
                "• Button: RecruitButton\n" +
                "• CostText assigned\n" +
                "• RecruitUI component configured\n" +
                "• WorkerAssignmentUI references updated\n\n" +
                "⚠️ Remember to SAVE the prefab! (Ctrl+S or Apply)");
        }
    }
}
