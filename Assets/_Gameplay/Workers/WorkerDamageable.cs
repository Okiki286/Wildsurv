using UnityEngine;
using WildernessSurvival.Gameplay.Combat;
using WildernessSurvival.Gameplay.Enemies;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Bridge component that exposes WorkerInstance's IDamageable interface
    /// to the physics/targeting system.
    /// 
    /// Attach to Worker prefab root (same GO as WorkerController and Collider).
    /// Enemy targeting uses GetComponent<IDamageable>() which only finds MonoBehaviours.
    /// This component delegates damage to the linked WorkerInstance.
    /// </summary>
    [RequireComponent(typeof(WorkerController))]
    public class WorkerDamageable : MonoBehaviour, IDamageable
    {
        private WorkerController controller;
        private WorkerInstance linkedInstance;

        public float CurrentHealth => linkedInstance?.CurrentHealth ?? 0f;
        public float MaxHealth => linkedInstance?.MaxHealth ?? 100f;
        public bool IsAlive => linkedInstance?.IsAlive ?? false;

        private void Awake()
        {
            controller = GetComponent<WorkerController>();
        }

        /// <summary>
        /// Called by WorkerController.LinkToInstance to wire up the damage routing.
        /// </summary>
        public void LinkToInstance(WorkerInstance instance)
        {
            linkedInstance = instance;
#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerDamageable]</color> Linked to {instance?.CustomName}");
#endif
        }

        public void TakeDamage(float damage)
        {
            if (linkedInstance == null)
            {
                Debug.LogWarning($"[WorkerDamageable] {gameObject.name} has no linked instance!");
                return;
            }

            linkedInstance.TakeDamage(damage);
#if UNITY_EDITOR
            Debug.Log($"<color=red>[WorkerDamageable]</color> {linkedInstance.CustomName} took {damage:F1} damage ({CurrentHealth:F0}/{MaxHealth:F0} HP)");
#endif
        }

        public void TakeDamage(float damage, DamageType damageType)
        {
            if (linkedInstance == null)
            {
                Debug.LogWarning($"[WorkerDamageable] {gameObject.name} has no linked instance!");
                return;
            }

            linkedInstance.TakeDamage(damage, damageType);
#if UNITY_EDITOR
            Debug.Log($"<color=red>[WorkerDamageable]</color> {linkedInstance.CustomName} took {damage:F1} {damageType} damage ({CurrentHealth:F0}/{MaxHealth:F0} HP)");
#endif
        }
    }
}
