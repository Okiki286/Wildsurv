namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Interfaccia per entità che possono ricevere debuff dal Waystone.
    /// Implementare su nemici/entità ostili che devono essere rallentate dall'aura.
    /// </summary>
    public interface IDebuffable
    {
        /// <summary>
        /// Applica il debuff del Waystone.
        /// </summary>
        /// <param name="moveMultiplier">Moltiplicatore movimento (es. 0.75 = 75% velocità)</param>
        /// <param name="attackMultiplier">Moltiplicatore attacco (es. 0.85 = 85% danno/velocità attacco)</param>
        void ApplyWaystoneDebuff(float moveMultiplier, float attackMultiplier);

        /// <summary>
        /// Rimuove il debuff del Waystone.
        /// </summary>
        void RemoveWaystoneDebuff();

        /// <summary>
        /// True se il debuff è attualmente attivo.
        /// </summary>
        bool HasWaystoneDebuff { get; }
    }
}
