using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text.RegularExpressions;
using WildernessSurvival.Core.Managers;

namespace WildernessSurvival.Core.Editor
{
    /// <summary>
    /// Tool automatico per integrare GameFlowManager nel progetto.
    /// Modifica GameManager, UI scripts, e crea il GameObject necessario.
    ///
    /// USAGE: Tools → Wilderness → Integration → Auto-Integrate GameFlowManager
    /// </summary>
    public static class GameFlowIntegrationTool
    {
        private const string GAME_MANAGER_PATH = "Assets/_Core/Managers/GameManager.cs";
        private const string GAMEOVER_UI_PATH = "Assets/_UI/Scripts/GameOverUI.cs";
        private const string VICTORY_UI_PATH = "Assets/_UI/Scripts/VictoryUI.cs";

        [MenuItem("Tools/Wilderness/Integration/Auto-Integrate GameFlowManager")]
        public static void AutoIntegrate()
        {
            if (!EditorUtility.DisplayDialog(
                "GameFlowManager Auto-Integration",
                "This tool will:\n\n" +
                "1. Create GameFlowManager GameObject in current scene\n" +
                "2. Update GameManager.cs (TriggerVictory, TriggerGameOver, RestartGame)\n" +
                "3. Update GameOverUI.cs (add Main Menu button support)\n" +
                "4. Update VictoryUI.cs (add Main Menu button support)\n" +
                "5. Backup all modified files (.backup)\n\n" +
                "⚠️ Make sure to save your work first!\n\n" +
                "Continue?",
                "Yes, Integrate",
                "Cancel"))
            {
                Debug.Log("[GameFlowIntegration] Integration cancelled by user.");
                return;
            }

            Debug.Log("<color=cyan>[GameFlowIntegration]</color> ========== STARTING AUTO-INTEGRATION ==========");

            int successCount = 0;
            int totalSteps = 4;

            // Step 1: Create GameObject
            if (CreateGameFlowManagerObject())
            {
                successCount++;
                Debug.Log("<color=green>[GameFlowIntegration]</color> ✅ Step 1/4: GameObject created");
            }
            else
            {
                Debug.LogWarning("<color=orange>[GameFlowIntegration]</color> ⚠️ Step 1/4: GameObject creation skipped (already exists)");
            }

            // Step 2: Update GameManager.cs
            if (UpdateGameManager())
            {
                successCount++;
                Debug.Log("<color=green>[GameFlowIntegration]</color> ✅ Step 2/4: GameManager.cs updated");
            }
            else
            {
                Debug.LogWarning("<color=orange>[GameFlowIntegration]</color> ⚠️ Step 2/4: GameManager.cs update failed or skipped");
            }

            // Step 3: Update GameOverUI.cs
            if (UpdateGameOverUI())
            {
                successCount++;
                Debug.Log("<color=green>[GameFlowIntegration]</color> ✅ Step 3/4: GameOverUI.cs updated");
            }
            else
            {
                Debug.LogWarning("<color=orange>[GameFlowIntegration]</color> ⚠️ Step 3/4: GameOverUI.cs update failed or skipped");
            }

            // Step 4: Update VictoryUI.cs
            if (UpdateVictoryUI())
            {
                successCount++;
                Debug.Log("<color=green>[GameFlowIntegration]</color> ✅ Step 4/4: VictoryUI.cs updated");
            }
            else
            {
                Debug.LogWarning("<color=orange>[GameFlowIntegration]</color> ⚠️ Step 4/4: VictoryUI.cs update failed or skipped");
            }

            // Final Report
            Debug.Log("<color=cyan>[GameFlowIntegration]</color> ========== INTEGRATION COMPLETE ==========");
            Debug.Log($"<color=yellow>[GameFlowIntegration]</color> Steps completed: {successCount}/{totalSteps}");

            if (successCount == totalSteps)
            {
                EditorUtility.DisplayDialog(
                    "Integration Complete! ✅",
                    $"Successfully integrated GameFlowManager!\n\n" +
                    $"Steps completed: {successCount}/{totalSteps}\n\n" +
                    "Next Steps:\n" +
                    "1. Save the scene (Ctrl+S)\n" +
                    "2. Configure scene names in GameFlowManager Inspector\n" +
                    "3. Check Build Settings (File → Build Settings)\n" +
                    "4. Review GAMEFLOW_INTEGRATION_GUIDE.md for details\n\n" +
                    "⚠️ Backups created with .backup extension",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Integration Partially Complete ⚠️",
                    $"Integration completed with warnings.\n\n" +
                    $"Steps completed: {successCount}/{totalSteps}\n\n" +
                    "Check Console for details.\n" +
                    "Review backups (.backup) if needed.",
                    "OK");
            }

            // Refresh AssetDatabase
            AssetDatabase.Refresh();
        }

