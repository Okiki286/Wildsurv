using UnityEngine;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// [DEPRECATED] Gestiva i click sulle strutture tramite collider/OnMouseDown.
    ///
    /// NOTA: Questo sistema è stato SOSTITUITO da StructureSelectionManager
    /// che usa selezione grid-based (celle occupate) invece di physics/collider.
    ///
    /// Vantaggi del nuovo sistema:
    /// - Deterministico: niente dipendenza da collider size/shape
    /// - Coerente col sistema di placement: stesse celle per placement e selezione
    /// - Niente overlap/conflitti tra collider
    ///
    /// Questo componente è mantenuto per compatibilità ma è DISABILITATO.
    /// Se presente sui prefab esistenti, si auto-disabilita.
    /// </summary>
    [System.Obsolete("Usa StructureSelectionManager per selezione grid-based")]
    public class StructureClickHandler : MonoBehaviour
    {
        private void Awake()
        {
            // Auto-disabilita: selezione ora gestita da StructureSelectionManager
            enabled = false;

            // Log solo in editor per debug
#if UNITY_EDITOR
            Debug.Log($"[StructureClickHandler] DEPRECATED: {gameObject.name} - use StructureSelectionManager instead");
#endif
        }

        // Metodi mantenuti vuoti per evitare errori se chiamati da codice legacy
        private void OnMouseDown() { }
        private void Update() { }
    }
}

