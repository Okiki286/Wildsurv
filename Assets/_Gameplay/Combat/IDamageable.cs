using WildernessSurvival.Gameplay.Enemies;

namespace WildernessSurvival.Gameplay.Combat
{
    /// <summary>
    /// Interfaccia comune per entità che possono subire danno.
    /// Implementata da: EnemyInstance, StructureController, WorkerInstance, WaystoneBeaconController
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applica danno all'entità.
        /// </summary>
        /// <param name="damage">Quantità di danno</param>
        /// <param name="damageType">Tipo di danno (per resistenze/debolezze)</param>
        void TakeDamage(float damage, DamageType damageType = DamageType.None);

        /// <summary>
        /// HP correnti dell'entità
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// HP massimi dell'entità
        /// </summary>
        float MaxHealth { get; }

        /// <summary>
        /// True se l'entità è ancora in vita (HP > 0)
        /// </summary>
        bool IsAlive { get; }
    }
}
