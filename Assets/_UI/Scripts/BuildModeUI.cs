using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI Controller per il pulsante "Annulla Costruzione" durante il Build Mode.
    /// Utilizza asset grafici dal Modular Game UI Kit per mantenere coerenza visiva.
    ///
    /// INTEGRAZIONE:
    /// 1. Aggiungi questo script a un GameObject nella Canvas UI
    /// 2. Assegna i riferimenti in Inspector:
    ///    - cancelButton: Il Button UI
    ///    - buttonIcon: L'Image child del button per l'icona
    ///    - cancelSprite: Sprite "X-Delete-Close-Error" dal Modular Game UI Kit
    ///    - cancelColor: Colore rosso del UI Kit (#FF4444 o simile)
    /// 3. Collega l'evento OnClick del button al metodo OnCancelClicked()
    /// </summary>
    public class BuildModeUI : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("UI References")]
        [BoxGroup("UI References/Button")]
        [Required("Riferimento al Button UI per annullare costruzione")]
        [ChildGameObjectsOnly]
        [SerializeField] private Button cancelButton;

        [BoxGroup("UI References/Button")]
        [Required("Riferimento all'Image child del button per l'icona")]
        [ChildGameObjectsOnly]
        [Tooltip("Componente Image del button per mostrare l'icona cancel")]
        [SerializeField] private Image buttonIcon;

        // ============================================
        // ASSET REFERENCES (Modular Game UI Kit)
        // ============================================

        [TitleGroup("UI Kit Assets")]
        [BoxGroup("UI Kit Assets/Sprites")]
        [Required("Sprite 'X-Delete-Close-Error' dal Modular Game UI Kit")]
        [AssetsOnly]
        [PreviewField(50)]
        [Tooltip("Sprite path: Assets/ModularGameUIKit/Common/Sprites/Icon-Symbols/Basics/X-Delete-Close-Error.png")]
        [SerializeField] private Sprite cancelSprite;

        [BoxGroup("UI Kit Assets/Colors")]
        [Tooltip("Colore rosso/error dal dataset UI Kit (es. #FF4444)")]
        [SerializeField] private Color cancelColor = new Color(1f, 0.267f, 0.267f, 1f); // Red/Error default

        // ============================================
        // OPTIONS
        // ============================================

        [TitleGroup("Behavior Options")]
        [BoxGroup("Behavior Options/Auto-Exit")]
        [Tooltip("Se true, esce automaticamente dal build mode dopo il piazzamento di una struttura")]
        [ToggleLeft]
        [SerializeField] private bool autoExitAfterPlacement = false;

        [BoxGroup("Behavior Options/Visibility")]
        [Tooltip("Se true, il pulsante si nasconde quando non è in build mode (invece di disabilitarsi)")]
        [ToggleLeft]
        [SerializeField] private bool hideWhenInactive = true;

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug")]
        [ToggleLeft]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [ShowInInspector, ReadOnly]
        [BoxGroup("Runtime Status")]
        private bool isBuildModeActive = false;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Start()
        {
            // Validazione riferimenti critici
            if (cancelButton == null)
            {
                Debug.LogError("[BuildModeUI] CancelButton reference missing!", this);
                enabled = false;
                return;
            }

            if (buttonIcon == null)
            {
                Debug.LogWarning("[BuildModeUI] ButtonIcon reference missing - trying auto-find...", this);
                buttonIcon = cancelButton.GetComponentInChildren<Image>();

                if (buttonIcon == null)
                {
                    Debug.LogError("[BuildModeUI] Cannot find Image component in button children!", this);
                }
            }

            // Applica sprite e colore via codice (garantisce coerenza anche se Inspector viene resettato)
            if (buttonIcon != null && cancelSprite != null)
            {
                buttonIcon.sprite = cancelSprite;
                buttonIcon.color = cancelColor;

                if (debugMode)
                {
                    Debug.Log($"[BuildModeUI] Sprite e colore applicati: {cancelSprite.name}, {cancelColor}");
                }
            }
            else if (cancelSprite == null)
            {
                Debug.LogWarning("[BuildModeUI] CancelSprite not assigned! Assign 'X-Delete-Close-Error' sprite from Modular Game UI Kit.", this);
            }

            // Setup evento click se non già configurato in Inspector
            if (cancelButton != null)
            {
                // Rimuovi listener esistenti per evitare duplicati
                cancelButton.onClick.RemoveListener(OnCancelClicked);
                // Aggiungi listener
                cancelButton.onClick.AddListener(OnCancelClicked);
            }

            // Stato iniziale: nascosto/disabilitato
            UpdateButtonVisibility(false);
        }

        private void Update()
        {
            // Controlla stato BuildModeController ogni frame
            if (BuildModeController.Instance != null)
            {
                bool isInBuildMode = BuildModeController.Instance.IsInBuildMode;

                // Aggiorna UI solo se lo stato è cambiato
                if (isInBuildMode != isBuildModeActive)
                {
                    isBuildModeActive = isInBuildMode;
                    UpdateButtonVisibility(isBuildModeActive);

                    if (debugMode)
                    {
                        Debug.Log($"[BuildModeUI] Build mode {(isBuildModeActive ? "ATTIVO" : "INATTIVO")} - Button {(isBuildModeActive ? "visible" : "hidden")}");
                    }
                }
            }
        }

        // ============================================
        // UI MANAGEMENT
        // ============================================

        /// <summary>
        /// Aggiorna visibilità/interattività del pulsante in base allo stato build mode
        /// </summary>
        private void UpdateButtonVisibility(bool buildModeActive)
        {
            if (cancelButton == null) return;

            if (hideWhenInactive)
            {
                // Nascondi completamente quando non in build mode
                cancelButton.gameObject.SetActive(buildModeActive);
            }
            else
            {
                // Disabilita ma lascia visibile
                cancelButton.interactable = buildModeActive;

                // Opzionale: fade alpha quando disabilitato
                if (buttonIcon != null)
                {
                    Color iconColor = cancelColor;
                    iconColor.a = buildModeActive ? 1f : 0.5f;
                    buttonIcon.color = iconColor;
                }
            }
        }

        // ============================================
        // INTERACTION
        // ============================================

        /// <summary>
        /// Callback chiamato quando il pulsante viene cliccato.
        /// Cancella il ghost di costruzione e esce dal build mode.
        /// </summary>
        public void OnCancelClicked()
        {
            if (BuildModeController.Instance == null)
            {
                Debug.LogWarning("[BuildModeUI] BuildModeController.Instance is null!");
                return;
            }

            if (!BuildModeController.Instance.IsInBuildMode)
            {
                if (debugMode)
                {
                    Debug.Log("[BuildModeUI] Cancel clicked but not in build mode - ignoring");
                }
                return;
            }

            // Esce dal build mode (cancella automaticamente il ghost)
            BuildModeController.Instance.ExitBuildMode();

            if (debugMode)
            {
                Debug.Log("[BuildModeUI] Build mode cancellato dall'utente");
            }

            // Feedback audio/visuale opzionale
            // TODO: Aggiungi AudioSource.PlayOneShot() per feedback sonoro
        }

        // ============================================
        // PUBLIC API (opzionale per controllo esterno)
        // ============================================

        /// <summary>
        /// Abilita/disabilita l'opzione di auto-exit dopo piazzamento.
        /// Può essere chiamato da altri sistemi UI (es. settings menu).
        /// </summary>
        public void SetAutoExitAfterPlacement(bool enabled)
        {
            autoExitAfterPlacement = enabled;

            if (debugMode)
            {
                Debug.Log($"[BuildModeUI] Auto-exit after placement: {(enabled ? "ENABLED" : "DISABLED")}");
            }
        }

        /// <summary>
        /// Forza l'aggiornamento dello stato UI (utile per debug o reset)
        /// </summary>
        [Button("Force Update UI State"), FoldoutGroup("Debug Actions")]
        public void ForceUpdateUI()
        {
            if (BuildModeController.Instance != null)
            {
                isBuildModeActive = BuildModeController.Instance.IsInBuildMode;
                UpdateButtonVisibility(isBuildModeActive);
                Debug.Log($"[BuildModeUI] UI aggiornata manualmente - BuildMode: {isBuildModeActive}");
            }
        }

        // ============================================
        // EDITOR UTILITIES
        // ============================================

#if UNITY_EDITOR
        [Button("Auto-Find References"), FoldoutGroup("Debug Actions")]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void AutoFindReferences()
        {
            if (cancelButton == null)
            {
                cancelButton = GetComponentInChildren<Button>();
                if (cancelButton != null)
                {
                    Debug.Log($"[BuildModeUI] Auto-found Button: {cancelButton.name}");
                }
            }

            if (buttonIcon == null && cancelButton != null)
            {
                buttonIcon = cancelButton.GetComponentInChildren<Image>();
                if (buttonIcon != null)
                {
                    Debug.Log($"[BuildModeUI] Auto-found Image: {buttonIcon.name}");
                }
            }

            if (cancelSprite == null)
            {
                Debug.LogWarning("[BuildModeUI] Cannot auto-find sprite - please assign manually:\n" +
                    "Path: Assets/ModularGameUIKit/Common/Sprites/Icon-Symbols/Basics/X-Delete-Close-Error.png");
            }
        }

        [Button("Test Cancel Action"), FoldoutGroup("Debug Actions")]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void TestCancelAction()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[BuildModeUI] Enter Play Mode to test!");
                return;
            }

            OnCancelClicked();
        }
#endif
    }
}