        // ============================================
        // STEP 1: CREATE GAMEOBJECT
        // ============================================

        private static bool CreateGameFlowManagerObject()
        {
            // Check if already exists
            var existing = Object.FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Debug.LogWarning($"[GameFlowIntegration] GameFlowManager already exists: {existing.name}");
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return false;
            }

            // Create GameObject
            GameObject flowManager = new GameObject("--- GAME FLOW ---");
            flowManager.AddComponent<GameFlowManager>();

            // Position at top of hierarchy
            flowManager.transform.SetAsFirstSibling();

            // Select and ping
            Selection.activeGameObject = flowManager;
            EditorGUIUtility.PingObject(flowManager);

            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[GameFlowIntegration] Created GameObject '--- GAME FLOW ---' with GameFlowManager component");
            return true;
        }

        // ============================================
        // STEP 2: UPDATE GAMEMANAGER.CS
        // ============================================

        private static bool UpdateGameManager()
        {
            if (!File.Exists(GAME_MANAGER_PATH))
            {
                Debug.LogError($"[GameFlowIntegration] File not found: {GAME_MANAGER_PATH}");
                return false;
            }

            // Backup
            CreateBackup(GAME_MANAGER_PATH);

            // Read file
            string content = File.ReadAllText(GAME_MANAGER_PATH);
            string originalContent = content;

            // Check if already integrated
            if (content.Contains("GameFlowManager.Instance"))
            {
                Debug.LogWarning("[GameFlowIntegration] GameManager.cs already contains GameFlowManager integration.");
                return false;
            }

            // ===== MODIFY TriggerVictory =====
            content = ModifyTriggerVictory(content);

            // ===== MODIFY TriggerGameOver =====
            content = ModifyTriggerGameOver(content);

            // ===== MODIFY RestartGame =====
            content = ModifyRestartGame(content);

            // Write back if changed
            if (content != originalContent)
            {
                File.WriteAllText(GAME_MANAGER_PATH, content);
                Debug.Log("[GameFlowIntegration] GameManager.cs updated successfully");
                return true;
            }

            return false;
        }

        private static string ModifyTriggerVictory(string content)
        {
            // Pattern: public void TriggerVictory() { ... }
            string pattern = @"(public void TriggerVictory\(\)\s*\{[^}]*)(if \(victoryUI != null\)\s*\{[^}]*\}[^}]*)(else\s*\{[^}]*\}[^}]*)(\s*\})";

            string replacement = @"$1$2$3

            // Delegate to GameFlowManager for global state
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.TriggerVictory();
            }
$4";

            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
                Debug.Log("[GameFlowIntegration] ✓ TriggerVictory() updated");
            }
            else
            {
                Debug.LogWarning("[GameFlowIntegration] Could not find TriggerVictory() pattern, skipping...");
            }

            return content;
        }

        private static string ModifyTriggerGameOver(string content)
        {
            // Pattern: public void TriggerGameOver(string reason = "...") { ... }
            string pattern = @"(public void TriggerGameOver\(string reason[^}]*)(if \(gameOverUI != null\)\s*\{[^}]*\}[^}]*)(else\s*\{[^}]*\}[^}]*)(\s*\})";

            string replacement = @"$1$2$3

            // Delegate to GameFlowManager for global state
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.TriggerGameOver();
            }
