using UnityEngine;
using WildernessSurvival.Gameplay.Combat;
using WildernessSurvival.Gameplay.Enemies;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Componente per strutture difensive che attaccano automaticamente i nemici.
    /// Si attiva solo se StructureData.AttackDamage > 0 e AttackRange > 0.
    /// Attacco istantaneo (no proiettili) - selezione target più vicino.
    /// </summary>
    [RequireComponent(typeof(StructureController))]
    public class TowerAttack : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Tooltip("Layer dei nemici (se vuoto usa Physics.AllLayers)")]
        [SerializeField]
        private LayerMask enemyLayerMask = -1; // -1 = all layers

        [TitleGroup("Settings")]
        [Tooltip("Abilita log diagnostici per debugging")]
        [SerializeField]
        private bool debugDiagnostics = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime")]
        [ShowInInspector, ReadOnly]
        private float attackCooldown;

        [ShowInInspector, ReadOnly]
        private EnemyInstance currentTarget;

        [ShowInInspector, ReadOnly]
        private bool isEnabled;

        // ============================================
        // REFERENCES
        // ============================================

        private StructureController structure;
        private StructureData data;

        // Buffer per OverlapSphereNonAlloc (riuso per evitare GC)
        private static readonly Collider[] scanBuffer = new Collider[64];

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            structure = GetComponent<StructureController>();
        }

        private void Start()
        {
            data = structure.Data;

            // Verifica se questa struttura può attaccare
            if (data == null)
            {
                isEnabled = false;
                enabled = false;
                return;
            }

            // Solo strutture con danno e range > 0 possono attaccare
            isEnabled = data.AttackDamage > 0f && data.AttackRange > 0f;

            if (!isEnabled)
            {
                enabled = false;
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Diagnostic warning for misconfigured layer mask
            if (enemyLayerMask.value == 0)
            {
                Debug.LogError($"<color=red>[TowerAttack]</color> {name} has enemyLayerMask=0! Tower will NEVER find enemies. Set layer mask in Inspector.");
            }
            else if (debugDiagnostics)
            {
                Debug.Log($"<color=cyan>[TowerAttack]</color> {name} initialized: dmg={data.AttackDamage}, range={data.AttackRange}m, interval={data.AttackInterval}s, layerMask={enemyLayerMask.value}");
            }
#endif
        }

        private void Update()
        {
            if (!isEnabled) return;
            if (!structure.IsOperational) return;

            // Update cooldown
            if (attackCooldown > 0f)
            {
                attackCooldown -= Time.deltaTime;
                return;
            }

            // Cerca target e attacca
            FindAndAttackTarget();
        }

        // ============================================
        // COMBAT LOGIC
        // ============================================

        private void FindAndAttackTarget()
        {
            // Valida target corrente
            if (currentTarget != null && !currentTarget.IsAlive)
            {
                currentTarget = null;
            }

            // Se abbiamo un target valido in range, attacca
            if (currentTarget != null)
            {
                float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (dist <= data.AttackRange)
                {
                    PerformAttack(currentTarget);
                    return;
                }
                else
                {
                    // Target fuori range
                    currentTarget = null;
                }
            }

            // Cerca nuovo target
            currentTarget = FindClosestEnemy();

            if (currentTarget != null)
            {
                PerformAttack(currentTarget);
            }
        }

        private EnemyInstance FindClosestEnemy()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                data.AttackRange,
                scanBuffer,
                enemyLayerMask
            );

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugDiagnostics)
            {
                Debug.Log($"<color=cyan>[TowerAttack]</color> {name}: Scan pos={transform.position}, range={data.AttackRange}m, mask={enemyLayerMask.value}, found={hitCount} colliders");
            }
#endif

            EnemyInstance closestEnemy = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = scanBuffer[i];
                if (col == null) continue;

                EnemyInstance enemy = col.GetComponent<EnemyInstance>();
                if (enemy == null || !enemy.IsAlive) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestEnemy = enemy;
                }
            }

            return closestEnemy;
        }

        private void PerformAttack(EnemyInstance target)
        {
            if (target == null || !target.IsAlive) return;

            // Calcola danno con level scaling
            float damage = data.GetDamageAtLevel(structure.CurrentLevel);

            // Infliggi danno (tipo Physical di default per torri)
            target.TakeDamage(damage, DamageType.Physical);

            // Telemetry
            CombatTelemetry.Instance?.RecordTowerDamage(damage);

            // Reset cooldown
            attackCooldown = data.AttackInterval;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugDiagnostics)
            {
                Debug.Log($"<color=yellow>[Tower]</color> {data.DisplayName} attacked {target.Data?.DisplayName ?? target.name} for {damage:F1} damage");
            }
#endif

            // Se target è morto, clear reference
            if (!target.IsAlive)
            {
                currentTarget = null;
            }
        }

        // ============================================
        // GIZMOS
        // ============================================

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (data == null && structure != null)
            {
                data = structure.Data;
            }

            if (data == null || data.AttackRange <= 0) return;

            // Disegna range di attacco
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, data.AttackRange);

            // Linea verso target corrente
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
#endif
    }
}
