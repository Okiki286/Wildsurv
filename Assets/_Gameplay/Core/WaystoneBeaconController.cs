using UnityEngine;
using System;
using WildernessSurvival.Core.Events;
using WildernessSurvival.Core.Managers;
using WildernessSurvival.Gameplay.Combat;
using WildernessSurvival.Gameplay.Enemies;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace WildernessSurvival.Gameplay.Core
{
    /// <summary>
    /// Controller principale del WaystoneBeacon (Pietra-Faro Runica).
    /// Obiettivo centrale che i nemici devono attaccare.
    /// Si integra con il ciclo Day/Night per effetti visuali e rigenerazione.
    /// </summary>
    public class WaystoneBeaconController : MonoBehaviour, IDamageable
    {
        // ============================================
        // CONFIGURAZIONE
        // ============================================

        #if UNITY_EDITOR
        [TitleGroup("Statistiche")]
        #endif
        [Header("Health")]
        [SerializeField] private int maxHP = 200;
        
        #if UNITY_EDITOR
        [ShowInInspector, ReadOnly, ProgressBar(0, 200, ColorGetter = "GetHPColor")]
        #endif
        private int currentHP;

        #if UNITY_EDITOR
        [TitleGroup("Statistiche")]
        #endif
        [Header("Influence")]
        [Tooltip("Raggio di influenza del beacon (solo per gizmos/debug)")]
        [SerializeField] private float influenceRadius = 18f;

        #if UNITY_EDITOR
        [TitleGroup("DayNight Integration")]
        [InfoBox("Assegna i GameEvent per attivare gli effetti giorno/notte. Se vuoti, il beacon funziona comunque.")]
        #endif
        [Header("Day/Night Events (Optional)")]
        [SerializeField] private GameEvent onDayStarted;
        [SerializeField] private GameEvent onNightStarted;

        #if UNITY_EDITOR
        [TitleGroup("Rigenerazione")]
        #endif
        [Header("Day Repair")]
        [Tooltip("HP rigenerati ogni tick durante il giorno")]
        [SerializeField] private int dayRepairAmount = 2;
        
        [Tooltip("Intervallo in secondi tra i tick di riparazione")]
        [SerializeField] private float dayRepairInterval = 5f;

        #if UNITY_EDITOR
        [TitleGroup("Debug")]
        [ToggleLeft]
        #endif
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private bool isNight = false;
        private bool isDestroyed = false;
        private float repairTimer = 0f;
        
        // Riferimenti visuali
        private Light beaconLight;
        private Transform crystalTransform;
        private Renderer crystalRenderer;
        private Material cachedCrystalMaterial;

        // ============================================
        // EVENTS
        // ============================================

        /// <summary>Invocato quando il beacon subisce danno (amount, currentHP)</summary>
        public event Action<int, int> OnDamaged;
        
        /// <summary>Invocato quando il beacon viene distrutto</summary>
        public event Action OnDestroyed;

        // ============================================
        // PROPERTIES
        // ============================================

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public float HealthPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;
        public bool IsDestroyed => isDestroyed;
        public float InfluenceRadius => influenceRadius;
        public bool IsNightMode => isNight;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void Start()
        {
            // Cache riferimenti visuali se esistono
            CacheVisualReferences();
            
            // Sottoscrivi ai GameEvent se assegnati
            SubscribeToEvents();
            
            // Imposta stato iniziale (assume giorno)
            SetDayMode();
        }

        private void OnDestroy()
        {
            CancelInvoke(); // Cancella RestoreEmissionColor pending
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Gestisci rigenerazione solo di giorno e se non distrutto
            if (!isNight && !isDestroyed && currentHP < maxHP)
            {
                HandleDayRepair();
            }
        }

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        /// <summary>
        /// Inizializza lo stato del beacon se necessario.
        /// Può essere chiamato esternamente per reset.
        /// </summary>
        public void InitializeIfNeeded()
        {
            if (currentHP <= 0 && !isDestroyed)
            {
                currentHP = maxHP;
            }
        }

        private void CacheVisualReferences()
        {
            // Cerca light nel figlio
            beaconLight = GetComponentInChildren<Light>();
            
            // Cerca il cristallo
            Transform crystal = transform.Find("Crystal");
            if (crystal != null)
            {
                crystalTransform = crystal;
                crystalRenderer = crystal.GetComponent<Renderer>();
                
                // Cache material per evitare leak (renderer.material crea istanza)
                if (crystalRenderer != null)
                {
                    cachedCrystalMaterial = crystalRenderer.material;
                }
            }
        }

        // ============================================
        // EVENT SUBSCRIPTION
        // ============================================

        private void SubscribeToEvents()
        {
            if (onDayStarted != null)
            {
                onDayStarted.AddListener(OnDayStartedHandler);
            }
            
            if (onNightStarted != null)
            {
                onNightStarted.AddListener(OnNightStartedHandler);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (onDayStarted != null)
            {
                onDayStarted.RemoveListener(OnDayStartedHandler);
            }
            
            if (onNightStarted != null)
            {
                onNightStarted.RemoveListener(OnNightStartedHandler);
            }
        }

        // ============================================
        // DAY/NIGHT HANDLERS
        // ============================================

        private void OnDayStartedHandler()
        {
            SetDayMode();
            
            if (debugMode)
            {
                Debug.Log("<color=yellow>[WaystoneBeacon]</color> ☀️ Giorno - Beacon calmo, rigenerazione attiva");
            }
        }

        private void OnNightStartedHandler()
        {
            SetNightMode();
            
            if (debugMode)
            {
                Debug.Log("<color=blue>[WaystoneBeacon]</color> 🌙 Notte - Beacon ACCESO!");
            }
        }

        private void SetDayMode()
        {
            isNight = false;
            repairTimer = 0f;
            
            // Effetti visuali: luce bassa, cristallo calmo
            if (beaconLight != null)
            {
                beaconLight.intensity = 0.5f;
                beaconLight.color = new Color(0.8f, 0.9f, 1f); // Bianco-azzurro tenue
            }
            
            if (crystalTransform != null)
            {
                crystalTransform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
            }
            
            // Cambia colore emission (tenue di giorno) - ciano HDR basso
            UpdateCrystalEmission(new Color(0f, 0.8f, 1f) * 1.5f);
        }

        private void SetNightMode()
        {
            isNight = true;
            
            // Effetti visuali: luce forte, cristallo "attivo"
            if (beaconLight != null)
            {
                beaconLight.intensity = 2.5f;
                beaconLight.color = new Color(0.4f, 0.7f, 1f); // Azzurro brillante
            }
            
            if (crystalTransform != null)
            {
                crystalTransform.localScale = new Vector3(0.5f, 1f, 0.5f);
            }
            
            // Colore emission intenso - ciano HDR forte
            UpdateCrystalEmission(new Color(0f, 0.8f, 1f) * 4f);
        }

        private void UpdateCrystalEmission(Color emissionColor)
        {
            // Usa materiale cached per evitare leak
            if (cachedCrystalMaterial != null && cachedCrystalMaterial.HasProperty("_EmissionColor"))
            {
                cachedCrystalMaterial.SetColor("_EmissionColor", emissionColor);
                cachedCrystalMaterial.EnableKeyword("_EMISSION");
            }
        }

        // ============================================
        // DAY REPAIR
        // ============================================

        private void HandleDayRepair()
        {
            repairTimer += Time.deltaTime;
            
            if (repairTimer >= dayRepairInterval)
            {
                repairTimer = 0f;
                
                int oldHP = currentHP;
                currentHP = Mathf.Min(currentHP + dayRepairAmount, maxHP);
                
                if (debugMode && currentHP > oldHP)
                {
                    Debug.Log($"<color=green>[WaystoneBeacon]</color> 🔧 Riparato +{currentHP - oldHP} HP ({currentHP}/{maxHP})");
                }
            }
        }

        // ============================================
        // DAMAGE SYSTEM (IDamageable)
        // ============================================

        // IDamageable explicit implementation
        float IDamageable.CurrentHealth => currentHP;
        float IDamageable.MaxHealth => maxHP;
        bool IDamageable.IsAlive => !isDestroyed && currentHP > 0;

        /// <summary>
        /// Applica danno al beacon (legacy, int)
        /// </summary>
        public void TakeDamage(int amount)
        {
            TakeDamage((float)amount, DamageType.None);
        }

        /// <summary>
        /// Applica danno al beacon con tipo danno (IDamageable)
        /// </summary>
        public void TakeDamage(float damage, DamageType damageType = DamageType.None)
        {
            if (isDestroyed || damage <= 0) return;

            // Waystone non ha resistenze, ignora damageType
            int amount = Mathf.CeilToInt(damage);
            currentHP = Mathf.Max(0, currentHP - amount);

            if (debugMode)
            {
                Debug.Log($"<color=red>[WaystoneBeacon]</color> Danno! -{amount} HP ({currentHP}/{maxHP})");
            }

            OnDamaged?.Invoke(amount, currentHP);

            // Flash visivo sul danno
            FlashDamage();

            if (currentHP <= 0)
            {
                Die();
            }
        }

        private void FlashDamage()
        {
            // Effetto flash semplice cambiando colore temporaneo
            if (crystalRenderer != null && crystalRenderer.material != null)
            {
                // Flash rosso
                UpdateCrystalEmission(Color.red * 3f);
                
                // Torna al colore normale dopo 0.1s
                Invoke(nameof(RestoreEmissionColor), 0.1f);
            }
        }

        private void RestoreEmissionColor()
        {
            if (isNight)
            {
                UpdateCrystalEmission(new Color(0f, 0.8f, 1f) * 4f);
            }
            else
            {
                UpdateCrystalEmission(new Color(0f, 0.8f, 1f) * 1.5f);
            }
        }

        private void Die()
        {
            if (isDestroyed) return;
            
            isDestroyed = true;
            
            Debug.Log("<color=red>[WaystoneBeacon]</color> ⚠️ BEACON DISTRUTTO! Game Over imminente...");
            
            OnDestroyed?.Invoke();
            
            // Trigger Game Over via GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver("Waystone Destroyed");
            }
            else
            {
                Debug.LogError("[WaystoneBeacon] GameManager.Instance is null! Cannot trigger Game Over.");
            }
            
            // Disabilita collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
            
            // Spegni luce
            if (beaconLight != null)
            {
                beaconLight.enabled = false;
            }
            
            // Riduci scala cristallo (effetto "distruzione")
            if (crystalTransform != null)
            {
                crystalTransform.localScale = Vector3.one * 0.1f;
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Ripristina il beacon a piena salute.
        /// </summary>
        public void FullRestore()
        {
            currentHP = maxHP;
            isDestroyed = false;
            
            // Riabilita collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
            
            // Riaccendi luce
            if (beaconLight != null)
            {
                beaconLight.enabled = true;
            }
            
            // Restaura visuale
            if (isNight)
            {
                SetNightMode();
            }
            else
            {
                SetDayMode();
            }
            
            if (debugMode)
            {
                Debug.Log("<color=green>[WaystoneBeacon]</color> ✨ Beacon completamente ripristinato!");
            }
        }

        // ============================================
        // GIZMOS
        // ============================================

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Disegna sfera di influenza
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, influenceRadius);
            
            // Sfera piena più piccola per il core
            Gizmos.color = new Color(0.2f, 0.5f, 0.9f, 0.1f);
            Gizmos.DrawSphere(transform.position, influenceRadius * 0.1f);
        }

        private Color GetHPColor()
        {
            float percent = HealthPercent;
            if (percent > 0.6f) return Color.green;
            if (percent > 0.3f) return Color.yellow;
            return Color.red;
        }

        // ============================================
        // DEBUG BUTTONS (ODIN)
        // ============================================

        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("💥 Take 50 Damage", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.6f)]
        private void DebugTakeDamage()
        {
            TakeDamage(50);
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("✨ Full Restore", ButtonSizes.Medium)]
        [GUIColor(0.6f, 1f, 0.6f)]
        private void DebugFullRestore()
        {
            FullRestore();
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("☀️ Set Day Mode", ButtonSizes.Medium)]
        [GUIColor(1f, 0.9f, 0.6f)]
        private void DebugSetDayMode()
        {
            SetDayMode();
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("🌙 Set Night Mode", ButtonSizes.Medium)]
        [GUIColor(0.6f, 0.7f, 1f)]
        private void DebugSetNightMode()
        {
            SetNightMode();
        }
        #endif
    }
}
