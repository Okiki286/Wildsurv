using System.Diagnostics;
using UnityEngine;

namespace WildernessSurvival.Core.Utils
{
    /// <summary>
    /// Conditional logging utility that eliminates string allocations in Release builds.
    /// Uses [Conditional] attribute to completely remove calls in non-Editor builds.
    ///
    /// USAGE:
    /// Instead of:
    /// #if UNITY_EDITOR
    /// Debug.Log($"Worker {name} spawned");
    /// #endif
    ///
    /// Use:
    /// DevLog.Log($"Worker {name} spawned");
    ///
    /// OPTIMIZATION: String interpolation is NOT evaluated in Release builds.
    /// Compiler removes the entire method call and parameter evaluation.
    /// </summary>
    public static class DevLog
    {
        /// <summary>
        /// Logs a message (Editor and Development builds only).
        /// String is NOT allocated in Release builds.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// Logs a message with context object (Editor and Development builds only).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message, Object context)
        {
            UnityEngine.Debug.Log(message, context);
        }

        /// <summary>
        /// Logs a warning (Editor and Development builds only).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        /// <summary>
        /// Logs a warning with context (Editor and Development builds only).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message, Object context)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }

        /// <summary>
        /// Logs an error (ALWAYS logged, even in Release builds).
        /// Use this for critical errors that need investigation.
        /// </summary>
        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        /// <summary>
        /// Logs an error with context (ALWAYS logged).
        /// </summary>
        public static void LogError(string message, Object context)
        {
            UnityEngine.Debug.LogError(message, context);
        }

        /// <summary>
        /// Formatted log with color (Editor and Development builds only).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(format, args);
        }

        /// <summary>
        /// Logs with color tag (Editor and Development builds only).
        /// Example: DevLog.LogColored("cyan", "Worker spawned");
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogColored(string color, string message)
        {
            UnityEngine.Debug.Log($"<color={color}>{message}</color>");
        }

        /// <summary>
        /// Logs with green color (success messages).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogSuccess(string message)
        {
            UnityEngine.Debug.Log($"<color=green>{message}</color>");
        }

        /// <summary>
        /// Logs with cyan color (info messages).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInfo(string message)
        {
            UnityEngine.Debug.Log($"<color=cyan>{message}</color>");
        }

        /// <summary>
        /// Logs with yellow color (caution messages).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogCaution(string message)
        {
            UnityEngine.Debug.Log($"<color=yellow>{message}</color>");
        }
    }
}
