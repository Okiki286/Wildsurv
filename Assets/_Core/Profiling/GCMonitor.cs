using UnityEngine;
using Sirenix.OdinInspector;
using System;

namespace WildernessSurvival.Core.Profiling
{
    /// <summary>
    /// Monitors garbage collection allocations in real-time.
    /// Useful for verifying pooling optimizations are working.
    ///
    /// USAGE: Attach to any GameObject in the scene.
    /// BEFORE: ~15MB/min GC allocations
    /// AFTER POOLING: <1MB/min target
    /// </summary>
    public class GCMonitor : MonoBehaviour
    {
        [TitleGroup("Configuration")]
        [SerializeField]
        [Range(0.1f, 5f)]
        [Tooltip("Update interval in seconds")]
        private float updateInterval = 1f;

        [SerializeField]
        [Tooltip("Show GC stats in Unity Editor console")]
        private bool logToConsole = true;

        [SerializeField]
        [Tooltip("Show GC stats in on-screen GUI")]
        private bool showOnScreenGUI = true;

        [TitleGroup("Runtime Stats")]
        [ShowInInspector, ReadOnly]
        [SuffixLabel("MB")]
        [ProgressBar(0, 100, ColorGetter = "GetMemoryColor")]
        private float currentMemoryMB;

        [ShowInInspector, ReadOnly]
        [SuffixLabel("KB/sec")]
        [ProgressBar(0, 1000, ColorGetter = "GetAllocationRateColor")]
        private float allocationRateKBPerSec;

        [ShowInInspector, ReadOnly]
        [SuffixLabel("times")]
        private int gcCollectCount;

        [ShowInInspector, ReadOnly]
        [SuffixLabel("sec ago")]
        private float timeSinceLastGC;

        // Internal tracking
        private long lastTotalMemory;
        private float timer;
        private int lastGCCount;
        private float lastGCTime;

        // GUI
        private GUIStyle guiStyle;
        private Rect guiRect = new Rect(10, 10, 300, 120);

        private void Start()
        {
            lastTotalMemory = GC.GetTotalMemory(false);
            lastGCCount = GC.CollectionCount(0);
            lastGCTime = Time.time;

            // Initialize GUI style
            guiStyle = new GUIStyle();
            guiStyle.fontSize = 14;
            guiStyle.normal.textColor = Color.white;
            guiStyle.alignment = TextAnchor.UpperLeft;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            timeSinceLastGC += Time.deltaTime;

            // Check for GC collection
            int currentGCCount = GC.CollectionCount(0);
            if (currentGCCount > lastGCCount)
            {
                gcCollectCount = currentGCCount;
                timeSinceLastGC = 0f;
                lastGCTime = Time.time;
                lastGCCount = currentGCCount;

                if (logToConsole)
                {
                    Debug.LogWarning($"<color=red>[GC]</color> Garbage Collection occurred! Total collections: {gcCollectCount}");
                }
            }

            // Update stats at interval
            if (timer >= updateInterval)
            {
                timer = 0f;
                UpdateStats();
            }
        }

        private void UpdateStats()
        {
            long currentMemory = GC.GetTotalMemory(false);
            long delta = currentMemory - lastTotalMemory;

            // Calculate allocation rate
            allocationRateKBPerSec = (delta / 1024f) / updateInterval;
            currentMemoryMB = currentMemory / 1024f / 1024f;

            if (logToConsole && Mathf.Abs(allocationRateKBPerSec) > 1f) // Only log if significant
            {
                string color = allocationRateKBPerSec > 100f ? "red" : (allocationRateKBPerSec > 50f ? "orange" : "cyan");
                Debug.Log($"<color={color}>[GC]</color> Allocated: {allocationRateKBPerSec:F1} KB/sec | Total: {currentMemoryMB:F1} MB | Last GC: {timeSinceLastGC:F1}s ago");
            }

            lastTotalMemory = currentMemory;
        }

        private void OnGUI()
        {
            if (!showOnScreenGUI) return;

            // Background
            GUI.Box(guiRect, "");

            // Text
            string stats = $"<b>GC MONITOR</b>\n" +
                          $"Memory: {currentMemoryMB:F1} MB\n" +
                          $"Alloc Rate: {allocationRateKBPerSec:F1} KB/sec\n" +
                          $"GC Count: {gcCollectCount}\n" +
                          $"Last GC: {timeSinceLastGC:F1}s ago\n" +
                          $"<color={(allocationRateKBPerSec < 100 ? "lime" : "red")}>{GetPerformanceStatus()}</color>";

            GUI.Label(guiRect, stats, guiStyle);
        }

        private string GetPerformanceStatus()
        {
            if (allocationRateKBPerSec < 50f)
                return "✓ EXCELLENT";
            else if (allocationRateKBPerSec < 100f)
                return "✓ GOOD";
            else if (allocationRateKBPerSec < 500f)
                return "⚠ MODERATE";
            else
                return "✗ POOR";
        }

        private Color GetMemoryColor()
        {
            if (currentMemoryMB < 200f) return Color.green;
            if (currentMemoryMB < 500f) return Color.yellow;
            return Color.red;
        }

        private Color GetAllocationRateColor()
        {
            if (allocationRateKBPerSec < 50f) return Color.green;
            if (allocationRateKBPerSec < 100f) return Color.yellow;
            if (allocationRateKBPerSec < 500f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }

        [TitleGroup("Debug Actions")]
        [Button("Force Garbage Collection", ButtonSizes.Large)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void ForceGC()
        {
            Debug.Log("[GCMonitor] Forcing garbage collection...");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Debug.Log("[GCMonitor] Garbage collection complete.");
        }

        [Button("Reset Stats", ButtonSizes.Medium)]
        private void ResetStats()
        {
            lastTotalMemory = GC.GetTotalMemory(false);
            gcCollectCount = 0;
            lastGCCount = 0;
            timeSinceLastGC = 0f;
            Debug.Log("[GCMonitor] Stats reset.");
        }
    }
}
