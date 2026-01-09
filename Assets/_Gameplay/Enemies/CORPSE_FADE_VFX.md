# Corpse Fade Out VFX System 💀✨

## 📋 OVERVIEW

**Feature**: I cadaveri dei nemici ora spariscono gradualmente all'alba con un effetto **Fade Out + Sink** invece di scomparire istantaneamente ("pop").

**Tecnica Implementata**:
1. **Alpha Fade**: Riduce l'opacità dei materiali da 1.0 → 0.0
2. **Sink Effect**: Affonda il corpo nel terreno progressivamente
3. **Cleanup**: Distrugge o ritorna al pool solo alla fine dell'animazione

---

## 🛠️ IMPLEMENTAZIONE TECNICA

### Parametri Configurabili (Inspector)

Nuova sezione **"Corpse VFX"** in `EnemyInstance`:

```csharp
[TitleGroup("Corpse VFX")]
[SerializeField] private float corpseFadeDuration = 2.0f;  // Durata fade (secondi)
[SerializeField] private float corpseSinkSpeed = 0.5f;     // Velocità affondamento (m/s)
[SerializeField] private bool tryAlphaFade = true;         // Abilita alpha fade
```

**Valori Raccomandati**:
- `corpseFadeDuration`: **2.0s** (smooth ma non troppo lento)
- `corpseSinkSpeed`: **0.5 m/s** (affonda ~1 metro in 2 secondi)
- `tryAlphaFade`: **true** (prova sempre, fallback su sink se shader non supporta alpha)

---

### Coroutine `FadeAndDestroy()`

**Location**: `EnemyInstance.cs` (linee 1090-1177)

#### Fase 1: Setup (Frame 0)

```csharp
// Raccogli tutti i renderer (SkinnedMesh + Mesh)
SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();

// Cache materiali e colori originali
List<Material> materialsToFade = new List<Material>();
List<Color> originalColors = new List<Color>();
```

**Perché**:
- `GetComponentsInChildren<>()` trova tutti i renderer nel prefab (body, weapons, accessories)
- Cache colori originali per interpolazione smooth

#### Fase 2: Loop Fade (Frame 1 → N)

```csharp
while (elapsed < corpseFadeDuration)
{
    float t = elapsed / corpseFadeDuration;  // 0 → 1 (progresso)

    // ALPHA FADE: Interpola alpha dei materiali
    newColor.a = Mathf.Lerp(1f, 0f, t);
    material.color = newColor;

    // SINK: Muovi giù nel terreno
    transform.position = startPosition - new Vector3(0f, sinkAmount, 0f);

    yield return null;  // Prossimo frame
}
```

**Math Breakdown**:

| Time (s) | t (0→1) | Alpha | Y Offset (sink) |
|----------|---------|-------|-----------------|
| 0.0      | 0.0     | 1.0   | 0.0m            |
| 0.5      | 0.25    | 0.75  | -0.25m          |
| 1.0      | 0.5     | 0.5   | -0.5m           |
| 1.5      | 0.75    | 0.25  | -0.75m          |
| 2.0      | 1.0     | 0.0   | -1.0m           |

#### Fase 3: Cleanup (Fine Loop)

```csharp
// Ritorna al pool (se disponibile) o distruggi
if (EnemyPooler.Instance != null)
{
    EnemyPooler.Instance.ReturnEnemy(gameObject);
}
else
{
    Destroy(gameObject);
}
```

---

## 🎨 SHADER COMPATIBILITY

### Problema: KayKit Materials (Opaque Shader)

I materiali KayKit usano spesso **Standard Shader (Opaque)** che **ignora l'alpha channel**.

**Sintomo**: Alpha fade non funziona → cadavere resta opaco.

**Soluzione 1: Sink Effect (Always Works)**
- ✅ Il sink effect funziona **sempre** (movimento Transform)
- Il corpo affonda nel terreno e sparisce visivamente

**Soluzione 2: Cambio Rendering Mode (Advanced)**
Se vuoi alpha fade funzionante:

1. **Trova il materiale** in `Assets/KayKit/.../Materials/`
2. **Inspector** → Shader: `Standard`
3. **Rendering Mode**: Cambia da `Opaque` → `Fade` o `Transparent`
4. **Warning**: Cambiare rendering mode può causare artifacts (z-fighting, sorting issues)

