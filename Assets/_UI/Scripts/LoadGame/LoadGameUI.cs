using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Wilderness.Core;

namespace WildernessSurvival.UI.LoadGame
{
    /// <summary>
    /// Load Game Window Manager.
    /// Scans save files, populates UI list, handles load/delete actions.
    /// </summary>
    public class LoadGameUI : MonoBehaviour
    {
        // ============================================
        // UI REFERENCES (Assigned in Editor)
        // ============================================

        [Header("Window")]
        [SerializeField] private GameObject windowPanel;
        [SerializeField] private Button closeButton;

        [Header("Scroll View")]
        [SerializeField] private Transform slotContainer; // The Content object inside ScrollView
        [SerializeField] private SaveSlotItem slotPrefab;

        [Header("Empty State")]
        [SerializeField] private GameObject emptyStatePanel;
        [SerializeField] private TextMeshProUGUI emptyStateText;

        [Header("Confirmation Dialog (Optional)")]
        [SerializeField] private GameObject confirmDeletePanel;
        [SerializeField] private TextMeshProUGUI confirmDeleteText;
        [SerializeField] private Button confirmDeleteYesButton;
        [SerializeField] private Button confirmDeleteNoButton;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME DATA
        // ============================================

        private List<SaveSlotItem> spawnedSlots = new List<SaveSlotItem>();
        private string pendingDeleteFileName;
        private string pendingDeleteFilePath;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Wire close button
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            // Wire confirmation dialog buttons
            if (confirmDeleteYesButton != null)
            {
                confirmDeleteYesButton.onClick.RemoveAllListeners();
                confirmDeleteYesButton.onClick.AddListener(ConfirmDelete);
            }

            if (confirmDeleteNoButton != null)
            {
                confirmDeleteNoButton.onClick.RemoveAllListeners();
                confirmDeleteNoButton.onClick.AddListener(CancelDelete);
            }

            // Start hidden
            Hide();
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Opens the Load Game window and refreshes the save list.
        /// </summary>
        public void Show()
        {
            if (windowPanel != null)
            {
                windowPanel.SetActive(true);
            }

            RefreshList();

            if (debugMode)
            {
                Debug.Log("<color=cyan>[LoadGameUI]</color> Load Game window opened");
            }
        }

        /// <summary>
        /// Closes the Load Game window.
        /// </summary>
        public void Hide()
        {
            if (windowPanel != null)
            {
                windowPanel.SetActive(false);
            }

            if (debugMode)
            {
                Debug.Log("<color=cyan>[LoadGameUI]</color> Load Game window closed");
            }
        }

        /// <summary>
        /// Refreshes the save file list.
        /// Scans Application.persistentDataPath for *.json files.
        /// Sorts by Last Write Time (newest on top).
        /// </summary>
        public void RefreshList()
        {
            if (debugMode)
            {
                Debug.Log("<color=cyan>[LoadGameUI]</color> === RefreshList() STARTED ===");
            }

            // Clear existing slots
            ClearSlots();

            if (debugMode)
            {
                Debug.Log("<color=cyan>[LoadGameUI]</color> Cleared existing slots");
            }

            // Scan for save files
            List<SaveFileInfo> saveFiles = ScanSaveFiles();

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[LoadGameUI]</color> ScanSaveFiles returned {saveFiles.Count} files");
            }

            if (saveFiles.Count == 0)
            {
                // Show empty state
                if (debugMode)
                {
                    Debug.Log("<color=yellow>[LoadGameUI]</color> No save files found - showing empty state");
                }
                ShowEmptyState();
                return;
            }

            // Hide empty state
            HideEmptyState();

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[LoadGameUI]</color> Spawning {saveFiles.Count} slot items...");
            }

            // Spawn slot items
            foreach (var saveFile in saveFiles)
            {
                if (debugMode)
                {
                    Debug.Log($"<color=lime>[LoadGameUI]</color> Spawning slot for: {saveFile.fileName}");
                }
                SpawnSlotItem(saveFile);
            }

