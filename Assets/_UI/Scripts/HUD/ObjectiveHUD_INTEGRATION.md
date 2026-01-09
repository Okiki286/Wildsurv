# ObjectiveHUD - Guida all'Integrazione

## 📋 Panoramica

`ObjectiveHUD.cs` è un componente UI che mostra il progresso dell'obiettivo di sopravvivenza al giocatore.

**Formato testo:** `"Obiettivo: Sopravvivi X/Y Giorni"`

**Features:**
- ✅ Aggiornamento automatico quando cambia il giorno
- ✅ Cambio colore automatico all'ultimo giorno (tensione visiva)
- ✅ Animazione "Punch" (scale up/down) quando avanza il giorno
- ✅ Supporto DOTween con fallback a Coroutine se non disponibile
- ✅ Integrazione con SurvivalSystem e DayNightSystem

---

## 🔧 Setup in Unity

### 1. Creare l'UI GameObject

1. **Hierarchy** → Click destro → **UI** → **Text - TextMeshPro**
   - Questo crea automaticamente un Canvas se non esiste già

2. **Rinomina** il GameObject in `ObjectiveHUD`

3. **Configura RectTransform:**
   - **Anchor Preset:** Top-Center (Alt+click per auto-position)
   - **Position Y:** -40 (40 pixel sotto il top dello schermo)
   - **Width:** 400
   - **Height:** 60

### 2. Configurare il TextMeshProUGUI

Seleziona il componente **TextMeshProUGUI** nel GameObject `ObjectiveHUD`:

