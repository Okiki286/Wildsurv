using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildernessSurvival.UI
{
    public class StructureStatusUI : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("References")]
        [SerializeField] private StructureController structure;
        [SerializeField] private GameObject statusUIContainer;
        [SerializeField] private Image background;
        [SerializeField] private Image fill;
        [SerializeField] private TextMeshProUGUI percentageText;

        // ============================================
        // SETTINGS (MODIFICATI PER IL POSIZIONAMENTO)
        // ============================================

        [TitleGroup("Positioning Settings")]

        [SerializeField]
        [Tooltip("Quanto in alto rispetto ai 'piedi' della struttura.")]
        private float heightOffset = 0.2f;

        [SerializeField]
        [Tooltip("Quanto spostare la barra in AVANTI (verso la camera).")]
        private float forwardOffset = 1.0f;

        [TitleGroup("Visual Settings")]
        [SerializeField] private float barWidth = 150f;
        [SerializeField] private float barHeight = 15f;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
        [SerializeField] private Color fillColor = new Color(0.3f, 0.69f, 0.31f, 1f);

        // ============================================
        // RUNTIME VARIABLES
        // ============================================

        private Camera mainCamera;
        private bool isInitialized = false;
        private Collider cachedCollider; // Cache del collider per trovare i "piedi"

        private void Awake()
        {
            if (statusUIContainer != null) statusUIContainer.SetActive(false);
            if (fill != null) fill.fillAmount = 0f;

            // Trova StructureController
            if (structure == null) structure = GetComponent<StructureController>() ?? GetComponentInParent<StructureController>();

            if (structure == null)
            {
                enabled = false;
                return;
            }

            // Trova il Collider per calcolare i bounds esatti
            cachedCollider = structure.GetComponent<Collider>() ?? structure.GetComponentInChildren<Collider>();

            mainCamera = Camera.main;
            isInitialized = true;
        }

        private void Start()
        {
            if (fill != null) fill.fillAmount = 0f;
            UpdateVisibility();
        }

        private void LateUpdate()
        {
            if (!isInitialized) return;
            if (mainCamera == null) mainCamera = Camera.main;

            UpdateVisibility();

            if (statusUIContainer != null && statusUIContainer.activeSelf)
            {
                UpdatePosition();
                UpdateBillboard();
                UpdateProgressBar();
            }
        }

        private void UpdateVisibility()
        {
            if (structure == null || statusUIContainer == null) return;
            bool shouldShow = structure.State == StructureState.Building && structure.BuildProgress < 1.0f;
            if (statusUIContainer.activeSelf != shouldShow) statusUIContainer.SetActive(shouldShow);
        }

        // 👇 CORREZIONE: Direzione relativa alla Camera per posizionamento preciso
        private void UpdatePosition()
        {
            if (structure == null || statusUIContainer == null || mainCamera == null) return;

            // 1. Trova il punto base (i piedi) della struttura
            float groundY = structure.transform.position.y;
            if (cachedCollider != null)
            {
                groundY = cachedCollider.bounds.min.y;
            }

            // 2. Calcola la posizione base ai piedi
            Vector3 targetPosition = new Vector3(structure.transform.position.x, groundY, structure.transform.position.z);

            // 3. Calcola la direzione "verso il basso dello schermo" (verso il giocatore)
            // Prendiamo la direzione forward della camera e la appiattiamo sul piano XZ
            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0;
            camForward.Normalize();
            Vector3 directionToPlayer = -camForward;

            // 4. Applica gli offset: verso l'alto (Y) e verso il giocatore (cam-relative)
            targetPosition += Vector3.up * heightOffset;
            targetPosition += directionToPlayer * forwardOffset;

            statusUIContainer.transform.position = targetPosition;
        }

        private void UpdateBillboard()
        {
            if (mainCamera == null || statusUIContainer == null) return;
            statusUIContainer.transform.LookAt(
                statusUIContainer.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up
            );
        }

        private void UpdateProgressBar()
        {
            if (fill == null || structure == null) return;
            float progress = structure.BuildProgress;
            fill.fillAmount = progress;
            if (percentageText != null) percentageText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

#if UNITY_EDITOR
        [TitleGroup("🛠 Global Tools")]
        [Button("⚠️ Force Update Position Settings", ButtonSizes.Large), GUIColor(1f, 0.5f, 0f)]
        private void ForceUpdateSettings()
        {
            var allUIs = FindObjectsByType<StructureStatusUI>(FindObjectsSortMode.None);
            foreach (var ui in allUIs)
            {
                // Imposta valori che dovrebbero funzionare per la visuale isometrica
                ui.heightOffset = 0.2f; // Appena sopra l'erba
                ui.forwardOffset = 1.0f; // Sposta in "avanti" verso il giocatore

                // Cerca di trovare il collider se perso
                if (ui.cachedCollider == null && ui.structure != null)
                    ui.cachedCollider = ui.structure.GetComponent<Collider>() ?? ui.structure.GetComponentInChildren<Collider>();

                EditorUtility.SetDirty(ui);
            }
            Debug.Log($"<color=green>✅ Aggiornate {allUIs.Length} UI con i nuovi offset!</color>");
        }
#endif
    }
}