using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.UI;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Sistema centralizzato per feedback visivo/audio dell'economia.
    /// Si sottoscrive a ResourceSystem.OnResourceChanged e gestisce:
    /// - HUD flash/pulse
    /// - Screen-space popup con pooling (sempre orizzontali, no tilt)
    /// - SFX con rate limiting
    /// - Coalescing per evitare spam
    /// </summary>
    public class EconomyFeedbackSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static EconomyFeedbackSystem Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Popup Prefab")]
        [SerializeField] private WorldResourcePopup popupPrefab;
        [SerializeField] private int poolSize = 32;

        [TitleGroup("World Position")]
        [Tooltip("Offset Y in world-space per posizionare popup sopra oggetti")]
        [SerializeField] private float worldYOffset = 1.8f;

        [TitleGroup("HUD Feedback")]
        [SerializeField] private bool enableHUDPulse = true;

        [TitleGroup("Audio")]
        [SerializeField] private AudioClip gainClip;
        [SerializeField] private AudioClip spendClip;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.5f;

        [TitleGroup("Coalescing")]
        [Tooltip("Finestra temporale per raggruppare cambiamenti stessa risorsa")]
        [SerializeField] private float coalesceWindow = 0.15f;

        [TitleGroup("Rate Limiting")]
        [SerializeField] private float sfxCooldown = 0.08f;

        [TitleGroup("Filter")]
        [Tooltip("Se true, non mostra popup per Production (troppo frequente)")]
        [SerializeField] private bool hideProductionPopups = true;
        [Tooltip("Delta minimo per mostrare popup")]
        [SerializeField] private int minDeltaForPopup = 1;

        [TitleGroup("Stacking (Pixel-based)")]
        [Tooltip("Finestra temporale per considerare popup 'stacked' (stessa posizione)")]
        [SerializeField] private float stackWindow = 0.3f;
        [Tooltip("Spaziatura verticale tra popup stacked (pixel)")]
        [SerializeField] private float stackVerticalSpacingPixels = 50f;
        [Tooltip("Spaziatura orizzontale alternata per popup stacked (pixel)")]
        [SerializeField] private float stackHorizontalSpacingPixels = 25f;
        [Tooltip("Precisione bucket per raggruppare posizioni vicine (es. 2 = arrotonda a 0.5m)")]
        [SerializeField] private float bucketPrecision = 2f;

        [TitleGroup("Canvas")]
        [Tooltip("Sorting order per il canvas popup (alto = sopra tutto)")]
        [SerializeField] private int canvasSortingOrder = 5000;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private WorldResourcePopup[] pool;
        private int poolIndex = 0;
        private Camera mainCamera;
        private AudioSource audioSource;

        // Canvas Screen Space Overlay per popup
        private Canvas popupCanvas;
        private RectTransform popupCanvasRect;

        // Coalescing state (per risorsa)
        private string lastResourceId;
        private int coalescedDelta;
        private Vector3 coalescedPosition;
        private bool coalescedHasWorldPos;
        private float lastEventTime;

        // SFX rate limiting
        private float lastGainSfxTime;
        private float lastSpendSfxTime;

        // Stacking system - bucket-based per evitare sovrapposizioni
        // Key: bucket hash, Value: (count, expiryTime)
        private readonly System.Collections.Generic.Dictionary<int, StackBucketData> stackBuckets =
            new System.Collections.Generic.Dictionary<int, StackBucketData>(16);
        private float lastBucketCleanupTime;
        private const float BUCKET_CLEANUP_INTERVAL = 0.5f;

        private struct StackBucketData
        {
            public int Count;
            public float ExpiryTime;
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

            mainCamera = Camera.main;
            CreatePopupCanvas();
            InitializePool();
            EnsureAudioSource();
        }

        private void OnEnable()
        {
            ResourceSystem.OnResourceChanged += HandleResourceChanged;
        }

        private void OnDisable()
        {
            ResourceSystem.OnResourceChanged -= HandleResourceChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            // Flush coalesced events a fine frame
            FlushCoalescedEvent();

            // Cleanup expired buckets (throttled)
            CleanupExpiredBuckets();
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void CreatePopupCanvas()
        {
            // Crea Canvas Screen Space Overlay per popup
            GameObject canvasObj = new GameObject("PopupCanvas");
            canvasObj.transform.SetParent(transform);

            popupCanvas = canvasObj.AddComponent<Canvas>();
            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            popupCanvas.sortingOrder = canvasSortingOrder;

            // Aggiungi CanvasScaler per gestire risoluzioni diverse
            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // GraphicRaycaster non necessario per popup non interattivi
            // ma lo aggiungiamo per compatibilità
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            popupCanvasRect = canvasObj.GetComponent<RectTransform>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=cyan>[EconomyFeedback]</color> Created Screen Space Overlay canvas with sortingOrder={canvasSortingOrder}");
            }
#endif
        }

        private void InitializePool()
        {
            if (popupPrefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[EconomyFeedbackSystem] popupPrefab is null! World popups disabled.");
#endif
                return;
            }

            if (popupCanvas == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[EconomyFeedbackSystem] popupCanvas is null! World popups disabled.");
#endif
                return;
            }

            pool = new WorldResourcePopup[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                // Istanzia popup come child del canvas
                WorldResourcePopup popup = Instantiate(popupPrefab, popupCanvas.transform);
                popup.gameObject.SetActive(false);
                popup.name = $"ResourcePopup_{i}";

                // Configura parent canvas e camera
                popup.SetParentCanvas(popupCanvas, mainCamera);

                pool[i] = popup;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=cyan>[EconomyFeedback]</color> Pool initialized: {poolSize} popups");
            }
#endif
        }

        private void EnsureAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D
        }

        // ============================================
        // EVENT HANDLER
        // ============================================

        private void HandleResourceChanged(ResourceChangeInfo info)
        {
            float currentTime = Time.time;

            // Filtro: skip Production popup se configurato
            if (hideProductionPopups && info.SourceTag == ResourceSourceTags.Production)
            {
                // Solo HUD feedback per production
                TriggerHUDFeedback(info);
                return;
            }

            // Coalescing: accumula se stessa risorsa in finestra temporale
            if (info.ResourceId == lastResourceId && currentTime - lastEventTime < coalesceWindow)
            {
                coalescedDelta += info.Delta;
                if (info.HasWorldPos)
                {
                    coalescedPosition = info.WorldPos;
                    coalescedHasWorldPos = true;
                }
            }
            else
            {
                // Flush evento precedente
                FlushCoalescedEvent();

                // Inizia nuovo accumulo
                lastResourceId = info.ResourceId;
                coalescedDelta = info.Delta;
                coalescedPosition = info.WorldPos;
                coalescedHasWorldPos = info.HasWorldPos;
            }

            lastEventTime = currentTime;

            // HUD feedback immediato (non coalesced)
            TriggerHUDFeedback(info);
        }

        private void FlushCoalescedEvent()
        {
            if (string.IsNullOrEmpty(lastResourceId) || coalescedDelta == 0) return;

            // World popup (se ha posizione e delta significativo)
            if (coalescedHasWorldPos && Mathf.Abs(coalescedDelta) >= minDeltaForPopup)
            {
                SpawnWorldPopup(lastResourceId, coalescedDelta, coalescedPosition);
            }

            // SFX (rate limited)
            if (coalescedDelta > 0)
            {
                TryPlayGainSfx();
            }
            else if (coalescedDelta < 0)
            {
                TryPlaySpendSfx();
            }

            // Reset
            lastResourceId = null;
            coalescedDelta = 0;
            coalescedHasWorldPos = false;
        }

        // ============================================
        // WORLD POPUP
        // ============================================

        private void SpawnWorldPopup(string resourceId, int delta, Vector3 worldPos)
        {
            if (pool == null || pool.Length == 0) return;

            // Get resource icon
            Sprite icon = null;
            if (ResourceSystem.Instance != null)
            {
                var data = ResourceSystem.Instance.GetResourceData(resourceId);
                if (data != null)
                {
                    icon = data.Icon;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (icon == null && debugMode)
                    {
                        Debug.LogWarning($"[EconomyFeedback] ResourceData '{resourceId}' has no Icon assigned. Check the ScriptableObject.");
                    }
#endif
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else if (debugMode)
                {
                    Debug.LogWarning($"[EconomyFeedback] ResourceData not found for id '{resourceId}'.");
                }
#endif
            }

            // Get popup from pool
            WorldResourcePopup popup = pool[poolIndex];
            poolIndex = (poolIndex + 1) % poolSize;

            // Force reset if still active
            if (popup.gameObject.activeSelf)
            {
                popup.ForceReturn();
            }

            // Calcola stacking offset in pixel per evitare sovrapposizioni
            Vector2 stackOffsetPixels = CalculateStackOffsetPixels(worldPos);

            // Posizione mondo con offset Y
            Vector3 worldPosWithOffset = worldPos + Vector3.up * worldYOffset;

            // Setup e show (passa offset pixel al popup)
            popup.Setup(icon, delta, worldPosWithOffset, stackOffsetPixels, OnPopupComplete);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=cyan>[EconomyFeedback]</color> Spawned popup: {(delta > 0 ? "+" : "")}{delta} {resourceId} at {worldPos} (stack offset: {stackOffsetPixels}px)");
            }
#endif
        }

        private void OnPopupComplete(WorldResourcePopup popup)
        {
            popup.gameObject.SetActive(false);
        }

        // ============================================
        // STACKING SYSTEM (Pixel-based)
        // ============================================

        /// <summary>
        /// Calcola offset in pixel per evitare sovrapposizione popup nella stessa posizione.
        /// Usa bucket hashing per raggruppare posizioni vicine.
        /// </summary>
        private Vector2 CalculateStackOffsetPixels(Vector3 worldPos)
        {
            float currentTime = Time.time;

            // Calcola bucket hash dalla posizione (quantizza x/z)
            int bucketX = Mathf.RoundToInt(worldPos.x * bucketPrecision);
            int bucketZ = Mathf.RoundToInt(worldPos.z * bucketPrecision);
            int bucketHash = bucketX * 73856093 ^ bucketZ * 19349663; // Hash spaziale

            int stackIndex = 0;

            if (stackBuckets.TryGetValue(bucketHash, out StackBucketData bucketData))
            {
                // Bucket esistente - verifica se ancora valido
                if (currentTime < bucketData.ExpiryTime)
                {
                    // Incrementa contatore
                    stackIndex = bucketData.Count;
                    bucketData.Count++;
                    bucketData.ExpiryTime = currentTime + stackWindow; // Estendi finestra
                    stackBuckets[bucketHash] = bucketData;
                }
                else
                {
                    // Bucket scaduto - reset
                    stackBuckets[bucketHash] = new StackBucketData
                    {
                        Count = 1,
                        ExpiryTime = currentTime + stackWindow
                    };
                }
            }
            else
            {
                // Nuovo bucket
                stackBuckets[bucketHash] = new StackBucketData
                {
                    Count = 1,
                    ExpiryTime = currentTime + stackWindow
                };
            }

            // Se primo popup nel bucket, nessun offset
            if (stackIndex == 0)
            {
                return Vector2.zero;
            }

            // Calcola offset in pixel: verticale + orizzontale alternato
            // Pattern: 0=center, 1=up+left, 2=up+right, 3=up+up+left, 4=up+up+right, ...
            float verticalOffset = stackIndex * stackVerticalSpacingPixels;

            // Alternanza sinistra/destra con ampiezza crescente
            int horizontalIndex = (stackIndex + 1) / 2;
            float horizontalSign = (stackIndex % 2 == 1) ? -1f : 1f;
            float horizontalOffset = horizontalSign * horizontalIndex * stackHorizontalSpacingPixels;

            return new Vector2(horizontalOffset, verticalOffset);
        }

        /// <summary>
        /// Pulisce bucket scaduti. Chiamato con throttling per evitare overhead.
        /// </summary>
        private void CleanupExpiredBuckets()
        {
            float currentTime = Time.time;

            // Throttle cleanup
            if (currentTime - lastBucketCleanupTime < BUCKET_CLEANUP_INTERVAL)
            {
                return;
            }
            lastBucketCleanupTime = currentTime;

            // Trova e rimuovi bucket scaduti
            if (stackBuckets.Count == 0) return;

            // Rimuovi bucket scaduti uno alla volta per evitare allocazioni
            int expiredKey = 0;
            bool foundExpired = false;

            foreach (var kvp in stackBuckets)
            {
                if (currentTime >= kvp.Value.ExpiryTime)
                {
                    expiredKey = kvp.Key;
                    foundExpired = true;
                    break; // Rimuovi uno alla volta per evitare modifiche durante iterazione
                }
            }

            if (foundExpired)
            {
                stackBuckets.Remove(expiredKey);
            }
        }

        // ============================================
        // HUD FEEDBACK
        // ============================================

        private void TriggerHUDFeedback(ResourceChangeInfo info)
        {
            if (!enableHUDPulse) return;

            // Chiama ResourceDisplayUI se disponibile
            if (ResourceDisplayUI.Instance != null)
            {
                ResourceDisplayUI.Instance.AnimateResourceChange(info.ResourceId, info.Delta);
            }
        }

        // ============================================
        // SFX
        // ============================================

        private void TryPlayGainSfx()
        {
            if (gainClip == null) return;

            float currentTime = Time.time;
            if (currentTime - lastGainSfxTime < sfxCooldown) return;

            audioSource.PlayOneShot(gainClip, sfxVolume);
            lastGainSfxTime = currentTime;
        }

        private void TryPlaySpendSfx()
        {
            if (spendClip == null) return;

            float currentTime = Time.time;
            if (currentTime - lastSpendSfxTime < sfxCooldown) return;

            audioSource.PlayOneShot(spendClip, sfxVolume);
            lastSpendSfxTime = currentTime;
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Cache main camera (chiamare se camera cambia a runtime).
        /// </summary>
        public void SetMainCamera(Camera cam)
        {
            mainCamera = cam;

            // Aggiorna camera in tutti i popup del pool
            if (pool != null)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    if (pool[i] != null)
                    {
                        pool[i].SetParentCanvas(popupCanvas, mainCamera);
                    }
                }
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Test Gain Popup", ButtonSizes.Medium)]
        private void DebugTestGain()
        {
            var info = new ResourceChangeInfo("shard", 10, 100, transform.position + transform.forward * 3f, true, "Debug");
            HandleResourceChanged(info);
            FlushCoalescedEvent();
        }

        [Button("Test Spend Popup", ButtonSizes.Medium)]
        private void DebugTestSpend()
        {
            var info = new ResourceChangeInfo("warmwood", -25, 75, transform.position + transform.forward * 3f, true, "Debug");
            HandleResourceChanged(info);
            FlushCoalescedEvent();
        }

        [Button("Stress Test (20 events)", ButtonSizes.Medium)]
        private void DebugStressTest()
        {
            for (int i = 0; i < 20; i++)
            {
                Vector3 pos = transform.position + Random.insideUnitSphere * 5f;
                pos.y = transform.position.y;
                var info = new ResourceChangeInfo("shard", Random.Range(1, 15), 100, pos, true, "Debug");
                HandleResourceChanged(info);
            }
        }

        [Button("Test Stacking (3 same pos)", ButtonSizes.Medium)]
        private void DebugTestStacking()
        {
            // Simula build cost con 3 risorse diverse nella stessa posizione
            Vector3 pos = transform.position + transform.forward * 3f;
            string[] resources = { "warmwood", "shard", "food" };
            int[] deltas = { -25, -10, -5 };

            for (int i = 0; i < 3; i++)
            {
                // Bypassa coalescing per test immediato
                SpawnWorldPopup(resources[i], deltas[i], pos);
            }

            Debug.Log($"<color=yellow>[EconomyFeedback]</color> Spawned 3 stacked popups at {pos}");
        }

        [Button("Clear Stack Buckets", ButtonSizes.Medium)]
        private void DebugClearBuckets()
        {
            stackBuckets.Clear();
            Debug.Log("<color=yellow>[EconomyFeedback]</color> Stack buckets cleared");
        }
#endif
    }
}
