using System.Text;
using UnityEngine;

namespace WildernessSurvival.UI.Utils
{
    /// <summary>
    /// Static utility for formatting UI text without string allocations.
    /// Uses cached StringBuilder to eliminate GC from dynamic text updates.
    ///
    /// USAGE:
    /// Instead of: text.text = $"Wood: {amount}";
    /// Use:        text.text = TextFormatters.FormatResource("Wood", amount);
    ///
    /// OPTIMIZATION: Single StringBuilder instance reused for all formatting.
    /// Safe for single-threaded UI updates (Unity main thread).
    /// </summary>
    public static class TextFormatters
    {
        // Cached StringBuilder (reused across all calls)
        private static readonly StringBuilder sb = new StringBuilder(128);

        // ============================================
        // RESOURCE FORMATTING
        // ============================================

        /// <summary>
        /// Formats resource name and amount: "Wood: 150"
        /// </summary>
        public static string FormatResource(string name, float amount)
        {
            sb.Clear();
            sb.Append(name);
            sb.Append(": ");
            sb.Append(Mathf.RoundToInt(amount));
            return sb.ToString();
        }

        /// <summary>
        /// Formats resource with icon: "[W] 150"
        /// </summary>
        public static string FormatResourceWithIcon(string icon, float amount)
        {
            sb.Clear();
            sb.Append("[");
            sb.Append(icon);
            sb.Append("] ");
            sb.Append(Mathf.RoundToInt(amount));
            return sb.ToString();
        }

        /// <summary>
        /// Formats resource with fraction: "Wood: 150 / 200"
        /// </summary>
        public static string FormatResourceWithMax(string name, float current, float max)
        {
            sb.Clear();
            sb.Append(name);
            sb.Append(": ");
            sb.Append(Mathf.RoundToInt(current));
            sb.Append(" / ");
            sb.Append(Mathf.RoundToInt(max));
            return sb.ToString();
        }

        // ============================================
        // WORKER/STRUCTURE FORMATTING
        // ============================================

        /// <summary>
        /// Formats worker count: "Workers: 12 / 20"
        /// </summary>
        public static string FormatWorkerCount(int current, int max)
        {
            sb.Clear();
            sb.Append("Workers: ");
            sb.Append(current);
            sb.Append(" / ");
            sb.Append(max);
            return sb.ToString();
        }

        /// <summary>
        /// Formats health bar: "HP: 85 / 100"
        /// </summary>
        public static string FormatHealth(float current, float max)
        {
            sb.Clear();
            sb.Append("HP: ");
            sb.Append(Mathf.RoundToInt(current));
            sb.Append(" / ");
            sb.Append(Mathf.RoundToInt(max));
            return sb.ToString();
        }

        /// <summary>
        /// Formats progress percentage: "75%"
        /// </summary>
        public static string FormatPercentage(float value)
        {
            sb.Clear();
            sb.Append(Mathf.RoundToInt(value * 100f));
            sb.Append("%");
            return sb.ToString();
        }

        // ============================================
        // TIME FORMATTING
        // ============================================

        /// <summary>
        /// Formats time in minutes:seconds: "05:32"
        /// </summary>
        public static string FormatTime(float totalSeconds)
        {
            int minutes = Mathf.FloorToInt(totalSeconds / 60f);
            int seconds = Mathf.FloorToInt(totalSeconds % 60f);

            sb.Clear();
            if (minutes < 10) sb.Append("0");
            sb.Append(minutes);
            sb.Append(":");
            if (seconds < 10) sb.Append("0");
            sb.Append(seconds);
            return sb.ToString();
        }

        /// <summary>
        /// Formats day number: "Day 7"
        /// </summary>
        public static string FormatDay(int dayNumber)
        {
            sb.Clear();
            sb.Append("Day ");
            sb.Append(dayNumber);
            return sb.ToString();
        }

        // ============================================
        // COST FORMATTING
        // ============================================

        /// <summary>
        /// Formats resource cost: "Wood: 50"
        /// With color coding based on affordability.
        /// </summary>
        public static string FormatCost(string resourceName, float amount, bool canAfford)
        {
            sb.Clear();
            if (!canAfford) sb.Append("<color=red>");
            sb.Append(resourceName);
            sb.Append(": ");
            sb.Append(Mathf.RoundToInt(amount));
            if (!canAfford) sb.Append("</color>");
            return sb.ToString();
        }

        // ============================================
        // GENERIC FORMATTERS
        // ============================================

        /// <summary>
        /// Formats label + value: "Speed: 5.2"
        /// </summary>
        public static string FormatLabelValue(string label, float value, int decimals = 1)
        {
            sb.Clear();
            sb.Append(label);
            sb.Append(": ");
            sb.Append(value.ToString($"F{decimals}"));
            return sb.ToString();
        }

        /// <summary>
        /// Formats label + integer value: "Count: 42"
        /// </summary>
        public static string FormatLabelInt(string label, int value)
        {
            sb.Clear();
            sb.Append(label);
            sb.Append(": ");
            sb.Append(value);
            return sb.ToString();
        }

        /// <summary>
        /// Appends multiple values to a comma-separated list: "A, B, C"
        /// </summary>
        public static string FormatList(params string[] items)
        {
            sb.Clear();
            for (int i = 0; i < items.Length; i++)
            {
                sb.Append(items[i]);
                if (i < items.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }

        // ============================================
        // UTILITY
        // ============================================

        /// <summary>
        /// Clears the StringBuilder cache.
        /// Normally not needed, but available if you want to free memory.
        /// </summary>
        public static void Clear()
        {
            sb.Clear();
        }

        /// <summary>
        /// Gets the current StringBuilder capacity.
        /// Useful for debugging memory usage.
        /// </summary>
        public static int Capacity => sb.Capacity;
    }
}
