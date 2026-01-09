# BuildModeUI - Cancel Build Button System

Sistema UI per il pulsante "Annulla Costruzione" durante il Build Mode, con integrazione automatica del Modular Game UI Kit.

---

## 🚀 Quick Setup (AUTOMATICO)

### Metodo 1: Editor Tool (CONSIGLIATO)

1. **Apri il tool:**
   - Menu Unity: `Tools → Wilderness Survival → Setup Build Mode UI`

2. **Click sul pulsante verde:**
   - `🚀 AUTO SETUP COMPLETE SYSTEM`

3. **Done!**
   - Il sistema è configurato e pronto all'uso
   - Entra in Play Mode e testa selezionando una struttura

---

## 📋 Cosa fa l'Auto-Setup

L'Editor Tool automatizza completamente questi passaggi:

✅ **Canvas Setup:**
- Trova la Canvas esistente o ne crea una nuova
- Configura CanvasScaler per mobile (1920x1080 reference)
- Crea EventSystem se mancante

✅ **Button Creation:**
- Crea GameObject "CancelBuildButton" con struttura corretta
- Configura RectTransform con anchors mobile-friendly
- Aggiunge Background Image semi-trasparente

✅ **Icon Setup:**
- Cerca automaticamente lo sprite `X-Delete-Close-Error` dal Modular Game UI Kit
- Assegna sprite e colore rosso (#FF4444)
- Configura padding e layout

✅ **Component Configuration:**
- Aggiunge BuildModeUI component
- Collega tutti i SerializeField automaticamente
- Configura evento onClick → OnCancelClicked()

✅ **Positioning:**
- Default: Bottom-Right (ideale per mobile)
- Margine: 20px dai bordi
- Dimensione: 80x80 px

---

## 🎨 Opzioni di Customizzazione (Editor Tool)

### Button Position
- **Top Left** - Angolo superiore sinistro
- **Top Right** - Angolo superiore destro
- **Bottom Left** - Angolo inferiore sinistro
- **Bottom Right** - Angolo inferiore destro *(default, consigliato mobile)*

### Behavior Options
- **Hide When Inactive** *(default: ON)*
  - `ON`: Il pulsante scompare quando non in build mode
  - `OFF`: Il pulsante rimane visibile ma disabilitato (grigio)

- **Debug Mode** *(default: OFF)*
  - Abilita log dettagliati in console

### Colors
- **Cancel Color**: Colore rosso/error dal UI Kit
  - Default: `#FF4444` (RGB: 255, 68, 68)
  - Personalizzabile via color picker

---

## 🛠️ Setup Manuale (Alternativo)

Se preferisci configurare manualmente invece di usare il tool:

### 1. Crea Button UI
```
Hierarchy → Right-click → UI → Button
Rinomina: "CancelBuildButton"
```

### 2. Struttura GameObject
```
CancelBuildButton (Button component)
└── Icon (Image component) ← Assegna X-Delete-Close-Error sprite qui
```

### 3. Aggiungi BuildModeUI Script
```
1. Crea GameObject "BuildModeUI_Manager" sotto Canvas
2. Add Component → BuildModeUI
3. Assegna riferimenti in Inspector:
   - Cancel Button: drag CancelBuildButton
   - Button Icon: drag Icon (child Image)
   - Cancel Sprite: drag X-Delete-Close-Error from Project
   - Cancel Color: #FF4444
```

### 4. Collega Evento onClick
```
1. Seleziona CancelBuildButton
2. Inspector → Button → OnClick()
3. Add → drag BuildModeUI_Manager
4. Function → BuildModeUI.OnCancelClicked()
```

---

## 📂 Sprite Location

**Path completo:**
```
Assets/ModularGameUIKit/Common/Sprites/Icon-Symbols/Basics/X-Delete-Close-Error.png
```

**Project window navigation:**
```
ModularGameUIKit
└── Common
    └── Sprites
        └── Icon-Symbols
            └── Basics
                └── X-Delete-Close-Error.png
```

**Se lo sprite non viene trovato:**
- Verifica che il Modular Game UI Kit sia importato
- Cerca manualmente nel Project con filtro: `X-Delete-Close-Error`
- Assegna manualmente in Inspector

---

## 🎮 Testing

### Play Mode Test
1. **Entra in Play Mode**
2. **Seleziona una struttura dal BuildMenu**
   - Il pulsante Cancel deve apparire automaticamente
3. **Click sul pulsante rosso**
   - Il ghost scompare
   - Esci dal build mode
4. **Test rotazione ghost (Q/E)**
   - Il pulsante rimane visibile durante la rotazione
5. **Right-click o ESC**
   - Esce dal build mode (alternativa al pulsante)

### Debug Mode Test
1. **Abilita Debug Mode** in BuildModeUI Inspector
2. **Play Mode → Seleziona struttura**
3. **Check Console** per log dettagliati:
   ```
   [BuildModeUI] Build mode ATTIVO - Button visible
   [BuildModeUI] Build mode INATTIVO - Button hidden
   ```

---

## 🔧 Editor Tool Features

### Auto-Find References Button
```
Trova automaticamente:
- Canvas nella scena
- Button components
- Image components
- Sprite nel progetto
```

### One-Click Actions
```
🚀 AUTO SETUP COMPLETE SYSTEM
   → Setup completo end-to-end

Create Button Only
   → Crea solo il button (configura manualmente il component)

Find & Assign Sprite
   → Cerca e assegna lo sprite dal UI Kit
```

### Status Messages
```
✓ Success - Verde
⚠ Warning - Giallo
❌ Error - Rosso
```

---

## 📱 Mobile Optimization

Il sistema è ottimizzato per mobile:

✅ **Touch-Friendly Size:** 80x80 px (facile da cliccare)
✅ **Safe Margins:** 20px dai bordi (evita notch/cutout)
✅ **High Contrast:** Rosso su sfondo scuro (visibilità ottimale)
✅ **Responsive:** Anchors configurati per scaling automatico
✅ **Hide/Show:** Animazioni veloci (no lag)

---

## 🐛 Troubleshooting

### Pulsante non appare in Play Mode
```
✓ Check: BuildModeController.Instance esiste nella scena?
✓ Check: Canvas è abilitata?
✓ Check: Hide When Inactive è configurato correttamente?
✓ Debug: Abilita Debug Mode per vedere log
```

### Sprite non trovato dal tool
```
✓ Verifica path: Assets/ModularGameUIKit/.../X-Delete-Close-Error.png
✓ Re-import Modular Game UI Kit
✓ Assegna manualmente in Inspector
✓ Disabilita "Auto-Find Sprite" e assegna custom sprite
```

### Click non funziona
```
✓ Check: EventSystem presente nella scena?
✓ Check: Button.onClick ha listener configurato?
✓ Check: Canvas → GraphicRaycaster è presente?
✓ Check: Button.interactable = true?
```

### BuildModeController.Instance è null
```
✓ Verifica che BuildModeController esista nella scena
✓ Check Singleton initialization in Awake()
✓ Check script execution order (BuildModeController prima di UI)
```

---

## 📖 API Reference

### BuildModeUI Public Methods

```csharp
// Callback click - chiamato automaticamente dal Button
public void OnCancelClicked()

// Abilita/disabilita auto-exit dopo piazzamento
public void SetAutoExitAfterPlacement(bool enabled)

// Forza aggiornamento UI (debug)
public void ForceUpdateUI()
```

### BuildModeUI Inspector Fields

```csharp
[SerializeField] Button cancelButton;          // Riferimento Button UI
[SerializeField] Image buttonIcon;             // Image child per icona
[SerializeField] Sprite cancelSprite;          // X-Delete-Close-Error sprite
[SerializeField] Color cancelColor;            // Rosso UI Kit (#FF4444)
[SerializeField] bool autoExitAfterPlacement;  // Toggle auto-exit
[SerializeField] bool hideWhenInactive;        // Nascondi vs Disable
[SerializeField] bool debugMode;               // Abilita log
```

---

## 🎯 Integration with Other Systems

### BuildModeController Integration
```csharp
// Check build mode status
bool isBuilding = BuildModeController.Instance.IsInBuildMode;

// Exit build mode programmatically
BuildModeController.Instance.ExitBuildMode();

// Enter build mode with structure
BuildModeController.Instance.SelectStructure(structureData);
```

### Future Extensions
```csharp
// Aggiungi feedback audio
AudioSource.PlayOneShot(cancelSound);

// Aggiungi animazioni
DOTween animations per fade/scale

// Aggiungi conferma dialog
ShowConfirmDialog("Annullare costruzione?");
```

---

## 📦 Files Created

```
Assets/
├── _UI/
│   └── Scripts/
│       ├── BuildModeUI.cs              ← Main component
│       └── BuildModeUI_README.md       ← This file
└── Editor/
    └── BuildModeUISetupTool.cs         ← Automation tool
```

---

## ✅ Checklist Setup Completo

- [ ] Tool aperto: `Tools → Wilderness Survival → Setup Build Mode UI`
- [ ] Click: `🚀 AUTO SETUP COMPLETE SYSTEM`
- [ ] Messaggio verde: "✅ SETUP COMPLETATO CON SUCCESSO!"
- [ ] GameObject "CancelBuildButton" presente in Hierarchy
- [ ] BuildModeUI_Manager configurato in Canvas
- [ ] Sprite X-Delete-Close-Error assegnato
- [ ] Play Mode test: pulsante appare quando selezioni struttura
- [ ] Click test: pulsante cancella il ghost
- [ ] Mobile test: pulsante visibile e cliccabile su device

---

## 📞 Support

**Issues comuni:**
- Sprite not found → Check Modular Game UI Kit import
- Button not visible → Check Canvas settings
- Click not working → Check EventSystem

**Debug Tips:**
1. Abilita Debug Mode in Inspector
2. Check Console per log dettagliati
3. Verifica BuildModeController.Instance != null
4. Test in Play Mode, non Edit Mode

---

## 🚀 Quick Reference

**ONE-LINE SETUP:**
```
Tools → Wilderness Survival → Setup Build Mode UI → Click GREEN button → DONE!
```

**Manual Test:**
```
Play → Select Structure → Click Red Button → Ghost disappears ✓
```

**Customization:**
```
Inspector → BuildModeUI → Modify colors/behavior → Test in Play Mode
```

---

*Ultima modifica: 2026-01-01*
*Version: 1.0*
*Compatibilità: Unity 2021.3+ | Modular Game UI Kit*
