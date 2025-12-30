using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Captures Unity console logs in a circular buffer for MCP Agent access.
    /// Provides efficient, thread-safe log retrieval without reading Editor.log files.
    /// </summary>
    public class McpConsoleExposer : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static McpConsoleExposer Instance { get; private set; }

        /// <summary>
        /// Reset static for domain reload in Editor (prevents Instance stale).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            Instance = null;
        }

        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Configuration")]
        [InfoBox("MCP Console Exposer captures Unity logs in a circular buffer for efficient retrieval by AI agents.", InfoMessageType.Info)]

        [LabelWidth(150)]
        [Tooltip("Maximum number of log entries to store")]
        [SerializeField] private int maxLogCapacity = 200;

        [LabelWidth(150)]
        [Tooltip("Capture Info level logs")]
        [SerializeField] private bool captureInfo = true;

        [LabelWidth(150)]
        [Tooltip("Capture Warning level logs")]
        [SerializeField] private bool captureWarnings = true;

        [LabelWidth(150)]
        [Tooltip("Capture Error level logs")]
        [SerializeField] private bool captureErrors = true;

        [LabelWidth(150)]
        [Tooltip("Strip Unity rich text tags (e.g., <color>) for AI parsing")]
        [SerializeField] private bool stripRichText = true;

        [LabelWidth(150)]
        [Tooltip("Include stack traces in captured logs")]
        [SerializeField] private bool includeStackTrace = false;

        [LabelWidth(150)]
        [Tooltip("Debug mode - logs capture events to console")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME DATA
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        [ProgressBar(0, "maxLogCapacity", ColorGetter = "GetProgressBarColor")]
        [LabelText("Buffer Usage")]
        private int BufferCount => logBuffer.Count;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Total Logs Captured")]
        private long totalLogsCaptured = 0;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Logs Dropped (Overflow)")]
        private long logsDropped = 0;

        // Thread-safe log buffer
        private readonly Queue<LogEntry> logBuffer = new Queue<LogEntry>();
        private readonly object bufferLock = new object();

        // Regex for stripping rich text
        private static readonly Regex RichTextRegex = new Regex(@"<[^>]*>", RegexOptions.Compiled);

        // ============================================
        // LOG ENTRY STRUCTURE
        // ============================================

        private struct LogEntry
        {
            public LogType type;
            public string message;
            public string stackTrace;
            public DateTime timestamp;

            public override string ToString()
            {
                string typePrefix = type switch
                {
                    LogType.Error => "[Error]",
                    LogType.Warning => "[Warning]",
                    LogType.Log => "[Info]",
                    LogType.Exception => "[Exception]",
                    LogType.Assert => "[Assert]",
                    _ => "[Log]"
                };

                string timeStr = timestamp.ToString("HH:mm:ss");
                return $"{typePrefix} [{timeStr}] {message}";
            }

            public string ToStringWithStackTrace()
            {
                string baseStr = ToString();
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    return $"{baseStr}\n{stackTrace}";
                }
                return baseStr;
            }
        }

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to log messages (threaded for performance)
            Application.logMessageReceivedThreaded += HandleLogMessageThreaded;

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[McpConsoleExposer]</color> Initialized - Buffer capacity: {maxLogCapacity}");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from log messages
            Application.logMessageReceivedThreaded -= HandleLogMessageThreaded;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // LOG CAPTURE (THREAD-SAFE)
        // ============================================

        private void HandleLogMessageThreaded(string message, string stackTrace, LogType type)
        {
            // Filter by type
            if (!ShouldCaptureLogType(type)) return;

            // Strip rich text if enabled
            string processedMessage = stripRichText ? StripRichText(message) : message;
            string processedStackTrace = stripRichText ? StripRichText(stackTrace) : stackTrace;

            // Create log entry
            LogEntry entry = new LogEntry
            {
                type = type,
                message = processedMessage,
                stackTrace = includeStackTrace ? processedStackTrace : "",
                timestamp = DateTime.Now
            };

            // Thread-safe buffer management
            lock (bufferLock)
            {
                // If buffer is full, dequeue oldest entry
                if (logBuffer.Count >= maxLogCapacity)
                {
                    logBuffer.Dequeue();
                    logsDropped++;
                }

                logBuffer.Enqueue(entry);
                totalLogsCaptured++;
            }

            if (debugMode && type == LogType.Error)
            {
                Debug.Log($"<color=orange>[McpConsoleExposer]</color> Captured {type}: {processedMessage.Substring(0, Math.Min(50, processedMessage.Length))}...");
            }
        }

        private bool ShouldCaptureLogType(LogType type)
        {
            return type switch
            {
                LogType.Log => captureInfo,
                LogType.Warning => captureWarnings,
                LogType.Error => captureErrors,
                LogType.Exception => captureErrors,
                LogType.Assert => captureErrors,
                _ => false
            };
        }

        private string StripRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return RichTextRegex.Replace(text, "");
        }

        // ============================================
        // PUBLIC API (MCP AGENT ACCESS)
        // ============================================

        /// <summary>
        /// Get the most recent logs as a formatted string.
        /// Thread-safe and zero-allocation until string building.
        /// </summary>
        /// <param name="count">Number of recent logs to retrieve (default: 50)</param>
        /// <returns>Formatted log string</returns>
        public string GetRecentLogs(int count = 50)
        {
            LogEntry[] entries;

            // Thread-safe copy
            lock (bufferLock)
            {
                int actualCount = Math.Min(count, logBuffer.Count);
                entries = new LogEntry[actualCount];

                // Copy from queue (oldest to newest)
                int startIndex = Math.Max(0, logBuffer.Count - actualCount);
                int i = 0;
                int skipCount = 0;

                foreach (var entry in logBuffer)
                {
                    if (skipCount < startIndex)
                    {
                        skipCount++;
                        continue;
                    }

                    if (i >= actualCount) break;
                    entries[i++] = entry;
                }
            }

            // Build string outside lock (avoid holding lock during string operations)
            StringBuilder sb = new StringBuilder(entries.Length * 100); // Estimate ~100 chars per log
            sb.AppendLine($"=== MCP Console Logs (Recent {entries.Length}) ===");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Captured: {totalLogsCaptured}, Dropped: {logsDropped}");
            sb.AppendLine("=====================================");
            sb.AppendLine();

            foreach (var entry in entries)
            {
                if (includeStackTrace)
                {
                    sb.AppendLine(entry.ToStringWithStackTrace());
                }
                else
                {
                    sb.AppendLine(entry.ToString());
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== End of Logs ===");

            return sb.ToString();
        }

        /// <summary>
        /// Get logs filtered by type.
        /// </summary>
        public string GetLogsByType(LogType type, int count = 50)
        {
            List<LogEntry> filteredEntries = new List<LogEntry>();

            lock (bufferLock)
            {
                foreach (var entry in logBuffer)
                {
                    if (entry.type == type)
                    {
                        filteredEntries.Add(entry);
                        if (filteredEntries.Count >= count) break;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== MCP Console Logs ({type} only, {filteredEntries.Count}) ===");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("=====================================");
            sb.AppendLine();

            foreach (var entry in filteredEntries)
            {
                sb.AppendLine(includeStackTrace ? entry.ToStringWithStackTrace() : entry.ToString());
            }

            sb.AppendLine();
            sb.AppendLine("=== End of Logs ===");

            return sb.ToString();
        }

        /// <summary>
        /// Get only errors and exceptions.
        /// </summary>
        public string GetErrors(int count = 50)
        {
            List<LogEntry> errorEntries = new List<LogEntry>();

            lock (bufferLock)
            {
                foreach (var entry in logBuffer)
                {
                    if (entry.type == LogType.Error || entry.type == LogType.Exception)
                    {
                        errorEntries.Add(entry);
                        if (errorEntries.Count >= count) break;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== MCP Console Errors ({errorEntries.Count}) ===");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("=====================================");
            sb.AppendLine();

            foreach (var entry in errorEntries)
            {
                sb.AppendLine(entry.ToStringWithStackTrace()); // Always include stack trace for errors
            }

            sb.AppendLine();
            sb.AppendLine("=== End of Errors ===");

            return sb.ToString();
        }

        /// <summary>
        /// Clear the log buffer.
        /// </summary>
        public void ClearLogs()
        {
            lock (bufferLock)
            {
                logBuffer.Clear();
                totalLogsCaptured = 0;
                logsDropped = 0;
            }

            if (debugMode)
            {
                Debug.Log("<color=cyan>[McpConsoleExposer]</color> Log buffer cleared");
            }
        }

        // ============================================
        // ODIN INSPECTOR BUTTONS (TESTING)
        // ============================================

        [TitleGroup("Testing & Debug")]
        [Button("📋 Copy Recent Logs to Clipboard", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        [PropertyTooltip("Copy the last 100 logs to clipboard for manual inspection")]
        private void CopyRecentLogsToClipboard()
        {
            string logs = GetRecentLogs(100);
            GUIUtility.systemCopyBuffer = logs;

            Debug.Log($"<color=green>[McpConsoleExposer]</color> ✅ Copied {BufferCount} logs to clipboard!");
            Debug.Log($"<color=cyan>[McpConsoleExposer]</color> Preview:\n{logs.Substring(0, Math.Min(500, logs.Length))}...");
        }

        [TitleGroup("Testing & Debug")]
        [Button("🔴 Copy Errors Only", ButtonSizes.Medium)]
        [GUIColor(1f, 0.4f, 0.4f)]
        [PropertyTooltip("Copy only errors and exceptions to clipboard")]
        private void CopyErrorsToClipboard()
        {
            string errors = GetErrors(50);
            GUIUtility.systemCopyBuffer = errors;

            Debug.Log($"<color=green>[McpConsoleExposer]</color> ✅ Copied errors to clipboard!");
        }

        [TitleGroup("Testing & Debug")]
        [Button("🧹 Clear Log Buffer", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.4f)]
        [PropertyTooltip("Clear all captured logs from the buffer")]
        private void ClearLogBuffer()
        {
            int count = BufferCount;
            ClearLogs();
            Debug.Log($"<color=yellow>[McpConsoleExposer]</color> Cleared {count} logs from buffer");
        }

        [TitleGroup("Testing & Debug")]
        [Button("🧪 Generate Test Logs", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [PropertyTooltip("Generate test logs for debugging")]
        private void GenerateTestLogs()
        {
            Debug.Log("[McpConsoleExposer] Test Info Log");
            Debug.LogWarning("[McpConsoleExposer] Test Warning Log");
            Debug.LogError("[McpConsoleExposer] Test Error Log");
            Debug.Log("<color=green>[McpConsoleExposer]</color> ✅ Generated test logs");
        }

        [TitleGroup("Testing & Debug")]
        [Button("📊 Print Buffer Statistics", ButtonSizes.Medium)]
        [GUIColor(0.6f, 0.8f, 1f)]
        private void PrintStatistics()
        {
            Debug.Log($"=== McpConsoleExposer Statistics ===\n" +
                $"Buffer Count: {BufferCount}/{maxLogCapacity}\n" +
                $"Total Captured: {totalLogsCaptured}\n" +
                $"Logs Dropped: {logsDropped}\n" +
                $"Capture Settings: Info={captureInfo}, Warnings={captureWarnings}, Errors={captureErrors}\n" +
                $"Strip Rich Text: {stripRichText}\n" +
                $"Include Stack Trace: {includeStackTrace}");
        }

        // ============================================
        // ODIN HELPERS
        // ============================================

        private Color GetProgressBarColor()
        {
            float percentage = (float)BufferCount / maxLogCapacity;
            if (percentage < 0.5f) return Color.green;
            if (percentage < 0.8f) return Color.yellow;
            return Color.red;
        }

        // ============================================
        // STATIC CONVENIENCE METHODS (MCP AGENT)
        // ============================================

        /// <summary>
        /// Static convenience method for MCP agents to quickly get logs.
        /// </summary>
        public static string GetLogs(int count = 50)
        {
            if (Instance == null)
            {
                return "[ERROR] McpConsoleExposer instance not found in scene!";
            }
            return Instance.GetRecentLogs(count);
        }

        /// <summary>
        /// Static convenience method to get only errors.
        /// </summary>
        public static string GetErrorLogs(int count = 50)
        {
            if (Instance == null)
            {
                return "[ERROR] McpConsoleExposer instance not found in scene!";
            }
            return Instance.GetErrors(count);
        }
    }
}