- **Font:** Scegli un font leggibile (es. LiberationSans SDF)
- **Font Size:** 24-28
- **Alignment:** Center + Middle
- **Color:** Bianco (o grigio chiaro #CCCCCC)
- **Overflow:** Ellipsis o Overflow
- **Wrapping:** Disabled

**Testo placeholder:** `"Obiettivo: Sopravvivi 0/5 Giorni"`

### 3. Aggiungere il componente ObjectiveHUD

1. Seleziona il GameObject `ObjectiveHUD`
2. **Add Component** → Cerca `ObjectiveHUD`
3. **Assegna Reference:**
   - **Objective Text:** Trascina il componente **TextMeshProUGUI** (dello stesso GameObject)

### 4. Configurare i Parametri

Nel componente `ObjectiveHUD`:

#### **UI References**
- ✅ **Objective Text:** (già assegnato)

#### **Colors**
- **Normal Color:** `#CCCCCC` (Grigio chiaro - giorni normali)
- **Last Day Color:** `#FFD940` (Giallo/Oro - ultimo giorno)

#### **Animation Settings**
- **Enable Punch Animation:** ✅ (spuntato)
- **Punch Scale:** `1.2` (scala al 120% = +20% di size)
- **Punch Duration:** `0.5s`
- **Punch Elasticity:** `1` (bounce leggero)

#### **Debug**
- **Debug Mode:** ✅ (spuntato durante testing, poi disattivare)

---

## 🎬 Come Funziona

### Flusso di Aggiornamento

```
DayNightSystem.Update()
    ↓
CurrentDayNumber cambia (da 1 a 2)
    ↓
ObjectiveHUD.Update() rileva il cambio
    ↓
UpdateObjectiveText()
    - Aggiorna il testo: "Obiettivo: Sopravvivi 2/5 Giorni"
    - Controlla se è l'ultimo giorno (current == target - 1)
    - Cambia colore se necessario
    ↓
PlayPunchAnimation()
    - Se DOTween disponibile: DOPunchScale()
    - Altrimenti: SimplePunchAnimation() coroutine
```

### Polling Pattern

Il componente usa **polling in Update()** (stesso pattern del `SurvivalSystem`):

```csharp
private void Update()
{
    int currentDay = DayNightSystem.Instance.CurrentDayNumber;

    if (currentDay != lastDayNumber)
    {
        lastDayNumber = currentDay;
        UpdateObjectiveText();
        PlayPunchAnimation();
    }
}
```

**Perché polling?** Il `DayNightSystem` usa eventi `GameEvent` che non espongono `AddListener()` pubblicamente. Il polling è lightweight (1 int comparison per frame) e consistente con l'architettura esistente.

---

## 🎨 Animazione Punch

### Con DOTween (Raccomandato)

Se DOTween è installato, viene usato `DOPunchScale()`:

```csharp
#if DOTWEEN_ENABLED
    textRectTransform.DOPunchScale(
        new Vector3(0.2f, 0.2f, 0f),  // +20% scale
        0.5f,                          // 0.5s duration
        1                              // Elasticity
    );
#endif
```

**Vantaggi:**
- ✅ Smooth & elastic bounce
- ✅ Performance ottimizzate
- ✅ Automatico overshoot

### Senza DOTween (Fallback)

Se DOTween non è disponibile, usa una **Coroutine semplice**:

1. **Fase 1 (40%):** Scale up da 1.0 → 1.2 (smooth lerp)
2. **Fase 2 (60%):** Scale down da 1.2 → 1.0 con overshoot (bounce)
3. **Fase 3:** Reset alla scala originale esatta

**Nota:** Il fallback è visivamente simile ma meno fluido del DOTween.

---

## 🔗 Dipendenze

### Required (CRITICAL)
- ✅ **SurvivalSystem.Instance** - Fornisce DaysSurvived, DaysToWin
- ✅ **DayNightSystem.Instance** - Fornisce CurrentDayNumber per sincronizzazione
- ✅ **TextMeshPro** - Per il rendering del testo

### Optional
- 🟡 **DOTween** - Per animazioni smooth (fallback disponibile se assente)

### ⚠️ Nota sulla Sincronizzazione
Il `SurvivalSystem` aggiorna `DaysSurvived` in polling (Update), quindi c'è un frame di ritardo all'avvio:
- **Start gioco**: `DaysSurvived = 0` (non ancora sincronizzato), `CurrentDayNumber = 1`
- **ObjectiveHUD** usa `Mathf.Max(DaysSurvived, CurrentDayNumber)` per mostrare sempre il giorno corretto
- Questo previene il bug "0/5 Giorni → 2/5 Giorni" (skip del giorno 1)

---

## 📊 Testing

### Test in Play Mode

1. **Avvia il gioco** in Unity
2. **Apri l'Inspector** dell'ObjectiveHUD
3. Nel componente `ObjectiveHUD`, sezione **Debug Actions:**

#### Test Manuale Aggiornamento:
- Click su **"🔄 Force Update"** → Aggiorna il testo immediatamente

#### Test Animazione:
- Click su **"🎬 Test Punch Animation"** → Testa l'animazione punch

### Test Automatico (Cambio Giorno)

1. **Trova il DayNightSystem** nella scena
2. Nell'Inspector, sezione **Quick Actions:**
   - Click **"☀️ Skip to Day"** per avanzare al giorno successivo
3. **Osserva l'ObjectiveHUD:**
   - Il testo dovrebbe aggiornarsi (es: `1/5` → `2/5`)
   - L'animazione punch dovrebbe attivarsi
   - Il colore dovrebbe cambiare se sei all'ultimo giorno

### Verifica Console (Debug Mode)

Con **Debug Mode** abilitato, dovresti vedere:

```
<color=cyan>[ObjectiveHUD]</color> Aggiornato: 2/5 - Status: Normale
<color=green>[ObjectiveHUD]</color> 🎬 Punch animation (DOTween) triggered!
```

Oppure (se DOTween non disponibile):

```
<color=yellow>[ObjectiveHUD]</color> 🎬 Punch animation (Coroutine) triggered!
```

---

## 🎯 Caso d'Uso: Ultimo Giorno

### Scenario

- **DaysToWin:** 5
- **CurrentDay:** 4 (ultimo giorno prima della vittoria)

### Comportamento Atteso

```csharp
bool isLastDay = (current == target - 1); // 4 == 5 - 1 → TRUE
objectiveText.color = lastDayColor; // GIALLO/ORO
```

**Testo:** `"Obiettivo: Sopravvivi 4/5 Giorni"`
**Colore:** 🟡 Giallo (#FFD940)
**Animazione:** Punch quando cambia da giorno 3 → 4

### Quando torna normale?

Solo se i giorni sopravvissuti diminuiscono (non dovrebbe accadere in gameplay normale). Se `current < target - 1`, il colore torna a `normalColor`.

---

## 🛠️ Personalizzazione

### Cambiare il Formato Testo

**File:** `ObjectiveHUD.cs` → riga ~128

```csharp
// Formato attuale
objectiveText.text = $"Obiettivo: Sopravvivi {current}/{target} Giorni";

// Esempio alternativo (inglese)
objectiveText.text = $"Survive: {current}/{target} Days";

// Esempio alternativo (emoji)
objectiveText.text = $"🎯 Survive: {current}/{target} Days";
```

### Cambiare i Colori

**Opzione 1:** Inspector (Runtime)
- Modifica **Normal Color** e **Last Day Color** direttamente nell'Inspector

**Opzione 2:** Script (via codice)
```csharp
ObjectiveHUD hud = FindObjectOfType<ObjectiveHUD>();
hud.SetColors(
    new Color(0.7f, 0.7f, 0.7f, 1f),  // Normal (grigio)
    new Color(1f, 0f, 0f, 1f)          // Last Day (rosso)
);
```

### Cambiare l'Animazione

**Parametri modificabili:**
- **Punch Scale:** `1.2` → `1.5` (più dramatic)
- **Punch Duration:** `0.5s` → `0.8s` (più lento)
- **Punch Elasticity:** `1` → `3` (più bounce)

**Disabilitare l'animazione:**
```csharp
hud.SetPunchAnimationEnabled(false);
```

---

## 📍 Posizionamento Raccomandato

### Layout Tipico HUD

```
┌─────────────────────────────┐
│   [ObjectiveHUD]            │  ← Top-Center, Y: -40
│   Sopravvivi 2/5 Giorni     │
│                             │
│  [Day/Night UI]   [Resources] ← Altri HUD elements
│                             │
│                             │
│         [Game View]         │
│                             │
│                             │
│   [Build Menu] [Workers]    │  ← Bottom UI
└─────────────────────────────┘
```

### Safe Area (Mobile)

Se il gioco supporta mobile, considera il **Safe Area**:

- **iOS:** Notch + Home Indicator
- **Android:** Notch vari

**Posizionamento sicuro:** Y: -80 (invece di -40) per evitare overlap con notch.

---

## ❓ Troubleshooting

### Problema: "Il testo non si aggiorna"

**Verifica:**
1. ✅ `SurvivalSystem.Instance != null` in console
2. ✅ `DayNightSystem.Instance != null` in console
3. ✅ Objective Text reference è assegnato nell'Inspector
4. ✅ Debug Mode abilitato → Controlla console per log

**Soluzione:**
- Assicurati che `SurvivalSystem` e `DayNightSystem` siano nella scena
- Controlla che non ci siano errori di NullReference in console

### Problema: "L'animazione non parte"

**Verifica:**
1. ✅ Enable Punch Animation è spuntato
2. ✅ DOTween importato? Controlla se `#if DOTWEEN_ENABLED` è definito
3. ✅ RectTransform è presente sul GameObject

**Soluzione:**
- Se DOTween non è installato, il fallback Coroutine dovrebbe comunque funzionare
- Testa manualmente con il button **"🎬 Test Punch Animation"**

### Problema: "Colore non cambia all'ultimo giorno"

**Verifica:**
1. ✅ Sei effettivamente all'ultimo giorno? (current == target - 1)
2. ✅ Last Day Color è diverso da Normal Color?
3. ✅ Debug log mostra "Status: ULTIMO GIORNO!"?

**Esempio di log atteso:**
```
<color=cyan>[ObjectiveHUD]</color> Aggiornato: 4/5 - Status: ULTIMO GIORNO!
```

### Problema: "Il contatore salta da 0/5 a 2/5 (skip giorno 1)"

**Causa:** Il `SurvivalSystem` sincronizza `DaysSurvived` nel primo Update(), non in Start().

**Soluzione:** Già implementata! L'ObjectiveHUD usa `Mathf.Max(DaysSurvived, CurrentDayNumber)` per compensare.

**Verifica Fix:**
Con Debug Mode attivo, dovresti vedere:
```
<color=cyan>[ObjectiveHUD]</color> Aggiornato: 1/5 (Survival:0, Day:1) - Status: Normale
```

Se vedi `(Survival:0, Day:1)` ma il testo mostra `1/5`, il fix funziona correttamente!

---

## 🎓 API Pubblica

### Metodi Pubblici

#### `ForceUpdate()`
Forza un aggiornamento immediato del testo (ignora il polling).

```csharp
ObjectiveHUD hud = GetComponent<ObjectiveHUD>();
hud.ForceUpdate();
```

#### `TestPunchAnimation()`
Testa l'animazione punch manualmente.

```csharp
hud.TestPunchAnimation();
```

#### `SetColors(Color normal, Color lastDay)`
Imposta colori personalizzati.

```csharp
hud.SetColors(Color.white, Color.red);
```

#### `SetPunchAnimationEnabled(bool enabled)`
Abilita/disabilita l'animazione a runtime.

```csharp
hud.SetPunchAnimationEnabled(false); // Disattiva animazioni
```

---

## 📦 Checklist Finale

Prima di considerare l'integrazione completa:

- [ ] GameObject `ObjectiveHUD` creato nella scena
- [ ] Componente `ObjectiveHUD.cs` aggiunto
- [ ] Reference **Objective Text** assegnato
- [ ] Colori configurati (Normal + Last Day)
- [ ] Animazione testata manualmente (button debug)
- [ ] Test automatico: Skip giorno e verifica aggiornamento
- [ ] Debug Mode disabilitato per release
- [ ] Safe Area considerato (se mobile)

---

## 📝 Note Finali

### Performance

- **Update Polling:** 1 int comparison per frame (~0.001ms overhead)
- **Animazione DOTween:** Altamente ottimizzata, zero allocazioni
- **Coroutine Fallback:** Minimal GC pressure (Vector3 stack-allocated)

### Estensibilità Futura

Possibili miglioramenti:

1. **Event-Based Update:** Se `DayNightSystem` espone `OnDayChanged` pubblicamente
2. **Localization:** Sostituire stringhe hard-coded con LocalizationTable
3. **Sound Effects:** Aggiungere SFX quando il giorno avanza
4. **Progress Bar:** Aggiungere slider visuale oltre al testo

---

**Autore:** Unity UI Programmer
**Versione:** 1.0
**Data:** 2026-01-02
**Dependencies:** SurvivalSystem, DayNightSystem, TextMeshPro
