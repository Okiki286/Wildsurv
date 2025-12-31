# ⚡ QUICK FIX - Android Build Menu (30 secondi)

**Problema**: Build Menu vuoto su Android
**Soluzione**: Spostare cartella Structures in Resources

---

## 🎯 Fix Rapido (Drag & Drop)

### Passo 1: Apri Unity Project Window

### Passo 2: Naviga a questa cartella
```
Assets/_Content/Data/Structures/
```

**Contiene**:
- Farm.asset
- House.asset
- Sawmill.asset
- Tower.asset
- Waystone.asset

### Passo 3: SPOSTA la cartella Structures

**DA**:
```
Assets/_Content/Data/Structures/
```

**A**:
```
Assets/_Content/Data/Resources/Structures/
```

**Come fare**:
1. Seleziona cartella `Structures` (contiene 5 file .asset)
2. Drag & Drop in `Assets/_Content/Data/Resources/`
3. Conferma se Unity chiede

### Passo 4: Verifica Path Finale
```
Assets/
└── _Content/
    └── Data/
        └── Resources/
            └── Structures/       ← QUI!
                ├── Farm.asset
                ├── House.asset
                ├── Sawmill.asset
                ├── Tower.asset
                └── Waystone.asset
```

---

## ✅ Test in Editor

1. Unity → Play Mode
2. Console → Cerca:
```
[BuildMenu] Strategy 2 (all Resources): Found 5 structures
[BuildMenu] ✓ Loaded 5 structures from ALL Resources folders
```

3. Press `B` → Build Menu si apre con 5 pulsanti

---

## 📱 Build Android e Test

1. File → Build Settings → Android
2. Build APK
3. Installa su device
4. Apri app
5. Tap Build button → Menu si apre con pulsanti! ✅

---

## 🐛 Se Non Funziona

### Check 1: Verifica Path
```
Project Window → Assets/_Content/Data/Resources/Structures/
```
Deve contenere i 5 file .asset

### Check 2: Console Log
```
[BuildMenu] === FINAL RESULT: 5 STRUCTURES LOADED ===
```
Se count = 0 → Path errato

### Check 3: Logcat Android
```
adb logcat | grep BuildMenu
```
Deve mostrare: `Found 5 structures`

---

## 🔄 Alternative (se Drag & Drop non funziona)

### Opzione A: Crea Nuova Cartella Resources
```
1. Assets → Create → Folder → "Resources"
2. Dentro Resources → Create → Folder → "Data"
3. Dentro Data → Create → Folder → "Structures"
4. Drag & Drop i 5 file .asset in Structures/
```

### Opzione B: Lista Manuale Inspector
```
1. Hierarchy → BuildMenuUI GameObject
2. Inspector → "Mobile Build Fallback"
3. Manual Structures List → Size: 5
4. Drag & Drop:
   - Element 0: Farm.asset
   - Element 1: House.asset
   - Element 2: Sawmill.asset
   - Element 3: Tower.asset
   - Element 4: Waystone.asset
5. Play → Console: "✓ Loaded 5 structures from MANUAL LIST"
```

---

## ✅ FATTO!

**Tempo**: 30 secondi
**Rischio**: Zero (solo spostare files)
**Result**: Build Menu funziona su Android! 🎉

---

**Path Finale Corretto**:
```
Assets/_Content/Data/Resources/Structures/
```

**Codice già fixato** in `BuildMenuUI.cs` - usa `Resources.LoadAll("")` che trova automaticamente i file in qualsiasi sottocartella di Resources.