            if (debugMode)
            {
                Debug.Log($"<color=green>[LoadGameUI]</color> === RefreshList() COMPLETED - {saveFiles.Count} files loaded ===");
            }
        }

        /// <summary>
        /// Loads a specific save file.
        /// Called by SaveSlotItem when Load button is clicked.
        /// </summary>
        public void LoadSaveFile(string fileName)
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[LoadGameUI] SaveManager.Instance is null!");
                return;
            }

            Debug.Log($"<color=lime>[LoadGameUI]</color> Loading save file: {fileName}");

            // Call SaveManager to load this file
            SaveManager.Instance.LoadGame(fileName);

            // Close window
            Hide();

            // Optional: Reload the scene or transition to game
            // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        /// <summary>
        /// Deletes a specific save file.
        /// Called by SaveSlotItem when Delete button is clicked.
        /// Shows confirmation dialog first.
        /// </summary>
        public void DeleteSaveFile(string fileName, string filePath)
        {
            // Store pending delete info
            pendingDeleteFileName = fileName;
            pendingDeleteFilePath = filePath;

            // Show confirmation dialog
            if (confirmDeletePanel != null)
            {
                confirmDeletePanel.SetActive(true);
                if (confirmDeleteText != null)
                {
                    confirmDeleteText.text = $"Delete save file:\n{fileName}?\n\nThis cannot be undone.";
                }
            }
            else
            {
                // No confirmation dialog - delete immediately (not recommended)
                ConfirmDelete();
            }
        }

        // ============================================
        // INTERNAL LOGIC
        // ============================================

        private List<SaveFileInfo> ScanSaveFiles()
        {
            List<SaveFileInfo> saveFiles = new List<SaveFileInfo>();

            string savePath = Application.persistentDataPath;

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[LoadGameUI]</color> Scanning for save files in: {savePath}");
            }

            if (!Directory.Exists(savePath))
            {
                Debug.LogWarning($"[LoadGameUI] Save directory does not exist: {savePath}");
                return saveFiles;
            }

            // Get all *.json files
            string[] files = Directory.GetFiles(savePath, "*.json");

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[LoadGameUI]</color> Found {files.Length} .json files");
                foreach (string file in files)
                {
                    Debug.Log($"  - {Path.GetFileName(file)}");
                }
            }

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                FileInfo fileInfo = new FileInfo(filePath);

                // Read day number from file
                int dayNumber = ReadDayNumberFromFile(filePath);

                if (debugMode)
                {
                    Debug.Log($"<color=lime>[LoadGameUI]</color> Processing: {fileName} | Day: {dayNumber} | Modified: {fileInfo.LastWriteTime}");
                }

                saveFiles.Add(new SaveFileInfo
                {
                    fileName = fileName,
                    filePath = filePath,
                    lastModified = fileInfo.LastWriteTime,
                    dayNumber = dayNumber
                });
            }

            // Sort by Last Write Time (newest first)
            saveFiles = saveFiles.OrderByDescending(f => f.lastModified).ToList();

            if (debugMode)
            {
                Debug.Log($"<color=green>[LoadGameUI]</color> Returning {saveFiles.Count} save files");
            }

            return saveFiles;
        }

        private int ReadDayNumberFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                if (data == null || data.globalState == null)
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"<color=yellow>[LoadGameUI]</color> Failed to parse {Path.GetFileName(filePath)} - data is null");
                    }
                    return -1;
                }

                return data.globalState.currentDay;
            }
            catch (System.Exception e)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"<color=yellow>[LoadGameUI]</color> Error reading {Path.GetFileName(filePath)}: {e.Message}");
                }
                return -1;
            }
        }

        private void SpawnSlotItem(SaveFileInfo saveFile)
        {
            if (slotPrefab == null || slotContainer == null)
            {
                Debug.LogError("[LoadGameUI] slotPrefab or slotContainer is null!");
                if (slotPrefab == null)
                {
                    Debug.LogError("  -> slotPrefab is NULL");
                }
                if (slotContainer == null)
                {
                    Debug.LogError("  -> slotContainer is NULL");
                }
                return;
            }

            if (debugMode)
            {
                Debug.Log($"<color=lime>[LoadGameUI]</color> Instantiating slot prefab for: {saveFile.fileName}");
            }

            // Instantiate prefab with worldPositionStays = false to prevent offset issues
            SaveSlotItem slot = Instantiate(slotPrefab, slotContainer, false);

            // CRITICAL FIX: Reset local position to prevent UI offset glitches
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect != null)
            {
                slotRect.anchoredPosition = Vector2.zero;
                slotRect.localScale = Vector3.one;
            }

            // CRITICAL: Ensure the instantiated slot is active
            slot.gameObject.SetActive(true);

            if (debugMode)
            {
                Debug.Log($"<color=lime>[LoadGameUI]</color> Slot activated: {slot.gameObject.activeSelf}");
                Debug.Log($"<color=lime>[LoadGameUI]</color> Initializing slot with data: {saveFile.fileName}, Day {saveFile.dayNumber}");
            }

            // Initialize with data
            slot.Initialize(
                saveFile.fileName,
                saveFile.filePath,
                saveFile.lastModified,
                saveFile.dayNumber,
                this
            );

            // Track spawned slot
            spawnedSlots.Add(slot);

            if (debugMode)
            {
                Debug.Log($"<color=green>[LoadGameUI]</color> ✅ Slot spawned successfully: {saveFile.fileName}");
            }
        }

        private void ClearSlots()
        {
            foreach (var slot in spawnedSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            spawnedSlots.Clear();
        }

        private void ShowEmptyState()
        {
            if (emptyStatePanel != null)
            {
                emptyStatePanel.SetActive(true);
            }

            if (emptyStateText != null)
            {
                emptyStateText.text = "No save files found.\n\nStart a new game to create a save.";
            }
        }

        private void HideEmptyState()
        {
            if (emptyStatePanel != null)
            {
                emptyStatePanel.SetActive(false);
            }
        }

        private void ConfirmDelete()
        {
            if (string.IsNullOrEmpty(pendingDeleteFilePath))
            {
                Debug.LogWarning("[LoadGameUI] No pending delete file!");
                return;
            }

            try
            {
                // Delete the file
                File.Delete(pendingDeleteFilePath);

                Debug.Log($"<color=yellow>[LoadGameUI]</color> Deleted save file: {pendingDeleteFileName}");

                // Refresh the list
                RefreshList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadGameUI] Failed to delete file: {e.Message}");
            }
            finally
            {
                // Clear pending delete
                pendingDeleteFileName = null;
                pendingDeleteFilePath = null;

                // Hide confirmation dialog
                if (confirmDeletePanel != null)
                {
                    confirmDeletePanel.SetActive(false);
                }
            }
        }

        private void CancelDelete()
        {
            // Clear pending delete
            pendingDeleteFileName = null;
            pendingDeleteFilePath = null;

            // Hide confirmation dialog
            if (confirmDeletePanel != null)
            {
                confirmDeletePanel.SetActive(false);
            }

            if (debugMode)
            {
                Debug.Log("<color=cyan>[LoadGameUI]</color> Delete cancelled");
            }
        }

        // ============================================
        // DATA STRUCTURES
        // ============================================

        private struct SaveFileInfo
        {
            public string fileName;
            public string filePath;
            public DateTime lastModified;
            public int dayNumber;
        }
    }
}
