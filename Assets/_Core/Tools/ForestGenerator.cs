using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildernessSurvival.Tools
{
    /// <summary>
    /// Procedural forest generator that populates an area with trees and rocks.
    /// Uses raycasting to place objects on the ground and supports Undo operations.
    /// </summary>
    public class ForestGenerator : MonoBehaviour
    {
        [Header("Generation Area")]
        [Tooltip("Radius of the circular area to populate with objects.")]
        public float radius = 50f;

        [Header("Density Settings")]
        [Tooltip("Number of trees to spawn.")]
        public int treeDensity = 100;

        [Tooltip("Number of rocks to spawn.")]
        public int rockDensity = 20;

        [Header("Scale Randomization")]
        [Tooltip("Minimum scale multiplier for spawned objects.")]
        public float minScale = 0.8f;

        [Tooltip("Maximum scale multiplier for spawned objects.")]
        public float maxScale = 1.2f;

        [Header("Prefab Lists")]
        [Tooltip("List of tree prefabs to randomly choose from.")]
        public List<GameObject> treePrefabs = new List<GameObject>();

        [Tooltip("List of rock prefabs to randomly choose from.")]
        public List<GameObject> rockPrefabs = new List<GameObject>();

        [Header("Raycast Settings")]
        [Tooltip("Layer mask for ground detection. Only objects on these layers will be considered as valid spawn points.")]
        public LayerMask groundLayer = ~0; // Default to everything

        [Tooltip("Maximum raycast distance from above.")]
        public float raycastHeight = 100f;

        [Tooltip("Height offset above the raycast start position.")]
        public float spawnHeightOffset = 0f;

        /// <summary>
        /// Clears all child objects of this transform immediately.
        /// In editor mode, uses DestroyImmediate. In play mode, uses Destroy.
        /// </summary>
        public void ClearForest()
        {
#if UNITY_EDITOR
            // Record undo for clear operation
            Undo.RegisterFullObjectHierarchyUndo(gameObject, "Clear Forest");
#endif

            // Collect children first to avoid modifying collection during iteration
            List<GameObject> children = new List<GameObject>();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                children.Add(transform.GetChild(i).gameObject);
            }

            // Destroy all children
            foreach (var child in children)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child);
                }
                else
                {
                    Destroy(child);
                }
#else
                Destroy(child);
#endif
            }

            Debug.Log($"[ForestGenerator] Cleared {children.Count} objects.");
        }

        /// <summary>
        /// Generates trees and rocks within the specified radius.
        /// Uses raycasting to find valid ground positions.
        /// </summary>
        public void Generate()
        {
            if (treePrefabs.Count == 0 && rockPrefabs.Count == 0)
            {
                Debug.LogWarning("[ForestGenerator] No prefabs assigned. Please add tree and/or rock prefabs.");
                return;
            }

#if UNITY_EDITOR
            Undo.SetCurrentGroupName("Generate Forest");
            int undoGroup = Undo.GetCurrentGroup();
#endif

            int treesSpawned = 0;
            int rocksSpawned = 0;

            // Spawn trees
            if (treePrefabs.Count > 0)
            {
                for (int i = 0; i < treeDensity; i++)
                {
                    if (TrySpawnObject(treePrefabs))
                    {
                        treesSpawned++;
                    }
                }
            }

            // Spawn rocks
            if (rockPrefabs.Count > 0)
            {
                for (int i = 0; i < rockDensity; i++)
                {
                    if (TrySpawnObject(rockPrefabs))
                    {
                        rocksSpawned++;
                    }
                }
            }

#if UNITY_EDITOR
            Undo.CollapseUndoOperations(undoGroup);
#endif

            Debug.Log($"[ForestGenerator] Generated {treesSpawned}/{treeDensity} trees and {rocksSpawned}/{rockDensity} rocks.");
        }

        /// <summary>
        /// Attempts to spawn an object from the given prefab list at a random position.
        /// </summary>
        /// <param name="prefabList">List of prefabs to choose from.</param>
        /// <returns>True if spawn was successful, false otherwise.</returns>
        private bool TrySpawnObject(List<GameObject> prefabList)
        {
            if (prefabList == null || prefabList.Count == 0) return false;

            // Get random point inside radius (circular distribution)
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, raycastHeight, randomCircle.y);

            // Raycast down to find ground
            if (Physics.Raycast(spawnPosition, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
            {
                // Pick random prefab
                GameObject prefab = prefabList[Random.Range(0, prefabList.Count)];
                if (prefab == null) return false;

                // Calculate spawn position with offset
                Vector3 finalPosition = hit.point + Vector3.up * spawnHeightOffset;

                // Random Y rotation
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                // Random scale
                float scale = Random.Range(minScale, maxScale);

                // Instantiate
                GameObject instance;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
                    instance.transform.position = finalPosition;
                    instance.transform.rotation = rotation;
                    instance.transform.localScale = prefab.transform.localScale * scale;
                    Undo.RegisterCreatedObjectUndo(instance, "Spawn Forest Object");
                }
                else
                {
                    instance = Instantiate(prefab, finalPosition, rotation, transform);
                    instance.transform.localScale = prefab.transform.localScale * scale;
                }
#else
                instance = Instantiate(prefab, finalPosition, rotation, transform);
                instance.transform.localScale = prefab.transform.localScale * scale;
#endif

                return true;
            }

            return false;
        }

        /// <summary>
        /// Draws the generation area in the Scene view.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, radius);

            // Draw filled disc
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.1f);
            // Draw a simple representation
            int segments = 32;
            Vector3 prevPoint = transform.position + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 nextPoint = transform.position + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
    }
}
