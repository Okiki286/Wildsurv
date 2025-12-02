using UnityEngine;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Gestisce i click sulle strutture per aprire il panel assignment.
    /// Aggiungi questo component ai prefab delle strutture.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StructureClickHandler : MonoBehaviour
    {
        private StructureController structureController;

        private void Awake()
        {
            structureController = GetComponent<StructureController>();
            if (structureController == null)
            {
                structureController = GetComponentInParent<StructureController>();
            }

            if (structureController == null)
            {
                Debug.LogWarning("[StructureClickHandler] No StructureController found!", this);
            }
        }

        private void OnMouseDown()
        {
            // Ignora se siamo in build mode
            // TODO: Uncomment when BuildModeController is available
            // if (BuildModeController.Instance != null && BuildModeController.Instance.IsInBuildMode)
            // {
            //     return;
            // }

            if (structureController != null)
            {
                // Open worker assignment UI directly via singleton
                if (WildernessSurvival.UI.WorkerAssignmentUI.Instance != null)
                {
                    WildernessSurvival.UI.WorkerAssignmentUI.Instance.OpenForStructure(structureController);
                }
            }
        }

        // Alternative: usa questo se preferisci raycast manuale invece di OnMouseDown
        private void Update()
        {
            // Questo è un approccio alternativo se OnMouseDown non funziona bene
            // Puoi commentare OnMouseDown e usare questo invece
            /*
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        if (structureController != null)
                        {
                            structureController.OnClick();
                        }
                    }
                }
            }
            */
        }
    }
}
