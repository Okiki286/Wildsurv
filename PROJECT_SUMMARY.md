# 🏕️ Wilderness Survival - Project Summary

## 📋 Overview

**Wilderness Survival** è un gioco di sopravvivenza/gestione in stile city-builder con visuale isometrica. Il giocatore gestisce un insediamento, assegna lavoratori a strutture, raccoglie risorse, e difende il villaggio da ondate di nemici.

---

## 🏗️ Architettura del Progetto

```
Assets/
├── _Core/           # Sistemi fondamentali (GameManager, Camera, Events)
├── _Gameplay/       # Logica di gioco principale
│   ├── Workers/     # Sistema lavoratori
│   ├── Structures/  # Edifici e costruzione
│   ├── Resources/   # Gestione risorse
│   ├── Enemies/     # Sistema nemici
│   ├── Combat/      # Combattimento
│   ├── Map/         # Zone e marcatori mappa
│   ├── World/       # Generazione mondo
│   └── TechTree/    # Albero tecnologico
├── _UI/             # Interfaccia utente
├── Editor/          # Tool personalizzati per l'Editor Unity
├── ModularGameUIKit/ # Asset UI Kit
├── Synty/           # Asset 3D (Polygon Fantasy Kingdom, ecc)
└── ToonyTinyPeople/ # Asset personaggi
```

---

## 👥 Sistema Worker

### File Principali
| File | Descrizione |
|------|-------------|
| `WorkerSystem.cs` | Singleton che gestisce spawn, assegnazione automatica, tick loop |
| `WorkerInstance.cs` | Istanza runtime di un worker (stats, job, assignment) |
| `WorkerController.cs` | Controller fisico (movimento NavMesh, animazioni, visual swap) |
| `WorkerData.cs` | ScriptableObject con dati base del worker |
| `WorkerJobData.cs` | ScriptableObject che definisce un ruolo/job |
| `JobDatabase.cs` | Database singleton di tutti i WorkerJobData |

### Flusso
```
WorkerSystem.CreateWorkerInstance(WorkerData)
    → Crea WorkerInstance
    → Istanzia Prefab fisico
    → Linka WorkerController ↔ WorkerInstance
    → Registra nel sistema
```

### Job System
- **Villager**: Unità base, può costruire e lavorare
- **Gatherer**: Raccoglie risorse
- **Guard**: Protegge strutture
- *(Scout, Crafter, Researcher - futuri)*

---

## 🏰 Sistema Strutture

### File Principali
| File | Descrizione |
|------|-------------|
| `StructureSystem.cs` | Gestisce spawn, overlap check, registrazione |
| `StructureController.cs` | Controller runtime di una struttura |
| `StructureData.cs` | ScriptableObject con dati della struttura |
| `BuildModeController.cs` | Modalità costruzione (ghost preview, placement) |

### Stati
```
Preview → Building → Operating → (Destroyed)
```

### Flusso Costruzione
1. `BuildModeController` attiva ghost preview
2. Click piazza struttura → `StructureSystem.SpawnStructure()`
3. Stato `Building`: worker assegnati costruiscono
4. `TickConstruction()` progressa la costruzione
5. Completato → Stato `Operating`
6. Worker rilasciati e disponibili

---

## 💰 Sistema Risorse

### File Principali
| File | Descrizione |
|------|-------------|
| `ResourceSystem.cs` | Singleton gestione risorse globali |
| `ResourceData.cs` | ScriptableObject definizione risorsa |

### Risorse
- **Wood** 🪵
- **Stone** ⛏️
- **Food** 🌾
- **Gold** 💰
- *(Espandibile)*

---

## ⚔️ Sistema Nemici

### File Principali
| File | Descrizione |
|------|-------------|
| `EnemyData.cs` | ScriptableObject definizione nemico |
| `WaveData.cs` | Definizione ondata di nemici |

---

## 🗺️ Sistema Mappa/Mondo

### File Principali
| File | Descrizione |
|------|-------------|
| `MapZone.cs` | Definisce zone sulla mappa |
| `MapMarker.cs` | Marcatori visivi |
| `MapGenerator_StampBased.cs` | Generazione mondo con stamp |
| `BiomeDefinition.cs` | Definizione biomi |
| `MapStamp.cs` | Stamp prefabbricati per generazione |

---

## 🎨 Sistema UI

### Componenti
- **HUD**: Risorse, tempo, stato
- **Build Menu**: Selezione strutture
- **Worker Assignment**: Pannello assegnazione lavoratori
- **Structure Status UI**: Progress bar costruzione

### Asset
- Usa **Modular Game UI Kit** per stile coerente
- Font personalizzati
- Sprite e pannelli modulari

---

## 🔧 Editor Tools

| Tool | Descrizione |
|------|-------------|
| `GameReadySetupTool.cs` | Setup iniziale del gioco |
| `EnvironmentSetupTool.cs` | Configurazione ambiente |
| `WorldGeneratorTool.cs` | Generazione mondo procedurale |
| `MapArchitectTool.cs` | Design mappe |
| `StampCreatorTool.cs` | Creazione stamp per generazione |
| `ZonePopulatorTool.cs` | Popolamento zone |
| `CharacterIntegratorTool.cs` | Integrazione personaggi |
| `WorkerPrefabGenerator.cs` | Generazione prefab worker |
| `SceneSystemClonerTool.cs` | Clonazione sistemi tra scene |
| `SceneReferenceFixerTool.cs` | Fix riferimenti mancanti |

---

## 📦 Asset Esterni

| Asset | Uso |
|-------|-----|
| **Synty Polygon Fantasy Kingdom** | Strutture 3D, ambiente |
| **ToonyTinyPeople** | Personaggi worker |
| **Modular Game UI Kit** | Interfaccia utente |
| **Polygon Particle FX** | Effetti particellari |
| **Simple Sky** | Skybox |
| **Odin Inspector** | Inspector avanzato |
| **Behaviour Tree** | AI behaviour trees |

---

## 🔄 Loop di Gioco Principale

```
Update()
├── WorkerSystem.Update()
│   ├── TickConstruction() per strutture Building
│   ├── TickProduction() per strutture Operating
│   ├── ManualUpdate() per ogni WorkerController
│   └── CheckAutoAssignments() ogni N secondi
│
├── EnemySystem.Update() (futuro)
│   └── Spawn ondate, AI nemici
│
└── UISystem.Update()
    └── Aggiornamento HUD, pannelli
```

---

## 🎯 Stato Attuale

### ✅ Funzionante
- Spawn worker con NavMesh
- Build mode con ghost preview
- Costruzione strutture con progress bar
- Auto-assegnazione worker a strutture
- Sistema risorse base
- Visual swap worker (con cleanup automatico)
- Animazioni worker (movement, idle, work)

### 🚧 In Sviluppo
- Sistema nemici/combattimento
- Tech tree
- Più tipi di strutture

### 📋 TODO Futuri
- Salvataggio/Caricamento
- Multiplayer?
- Più biomi e generazione mondo
- Sistema giorno/notte

---

## 📝 Note Tecniche

- **Unity Version**: 2022.3+ (LTS)
- **Render Pipeline**: URP
- **Input**: New Input System
- **Navigation**: NavMesh
- **Inspector**: Odin Inspector (Sirenix)
- **Architecture**: Singleton pattern per sistemi principali

---

*Ultimo aggiornamento: 8 Dicembre 2024*
