using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Map
{
    /// <summary>
    /// Singleton manager for the handcrafted map system.
    /// Catalogs all MapMarkers and MapZones in the scene and provides query APIs.
    /// </summary>
    public class GameMapManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════════════════════════════

        public static GameMapManager Instance { get; private set; }

        // ═══════════════════════════════════════════════════════════════════
        // CATALOGED DATA
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Cataloged Markers")]
        [ReadOnly, ShowInInspector]
        [DictionaryDrawerSettings(IsReadOnly = true)]
        private Dictionary<MapMarkerType, List<MapMarker>> markersByType = new Dictionary<MapMarkerType, List<MapMarker>>();

        [TitleGroup("Cataloged Zones")]
        [ReadOnly, ShowInInspector]
        [DictionaryDrawerSettings(IsReadOnly = true)]
        private Dictionary<MapZoneType, List<MapZone>> zonesByType = new Dictionary<MapZoneType, List<MapZone>>();

        // Raw lists for quick iteration
        private List<MapMarker> allMarkers = new List<MapMarker>();
        private List<MapZone> allZones = new List<MapZone>();

        // ═══════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Total number of markers in the scene.</summary>
        [TitleGroup("Statistics")]
        [ShowInInspector, ReadOnly]
        public int TotalMarkers => allMarkers.Count;

        /// <summary>Total number of zones in the scene.</summary>
        [ShowInInspector, ReadOnly]
        public int TotalZones => allZones.Count;

        // ═══════════════════════════════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameMapManager] Duplicate instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize dictionaries
            InitializeDictionaries();

            // Catalog all markers and zones
            CatalogSceneElements();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Initializes the dictionaries with empty lists for each enum value.
        /// </summary>
        private void InitializeDictionaries()
        {
            markersByType.Clear();
            zonesByType.Clear();

            // Initialize marker type lists
            foreach (MapMarkerType type in System.Enum.GetValues(typeof(MapMarkerType)))
            {
                markersByType[type] = new List<MapMarker>();
            }

            // Initialize zone type lists
            foreach (MapZoneType type in System.Enum.GetValues(typeof(MapZoneType)))
            {
                zonesByType[type] = new List<MapZone>();
            }
        }

        /// <summary>
        /// Finds and catalogs all MapMarkers and MapZones in the scene.
        /// </summary>
        [Button("🔍 Re-Catalog Scene Elements", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void CatalogSceneElements()
        {
            // Clear existing
            allMarkers.Clear();
            allZones.Clear();
            InitializeDictionaries();

            // Find all markers
            MapMarker[] markers = FindObjectsByType<MapMarker>(FindObjectsSortMode.None);
            foreach (var marker in markers)
            {
                if (marker != null)
                {
                    allMarkers.Add(marker);
                    markersByType[marker.type].Add(marker);
                }
            }

            // Find all zones
            MapZone[] zones = FindObjectsByType<MapZone>(FindObjectsSortMode.None);
            foreach (var zone in zones)
            {
                if (zone != null)
                {
                    allZones.Add(zone);
                    zonesByType[zone.type].Add(zone);
                }
            }

            Debug.Log($"<color=cyan>[GameMapManager]</color> Cataloged {allMarkers.Count} markers and {allZones.Count} zones.");

            // Log breakdown
            LogCatalogSummary();
        }

        /// <summary>
        /// Logs a summary of cataloged elements.
        /// </summary>
        private void LogCatalogSummary()
        {
            string summary = "📍 Markers: ";
            foreach (var kvp in markersByType)
            {
                if (kvp.Value.Count > 0)
                    summary += $"{kvp.Key}({kvp.Value.Count}) ";
            }

            summary += "\n📦 Zones: ";
            foreach (var kvp in zonesByType)
            {
                if (kvp.Value.Count > 0)
                    summary += $"{kvp.Key}({kvp.Value.Count}) ";
            }

            Debug.Log($"<color=cyan>[GameMapManager]</color> {summary}");
        }

        // ═══════════════════════════════════════════════════════════════════
        // MARKER API
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gets the position of a marker by type.
        /// Returns the first found, or a random one if multiple exist.
        /// </summary>
        /// <param name="type">Type of marker to find.</param>
        /// <returns>World position of the marker, or Vector3.zero if not found.</returns>
        public Vector3 GetMarkerPosition(MapMarkerType type)
        {
            var marker = GetMarker(type);
            return marker != null ? marker.Position : Vector3.zero;
        }

        /// <summary>
        /// Gets a single marker by type (first found or random if multiple).
        /// </summary>
        public MapMarker GetMarker(MapMarkerType type)
        {
            if (!markersByType.TryGetValue(type, out var list) || list.Count == 0)
            {
                Debug.LogWarning($"[GameMapManager] No marker of type '{type}' found.");
                return null;
            }

            // Return random if multiple exist
            return list.Count == 1 ? list[0] : list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Gets all markers of a specific type.
        /// </summary>
        public List<MapMarker> GetMarkers(MapMarkerType type)
        {
            if (markersByType.TryGetValue(type, out var list))
            {
                return new List<MapMarker>(list); // Return copy to prevent modification
            }
            return new List<MapMarker>();
        }

        /// <summary>
        /// Gets all markers in the scene.
        /// </summary>
        public List<MapMarker> GetAllMarkers()
        {
            return new List<MapMarker>(allMarkers);
        }

        /// <summary>
        /// Gets the nearest marker of a type to a position.
        /// </summary>
        public MapMarker GetNearestMarker(Vector3 position, MapMarkerType type)
        {
            var markers = GetMarkers(type);
            if (markers.Count == 0) return null;

            return markers.OrderBy(m => Vector3.Distance(position, m.Position)).FirstOrDefault();
        }

        /// <summary>
        /// Checks if any marker of the specified type exists.
        /// </summary>
        public bool HasMarker(MapMarkerType type)
        {
            return markersByType.TryGetValue(type, out var list) && list.Count > 0;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ZONE API
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Checks if a point is inside any zone of the specified type.
        /// </summary>
        public bool IsPointInZone(Vector3 point, MapZoneType type)
        {
            if (!zonesByType.TryGetValue(type, out var zones))
                return false;

            foreach (var zone in zones)
            {
                if (zone != null && zone.ContainsPoint(point))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all zones that contain a specific point.
        /// </summary>
        public List<MapZone> GetZonesAtPoint(Vector3 point)
        {
            return allZones.Where(z => z != null && z.ContainsPoint(point)).ToList();
        }

        /// <summary>
        /// Gets all zones of a specific type.
        /// </summary>
        public List<MapZone> GetZones(MapZoneType type)
        {
            if (zonesByType.TryGetValue(type, out var list))
            {
                return new List<MapZone>(list);
            }
            return new List<MapZone>();
        }

        /// <summary>
        /// Gets all zones in the scene.
        /// </summary>
        public List<MapZone> GetAllZones()
        {
            return new List<MapZone>(allZones);
        }

        /// <summary>
        /// Checks if building is allowed at a position.
        /// Returns true if in a BuildAllowed zone and NOT in a NoBuild zone.
        /// </summary>
        public bool CanBuildAt(Vector3 point)
        {
            // Check if in NoBuild zone first (takes priority)
            if (IsPointInZone(point, MapZoneType.NoBuild))
                return false;

            // Check if in BuildAllowed zone
            return IsPointInZone(point, MapZoneType.BuildAllowed);
        }

        /// <summary>
        /// Gets the primary resource type available at a position.
        /// Returns null if no resource zone contains the point.
        /// </summary>
        public MapZoneType? GetResourceTypeAt(Vector3 point)
        {
            // Check each resource type
            if (IsPointInZone(point, MapZoneType.Resource_Wood))
                return MapZoneType.Resource_Wood;
            
            if (IsPointInZone(point, MapZoneType.Resource_Stone))
                return MapZoneType.Resource_Stone;
            
            if (IsPointInZone(point, MapZoneType.Resource_Food))
                return MapZoneType.Resource_Food;

            return null;
        }

        // ═══════════════════════════════════════════════════════════════════
        // DYNAMIC REGISTRATION
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Registers a new marker dynamically (for runtime-spawned markers).
        /// </summary>
        public void RegisterMarker(MapMarker marker)
        {
            if (marker == null) return;

            if (!allMarkers.Contains(marker))
            {
                allMarkers.Add(marker);
                markersByType[marker.type].Add(marker);
                Debug.Log($"<color=cyan>[GameMapManager]</color> Registered marker: {marker.type}");
            }
        }

        /// <summary>
        /// Unregisters a marker (for destroyed markers).
        /// </summary>
        public void UnregisterMarker(MapMarker marker)
        {
            if (marker == null) return;

            allMarkers.Remove(marker);
            if (markersByType.TryGetValue(marker.type, out var list))
            {
                list.Remove(marker);
            }
        }

        /// <summary>
        /// Registers a new zone dynamically.
        /// </summary>
        public void RegisterZone(MapZone zone)
        {
            if (zone == null) return;

            if (!allZones.Contains(zone))
            {
                allZones.Add(zone);
                zonesByType[zone.type].Add(zone);
                Debug.Log($"<color=cyan>[GameMapManager]</color> Registered zone: {zone.type}");
            }
        }

        /// <summary>
        /// Unregisters a zone.
        /// </summary>
        public void UnregisterZone(MapZone zone)
        {
            if (zone == null) return;

            allZones.Remove(zone);
            if (zonesByType.TryGetValue(zone.type, out var list))
            {
                list.Remove(zone);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // DEBUG
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Debug")]
        [Button("📊 Print Catalog Summary", ButtonSizes.Medium)]
        private void DebugPrintSummary()
        {
            Debug.Log("=== GAME MAP MANAGER CATALOG ===");
            
            Debug.Log("📍 MARKERS:");
            foreach (var kvp in markersByType)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value.Count}");
                foreach (var marker in kvp.Value)
                {
                    Debug.Log($"    - {marker.gameObject.name} at {marker.Position}");
                }
            }

            Debug.Log("📦 ZONES:");
            foreach (var kvp in zonesByType)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value.Count}");
                foreach (var zone in kvp.Value)
                {
                    Debug.Log($"    - {zone.gameObject.name}");
                }
            }
        }

        [Button("🎯 Test Point (0,0,0)", ButtonSizes.Medium)]
        private void DebugTestPoint()
        {
            Vector3 testPoint = Vector3.zero;
            
            Debug.Log($"=== Testing point {testPoint} ===");
            Debug.Log($"Can build: {CanBuildAt(testPoint)}");
            Debug.Log($"Resource type: {GetResourceTypeAt(testPoint)?.ToString() ?? "None"}");
            
            var zones = GetZonesAtPoint(testPoint);
            Debug.Log($"Zones containing point: {zones.Count}");
            foreach (var zone in zones)
            {
                Debug.Log($"  - {zone.gameObject.name} ({zone.type})");
            }
        }
    }
}
