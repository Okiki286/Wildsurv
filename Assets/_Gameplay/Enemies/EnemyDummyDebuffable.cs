using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Dummy test per verificare che IDebuffable e WaystoneDebuffAura funzionino.
    /// Logga quando il debuff viene applicato/rimosso.
    /// Usare per testing prima di implementare su nemici reali.
    /// </summary>
    public class EnemyDummyDebuffable : MonoBehaviour, IDebuffable
    {
        // ============================================
        // STATE
        // ============================================

        [TitleGroup("Debuff Status")]
        [ShowInInspector]
        [ReadOnly]
        private bool hasDebuff = false;

        [ShowInInspector]
        [ReadOnly]
        private float currentMoveMultiplier = 1f;

        [ShowInInspector]
        [ReadOnly]
        private float currentAttackMultiplier = 1f;

        // ============================================
        // BASE STATS (per debug)
        // ============================================

        [TitleGroup("Base Stats")]
        [SerializeField] private float baseSpeed = 5f;
        [SerializeField] private float baseAttackDamage = 10f;

        [TitleGroup("Current Stats")]
        [ShowInInspector]
        [ReadOnly]
        public float CurrentSpeed => baseSpeed * currentMoveMultiplier;

        [ShowInInspector]
        [ReadOnly]
        public float CurrentAttack => baseAttackDamage * currentAttackMultiplier;

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug")]
        [SerializeField] private bool logDebuffChanges = true;

        // ============================================
        // IDebuffable IMPLEMENTATION
        // ============================================

        public bool HasWaystoneDebuff => hasDebuff;

        public void ApplyWaystoneDebuff(float moveMultiplier, float attackMultiplier)
        {
            // Se il debuff è già applicato con gli stessi valori, skip
            if (hasDebuff &&
                Mathf.Approximately(currentMoveMultiplier, moveMultiplier) &&
                Mathf.Approximately(currentAttackMultiplier, attackMultiplier))
            {
                return;
            }

            hasDebuff = true;
            currentMoveMultiplier = moveMultiplier;
            currentAttackMultiplier = attackMultiplier;

            if (logDebuffChanges)
            {
                Debug.Log($"<color=yellow>[DummyEnemy]</color> {gameObject.name} - DEBUFF APPLIED: " +
                    $"Speed {baseSpeed:F1} -> {CurrentSpeed:F1} ({moveMultiplier:P0}), " +
                    $"Attack {baseAttackDamage:F1} -> {CurrentAttack:F1} ({attackMultiplier:P0})");
            }

            // Qui integreresti con NavMeshAgent, AI, ecc.
            // Es: navAgent.speed = baseSpeed * currentMoveMultiplier;
        }

        public void RemoveWaystoneDebuff()
        {
            if (!hasDebuff) return;

            hasDebuff = false;
            currentMoveMultiplier = 1f;
            currentAttackMultiplier = 1f;

            if (logDebuffChanges)
            {
                Debug.Log($"<color=green>[DummyEnemy]</color> {gameObject.name} - DEBUFF REMOVED: " +
                    $"Speed restored to {baseSpeed:F1}, Attack restored to {baseAttackDamage:F1}");
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Test Apply Debuff (75%/85%)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.4f)]
        private void DebugTestApply()
        {
            ApplyWaystoneDebuff(0.75f, 0.85f);
        }

        [Button("Test Remove Debuff", ButtonSizes.Medium)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void DebugTestRemove()
        {
            RemoveWaystoneDebuff();
        }
#endif
    }
}
