using UnityEngine;

namespace WildernessSurvival.Gameplay.Map
{
    /// <summary>
    /// Types of map markers that can be placed in the scene.
    /// Used for spawn points and points of interest.
    /// </summary>
    public enum MapMarkerType
    {
        /// <summary>Player spawn location</summary>
        PlayerSpawn,
        
        /// <summary>Bonfire/base location</summary>
        Bonfire,
        
        /// <summary>Enemy spawn point (placeholder for future)</summary>
        EnemySpawn
    }

    /// <summary>
    /// Types of map zones that define gameplay rules for areas.
    /// Used for build permissions and resource availability.
    /// </summary>
    public enum MapZoneType
    {
        /// <summary>Building is allowed in this area</summary>
        BuildAllowed,
        
        /// <summary>Building is NOT allowed in this area</summary>
        NoBuild,
        
        /// <summary>Wood resources can be gathered here</summary>
        Resource_Wood,
        
        /// <summary>Stone resources can be gathered here</summary>
        Resource_Stone,
        
        /// <summary>Food resources can be gathered here</summary>
        Resource_Food
    }
}