$4";

            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
                Debug.Log("[GameFlowIntegration] ✓ TriggerGameOver() updated");
            }
            else
            {
                Debug.LogWarning("[GameFlowIntegration] Could not find TriggerGameOver() pattern, skipping...");
            }

            return content;
        }

        private static string ModifyRestartGame(string content)
        {
            // Find RestartGame method and replace entire body
            string pattern = @"public void RestartGame\(\)\s*\{[^}]*UnityEngine\.SceneManagement\.SceneManager\.LoadScene\([^)]*\);[^}]*\}";

            string replacement = @"public void RestartGame()
        {
            Debug.Log(""[GameManager] Restart requested, delegating to GameFlowManager..."");

            // Reset local state (will be re-initialized on scene load)
            isPaused = false;
            isGameOver = false;
            isInitialized = false;
            bootSequenceComplete = false;

            // Delegate to GameFlowManager
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.RestartGame();
            }
            else
            {
                // Fallback if GameFlowManager doesn't exist
                Debug.LogWarning(""[GameManager] GameFlowManager not found, using fallback scene reload."");
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }";

            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
                Debug.Log("[GameFlowIntegration] ✓ RestartGame() updated");
            }
            else
            {
                Debug.LogWarning("[GameFlowIntegration] Could not find RestartGame() pattern, skipping...");
            }

            return content;
        }

        // ============================================
        // STEP 3: UPDATE GAMEOVERUI.CS
        // ============================================

        private static bool UpdateGameOverUI()
        {
            if (!File.Exists(GAMEOVER_UI_PATH))
            {
                Debug.LogError($"[GameFlowIntegration] File not found: {GAMEOVER_UI_PATH}");
                return false;
            }

            // Backup
            CreateBackup(GAMEOVER_UI_PATH);

            // Read file
            string content = File.ReadAllText(GAMEOVER_UI_PATH);
            string originalContent = content;

            // Check if already integrated
            if (content.Contains("GameFlowManager.Instance"))
            {
                Debug.LogWarning("[GameFlowIntegration] GameOverUI.cs already contains GameFlowManager integration.");
                return false;
            }

            // Add Main Menu button field
            content = AddMainMenuButtonField(content, "GameOverUI");

            // Update Awake to wire Main Menu button
            content = UpdateUIAwake(content, "GameOverUI");

            // Update OnDestroy to unwire Main Menu button
            content = UpdateUIOnDestroy(content);

            // Update OnRestartClicked to use GameFlowManager
            content = UpdateOnRestartClicked(content, "GameOverUI");

            // Add OnMainMenuClicked handler
            content = AddOnMainMenuClicked(content);

            // Write back if changed
            if (content != originalContent)
            {
                File.WriteAllText(GAMEOVER_UI_PATH, content);
                Debug.Log("[GameFlowIntegration] GameOverUI.cs updated successfully");
                return true;
            }

            return false;
        }

        // ============================================
        // STEP 4: UPDATE VICTORYUI.CS
        // ============================================

        private static bool UpdateVictoryUI()
        {
            if (!File.Exists(VICTORY_UI_PATH))
            {
                Debug.LogError($"[GameFlowIntegration] File not found: {VICTORY_UI_PATH}");
                return false;
            }

            // Backup
            CreateBackup(VICTORY_UI_PATH);

            // Read file
            string content = File.ReadAllText(VICTORY_UI_PATH);
            string originalContent = content;

            // Check if already integrated
            if (content.Contains("GameFlowManager.Instance"))
            {
                Debug.LogWarning("[GameFlowIntegration] VictoryUI.cs already contains GameFlowManager integration.");
                return false;
            }

            // Add Main Menu button field
            content = AddMainMenuButtonField(content, "VictoryUI");

            // Update Awake to wire Main Menu button
            content = UpdateUIAwake(content, "VictoryUI");

            // Update OnDestroy to unwire Main Menu button
            content = UpdateUIOnDestroy(content);

            // Update OnRestartClicked to use GameFlowManager
            content = UpdateOnRestartClicked(content, "VictoryUI");

            // Add OnMainMenuClicked handler
            content = AddOnMainMenuClicked(content);

            // Write back if changed
            if (content != originalContent)
            {
                File.WriteAllText(VICTORY_UI_PATH, content);
                Debug.Log("[GameFlowIntegration] VictoryUI.cs updated successfully");
                return true;
            }

            return false;
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        private static string AddMainMenuButtonField(string content, string className)
        {
            // Find the line with restartButton and add mainMenuButton after it
            string pattern = @"(\[SerializeField\] private Button restartButton;)";
            string replacement = "$1\n        [SerializeField] private Button mainMenuButton;";

            if (Regex.IsMatch(content, pattern))
            {
                content = Regex.Replace(content, pattern, replacement);
                Debug.Log($"[GameFlowIntegration] ✓ Added mainMenuButton field to {className}");
            }

            return content;
        }

        private static string UpdateUIAwake(string content, string className)
        {
            // Add mainMenuButton listener after restartButton
            string pattern = @"(restartButton\.onClick\.AddListener\(OnRestartClicked\);)(\s*\})";
            string replacement = @"$1

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }$2";

            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
                Debug.Log($"[GameFlowIntegration] ✓ Updated Awake() in {className}");
            }

            return content;
        }

        private static string UpdateUIOnDestroy(string content)
        {
            // Add mainMenuButton unwire after restartButton
            string pattern = @"(restartButton\.onClick\.RemoveListener\(OnRestartClicked\);)(\s*\})";
            string replacement = @"$1

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }$2";

            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
                Debug.Log("[GameFlowIntegration] ✓ Updated OnDestroy()");
            }

            return content;
        }

        private static string UpdateOnRestartClicked(string content, string className)
        {
            // Replace OnRestartClicked body to use GameFlowManager first
            string pattern = @"private void OnRestartClicked\(\)\s*\{[^}]*\}";

            string replacement = $@"private void OnRestartClicked()
        {{
            Debug.Log(""[{className}] Restart button clicked!"");

            if (GameFlowManager.Instance != null)
            {{
                GameFlowManager.Instance.RestartGame();
            }}
            else if (GameManager.Instance != null)
            {{
                GameManager.Instance.RestartGame();
            }}
            else
            {{
                Debug.LogWarning(""[{className}] No manager found, reloading scene directly."");
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }}
        }}";

            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
                Debug.Log($"[GameFlowIntegration] ✓ Updated OnRestartClicked() in {className}");
            }

            return content;
        }

        private static string AddOnMainMenuClicked(string content)
        {
            // Add OnMainMenuClicked after OnRestartClicked
            string pattern = @"(private void OnRestartClicked\(\)[^}]*\})";

            string replacement = @"$1

        private void OnMainMenuClicked()
        {
            Debug.Log(""[UI] Main Menu button clicked!"");

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.LoadMainMenu();
            }
            else
            {
                Debug.LogError(""[UI] GameFlowManager not found! Cannot load Main Menu."");
            }
        }";

            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
                Debug.Log("[GameFlowIntegration] ✓ Added OnMainMenuClicked()");
            }

            return content;
        }

        private static void CreateBackup(string filePath)
        {
            string backupPath = filePath + ".backup";
            File.Copy(filePath, backupPath, overwrite: true);
            Debug.Log($"[GameFlowIntegration] Backup created: {backupPath}");
        }

        // ============================================
        // ROLLBACK TOOL
        // ============================================

        [MenuItem("Tools/Wilderness/Integration/Rollback GameFlowManager Integration")]
        public static void Rollback()
        {
            if (!EditorUtility.DisplayDialog(
                "Rollback GameFlowManager Integration",
                "This will restore all files from .backup copies:\n\n" +
                "- GameManager.cs\n" +
                "- GameOverUI.cs\n" +
                "- VictoryUI.cs\n\n" +
                "⚠️ Current changes will be lost!\n\n" +
                "Continue?",
                "Yes, Rollback",
                "Cancel"))
            {
                return;
            }

            int restoredCount = 0;

            restoredCount += RestoreBackup(GAME_MANAGER_PATH);
            restoredCount += RestoreBackup(GAMEOVER_UI_PATH);
            restoredCount += RestoreBackup(VICTORY_UI_PATH);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Rollback Complete",
                $"Restored {restoredCount} file(s) from backup.\n\n" +
                "Note: GameFlowManager GameObject was NOT removed.\n" +
                "Delete it manually if needed.",
                "OK");

            Debug.Log($"<color=yellow>[GameFlowIntegration]</color> Rollback complete. {restoredCount} files restored.");
        }

        private static int RestoreBackup(string filePath)
        {
            string backupPath = filePath + ".backup";

            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, filePath, overwrite: true);
                Debug.Log($"[GameFlowIntegration] Restored: {filePath}");
                return 1;
            }
            else
            {
                Debug.LogWarning($"[GameFlowIntegration] Backup not found: {backupPath}");
                return 0;
            }
        }
    }
}
