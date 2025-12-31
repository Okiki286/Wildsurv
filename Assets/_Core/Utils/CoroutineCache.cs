using UnityEngine;
using System.Collections.Generic;

namespace WildernessSurvival.Core.Utils
{
    /// <summary>
    /// Cache for commonly used coroutine yield instructions.
    /// Eliminates WaitForSeconds allocations in hot paths.
    ///
    /// USAGE:
    /// Instead of: yield return new WaitForSeconds(0.5f);
    /// Use:        yield return CoroutineCache.WaitForSeconds(0.5f);
    ///
    /// OPTIMIZATION: Pre-cached common intervals + dynamic caching for custom values.
    /// </summary>
    public static class CoroutineCache
    {
        // ============================================
        // COMMON PRE-CACHED VALUES
        // ============================================

        public static readonly WaitForSeconds Wait01 = new WaitForSeconds(0.1f);
        public static readonly WaitForSeconds Wait02 = new WaitForSeconds(0.2f);
        public static readonly WaitForSeconds Wait05 = new WaitForSeconds(0.5f);
        public static readonly WaitForSeconds Wait1 = new WaitForSeconds(1f);
        public static readonly WaitForSeconds Wait2 = new WaitForSeconds(2f);
        public static readonly WaitForSeconds Wait5 = new WaitForSeconds(5f);

        public static readonly WaitForEndOfFrame WaitEndOfFrame = new WaitForEndOfFrame();
        public static readonly WaitForFixedUpdate WaitFixedUpdate = new WaitForFixedUpdate();

        // ============================================
        // DYNAMIC CACHE
        // ============================================

        private static readonly Dictionary<float, WaitForSeconds> waitCache = new Dictionary<float, WaitForSeconds>(32);

        /// <summary>
        /// Gets a cached WaitForSeconds for the specified duration.
        /// Creates and caches new instance if not found.
        /// </summary>
        /// <param name="seconds">Wait duration in seconds</param>
        /// <returns>Cached WaitForSeconds instance</returns>
        public static WaitForSeconds WaitForSeconds(float seconds)
        {
            // Check common pre-cached values first (faster lookup)
            if (seconds == 0.1f) return Wait01;
            if (seconds == 0.2f) return Wait02;
            if (seconds == 0.5f) return Wait05;
            if (seconds == 1f) return Wait1;
            if (seconds == 2f) return Wait2;
            if (seconds == 5f) return Wait5;

            // Check dynamic cache
            if (!waitCache.TryGetValue(seconds, out var wait))
            {
                wait = new WaitForSeconds(seconds);
                waitCache[seconds] = wait;
            }

            return wait;
        }

        /// <summary>
        /// Clears the dynamic cache (not the pre-cached values).
        /// Call this if you want to free memory from rarely-used intervals.
        /// </summary>
        public static void ClearCache()
        {
            waitCache.Clear();
        }

        /// <summary>
        /// Gets the number of dynamically cached intervals.
        /// </summary>
        public static int CacheCount => waitCache.Count;
    }
}
