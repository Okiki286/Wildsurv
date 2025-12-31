using UnityEngine;
using UnityEngine.Pool;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace WildernessSurvival.UI.Pooling
{
    /// <summary>
    /// Generic UI element pooling component using Unity's native ObjectPool.
    /// Attach this to a UI container (e.g., menu panel) to pool child elements.
    ///
    /// USAGE:
    /// 1. Attach to parent container GameObject
    /// 2. Assign prefab to pool
    /// 3. Call Get<T>() to spawn elements
    /// 4. Call ReturnAll() to return all active elements to pool
    ///
    /// OPTIMIZATION: Zero-allocation UI lifecycle for menus, lists, grids.
    /// </summary>
    public class UIElementPool : MonoBehaviour
    {
        [TitleGroup("Pool Configuration")]
        [SerializeField]
        [Required]
        [AssetsOnly]
        [Tooltip("UI prefab to pool (must have a component of type T when calling Get<T>())")]
        private GameObject elementPrefab;

        [SerializeField]
        [Required]
        [ChildGameObjectsOnly]
        [Tooltip("Parent transform where pooled elements will be placed")]
        private Transform container;

        [SerializeField]
        [Range(0, 50)]
        [Tooltip("Number of elements to pre-instantiate at start (reduces first spawn spike)")]
        private int prewarmCount = 5;

        [SerializeField]
        [Range(10, 200)]
        [Tooltip("Maximum pool size (elements beyond this are destroyed)")]
        private int maxPoolSize = 50;

        [TitleGroup("Runtime Stats")]
        [ShowInInspector, ReadOnly]
        [ProgressBar(0, "maxPoolSize", ColorGetter = "GetPoolUsageColor")]
        private int activeCount = 0;

        [ShowInInspector, ReadOnly]
        private int pooledCount = 0;

        [ShowInInspector, ReadOnly]
        [InfoBox("Active + Pooled = Total instances in memory", InfoMessageType.None)]
        private int totalInstances => activeCount + pooledCount;

        // Native Unity pool
        private ObjectPool<GameObject> pool;

        // Track active elements for ReturnAll()
        private HashSet<GameObject> activeElements = new HashSet<GameObject>();

        private void Awake()
        {
            InitializePool();
        }

        /// <summary>
        /// Initializes the native Unity ObjectPool with callbacks.
        /// </summary>
        private void InitializePool()
        {
            if (elementPrefab == null)
            {
                Debug.LogError($"[UIElementPool] Element prefab not assigned on {gameObject.name}!");
                return;
            }

            if (container == null)
            {
                Debug.LogWarning($"[UIElementPool] Container not assigned, using self as container");
                container = transform;
            }

            // Create native pool
            pool = new ObjectPool<GameObject>(
                createFunc: CreateElement,
                actionOnGet: OnGetElement,
                actionOnRelease: OnReleaseElement,
                actionOnDestroy: OnDestroyElement,
                collectionCheck: false, // Disable in release for performance
                defaultCapacity: prewarmCount,
                maxSize: maxPoolSize
            );

            // Pre-warm pool
            if (prewarmCount > 0)
            {
                List<GameObject> temp = new List<GameObject>(prewarmCount);
                for (int i = 0; i < prewarmCount; i++)
                {
                    temp.Add(pool.Get());
                }
                foreach (var element in temp)
                {
                    pool.Release(element);
                }

                pooledCount = prewarmCount;
                Debug.Log($"<color=cyan>[UIElementPool]</color> Pre-warmed pool '{gameObject.name}' with {prewarmCount} elements");
            }
        }

        /// <summary>
        /// Factory method to create new UI elements.
        /// </summary>
        private GameObject CreateElement()
        {
            GameObject element = Instantiate(elementPrefab, container);
            element.SetActive(false); // Start inactive
            return element;
        }

        /// <summary>
        /// Called when an element is taken from the pool.
        /// </summary>
        private void OnGetElement(GameObject element)
        {
            element.SetActive(true);
            element.transform.SetParent(container, false); // Keep RectTransform settings
            activeElements.Add(element);
            activeCount++;
            pooledCount--;
        }

        /// <summary>
        /// Called when an element is returned to the pool.
        /// </summary>
        private void OnReleaseElement(GameObject element)
        {
            element.SetActive(false);
            element.transform.SetParent(container, false);
            activeElements.Remove(element);
            activeCount--;
            pooledCount++;
        }

        /// <summary>
        /// Called when an element exceeds maxPoolSize and is destroyed.
        /// </summary>
        private void OnDestroyElement(GameObject element)
        {
            if (element != null)
            {
                Destroy(element);
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Gets an element from the pool and returns its component.
        /// </summary>
        /// <typeparam name="T">Component type to retrieve</typeparam>
        /// <returns>Component of type T, or null if not found</returns>
        public T Get<T>() where T : Component
        {
            if (pool == null)
            {
                Debug.LogError($"[UIElementPool] Pool not initialized on {gameObject.name}!");
                return null;
            }

            GameObject element = pool.Get();
            T component = element.GetComponent<T>();

            if (component == null)
            {
                Debug.LogError($"[UIElementPool] Element prefab missing component {typeof(T).Name}!");
                pool.Release(element); // Return to pool immediately
                return null;
            }

            return component;
        }

        /// <summary>
        /// Gets a raw GameObject from the pool (no component required).
        /// </summary>
        public GameObject Get()
        {
            if (pool == null)
            {
                Debug.LogError($"[UIElementPool] Pool not initialized on {gameObject.name}!");
                return null;
            }

            return pool.Get();
        }

        /// <summary>
        /// Returns a single element to the pool.
        /// </summary>
        public void Return(GameObject element)
        {
            if (pool == null || element == null) return;

            if (activeElements.Contains(element))
            {
                pool.Release(element);
            }
            else
            {
                Debug.LogWarning($"[UIElementPool] Attempted to return element not from this pool: {element.name}");
            }
        }

        /// <summary>
        /// Returns a single element to the pool by component reference.
        /// </summary>
        public void Return<T>(T component) where T : Component
        {
            if (component != null)
            {
                Return(component.gameObject);
            }
        }

        /// <summary>
        /// Returns ALL active elements to the pool at once.
        /// Use this when closing a menu or clearing a list.
        /// </summary>
        public void ReturnAll()
        {
            if (pool == null) return;

            // Copy to temp list to avoid modification during iteration
            List<GameObject> temp = new List<GameObject>(activeElements);
            foreach (var element in temp)
            {
                if (element != null)
                {
                    pool.Release(element);
                }
            }

            Debug.Log($"<color=cyan>[UIElementPool]</color> Returned {temp.Count} elements to pool '{gameObject.name}'");
        }

        /// <summary>
        /// Clears the pool and destroys all pooled elements.
        /// Called automatically on destroy.
        /// </summary>
        public void ClearPool()
        {
            if (pool == null) return;

            pool.Clear();
            activeElements.Clear();
            activeCount = 0;
            pooledCount = 0;
        }

        private void OnDestroy()
        {
            ClearPool();
        }

        // ============================================
        // ODIN INSPECTOR HELPERS
        // ============================================

        private Color GetPoolUsageColor()
        {
            float usage = (float)activeCount / maxPoolSize;
            if (usage < 0.5f) return Color.green;
            if (usage < 0.8f) return Color.yellow;
            return Color.red;
        }

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Test Get Element", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void DebugGetElement()
        {
            var element = Get();
            if (element != null)
            {
                Debug.Log($"[UIElementPool] Got element: {element.name}");
            }
        }

        [Button("Test Return All", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.6f, 0.4f)]
        private void DebugReturnAll()
        {
            ReturnAll();
        }

        [Button("Clear Pool", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugClearPool()
        {
            ClearPool();
            Debug.Log($"[UIElementPool] Pool cleared on {gameObject.name}");
        }
#endif
    }
}
