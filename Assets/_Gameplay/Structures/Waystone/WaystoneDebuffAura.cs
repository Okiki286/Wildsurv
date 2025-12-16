using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Enemies;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Aura di debuff del Waystone.
    /// Rallenta e indebolisce i nemici nel raggio d'azione.
    /// Il raggio è sincronizzato con la luce dell'aura.
    ///
    /// REQUISITI UNITY per trigger detection:
    /// 1. Questo script deve essere sullo stesso GameObject del SphereCollider (isTrigger=true)
    /// 2. Serve un Rigidbody (isKinematic=true) su questo GO o sul nemico
    /// 3. Il nemico deve essere su layer 11 ("Enemy") e avere IDebuffable
    /// </summary>
    public class WaystoneDebuffAura : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("References")]
        [Tooltip("Luce che definisce il raggio dell'aura. Se null, usa auraTrigger.radius.")]
        [SerializeField] private Light auraLight;

        [TitleGroup("References")]
        [Tooltip("Trigger collider per rilevare nemici. Deve essere isTrigger=true.")]
        [SerializeField] private SphereCollider auraTrigger;

        // ============================================
        // DEBUFF SETTINGS
        // ============================================

        [TitleGroup("Debuff Settings")]
        [Tooltip("Layer dei nemici (default: layer 11)")]
        [SerializeField] private LayerMask enemyLayer = 1 << 11; // Layer 11 "Enemy"

        [TitleGroup("Debuff Settings")]
        [Tooltip("Moltiplicatore velocità movimento (0.75 = 75% della velocità normale)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float moveMultiplier = 0.75f;

        [TitleGroup("Debuff Settings")]
        [Tooltip("Moltiplicatore attacco (0.85 = 85% del danno/velocità normale)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float attackMultiplier = 0.85f;

        [TitleGroup("Debuff Settings")]
        [Tooltip("Intervallo tra tick di applicazione debuff (ottimizzazione)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float tickInterval = 0.25f;

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        [TitleGroup("Debug")]
        [Tooltip("Log TUTTI i trigger events (anche quelli filtrati). Utile per diagnosticare layer/setup issues.")]
        [SerializeField] private bool verboseTriggerDebug = false;

        [TitleGroup("Behavior")]
        [Tooltip("Se true, rimuove i debuff da tutti i target quando OnDisable viene chiamato. Default false per evitare problemi durante scene changes/costruzione.")]
        [SerializeField] private bool removeDebuffsOnDisable = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        // Set di nemici attualmente nell'aura (evita allocazioni)
        private HashSet<IDebuffable> trackedTargets = new HashSet<IDebuffable>();

        // Lista temporanea per iterazione sicura
        private List<IDebuffable> tempTargetList = new List<IDebuffable>();

        // Timer per tick
        private float tickTimer = 0f;

        // Flag per errori loggati (log 1 volta)
        private bool hasLoggedSetupError = false;

        // Debug: contatore trigger events per diagnostica
        private int triggerEnterCount = 0;
        private float timeSinceStart = 0f;
        private bool hasLoggedNoTriggerWarning = false;

        // ============================================
        // PROPERTIES
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        public int TrackedTargetCount => trackedTargets.Count;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            ValidateSetup();
            EnsureRigidbody();
        }

        private void OnEnable()
        {
            // Reset tracked targets quando si riattiva
            trackedTargets.Clear();
            triggerEnterCount = 0;
            timeSinceStart = 0f;
            hasLoggedNoTriggerWarning = false;
        }

        private void OnDisable()
        {
            // Rimuovi debuff solo se esplicitamente richiesto (evita problemi durante scene changes/costruzione)
            if (removeDebuffsOnDisable)
            {
                RemoveAllDebuffs();
            }
            else if (debugMode)
            {
                Debug.Log($"<color=cyan>[WaystoneAura]</color> OnDisable - NOT removing debuffs (removeDebuffsOnDisable=false). Tracking {trackedTargets.Count} targets.");
            }
        }

        private void OnDestroy()
        {
            // Cleanup: rimuovi sempre i debuff quando l'aura viene distrutta
            RemoveAllDebuffs();
            if (debugMode)
            {
                Debug.Log("<color=cyan>[WaystoneAura]</color> OnDestroy - All debuffs removed (cleanup)");
            }
        }

        private void Update()
        {
            // Sincronizza raggio trigger con luce
            SyncTriggerRadius();

            // Tick debuff
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                TickDebuffs();
                tickTimer = 0f;
            }

            // Debug: warning se nessun trigger dopo 10 secondi (potenziale problema setup)
            if (debugMode && !hasLoggedNoTriggerWarning)
            {
                timeSinceStart += Time.deltaTime;
                if (timeSinceStart > 10f && triggerEnterCount == 0)
                {
                    hasLoggedNoTriggerWarning = true;
                    Debug.LogWarning($"<color=orange>[WaystoneAura]</color> No trigger events after 10s! Check:\n" +
                        $"  1. Rigidbody present: {GetComponent<Rigidbody>() != null}\n" +
                        $"  2. SphereCollider isTrigger: {(auraTrigger != null ? auraTrigger.isTrigger.ToString() : "NULL")}\n" +
                        $"  3. Trigger radius: {(auraTrigger != null ? auraTrigger.radius.ToString("F1") : "NULL")}\n" +
                        $"  4. EnemyLayer mask: {enemyLayer.value} (layer 11 = {((enemyLayer.value & (1 << 11)) != 0 ? "YES" : "NO")})\n" +
                        $"  5. Enemies must have Collider + be on layer 11");
                }
            }
        }

        // ============================================
        // SETUP VALIDATION
        // ============================================

        private void ValidateSetup()
        {
            bool hasError = false;

            // Auto-find light se non assegnata
            if (auraLight == null)
            {
                auraLight = GetComponentInChildren<Light>();
            }

            // Auto-find trigger se non assegnato
            if (auraTrigger == null)
            {
                auraTrigger = GetComponent<SphereCollider>();
                if (auraTrigger == null)
                {
                    auraTrigger = GetComponentInChildren<SphereCollider>();
                }
            }

            // Verifica trigger
            if (auraTrigger == null)
            {
                if (!hasLoggedSetupError)
                {
                    Debug.LogError("[WaystoneDebuffAura] No SphereCollider (trigger) found! Aura disabled.");
                    hasLoggedSetupError = true;
                }
                hasError = true;
            }
            else if (!auraTrigger.isTrigger)
            {
                if (!hasLoggedSetupError)
                {
                    Debug.LogError("[WaystoneDebuffAura] SphereCollider must be isTrigger=true! Fixing...");
                    hasLoggedSetupError = true;
                }
                auraTrigger.isTrigger = true;
            }

            // Se manca luce, usa raggio fisso dal trigger
            if (auraLight == null && auraTrigger != null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[WaystoneDebuffAura] No Light found, using trigger radius only.");
                }
            }

            if (hasError)
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Assicura che ci sia un Rigidbody (kinematic) per far funzionare i trigger.
        /// Unity richiede almeno un Rigidbody tra i due collider coinvolti.
        /// </summary>
        private void EnsureRigidbody()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                if (debugMode)
                {
                    Debug.Log("<color=cyan>[WaystoneAura]</color> Added kinematic Rigidbody for trigger detection");
                }
            }
            else if (!rb.isKinematic)
            {
                // Se c'è già un Rigidbody non kinematic, log warning
                if (debugMode)
                {
                    Debug.LogWarning("[WaystoneDebuffAura] Rigidbody exists but is not kinematic. Aura might move unexpectedly.");
                }
            }
        }

        // ============================================
        // RADIUS SYNC
        // ============================================

        private void SyncTriggerRadius()
        {
            if (auraTrigger == null) return;

            // Sincronizza raggio con luce (se disponibile)
            if (auraLight != null)
            {
                auraTrigger.radius = auraLight.range;
            }
        }

        // ============================================
        // TRIGGER EVENTS
        // ============================================

        private void OnTriggerEnter(Collider other)
        {
            triggerEnterCount++;

            // Verbose debug: log OGNI trigger event (utile per diagnostica layer)
            if (verboseTriggerDebug)
            {
                bool passedLayerCheck = IsInEnemyLayer(other.gameObject);
                IDebuffable testDebuffable = other.GetComponentInParent<IDebuffable>();
                Debug.Log($"<color=magenta>[WaystoneAura VERBOSE]</color> OnTriggerEnter:\n" +
                    $"  Collider: {other.name}\n" +
                    $"  Layer: {other.gameObject.layer} ({LayerMask.LayerToName(other.gameObject.layer)})\n" +
                    $"  Tag: {other.tag}\n" +
                    $"  PassedLayerCheck: {passedLayerCheck}\n" +
                    $"  HasIDebuffable: {testDebuffable != null}\n" +
                    $"  IDebuffable on: {(testDebuffable is MonoBehaviour mb ? mb.gameObject.name : "N/A")}");
            }

            if (!IsInEnemyLayer(other.gameObject))
            {
                if (verboseTriggerDebug)
                {
                    Debug.Log($"<color=gray>[WaystoneAura]</color> {other.name} filtered out (layer {other.gameObject.layer}, expected mask {enemyLayer.value})");
                }
                return;
            }

            // Cerca IDebuffable nel parent (nemico potrebbe avere collider child)
            IDebuffable debuffable = other.GetComponentInParent<IDebuffable>();
            if (debuffable == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"<color=yellow>[WaystoneAura]</color> {other.name} is on Enemy layer but has NO IDebuffable! Add EnemyDummyDebuffable for testing.");
                }
                return;
            }

            if (!trackedTargets.Contains(debuffable))
            {
                trackedTargets.Add(debuffable);
                debuffable.ApplyWaystoneDebuff(moveMultiplier, attackMultiplier);

                if (debugMode)
                {
                    string objName = (debuffable is MonoBehaviour mb) ? mb.gameObject.name : other.name;
                    Debug.Log($"<color=cyan>[WaystoneAura]</color> ✓ Target entered: {objName} - debuff applied (move: {moveMultiplier:P0}, atk: {attackMultiplier:P0})");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (verboseTriggerDebug)
            {
                Debug.Log($"<color=magenta>[WaystoneAura VERBOSE]</color> OnTriggerExit: {other.name} (layer {other.gameObject.layer})");
            }

            if (!IsInEnemyLayer(other.gameObject)) return;

            IDebuffable debuffable = other.GetComponentInParent<IDebuffable>();
            if (debuffable != null && trackedTargets.Contains(debuffable))
            {
                trackedTargets.Remove(debuffable);
                debuffable.RemoveWaystoneDebuff();

                if (debugMode)
                {
                    string objName = (debuffable is MonoBehaviour mb) ? mb.gameObject.name : other.name;
                    Debug.Log($"<color=cyan>[WaystoneAura]</color> ✗ Target exited: {objName} - debuff removed");
                }
            }
        }

        private bool IsInEnemyLayer(GameObject obj)
        {
            return (enemyLayer.value & (1 << obj.layer)) != 0;
        }

        // ============================================
        // DEBUFF TICK
        // ============================================

        private void TickDebuffs()
        {
            // Copia per iterazione sicura (target potrebbe essere distrutto)
            tempTargetList.Clear();
            tempTargetList.AddRange(trackedTargets);

            foreach (var target in tempTargetList)
            {
                // Verifica se target è ancora valido (Unity null check)
                if (target == null || (target is MonoBehaviour mb && mb == null))
                {
                    trackedTargets.Remove(target);
                    continue;
                }

                // Riapplica debuff (in caso di reset o altri effetti)
                target.ApplyWaystoneDebuff(moveMultiplier, attackMultiplier);
            }
        }

        // ============================================
        // CLEANUP
        // ============================================

        private void RemoveAllDebuffs()
        {
            foreach (var target in trackedTargets)
            {
                if (target != null && !(target is MonoBehaviour mb && mb == null))
                {
                    target.RemoveWaystoneDebuff();
                }
            }
            trackedTargets.Clear();

            if (debugMode)
            {
                Debug.Log("<color=cyan>[WaystoneAura]</color> All debuffs removed");
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Log Tracked Targets", ButtonSizes.Medium)]
        private void DebugLogTargets()
        {
            Debug.Log($"[WaystoneAura] Tracking {trackedTargets.Count} targets");
            foreach (var target in trackedTargets)
            {
                if (target is MonoBehaviour mb)
                {
                    Debug.Log($"  - {mb.gameObject.name}");
                }
            }
        }

        [Button("Force Remove All Debuffs", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugForceRemoveAll()
        {
            RemoveAllDebuffs();
        }

        [Button("Run Setup Diagnostics", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void DebugRunDiagnostics()
        {
            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
            Debug.Log("<color=cyan>[WaystoneAura] SETUP DIAGNOSTICS</color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");

            // 1. Rigidbody check
            var rb = GetComponent<Rigidbody>();
            Debug.Log($"<color=white>1. Rigidbody:</color> {(rb != null ? $"<color=green>✓ Present</color> (kinematic: {rb.isKinematic})" : "<color=red>✗ MISSING - triggers won't work!</color>")}");

            // 2. SphereCollider check
            var col = auraTrigger ?? GetComponent<SphereCollider>();
            if (col != null)
            {
                Debug.Log($"<color=white>2. SphereCollider:</color> <color=green>✓ Present</color>");
                Debug.Log($"   - isTrigger: {(col.isTrigger ? "<color=green>✓ true</color>" : "<color=red>✗ FALSE - must be true!</color>")}");
                Debug.Log($"   - radius: {col.radius:F1}");
            }
            else
            {
                Debug.Log($"<color=white>2. SphereCollider:</color> <color=red>✗ MISSING!</color>");
            }

            // 3. Layer mask
            Debug.Log($"<color=white>3. EnemyLayer Mask:</color> {enemyLayer.value}");
            Debug.Log($"   - Layer 11 included: {((enemyLayer.value & (1 << 11)) != 0 ? "<color=green>✓ YES</color>" : "<color=red>✗ NO</color>")}");

            // 4. Light
            Debug.Log($"<color=white>4. Aura Light:</color> {(auraLight != null ? $"<color=green>✓ {auraLight.name}</color> (range: {auraLight.range:F1})" : "<color=yellow>⚠ Not assigned (using fixed radius)</color>")}");

            // 5. Script placement
            Debug.Log($"<color=white>5. Script on same GO as collider:</color> {((col != null && col.gameObject == gameObject) ? "<color=green>✓ YES</color>" : "<color=yellow>⚠ Different GO - ensure correct setup</color>")}");

            // 6. Trigger count
            Debug.Log($"<color=white>6. Trigger events received:</color> {triggerEnterCount}");
            Debug.Log($"<color=white>7. Currently tracking:</color> {trackedTargets.Count} targets");

            Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
        }

        private void OnDrawGizmosSelected()
        {
            float radius = 5f;

            if (auraTrigger != null)
            {
                radius = auraTrigger.radius;
            }
            else if (auraLight != null)
            {
                radius = auraLight.range;
            }

            // Disegna raggio aura
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, radius);

            Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
            Gizmos.DrawSphere(transform.position, radius);
        }
#endif
    }
}
