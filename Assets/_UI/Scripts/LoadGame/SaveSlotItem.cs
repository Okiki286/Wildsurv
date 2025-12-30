using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace WildernessSurvival.UI.LoadGame
{
    /// <summary>
    /// Represents a single save slot in the Load Game UI.
    /// Displays file info and provides Load/Delete actions.
    /// </summary>
    public class SaveSlotItem : MonoBehaviour
    {
        // ============================================
        // UI REFERENCES (Assigned in Editor)
        // ============================================

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI filenameText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private TextMeshProUGUI dayText;

        [Header("Buttons")]
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;

        // ============================================
        // DATA
        // ============================================

        private string fileName;
        private string filePath;
        private DateTime lastModified;
        private int dayNumber;
        private LoadGameUI parentUI;

        // ============================================
        // INITIALIZATION
        // ============================================

        /// <summary>
        /// Initializes this slot with save file data.
        /// Called by LoadGameUI when spawning slot items.
        /// </summary>
        public void Initialize(string fileName, string filePath, DateTime lastModified, int dayNumber, LoadGameUI parent)
        {
            this.fileName = fileName;
            this.filePath = filePath;
            this.lastModified = lastModified;
            this.dayNumber = dayNumber;
            this.parentUI = parent;

            // Update UI text
            UpdateUI();

            // Wire button events
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(OnLoadClicked);

            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        private void UpdateUI()
        {
            // Filename display (e.g., "Day 5 - AutoSave 0")
            if (fileName.StartsWith("autosave_"))
            {
                string slotNumber = fileName.Replace("autosave_", "").Replace(".json", "");
                filenameText.text = $"AutoSave {slotNumber}";
            }
            else if (fileName == "savegame.json")
            {
                filenameText.text = "Manual Save";
            }
            else
            {
                filenameText.text = fileName.Replace(".json", "");
            }

            // Day number
            if (dayNumber >= 0)
            {
                dayText.text = $"Day {dayNumber}";
            }
            else
            {
                dayText.text = "Corrupt";
                dayText.color = new Color(0.8f, 0.3f, 0.3f); // Red tint for corrupt
            }

            // Timestamp (e.g., "2025-01-15 14:32")
            timestampText.text = lastModified.ToString("yyyy-MM-dd HH:mm");
        }

        // ============================================
        // BUTTON CALLBACKS
        // ============================================

        private void OnLoadClicked()
        {
            Debug.Log($"<color=cyan>[SaveSlotItem]</color> Loading save: {fileName}");

            if (parentUI != null)
            {
                parentUI.LoadSaveFile(fileName);
            }
        }

        private void OnDeleteClicked()
        {
            Debug.Log($"<color=yellow>[SaveSlotItem]</color> Delete requested: {fileName}");

            if (parentUI != null)
            {
                parentUI.DeleteSaveFile(fileName, filePath);
            }
        }

        // ============================================
        // PUBLIC ACCESSORS
        // ============================================

        public string FileName => fileName;
        public string FilePath => filePath;
        public int DayNumber => dayNumber;
    }
}
