using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Systems;

namespace WildernessSurvival.UI.RewardFeel
{
    /// <summary>
    /// Gestisce il pool di ShardPopupView e spawna popup quando shards vengono guadagnati.
    /// Singleton - richiede un'istanza in scena sotto un Canvas.
    /// </summary>
    public class ShardPopupSpawner : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static ShardPopupSpawner Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Pool Settings")]
        [SerializeField] private ShardPopupView popupPrefab;
        [SerializeField] private int poolSize = 24;
        [SerializeField] private Transform poolContainer;

        [TitleGroup("Camera")]
        [SerializeField] private Camera mainCamera;

        [TitleGroup("Fallback")]
        [Tooltip("Offset Y dalla posizione mondo per il popup")]
        [SerializeField] private float worldYOffset = 1.5f;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private ShardPopupView[] pool;
        private int poolIndex = 0;
        private RectTransform canvasRect;
        private Canvas parentCanvas;

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

            InitializePool();
            CacheCanvasInfo();
        }

        private void OnEnable()
        {
            ShardRewardEvents.OnShardsGained += HandleShardsGained;
        }

        private void OnDisable()
        {
            ShardRewardEvents.OnShardsGained -= HandleShardsGained;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void InitializePool()
        {
            if (popupPrefab == null)
            {
                Debug.LogError("[ShardPopupSpawner] popupPrefab is null! Cannot initialize pool.");
                return;
            }

            // Usa questo transform come container se non specificato
            if (poolContainer == null)
                poolContainer = transform;

            pool = new ShardPopupView[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                ShardPopupView popup = Instantiate(popupPrefab, poolContainer);
                popup.gameObject.SetActive(false);
                popup.name = $"ShardPopup_{i}";
                pool[i] = popup;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=cyan>[ShardPopupSpawner]</color> Pool initialized with {poolSize} popups");
            }
#endif
        }

        private void CacheCanvasInfo()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasRect = parentCanvas.GetComponent<RectTransform>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        // ============================================
        // EVENT HANDLER
        // ============================================

        private void HandleShardsGained(int amount, Vector3 worldPosition)
        {
            if (pool == null || pool.Length == 0) return;

            Vector2 screenPos = WorldToCanvasPosition(worldPosition);

            // Spawn popup dal pool
            SpawnPopup(amount, screenPos);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=cyan>[ShardPopupSpawner]</color> Spawned popup: +{amount} at screen {screenPos}");
            }
#endif
        }

        // ============================================
        // POOL MANAGEMENT
        // ============================================

        private void SpawnPopup(int amount, Vector2 screenPosition)
        {
            // Round-robin pool access
            ShardPopupView popup = pool[poolIndex];
            poolIndex = (poolIndex + 1) % poolSize;

            // Se il popup è ancora attivo, forzare reset
            if (popup.gameObject.activeSelf)
            {
                popup.ResetForPool();
            }

            popup.Show(amount, screenPosition, OnPopupComplete);
        }

        private void OnPopupComplete(ShardPopupView popup)
        {
            popup.ResetForPool();
        }

        // ============================================
        // COORDINATE CONVERSION
        // ============================================

        private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return GetFallbackPosition();
                }
            }

            // Offset Y per mostrare sopra il nemico
            Vector3 offsetPos = worldPosition + Vector3.up * worldYOffset;

            // World -> Screen
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(offsetPos);

            // Check se dietro la camera
            if (screenPoint.z < 0)
            {
                return GetFallbackPosition();
            }

            // Screen -> Canvas (per UI in Screen Space - Overlay)
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Per Overlay canvas, screen position = canvas position
                return new Vector2(screenPoint.x, screenPoint.y);
            }

            // Per Camera/World canvas, usa RectTransformUtility
            if (canvasRect != null)
            {
                Vector2 canvasPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    parentCanvas.worldCamera,
                    out canvasPos
                );
                return canvasPos;
            }

            return new Vector2(screenPoint.x, screenPoint.y);
        }

        private Vector2 GetFallbackPosition()
        {
            // Fallback: usa posizione Waystone se disponibile
            if (BaseCenterSystem.Instance != null && BaseCenterSystem.Instance.HasCenter)
            {
                Vector3 waystonePos = BaseCenterSystem.Instance.CenterPosition + Vector3.up * worldYOffset;
                if (mainCamera != null)
                {
                    Vector3 screenPoint = mainCamera.WorldToScreenPoint(waystonePos);
                    return new Vector2(screenPoint.x, screenPoint.y);
                }
            }

            // Ultimo fallback: centro schermo
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.6f);
        }

        // ============================================
        // PUBLIC API (per testing manuale)
        // ============================================

        [TitleGroup("Debug")]
        [Button("Test Spawn Popup", ButtonSizes.Medium)]
        [ShowIf("debugMode")]
        private void DebugTestSpawn()
        {
            Vector2 testPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            SpawnPopup(Random.Range(1, 20), testPos);
        }

        [Button("Stress Test (20 popups)", ButtonSizes.Medium)]
        [ShowIf("debugMode")]
        private void DebugStressTest()
        {
            for (int i = 0; i < 20; i++)
            {
                Vector2 testPos = new Vector2(
                    Screen.width * Random.Range(0.3f, 0.7f),
                    Screen.height * Random.Range(0.4f, 0.7f)
                );
                SpawnPopup(Random.Range(1, 15), testPos);
            }
        }
    }
}
