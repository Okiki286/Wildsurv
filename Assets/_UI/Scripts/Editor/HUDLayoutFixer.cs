#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using WildernessSurvival.UI;
using System.Linq;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Odin Editor Window to fix HUD layout and event wiring issues.
    /// Automatically groups Build and Pause buttons in a container with HorizontalLayoutGroup.
    /// Wires the Pause button onClick event to PauseMenuUI.TogglePause().
    /// Menu: Tools/Wilderness/HUD Layout Fixer
    /// </summary>
    public class HUDLayoutFixer : OdinEditorWindow
    {
        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness/HUD Layout Fixer")]
        private static void OpenWindow()
        {
            var window = GetWindow<HUDLayoutFixer>();
            window.titleContent = new GUIContent("HUD Layout Fixer");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        // ============================================
        // RUNTIME REFERENCES (Auto-detected)
        // ============================================

        [TitleGroup("Auto-Detection Results")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Canvas")]
        private Canvas detectedCanvas;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Build Button")]
        private GameObject detectedBuildButton;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Pause Button")]
        private GameObject detectedPauseButton;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("PauseMenuUI")]
        private PauseMenuUI detectedPauseMenuUI;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Existing Container")]
        private GameObject existingContainer;

        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Container Settings")]
        [InfoBox("Configure the HUD container layout settings.")]
        [LabelWidth(150)]
        [SerializeField] private string containerName = "HUD_BottomLeft_Container";

        [LabelWidth(150)]
        [SerializeField] private float containerSpacing = 20f;

        [LabelWidth(150)]
        [SerializeField] private TextAnchor childAlignment = TextAnchor.MiddleLeft;

        [LabelWidth(150)]
        [SerializeField] private Vector2 containerPosition = new Vector2(30, 30);

        [TitleGroup("Debug Options")]
        [SerializeField] private bool verboseLogging = true;

        // ============================================
        // MAIN ACTION
        // ============================================

        [TitleGroup("Actions")]
        [Button("🔍 Detect HUD Elements", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void DetectHUDElements()
        {
            Log("=== Starting HUD Detection ===", "cyan");

            // Find Canvas
            detectedCanvas = FindFirstObjectByType<Canvas>();
            if (detectedCanvas != null)
            {
                Log($"✅ Found Canvas: {detectedCanvas.gameObject.name}", "green");
            }
            else
            {
                LogError("❌ No Canvas found in scene!");
                return;
            }

            // Find Build Button
            detectedBuildButton = FindBuildButton();
            if (detectedBuildButton != null)
            {
                Log($"✅ Found Build Button: {detectedBuildButton.name}", "green");
            }
            else
            {
                LogWarning("⚠️ Build Button not found! (Will skip re-parenting)");
            }

            // Find Pause Button
            detectedPauseButton = FindPauseButton();
            if (detectedPauseButton != null)
            {
                Log($"✅ Found Pause Button: {detectedPauseButton.name}", "green");
            }
            else
            {
                LogError("❌ Pause Button not found! Cannot proceed.");
                return;
            }

            // Find PauseMenuUI
            detectedPauseMenuUI = FindFirstObjectByType<PauseMenuUI>();
            if (detectedPauseMenuUI != null)
            {
                Log($"✅ Found PauseMenuUI: {detectedPauseMenuUI.gameObject.name}", "green");
            }
            else
            {
                LogError("❌ PauseMenuUI not found! Cannot wire events.");
            }

            // Check for existing container
            existingContainer = GameObject.Find(containerName);
            if (existingContainer != null)
            {
                LogWarning($"⚠️ Container '{containerName}' already exists!");
            }

            Log("=== Detection Complete ===", "cyan");
        }

        [Button("🔧 Fix HUD Layout & Logic", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        [InfoBox("Automatically fixes HUD layout and wires the Pause button onClick event.", InfoMessageType.Info)]
        private void FixHUDLayoutAndLogic()
        {
            // Run detection first
            DetectHUDElements();

            if (detectedCanvas == null || detectedPauseButton == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Cannot proceed! Missing required elements:\n\n" +
                    $"Canvas: {(detectedCanvas == null ? "❌ NOT FOUND" : "✅")}\n" +
                    $"Pause Button: {(detectedPauseButton == null ? "❌ NOT FOUND" : "✅")}\n\n" +
                    "Please ensure the Pause button has been created first.",
                    "OK");
                return;
            }

            Log("=== Starting HUD Fix ===", "lime");

            // Step 1: Create or Find Container
            GameObject container = CreateOrFindContainer();

            // Step 2: Re-parent buttons
            ReparentButtons(container);

            // Step 3: Wire Pause Button
            WirePauseButton();

            // Step 4: Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Log("=== HUD Fix Complete ===", "lime");

            EditorUtility.DisplayDialog("Success!",
                "HUD Layout & Logic Fixed!\n\n" +
                $"✅ Container: {container.name}\n" +
                $"✅ Build Button: {(detectedBuildButton != null ? "Re-parented" : "Skipped")}\n" +
                $"✅ Pause Button: Re-parented & Wired\n\n" +
                "Changes saved to scene.",
                "OK");
        }

        // ============================================
        // DETECTION HELPERS
        // ============================================

        private GameObject FindBuildButton()
        {
            // Search by common names
            string[] possibleNames = new string[]
            {
                "Btn_BuildMode",
                "BuildButton",
                "Btn_Build",
                "Build Button",
                "BuildModeButton"
            };

            foreach (string name in possibleNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    Log($"Found Build Button by name: {name}", "cyan");
                    return obj;
                }
            }

            // Search in BottomLeft_Anchor
            Transform anchor = detectedCanvas.transform.Find("BottomLeft_Anchor");
            if (anchor != null)
            {
                foreach (string name in possibleNames)
                {
                    Transform child = anchor.Find(name);
                    if (child != null)
                    {
                        Log($"Found Build Button in BottomLeft_Anchor: {name}", "cyan");
                        return child.gameObject;
                    }
                }
            }

            // Fallback: Search all GameObjects with Button component
            var allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var button in allButtons)
            {
                if (button.gameObject.name.ToLower().Contains("build"))
                {
                    Log($"Found Build Button via component search: {button.gameObject.name}", "cyan");
                    return button.gameObject;
                }
            }

            return null;
        }

        private GameObject FindPauseButton()
        {
            // Search by name
            string[] possibleNames = new string[]
            {
                "Btn_Pause",
                "PauseButton",
                "Pause Button",
                "Btn_PauseMenu"
            };

            foreach (string name in possibleNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    Log($"Found Pause Button by name: {name}", "cyan");
                    return obj;
                }
            }

            // Search in BottomLeft_Anchor
            Transform anchor = detectedCanvas.transform.Find("BottomLeft_Anchor");
            if (anchor != null)
            {
                foreach (string name in possibleNames)
                {
                    Transform child = anchor.Find(name);
                    if (child != null)
                    {
                        Log($"Found Pause Button in BottomLeft_Anchor: {name}", "cyan");
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        // ============================================
        // LAYOUT FIX
        // ============================================

        private GameObject CreateOrFindContainer()
        {
            // Check if container already exists
            GameObject container = GameObject.Find(containerName);
            if (container != null)
            {
                Log($"Using existing container: {containerName}", "yellow");
                return container;
            }

            // Create new container
            Log($"Creating new container: {containerName}", "cyan");
            container = new GameObject(containerName);
            Undo.RegisterCreatedObjectUndo(container, "Create HUD Container");

            // Set parent to Canvas
            container.transform.SetParent(detectedCanvas.transform, false);

            // Add RectTransform
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0); // Bottom-Left
            containerRect.anchorMax = new Vector2(0, 0);
            containerRect.pivot = new Vector2(0, 0);
            containerRect.anchoredPosition = containerPosition;
            containerRect.sizeDelta = new Vector2(300, 120); // Auto-size based on children

            // Add HorizontalLayoutGroup
            HorizontalLayoutGroup layoutGroup = container.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = containerSpacing;
            layoutGroup.childAlignment = childAlignment;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            // Add ContentSizeFitter (optional - auto-size based on children)
            ContentSizeFitter sizeFitter = container.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Log($"✅ Container created with HorizontalLayoutGroup (spacing: {containerSpacing})", "green");
            return container;
        }

        private void ReparentButtons(GameObject container)
        {
            Log("Re-parenting buttons to container...", "cyan");

            // Re-parent Build Button (if found)
            if (detectedBuildButton != null)
            {
                Undo.SetTransformParent(detectedBuildButton.transform, container.transform, "Reparent Build Button");
                detectedBuildButton.transform.SetSiblingIndex(0); // First child
                Log($"✅ Build Button re-parented (index: 0)", "green");
            }

            // Re-parent Pause Button
            if (detectedPauseButton != null)
            {
                Undo.SetTransformParent(detectedPauseButton.transform, container.transform, "Reparent Pause Button");
                detectedPauseButton.transform.SetSiblingIndex(1); // Second child (to the right)
                Log($"✅ Pause Button re-parented (index: 1)", "green");
            }

            // Cleanup old BottomLeft_Anchor if empty
            Transform oldAnchor = detectedCanvas.transform.Find("BottomLeft_Anchor");
            if (oldAnchor != null && oldAnchor.childCount == 0)
            {
                Log("Destroying empty BottomLeft_Anchor", "yellow");
                Undo.DestroyObjectImmediate(oldAnchor.gameObject);
            }
        }

        // ============================================
        // EVENT WIRING FIX
        // ============================================

        private void WirePauseButton()
        {
            if (detectedPauseButton == null)
            {
                LogError("Cannot wire Pause Button - not found!");
                return;
            }

            if (detectedPauseMenuUI == null)
            {
                LogError("Cannot wire Pause Button - PauseMenuUI not found!");
                return;
            }

            Log("Wiring Pause Button onClick event...", "cyan");

            Button button = detectedPauseButton.GetComponent<Button>();
            if (button == null)
            {
                LogError("Pause Button does not have a Button component!");
                return;
            }

            // Clear existing listeners (runtime)
            button.onClick.RemoveAllListeners();

            // Clear all persistent listeners (editor)
            int listenerCount = button.onClick.GetPersistentEventCount();
            for (int i = listenerCount - 1; i >= 0; i--)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
            }

            // Add new persistent listener (survives play mode)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                button.onClick,
                detectedPauseMenuUI.TogglePause
            );

            EditorUtility.SetDirty(button);
            Log($"✅ Pause Button wired to {detectedPauseMenuUI.gameObject.name}.TogglePause()", "green");
        }

        // ============================================
        // MANUAL OVERRIDE SECTION
        // ============================================

        [TitleGroup("Manual Override")]
        [InfoBox("If auto-detection fails, manually assign references here and run the fix.", InfoMessageType.Warning)]
        [SerializeField] private GameObject manualBuildButton;
        [SerializeField] private GameObject manualPauseButton;
        [SerializeField] private PauseMenuUI manualPauseMenuUI;

        [TitleGroup("Manual Override")]
        [Button("Use Manual References", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.4f)]
        private void UseManualReferences()
        {
            if (manualBuildButton != null) detectedBuildButton = manualBuildButton;
            if (manualPauseButton != null) detectedPauseButton = manualPauseButton;
            if (manualPauseMenuUI != null) detectedPauseMenuUI = manualPauseMenuUI;

            Log("Manual references applied!", "yellow");
        }

        // ============================================
        // LOGGING HELPERS
        // ============================================

        private void Log(string message, string color)
        {
            if (verboseLogging)
            {
                Debug.Log($"<color={color}>[HUDLayoutFixer]</color> {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"<color=yellow>[HUDLayoutFixer]</color> {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"<color=red>[HUDLayoutFixer]</color> {message}");
        }

        // ============================================
        // CLEANUP HELPERS
        // ============================================

        [TitleGroup("Cleanup")]
        [Button("Remove Container & Reset", ButtonSizes.Medium)]
        [GUIColor(1f, 0.4f, 0.4f)]
        private void RemoveContainer()
        {
            GameObject container = GameObject.Find(containerName);
            if (container != null)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Confirm Removal",
                    $"This will destroy '{containerName}' and move its children back to Canvas root.\n\nContinue?",
                    "Yes, Remove",
                    "Cancel"
                );

                if (confirm)
                {
                    // Move children back to Canvas
                    Transform canvasTransform = detectedCanvas != null ? detectedCanvas.transform : FindFirstObjectByType<Canvas>()?.transform;
                    if (canvasTransform != null)
                    {
                        while (container.transform.childCount > 0)
                        {
                            Transform child = container.transform.GetChild(0);
                            Undo.SetTransformParent(child, canvasTransform, "Move child to Canvas");
                        }
                    }

                    Undo.DestroyObjectImmediate(container);
                    Log($"Container '{containerName}' removed", "yellow");

                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                    );
                }
            }
            else
            {
                LogWarning($"Container '{containerName}' not found!");
            }
        }
    }
}
#endif
