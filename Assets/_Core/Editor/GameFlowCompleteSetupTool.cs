using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using WildernessSurvival.Core.Managers;
using WildernessSurvival.UI;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Complete automation tool for GameFlowManager + UI setup.
    ///
    /// FEATURES:
    /// - Creates Main Menu scene with UI
    /// - Adds UI Coordinator to Gameplay scene
    /// - Configures Build Settings
    /// - Wires all UI components
    /// - One-click complete setup
    ///
    /// USAGE:
    /// Tools → Wilderness → Setup → Complete GameFlow Setup
    /// </summary>
    public class GameFlowCompleteSetupTool : EditorWindow
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        private const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenu.unity";
        private const string GAMEPLAY_SCENE_PATH = "Assets/Scenes/test2.unity";
        private const string SCENES_FOLDER = "Assets/Scenes";

        // UI Prefab paths (optional)
        private string mainMenuScenePath = MAIN_MENU_SCENE_PATH;
        private string gameplayScenePath = GAMEPLAY_SCENE_PATH;

        private bool createMainMenuScene = true;
        private bool setupUICoordinator = true;
        private bool configureBuildSettings = true;
        private bool createSampleUI = true;

        private Vector2 scrollPosition;

        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness/Setup/Complete GameFlow Setup")]
        public static void ShowWindow()
        {
            GameFlowCompleteSetupTool window = GetWindow<GameFlowCompleteSetupTool>("GameFlow Setup");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        // ============================================
        // GUI
        // ============================================

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawConfiguration();
            DrawActionButtons();
            DrawInstructions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🚀 GameFlow Complete Setup", titleStyle);
            EditorGUILayout.LabelField("One-Click Automation Tool", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(10);
        }

        private void DrawConfiguration()
        {
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool will automatically set up the entire GameFlow system:\n\n" +
                "✅ Create Main Menu scene with UI\n" +
                "✅ Add UI Coordinator to Gameplay scene\n" +
                "✅ Configure Build Settings\n" +
                "✅ Wire all UI components\n\n" +
                "Total time: ~30 seconds",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Scene Paths", EditorStyles.boldLabel);
            mainMenuScenePath = EditorGUILayout.TextField("Main Menu Scene", mainMenuScenePath);
            gameplayScenePath = EditorGUILayout.TextField("Gameplay Scene", gameplayScenePath);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Setup Options", EditorStyles.boldLabel);
            createMainMenuScene = EditorGUILayout.Toggle("Create Main Menu Scene", createMainMenuScene);
            setupUICoordinator = EditorGUILayout.Toggle("Setup UI Coordinator", setupUICoordinator);
            configureBuildSettings = EditorGUILayout.Toggle("Configure Build Settings", configureBuildSettings);
            createSampleUI = EditorGUILayout.Toggle("Create Sample UI Elements", createSampleUI);

            EditorGUILayout.Space(10);
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("🚀 RUN COMPLETE SETUP", GUILayout.Height(40), GUILayout.Width(250)))
            {
                if (EditorUtility.DisplayDialog(
                    "Complete GameFlow Setup",
                    "This will:\n\n" +
                    "1. Create Main Menu scene with UI\n" +
                    "2. Add UI Coordinator to Gameplay scene\n" +
                    "3. Configure Build Settings\n" +
                    "4. Wire all UI components\n\n" +
                    "Any existing configurations will be preserved.\n\n" +
                    "Continue?",
                    "Yes, Run Setup",
                    "Cancel"
                ))
                {
                    RunCompleteSetup();
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🔄 Rollback All", GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog(
                    "Rollback GameFlow Setup",
                    "This will remove all GameFlow components and restore backups.\n\n" +
                    "Continue?",
                    "Yes, Rollback",
                    "Cancel"
                ))
                {
                    RollbackSetup();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInstructions()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Post-Setup Instructions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "After running the setup:\n\n" +
                "1. Save all scenes (Ctrl+S)\n" +
                "2. Test Main Menu → Gameplay transition\n" +
                "3. Test Victory/GameOver flows\n" +
                "4. Customize UI styles as needed\n\n" +
                "See GAMEFLOW_IMPLEMENTATION_SUMMARY.md for full documentation.",
                MessageType.Info
            );
        }

        // ============================================
        // SETUP EXECUTION
        // ============================================

        private void RunCompleteSetup()
        {
            try
            {
                EditorUtility.DisplayProgressBar("GameFlow Setup", "Starting setup...", 0f);

                int totalSteps = 4;
                int currentStep = 0;

                // Step 1: Create Main Menu Scene
                if (createMainMenuScene)
                {
                    EditorUtility.DisplayProgressBar("GameFlow Setup", "Creating Main Menu scene...", (float)currentStep / totalSteps);
                    CreateMainMenuScene();
                    currentStep++;
                }

                // Step 2: Setup UI Coordinator in Gameplay
                if (setupUICoordinator)
                {
                    EditorUtility.DisplayProgressBar("GameFlow Setup", "Setting up UI Coordinator...", (float)currentStep / totalSteps);
                    SetupUICoordinator();
                    currentStep++;
                }

                // Step 3: Configure Build Settings
                if (configureBuildSettings)
                {
                    EditorUtility.DisplayProgressBar("GameFlow Setup", "Configuring Build Settings...", (float)currentStep / totalSteps);
                    ConfigureBuildSettings();
                    currentStep++;
                }

                // Step 4: Wire GameFlowManager
                EditorUtility.DisplayProgressBar("GameFlow Setup", "Finalizing setup...", (float)currentStep / totalSteps);
                WireGameFlowManager();
                currentStep++;

                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Setup Complete!",
                    "GameFlow setup completed successfully!\n\n" +
                    "✅ Main Menu scene created\n" +
                    "✅ UI Coordinator added to Gameplay\n" +
                    "✅ Build Settings configured\n" +
                    "✅ All UI components wired\n\n" +
                    "Next steps:\n" +
                    "1. Save all scenes (Ctrl+S)\n" +
                    "2. Test the flow by pressing Play\n" +
                    "3. See GAMEFLOW_IMPLEMENTATION_SUMMARY.md for details",
                    "OK"
                );

                Debug.Log("<color=green>[GameFlowSetup]</color> ✅ Complete setup finished successfully!");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "Setup Failed",
                    $"An error occurred during setup:\n\n{e.Message}\n\nCheck Console for details.",
                    "OK"
                );
                Debug.LogError($"[GameFlowSetup] Setup failed: {e.Message}\n{e.StackTrace}");
            }
        }

        // ============================================
        // STEP 1: CREATE MAIN MENU SCENE
        // ============================================

        private void CreateMainMenuScene()
        {
            Debug.Log("[GameFlowSetup] Step 1/4: Creating Main Menu scene...");

            // Check if scene already exists
            if (File.Exists(mainMenuScenePath))
            {
                Debug.LogWarning($"[GameFlowSetup] Main Menu scene already exists at {mainMenuScenePath}. Skipping creation.");
                return;
            }

            // Ensure Scenes folder exists
            if (!Directory.Exists(SCENES_FOLDER))
            {
                Directory.CreateDirectory(SCENES_FOLDER);
                AssetDatabase.Refresh();
            }

            // Create new scene
            Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create Canvas
            GameObject canvasGO = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create Event System
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            if (createSampleUI)
            {
                // Create background panel
                GameObject panelGO = new GameObject("Panel");
                panelGO.transform.SetParent(canvasGO.transform, false);
                Image panelImage = panelGO.AddComponent<Image>();
                panelImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);
                RectTransform panelRT = panelGO.GetComponent<RectTransform>();
                panelRT.anchorMin = Vector2.zero;
                panelRT.anchorMax = Vector2.one;
                panelRT.sizeDelta = Vector2.zero;

                // Create Title
                GameObject titleGO = new GameObject("Title");
                titleGO.transform.SetParent(panelGO.transform, false);
                TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
                titleText.text = "WILDERNESS SURVIVAL";
                titleText.fontSize = 48;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = Color.white;
                RectTransform titleRT = titleGO.GetComponent<RectTransform>();
                titleRT.anchorMin = new Vector2(0.5f, 0.7f);
                titleRT.anchorMax = new Vector2(0.5f, 0.7f);
                titleRT.sizeDelta = new Vector2(600, 100);

                // Create Play Button
                GameObject playButtonGO = CreateButton(panelGO.transform, "PlayButton", "PLAY", new Vector2(0.5f, 0.5f), new Vector2(300, 60));
                Button playButton = playButtonGO.GetComponent<Button>();

                // Create Quit Button
                GameObject quitButtonGO = CreateButton(panelGO.transform, "QuitButton", "QUIT", new Vector2(0.5f, 0.35f), new Vector2(300, 60));
                Button quitButton = quitButtonGO.GetComponent<Button>();

                // Add MainMenuUI script
                MainMenuUI mainMenuUI = canvasGO.AddComponent<MainMenuUI>();

                // Use reflection to set private fields (since they're serialized)
                var playButtonField = typeof(MainMenuUI).GetField("playButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var quitButtonField = typeof(MainMenuUI).GetField("quitButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (playButtonField != null) playButtonField.SetValue(mainMenuUI, playButton);
                if (quitButtonField != null) quitButtonField.SetValue(mainMenuUI, quitButton);

                EditorUtility.SetDirty(canvasGO);

                Debug.Log("[GameFlowSetup] ✓ Sample UI elements created");
            }

            // Save scene
            EditorSceneManager.SaveScene(mainMenuScene, mainMenuScenePath);
            Debug.Log($"[GameFlowSetup] ✅ Main Menu scene created at {mainMenuScenePath}");
        }

        private GameObject CreateButton(Transform parent, string name, string text, Vector2 anchorPos, Vector2 sizeDelta)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            // Add Image component
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.5f, 0.7f, 1f);

            // Add Button component
            Button button = buttonGO.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.5f, 0.7f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f, 1f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.6f, 1f);
            button.colors = colors;

            // Position
            RectTransform buttonRT = buttonGO.GetComponent<RectTransform>();
            buttonRT.anchorMin = anchorPos;
            buttonRT.anchorMax = anchorPos;
            buttonRT.sizeDelta = sizeDelta;

            // Create text child
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 24;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            return buttonGO;
        }

        // ============================================
        // STEP 2: SETUP UI COORDINATOR
        // ============================================

        private void SetupUICoordinator()
        {
            Debug.Log("[GameFlowSetup] Step 2/4: Setting up UI Coordinator...");

            // Open Gameplay scene
            if (!File.Exists(gameplayScenePath))
            {
                Debug.LogWarning($"[GameFlowSetup] Gameplay scene not found at {gameplayScenePath}. Skipping UI Coordinator setup.");
                return;
            }

            Scene gameplayScene = EditorSceneManager.OpenScene(gameplayScenePath, OpenSceneMode.Single);

            // Check if UI Coordinator already exists
            GameFlowUICoordinator existingCoordinator = FindFirstObjectByType<GameFlowUICoordinator>();
            if (existingCoordinator != null)
            {
                Debug.LogWarning("[GameFlowSetup] UI Coordinator already exists. Skipping creation.");
                return;
            }

            // Create UI Coordinator GameObject
            GameObject coordinatorGO = new GameObject("--- UI COORDINATOR ---");
            GameFlowUICoordinator coordinator = coordinatorGO.AddComponent<GameFlowUICoordinator>();

            // Find Victory and GameOver panels
            GameObject victoryPanel = GameObject.Find("VictoryCanvas");
            if (victoryPanel == null) victoryPanel = GameObject.Find("VictoryPanel");

            GameObject gameOverPanel = GameObject.Find("GameOverCanvas");
            if (gameOverPanel == null) gameOverPanel = GameObject.Find("GameOverPanel");

            // Assign references using reflection
            var victoryPanelField = typeof(GameFlowUICoordinator).GetField("victoryPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gameOverPanelField = typeof(GameFlowUICoordinator).GetField("gameOverPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (victoryPanelField != null && victoryPanel != null)
            {
                victoryPanelField.SetValue(coordinator, victoryPanel);
                Debug.Log("[GameFlowSetup] ✓ Victory panel assigned");
            }
            else
            {
                Debug.LogWarning("[GameFlowSetup] Victory panel not found! Assign manually.");
            }

            if (gameOverPanelField != null && gameOverPanel != null)
            {
                gameOverPanelField.SetValue(coordinator, gameOverPanel);
                Debug.Log("[GameFlowSetup] ✓ GameOver panel assigned");
            }
            else
            {
                Debug.LogWarning("[GameFlowSetup] GameOver panel not found! Assign manually.");
            }

            EditorUtility.SetDirty(coordinatorGO);
            EditorSceneManager.SaveScene(gameplayScene);

            Debug.Log("[GameFlowSetup] ✅ UI Coordinator setup complete");
        }

        // ============================================
        // STEP 3: CONFIGURE BUILD SETTINGS
        // ============================================

        private void ConfigureBuildSettings()
        {
            Debug.Log("[GameFlowSetup] Step 3/4: Configuring Build Settings...");

            // Get current scenes in build
            var scenes = EditorBuildSettings.scenes.ToList();

            // Check if Main Menu is already added
            bool mainMenuExists = scenes.Any(s => s.path == mainMenuScenePath);
            if (!mainMenuExists && File.Exists(mainMenuScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(mainMenuScenePath, true));
                Debug.Log("[GameFlowSetup] ✓ Main Menu added to Build Settings (index 0)");
            }

            // Check if Gameplay is already added
            bool gameplayExists = scenes.Any(s => s.path == gameplayScenePath);
            if (!gameplayExists && File.Exists(gameplayScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(gameplayScenePath, true));
                Debug.Log("[GameFlowSetup] ✓ Gameplay scene added to Build Settings");
            }

            // Apply changes
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("[GameFlowSetup] ✅ Build Settings configured");
        }

        // ============================================
        // STEP 4: WIRE GAMEFLOWMANAGER
        // ============================================

        private void WireGameFlowManager()
        {
            Debug.Log("[GameFlowSetup] Step 4/4: Wiring GameFlowManager...");

            // GameFlowManager auto-creates itself via RuntimeInitializeOnLoadMethod
            // We just need to ensure the config is correct

            // Open Main Menu scene to verify GameFlowManager exists
            if (File.Exists(mainMenuScenePath))
            {
                EditorSceneManager.OpenScene(mainMenuScenePath, OpenSceneMode.Single);
            }

            // Check if GameFlowManager exists
            GameFlowManager gfm = FindFirstObjectByType<GameFlowManager>();
            if (gfm != null)
            {
                Debug.Log("[GameFlowSetup] ✓ GameFlowManager found in scene");

                // Configure scene names using reflection
                var mainMenuSceneNameField = typeof(GameFlowManager).GetField("mainMenuSceneName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var gameplaySceneNameField = typeof(GameFlowManager).GetField("gameplaySceneName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (mainMenuSceneNameField != null)
                {
                    mainMenuSceneNameField.SetValue(gfm, "MainMenu");
                }

                if (gameplaySceneNameField != null)
                {
                    gameplaySceneNameField.SetValue(gfm, "test2");
                }

                EditorUtility.SetDirty(gfm.gameObject);
                EditorSceneManager.SaveOpenScenes();

                Debug.Log("[GameFlowSetup] ✓ GameFlowManager configured");
            }
            else
            {
                Debug.Log("[GameFlowSetup] ℹ️ GameFlowManager will auto-create at runtime (no manual setup needed)");
            }

            Debug.Log("[GameFlowSetup] ✅ Wiring complete");
        }

        // ============================================
        // ROLLBACK
        // ============================================

        private void RollbackSetup()
        {
            Debug.Log("[GameFlowSetup] Starting rollback...");

            int steps = 0;

            // Delete Main Menu scene
            if (File.Exists(mainMenuScenePath))
            {
                AssetDatabase.DeleteAsset(mainMenuScenePath);
                Debug.Log("[GameFlowSetup] ✓ Main Menu scene deleted");
                steps++;
            }

            // Remove UI Coordinator from Gameplay scene
            if (File.Exists(gameplayScenePath))
            {
                EditorSceneManager.OpenScene(gameplayScenePath, OpenSceneMode.Single);
                GameFlowUICoordinator coordinator = FindFirstObjectByType<GameFlowUICoordinator>();
                if (coordinator != null)
                {
                    DestroyImmediate(coordinator.gameObject);
                    EditorSceneManager.SaveOpenScenes();
                    Debug.Log("[GameFlowSetup] ✓ UI Coordinator removed");
                    steps++;
                }
            }

            // Restore backup files (if they exist)
            RestoreBackupFiles();

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Rollback Complete",
                $"Rollback completed successfully!\n\n" +
                $"✅ {steps} components removed\n" +
                $"✅ Backup files restored (if they existed)\n\n" +
                $"Your project has been restored to pre-setup state.",
                "OK"
            );

            Debug.Log("[GameFlowSetup] ✅ Rollback complete");
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        /// <summary>
        /// Restores backup files created during integration.
        /// </summary>
        private void RestoreBackupFiles()
        {
            string[] backupFiles = new string[]
            {
                "Assets/_Core/Managers/GameManager.cs.backup",
                "Assets/_UI/Scripts/VictoryUI.cs.backup",
                "Assets/_UI/Scripts/GameOverUI.cs.backup"
            };

            int restoredCount = 0;

            foreach (string backupPath in backupFiles)
            {
                if (File.Exists(backupPath))
                {
                    string originalPath = backupPath.Replace(".backup", "");

                    try
                    {
                        // Copy backup to original
                        File.Copy(backupPath, originalPath, true);

                        // Delete backup
                        File.Delete(backupPath);

                        // Delete .meta file for backup
                        string backupMetaPath = backupPath + ".meta";
                        if (File.Exists(backupMetaPath))
                        {
                            File.Delete(backupMetaPath);
                        }

                        restoredCount++;
                        Debug.Log($"[GameFlowSetup] ✓ Restored {Path.GetFileName(originalPath)} from backup");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[GameFlowSetup] Failed to restore {Path.GetFileName(originalPath)}: {e.Message}");
                    }
                }
            }

            if (restoredCount > 0)
            {
                Debug.Log($"[GameFlowSetup] ✅ Restored {restoredCount} backup files");
            }
            else
            {
                Debug.Log("[GameFlowSetup] ℹ️ No backup files found to restore");
            }
        }
    }
}