**Raccomandazione**: Usa **solo Sink Effect** (più stabile e performante).

---

## 🎮 COME TESTARE

### Test 1: Fade Out All'Alba

**Setup**:
1. **Play Mode** → Spawna nemici
2. **Uccidi nemici** durante la notte
3. **Abilita debug**: `EnemyInstance` → `debugMode = true`
4. **Salta al giorno**: DayNightSystem → "☀️ Skip to Day" (se hai debug button)

**Console Logs Attesi**:
```
[Corpse] W_Skeleton_Minion will be cleaned up at dawn (Day 2)
[Corpse] W_Skeleton_Minion starting fade out at dawn (Day 2)
[Corpse] W_Skeleton_Minion found 3 materials to fade
[Corpse] W_Skeleton_Minion fade complete, destroying
```

**Visual Check**:
- ✅ Cadavere inizia a **affondare** nel terreno (sink)
- ✅ Opacità diminuisce gradualmente (se shader supporta alpha)
- ✅ Dopo ~2 secondi: cadavere completamente sparito
- ❌ **NON** dovrebbe sparire istantaneamente ("pop")

---

### Test 2: Tweak Parameters

**Fade Rapido** (0.5s):
```csharp
corpseFadeDuration = 0.5f;
corpseSinkSpeed = 2.0f;  // Affonda veloce
```
→ Effetto veloce, meno smooth

**Fade Lento** (5s):
```csharp
corpseFadeDuration = 5.0f;
corpseSinkSpeed = 0.2f;  // Affonda lento
```
→ Molto graduale, quasi impercettibile

**Solo Sink (No Alpha)**:
```csharp
tryAlphaFade = false;
corpseSinkSpeed = 1.0f;
```
→ Solo affondamento, no transparency

---

### Test 3: Multiple Corpses

**Setup**:
1. Uccidi **10+ nemici** in un'area ristretta
2. Salta all'alba
3. **Expected**: Tutti i cadaveri fade contemporaneamente

**Performance Check**:
- Monitor FPS durante fade di 10+ cadaveri
- Se FPS drop significativo → riduci `corpseFadeDuration` o disabilita `tryAlphaFade`

---

## 🐛 TROUBLESHOOTING

### Problema 1: Cadavere NON affonda

**Possibile Causa**: Coroutine non parte.

**Debug**:
1. Verifica console log: `"starting fade out at dawn"` deve apparire
2. Verifica che `DespawnCorpse()` sia chiamato
3. Aggiungi breakpoint in `FadeAndDestroy()` per verificare esecuzione

**Fix**:
- Verifica che `DayNightSystem.Instance.OnDayStartedEvent` sia configurato
- Verifica che evento venga raised in `DayNightSystem.StartDay()`

---

### Problema 2: Alpha Fade NON funziona (corpo resta opaco)

**Causa**: Shader Opaque non supporta alpha.

**Verifica**:
1. Seleziona nemico in Play Mode
2. Inspector → Skinned Mesh Renderer → Materials
3. Click su materiale → Rendering Mode: `Opaque` (problema!)

**Fix**:
- **Option A (Facile)**: Disabilita alpha fade: `tryAlphaFade = false`
- **Option B (Advanced)**: Cambia rendering mode a `Fade` (vedi sezione Shader Compatibility)

**Nota**: Il sink effect **funziona sempre** anche senza alpha fade.

---

### Problema 3: Cadavere "galleggia" dopo sink

**Causa**: Transform position salvato in variabili esterne.

**Fix**: Non dovrebbe accadere, ma se succede:
- Verifica che `Die()` disabiliti correttamente `NavMeshAgent` (line 1019-1023)
- Verifica che nessun altro script modifichi `transform.position` dopo morte

---

### Problema 4: Materiali diventano neri durante fade

**Causa**: Modifica del colore invece che solo alpha.

**Soluzione**: Il codice preserva RGB e modifica solo Alpha:
```csharp
Color newColor = originalColors[i];  // ✅ Preserva RGB
newColor.a = Mathf.Lerp(1f, 0f, t);  // ✅ Modifica solo Alpha
```

