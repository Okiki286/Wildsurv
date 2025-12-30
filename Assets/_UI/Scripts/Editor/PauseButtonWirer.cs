#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using WildernessSurvival.UI;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Lightweight Odin Editor Window to automatically wire Btn_Pause to PauseMenuUI.
    /// Menu: Tools/Wilderness/Pause Button Wirer
    /// </summary>
    public class PauseButtonWirer : OdinEditorWindow
    {
        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness/Pause Button Wirer")]
        private static void OpenWindow()
        {
            var window = GetWindow<PauseButtonWirer>();
            window.titleContent = new GUIContent("Pause Button Wirer");
            window.minSize = new Vector2(400, 350);
            window.Show();
        }

        // ============================================
        // DETECTION RESULTS
        // ============================================

        [TitleGroup("Auto-Detection Results")]
        [InfoBox("Click 'Inspect Scene' to scan for Btn_Pause and PauseMenuUI.", InfoMessageType.Info)]

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Btn_Pause GameObject")]
        [PropertyTooltip("The button that should trigger the pause menu")]
        private GameObject detectedPauseButton;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Has Button Component")]
        [PropertyTooltip("Whether Btn_Pause has a Button component")]
#pragma warning disable CS0414 // Field assigned but never used - displayed in Odin Inspector
        private bool hasButtonComponent = false;
#pragma warning restore CS0414

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Current onClick Event Count")]
        [PropertyTooltip("Number of persistent listeners on the button")]
        private int currentEventCount;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("PauseMenuUI Instance")]
        [PropertyTooltip("The target script that handles pause logic")]
        private PauseMenuUI detectedPauseMenuUI;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Is Already Wired")]
        [PropertyTooltip("Whether the button is already correctly wired")]
        private bool isAlreadyWired;

        // ============================================
        // MANUAL OVERRIDE
        // ============================================

        [TitleGroup("Manual Override")]
        [InfoBox("If auto-detection fails, manually assign the objects here.", InfoMessageType.Warning)]
        [SerializeField] private GameObject manualPauseButton;
        [SerializeField] private PauseMenuUI manualPauseMenuUI;

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug Options")]
        [SerializeField] private bool verboseLogging = true;

        // ============================================
        // MAIN ACTIONS
        // ============================================

        [TitleGroup("Actions")]
        [Button("🔍 Inspect Scene", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        [PropertyTooltip("Scan the scene for Btn_Pause and PauseMenuUI")]
        private void InspectScene()
        {
            Log("=== Starting Scene Inspection ===", "cyan");

            // Reset state
            currentEventCount = 0;
            isAlreadyWired = false;

            // Find Btn_Pause
            detectedPauseButton = FindPauseButton();
            if (detectedPauseButton != null)
            {
                Log($"✅ Found Pause Button: {detectedPauseButton.name}", "green");

                // Check for Button component
                Button buttonComponent = detectedPauseButton.GetComponent<Button>();
                if (buttonComponent != null)
                {
                    hasButtonComponent = true;
                    currentEventCount = buttonComponent.onClick.GetPersistentEventCount();
                    Log($"✅ Has Button component (Listeners: {currentEventCount})", "green");

                    // Check if already wired
                    isAlreadyWired = CheckIfWired(buttonComponent);
                    if (isAlreadyWired)
                    {
                        Log("✅ Button is already wired to PauseMenuUI.TogglePause()", "green");
                    }
                }
                else
                {
                    hasButtonComponent = false;
                    LogWarning("⚠️ Btn_Pause does NOT have a Button component");
                }
            }
            else
            {
                LogError("❌ Btn_Pause not found in scene!");
            }

            // Find PauseMenuUI
            detectedPauseMenuUI = FindPauseMenuUI();
            if (detectedPauseMenuUI != null)
            {
                Log($"✅ Found PauseMenuUI: {detectedPauseMenuUI.gameObject.name}", "green");
            }
            else
            {
                LogError("❌ PauseMenuUI not found in scene!");
            }

            Log("=== Inspection Complete ===", "cyan");
        }

        [TitleGroup("Actions")]
        [Button("🔌 Wire Pause Button", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        [PropertyTooltip("Automatically wire Btn_Pause to PauseMenuUI.TogglePause()")]
        private void WirePauseButton()
        {
            // Run inspection first
            InspectScene();

            // Use manual overrides if provided
            if (manualPauseButton != null) detectedPauseButton = manualPauseButton;
            if (manualPauseMenuUI != null) detectedPauseMenuUI = manualPauseMenuUI;

            // Validation
            if (detectedPauseButton == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Btn_Pause not found!\n\n" +
                    "Please ensure a GameObject named 'Btn_Pause' exists in the scene,\n" +
                    "or assign it manually in the 'Manual Override' section.",
                    "OK");
                return;
            }

            if (detectedPauseMenuUI == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "PauseMenuUI not found!\n\n" +
                    "Please generate the Pause Menu first using:\n" +
                    "Tools → Wilderness → Pause Menu Generator",
                    "OK");
                return;
            }

            Log("=== Starting Wiring Process ===", "lime");

            // Step 1: Ensure Button component exists
            Button button = EnsureButtonComponent();
            if (button == null)
            {
                LogError("Failed to add Button component!");
                return;
            }

            // Step 2: Wire the onClick event
            WireOnClickEvent(button);

            // Step 3: Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Log("=== Wiring Complete ===", "lime");

            // Success dialog
            EditorUtility.DisplayDialog("Success!",
                $"✅ Pause Button Wired Successfully!\n\n" +
                $"Button: {detectedPauseButton.name}\n" +
                $"Target: {detectedPauseMenuUI.gameObject.name}.TogglePause()\n\n" +
                "The button will now open the pause menu when clicked.\n\n" +
                "Save the scene to keep changes.",
                "OK");

            // Re-inspect to update UI
            InspectScene();
        }

        // ============================================
        // DETECTION HELPERS
        // ============================================

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

            // Search in all GameObjects
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("pause") && obj.name.ToLower().Contains("btn"))
                {
                    Log($"Found Pause Button via search: {obj.name}", "cyan");
                    return obj;
                }
            }

            return null;
        }

        private PauseMenuUI FindPauseMenuUI()
        {
            PauseMenuUI pauseMenuUI = FindFirstObjectByType<PauseMenuUI>();
            if (pauseMenuUI != null)
            {
                Log($"Found PauseMenuUI: {pauseMenuUI.gameObject.name}", "cyan");
            }
            return pauseMenuUI;
        }

        private bool CheckIfWired(Button button)
        {
            if (button == null || detectedPauseMenuUI == null) return false;

            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                Object target = button.onClick.GetPersistentTarget(i);
                string methodName = button.onClick.GetPersistentMethodName(i);

                if (target == detectedPauseMenuUI && methodName == "TogglePause")
                {
                    return true;
                }
            }

            return false;
        }

        // ============================================
        // WIRING LOGIC
        // ============================================

        private Button EnsureButtonComponent()
        {
            Button button = detectedPauseButton.GetComponent<Button>();

            if (button == null)
            {
                Log("Adding Button component to Btn_Pause...", "cyan");
                button = Undo.AddComponent<Button>(detectedPauseButton);

                // Set default button colors (cream theme)
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(240f / 255f, 230f / 255f, 210f / 255f, 1f); // Cream
                colors.highlightedColor = new Color(255f / 255f, 245f / 255f, 220f / 255f, 1f); // Light yellow
                colors.pressedColor = new Color(220f / 255f, 210f / 255f, 190f / 255f, 1f); // Darker cream
                button.colors = colors;

                EditorUtility.SetDirty(button);
                Log("✅ Button component added", "green");
            }
            else
            {
                Log("Button component already exists", "cyan");
            }

            return button;
        }

        private void WireOnClickEvent(Button button)
        {
            Log("Wiring onClick event...", "cyan");

            // Check if already wired (avoid duplicates)
            if (CheckIfWired(button))
            {
                LogWarning("⚠️ Button is already wired to PauseMenuUI.TogglePause()");

                bool rewire = EditorUtility.DisplayDialog(
                    "Already Wired",
                    "The button is already wired to PauseMenuUI.TogglePause().\n\n" +
                    "Do you want to re-wire it (will clear existing listeners)?",
                    "Yes, Re-wire",
                    "Cancel"
                );

                if (!rewire)
                {
                    Log("Wiring cancelled by user", "yellow");
                    return;
                }
            }

            // Clear existing listeners (runtime)
            button.onClick.RemoveAllListeners();

            // Clear all persistent listeners (editor)
            int listenerCount = button.onClick.GetPersistentEventCount();
            for (int i = listenerCount - 1; i >= 0; i--)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
            }

            Log("Cleared existing listeners", "cyan");

            // Add new persistent listener
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                button.onClick,
                detectedPauseMenuUI.TogglePause
            );

            EditorUtility.SetDirty(button);
            Log($"✅ Pause Button wired to {detectedPauseMenuUI.gameObject.name}.TogglePause()", "green");
        }

        // ============================================
        // QUICK ACTIONS
        // ============================================

        [TitleGroup("Quick Actions")]
        [Button("🧹 Clear All Listeners", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.4f)]
        [PropertyTooltip("Remove all onClick listeners from Btn_Pause")]
        private void ClearAllListeners()
        {
            if (detectedPauseButton == null)
            {
                EditorUtility.DisplayDialog("Error", "Please run 'Inspect Scene' first to detect Btn_Pause.", "OK");
                return;
            }

            Button button = detectedPauseButton.GetComponent<Button>();
            if (button == null)
            {
                EditorUtility.DisplayDialog("Error", "Btn_Pause does not have a Button component.", "OK");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Confirm Clear",
                "This will remove ALL onClick listeners from Btn_Pause.\n\nContinue?",
                "Yes, Clear",
                "Cancel"
            );

            if (confirm)
            {
                // Clear runtime listeners
                button.onClick.RemoveAllListeners();

                // Clear persistent listeners
                int count = button.onClick.GetPersistentEventCount();
                for (int i = count - 1; i >= 0; i--)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
                }

                EditorUtility.SetDirty(button);
                Log("✅ All listeners cleared", "green");

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                );

                // Re-inspect
                InspectScene();
            }
        }

        [TitleGroup("Quick Actions")]
        [Button("📋 Use Manual Overrides", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [PropertyTooltip("Apply manually assigned objects to detection results")]
        private void UseManualOverrides()
        {
            if (manualPauseButton != null) detectedPauseButton = manualPauseButton;
            if (manualPauseMenuUI != null) detectedPauseMenuUI = manualPauseMenuUI;

            Log("Manual overrides applied", "yellow");
            InspectScene();
        }

        // ============================================
        // LOGGING
        // ============================================

        private void Log(string message, string color)
        {
            if (verboseLogging)
            {
                Debug.Log($"<color={color}>[PauseButtonWirer]</color> {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"<color=yellow>[PauseButtonWirer]</color> {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"<color=red>[PauseButtonWirer]</color> {message}");
        }

        // ============================================
        // INFO DISPLAY
        // ============================================

        [TitleGroup("Instructions")]
        [InfoBox(
            "This tool wires Btn_Pause to the PauseMenuUI.TogglePause() method.\n\n" +
            "Steps:\n" +
            "1. Click 'Inspect Scene' to detect objects\n" +
            "2. Click 'Wire Pause Button' to auto-wire\n" +
            "3. Save the scene\n\n" +
            "If detection fails, use 'Manual Override' section.",
            InfoMessageType.Info)]
        [Button("Open Pause Menu Generator")]
        [GUIColor(0.6f, 0.8f, 1f)]
        private void OpenPauseMenuGenerator()
        {
            EditorWindow.GetWindow<PauseMenuGenerator>().Show();
        }
    }
}
#endif
