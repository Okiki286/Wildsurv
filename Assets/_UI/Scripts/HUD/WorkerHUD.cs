using UnityEngine;
using TMPro;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Displays the worker count in the format "Idle / Total" in the Top HUD.
    /// Connects to WorkerSystem for real-time population data.
    /// </summary>
    public class WorkerHUD : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI workerCountText;

        // ============================================
        // CACHED VALUES
        // ============================================

        private int lastIdle = -1;
        private int lastTotal = -1;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Start()
        {
            // Force initial update
            UpdateDisplay(true);
        }

        private void Update()
        {
            UpdateDisplay(false);
        }

        // ============================================
        // DISPLAY LOGIC
        // ============================================

        /// <summary>
        /// Updates the text display. Only redraws if values changed to avoid GC allocations.
        /// </summary>
        /// <param name="force">If true, updates regardless of value changes.</param>
        private void UpdateDisplay(bool force)
        {
            if (workerCountText == null) return;

            // Get current values from WorkerSystem
            int idle = 0;
            int total = 0;

            if (WorkerSystem.Instance != null)
            {
                total = WorkerSystem.Instance.WorkerInstanceCount;
                idle = WorkerSystem.Instance.AvailableWorkerCount;
            }

            // Only update text if values changed (or forced)
            if (force || idle != lastIdle || total != lastTotal)
            {
                lastIdle = idle;
                lastTotal = total;

                // Format: "Idle / Total"
                workerCountText.SetText("{0} / {1}", idle, total);
            }
        }
    }
}