Se problema persiste → shader custom sta ignorando `material.color`.

---

## 🔧 CUSTOMIZATION IDEAS

### Idea 1: Particle VFX on Despawn

Aggiungi VFX quando fade inizia:

```csharp
// In DespawnCorpse() prima di StartCoroutine:
if (despawnVFX != null)
{
    Instantiate(despawnVFX, transform.position, Quaternion.identity);
}
```

**VFX Consigliati**:
- Smoke puff (cadavere si dissolve)
- Dust cloud (corpo crolla)
- Sparkles (magia di pulizia)
- Ravens flying away (tematico)

---

### Idea 2: Audio Cue

Aggiungi suono "whoosh" durante fade:

```csharp
// In FadeAndDestroy() setup:
AudioSource.PlayClipAtPoint(despawnSound, transform.position, 0.5f);
```

---

### Idea 3: Scale Down Effect

Combina sink + scale down per effetto "implosione":

```csharp
// In loop fade:
float scale = Mathf.Lerp(1f, 0.1f, t);
transform.localScale = Vector3.one * scale;
```

---

### Idea 4: Random Fade Delay (Stagger)

Se molti cadaveri fade insieme, stagger l'inizio:

```csharp
// In DespawnCorpse():
float randomDelay = Random.Range(0f, 1.0f);
StartCoroutine(FadeAndDestroyDelayed(randomDelay));
```

```csharp
private IEnumerator FadeAndDestroyDelayed(float delay)
{
    yield return new WaitForSeconds(delay);
    yield return FadeAndDestroy();
}
```

**Risultato**: Cadaveri spariscono in modo scaglionato, più naturale.

---

## 📊 PERFORMANCE NOTES

### Memory Allocation

**Per-Corpse Allocation**:
- `List<Material>`: ~24 bytes + (8 bytes × material count)
- `List<Color>`: ~24 bytes + (16 bytes × color count)
- **Totale**: ~100-200 bytes per cadavere

**Esempio**: 50 cadaveri = ~10 KB allocati

**Optimization**: Se problema, usa `MaterialPropertyBlock` invece di `material.color` (no allocations).

---

### Frame Time Impact

**Per-Frame Cost** (per cadavere in fade):
- Material color set: ~0.02ms × material count
- Transform.position set: ~0.01ms
- **Totale**: ~0.05-0.1ms per cadavere/frame

**Esempio**: 20 cadaveri in fade = ~2ms/frame (acceptable su 60 FPS target)

---

### GPU Performance

**Alpha Blending Cost**:
- Transparent/Fade rendering mode: +10-20% fragment shader cost
- Se troppi cadaveri in fade → FPS drop su GPU lenti

**Mitigation**:
- Usa `tryAlphaFade = false` su dispositivi low-end
- Solo sink effect ha zero GPU cost aggiuntivo

---

## ✅ CHECKLIST TEST FINALE

- [ ] Uccidi nemico durante notte
- [ ] Cadavere resta visibile fino all'alba
- [ ] All'alba: console log `"starting fade out"`
- [ ] Cadavere affonda gradualmente nel terreno (sink)
- [ ] Opacità diminuisce (se shader supporta)
- [ ] Dopo ~2 secondi: cadavere completamente sparito
- [ ] Nessun error in console
- [ ] FPS stabile durante fade (anche con 10+ cadaveri)
- [ ] Pooling funziona correttamente (cadavere ritornato al pool)

---

## 📝 SUMMARY

**Files Modified**: `EnemyInstance.cs`

**Lines Added**: ~100 (coroutine + parametri)

**Breaking Changes**: Nessuno (backward compatible)

**Performance Impact**: Minimo (~0.1ms/frame per cadavere in fade)

**Visual Impact**: Smooth fade-out invece di "pop" istantaneo ✅

**User Experience**: Molto migliorata 🌟

---

**Status**: ✅ READY FOR TESTING

**Recommended Settings**:
```
corpseFadeDuration: 2.0s
corpseSinkSpeed: 0.5 m/s
tryAlphaFade: true
```

**Fallback Mode** (if alpha issues):
```
corpseFadeDuration: 1.5s
corpseSinkSpeed: 1.0 m/s
tryAlphaFade: false  // Solo sink
```
