using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.UI;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor tool to create the Worker Count HUD with Cream & Wood theme.
    /// Creates panel, icon, and text with proper layout.
    /// </summary>
    public class WorkerHUDSetup : EditorWindow
    {
        // ============================================
        // WILDERNESS PALETTE
        // ============================================
        private static readonly Color CREAM_BG = new Color32(242, 230, 216, 255);      // #F2E6D8
        private static readonly Color WOOD_OUTLINE = new Color32(93, 64, 55, 255);     // #5D4037
        private static readonly Color DEEP_BROWN = new Color32(62, 39, 35, 255);       // #3E2723

        // ============================================
        // CONFIGURATION
        // ============================================
        private Sprite workerIconSprite;
        private TMP_FontAsset fontAsset;
        private Transform parentTransform;

        [MenuItem("Wilderness/UI Setup/Worker HUD Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<WorkerHUDSetup>("Worker HUD Setup");
            window.minSize = new Vector2(350, 200);
        }

        private void OnEnable()
        {
            // Try to auto-find font
            string[] fontGuids = AssetDatabase.FindAssets("Inter t:TMP_FontAsset");
            if (fontGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(fontGuids[0]);
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }

            // Try to auto-find worker icon
            string[] iconGuids = AssetDatabase.FindAssets("worker t:Sprite");
            if (iconGuids.Length == 0)
                iconGuids = AssetDatabase.FindAssets("villager t:Sprite");
            if (iconGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(iconGuids[0]);
                workerIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Worker Count HUD Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Creates a Worker Count display panel (Idle / Total) with Cream & Wood theme.", MessageType.Info);

            EditorGUILayout.Space(10);

            // Parent selector
            parentTransform = EditorGUILayout.ObjectField("Parent (Canvas/HUD)", parentTransform, typeof(Transform), true) as Transform;

            // Icon sprite
            workerIconSprite = EditorGUILayout.ObjectField("Worker Icon Sprite", workerIconSprite, typeof(Sprite), false) as Sprite;

            // Font
            fontAsset = EditorGUILayout.ObjectField("TMP Font", fontAsset, typeof(TMP_FontAsset), false) as TMP_FontAsset;

            EditorGUILayout.Space(20);

            // Auto-find Canvas button
            if (GUILayout.Button("Auto-Find Canvas", GUILayout.Height(25)))
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    parentTransform = canvas.transform;
                    Debug.Log($"[WorkerHUDSetup] Found Canvas: {canvas.name}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", "No Canvas found in scene.", "OK");
                }
            }

            EditorGUILayout.Space(5);

            GUI.enabled = parentTransform != null;
            if (GUILayout.Button("Create Worker HUD", GUILayout.Height(35)))
            {
                CreateWorkerHUD();
            }
            GUI.enabled = true;

            if (parentTransform == null)
            {
                EditorGUILayout.HelpBox("Select a parent Canvas or HUD panel first.", MessageType.Warning);
            }
        }

        private void CreateWorkerHUD()
        {
            Undo.SetCurrentGroupName("Create Worker HUD");
            int undoGroup = Undo.GetCurrentGroup();

            // ============================================
            // 1. CREATE MAIN PANEL
            // ============================================
            GameObject panelObj = new GameObject("WorkerDisplay_Panel");
            Undo.RegisterCreatedObjectUndo(panelObj, "Create WorkerDisplay_Panel");
            panelObj.transform.SetParent(parentTransform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1); // Top-Left
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(20, -60); // Below other HUD elements
            panelRect.sizeDelta = new Vector2(120, 40);

            // Background Image
            Image panelImg = panelObj.AddComponent<Image>();
            panelImg.color = CREAM_BG;

            // Outline
            Outline panelOutline = panelObj.AddComponent<Outline>();
            panelOutline.effectColor = WOOD_OUTLINE;
            panelOutline.effectDistance = new Vector2(2, -2);

            // HorizontalLayoutGroup
            HorizontalLayoutGroup hlg = panelObj.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // ContentSizeFitter
            ContentSizeFitter csf = panelObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ============================================
            // 2. CREATE WORKER ICON
            // ============================================
            GameObject iconObj = new GameObject("WorkerIcon");
            iconObj.transform.SetParent(panelObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(28, 28);

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = workerIconSprite;
            iconImg.color = Color.white; // Keep original sprite colors
            iconImg.preserveAspect = true;

            // LayoutElement for icon
            LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 28;
            iconLayout.preferredHeight = 28;

            // ============================================
            // 3. CREATE TEXT
            // ============================================
            GameObject textObj = new GameObject("WorkerCountText");
            textObj.transform.SetParent(panelObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(60, 28);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "0 / 0";
            tmp.fontSize = 18;
            tmp.color = DEEP_BROWN;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            if (fontAsset != null)
            {
                tmp.font = fontAsset;
            }

            // LayoutElement for text
            LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 60;
            textLayout.preferredHeight = 28;

            // ============================================
            // 4. ADD WORKERHUD COMPONENT
            // ============================================
            WorkerHUD hud = panelObj.AddComponent<WorkerHUD>();

            // Wire reference via SerializedObject
            SerializedObject so = new SerializedObject(hud);
            SerializedProperty textProp = so.FindProperty("workerCountText");
            if (textProp != null)
            {
                textProp.objectReferenceValue = tmp;
                so.ApplyModifiedProperties();
            }

            // ============================================
            // FINISH
            // ============================================
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = panelObj;

            EditorUtility.SetDirty(panelObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("<color=green>[WorkerHUDSetup]</color> Worker HUD created successfully!");
            Debug.Log("<color=cyan>[WorkerHUDSetup]</color> Assign Worker Icon sprite if needed.");
        }
    }
}
