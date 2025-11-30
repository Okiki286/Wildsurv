using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.UI;

namespace WildernessSurvival.EditorTools
{
    public class UISetupTools : EditorWindow
    {
        [MenuItem("Tools/Setup UI/Setup Build Menu and Resource Display")]
        public static void SetupUI()
        {
            // 1. Trova o crea Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            // 2. Setup Build Menu
            SetupBuildMenu(canvas.transform);

            // 3. Setup Resource Display
            SetupResourceDisplay(canvas.transform);

            Debug.Log("<color=green>UI Setup Complete!</color>");
        }

        private static void SetupBuildMenu(Transform parent)
        {
            // Crea oggetto principale
            GameObject buildMenuObj = FindOrCreateChild(parent, "BuildMenu");
            BuildMenuUI buildMenuUI = buildMenuObj.GetComponent<BuildMenuUI>();
            if (buildMenuUI == null) buildMenuUI = buildMenuObj.AddComponent<BuildMenuUI>();

            // Crea gerarchia
            GameObject panel = FindOrCreateChild(buildMenuObj.transform, "Panel");
            CreateImage(panel, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            SetFullScreen(panel.GetComponent<RectTransform>());

            GameObject header = FindOrCreateChild(panel.transform, "Header");
            TextMeshProUGUI headerText = CreateText(header, "BUILD MENU", 36);
            headerText.alignment = TextAlignmentOptions.Center;
            header.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);

            GameObject containerObj = FindOrCreateChild(panel.transform, "ButtonsContainer");
            GridLayoutGroup grid = containerObj.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = containerObj.AddComponent<GridLayoutGroup>();
            
            grid.cellSize = new Vector2(100, 120);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            containerObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            containerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 400);

            // Crea Prefab Bottone (Template)
            GameObject btnTemplate = FindOrCreateChild(buildMenuObj.transform, "StructureButtonTemplate");
            BuildMenuButton btnScript = btnTemplate.GetComponent<BuildMenuButton>();
            if (btnScript == null) btnScript = btnTemplate.AddComponent<BuildMenuButton>();
            
            Image btnBg = CreateImage(btnTemplate, new Color(0.2f, 0.2f, 0.25f));
            GameObject iconObj = FindOrCreateChild(btnTemplate.transform, "Icon");
            Image iconImg = CreateImage(iconObj, Color.white);
            iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);

            GameObject nameObj = FindOrCreateChild(btnTemplate.transform, "NameText");
            TextMeshProUGUI nameTxt = CreateText(nameObj, "Structure", 14);
            nameObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);

            GameObject costObj = FindOrCreateChild(btnTemplate.transform, "CostText");
            TextMeshProUGUI costTxt = CreateText(costObj, "Cost", 12);
            costObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);

            btnTemplate.SetActive(false); // Template disattivato

            // Assegna riferimenti via SerializedObject
            SerializedObject so = new SerializedObject(buildMenuUI);
            so.FindProperty("buildMenuPanel").objectReferenceValue = panel;
            so.FindProperty("structureButtonsContainer").objectReferenceValue = containerObj.transform;
            so.FindProperty("structureButtonPrefab").objectReferenceValue = btnTemplate;
            so.FindProperty("headerText").objectReferenceValue = headerText;
            so.ApplyModifiedProperties();
        }

        private static void SetupResourceDisplay(Transform parent)
        {
            GameObject resDisplayObj = FindOrCreateChild(parent, "ResourceDisplay");
            ResourceDisplayUI resUI = resDisplayObj.GetComponent<ResourceDisplayUI>();
            if (resUI == null) resUI = resDisplayObj.AddComponent<ResourceDisplayUI>();

            // Layout orizzontale in alto a sinistra
            HorizontalLayoutGroup layout = resDisplayObj.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = resDisplayObj.AddComponent<HorizontalLayoutGroup>();
            
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 0, 20, 0);
            
            RectTransform rt = resDisplayObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(600, 100);

            // Crea items
            SerializedObject so = new SerializedObject(resUI);
            
            SetupResourceItem(resDisplayObj.transform, "WarmwoodDisplay", so.FindProperty("warmwoodDisplay"), Color.green);
            SetupResourceItem(resDisplayObj.transform, "ShardDisplay", so.FindProperty("shardDisplay"), Color.cyan);
            SetupResourceItem(resDisplayObj.transform, "FoodDisplay", so.FindProperty("foodDisplay"), Color.red);

            so.ApplyModifiedProperties();
        }

        private static void SetupResourceItem(Transform parent, string name, SerializedProperty prop, Color color)
        {
            GameObject itemObj = FindOrCreateChild(parent, name);
            
            // Assicura che ci sia un RectTransform
            if (itemObj.GetComponent<RectTransform>() == null)
            {
                itemObj.AddComponent<RectTransform>();
            }

            itemObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 50);

            // Aggiungi sfondo semitrasparente per leggibilità
            Image bg = itemObj.GetComponent<Image>();
            if (bg == null) bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);

            GameObject iconObj = FindOrCreateChild(itemObj.transform, "Icon");
            Image iconImg = CreateImage(iconObj, color);
            iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
            iconObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-60, 0);

            GameObject textObj = FindOrCreateChild(itemObj.transform, "AmountText");
            TextMeshProUGUI amountTxt = CreateText(textObj, "0", 20);
            textObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);

            GameObject maxObj = FindOrCreateChild(itemObj.transform, "MaxText");
            TextMeshProUGUI maxTxt = CreateText(maxObj, "/100", 14);
            maxObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(40, 10);

            // Assegna property
            prop.FindPropertyRelative("container").objectReferenceValue = itemObj.GetComponent<RectTransform>();
            prop.FindPropertyRelative("iconImage").objectReferenceValue = iconImg;
            prop.FindPropertyRelative("amountText").objectReferenceValue = amountTxt;
            prop.FindPropertyRelative("maxText").objectReferenceValue = maxTxt;
        }

        // UTILS

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject obj = new GameObject(name);
                obj.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
                return obj;
            }
            return child.gameObject;
        }

        private static Image CreateImage(GameObject obj, Color color)
        {
            Image img = obj.GetComponent<Image>();
            if (img == null) img = obj.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI CreateText(GameObject obj, string content, float fontSize)
        {
            TextMeshProUGUI txt = obj.GetComponent<TextMeshProUGUI>();
            if (txt == null) txt = obj.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Midline;
            return txt;
        }

        private static void SetFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
