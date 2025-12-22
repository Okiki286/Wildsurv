# Feature Gap Analysis: Wilderness Survival project

This analysis verifies the implementation status of the four core pillars of the project based on the latest handoff.

## 📊 Summary of Implementation

| Pillar | Focus Area | Status | Key Findings |
| :--- | :--- | :--- | :--- |
| **1** | Economy & Population | ✅ Complete | Food-based recruitment and housing gating are fully operational. Upkeep is correctly disabled for MVP. |
| **2** | Combat & Waves | ✅ Complete | Object pooling, wave configurations, and combat telemetry are all implemented and integrated. |
| **3** | Visual Feedback & UX | ⚠️ Partial | World popups and HUD pulses are functional, but "Icon Outline" and reward timing polish are missing. |
| **4** | Core Loop Completion | ⚠️ Partial | The loop (Collect → Recruit → Build → Survive) is functional, but automated Win/Loss transitions and UI are missing. |

---

## 🏗️ Pillar 1: Economy & Population Architecture

- **Food-based Recruitment**: [PopulationSystem.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Core/Systems/PopulationSystem.cs) correctly handles food costs through the `RequestRecruit` method.
- **Housing Gating**: The system calculates capacity from operational [ShelterHome.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Gameplay/Structures/Housing/ShelterHome.cs) components. Recruitment is blocked if no beds are available.
- **MVP Compliance**: Upkeep logic in `PopulationSystem.ConsumeFoodUpkeep` is present but gated by `EconomyRules.FoodModel`, which defaults to `RecruitOnly`.

---

## ⚔️ Pillar 2: Combat & Waves Configuration

- **Object Pooling**: [EnemySpawner.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Gameplay/Enemies/EnemySpawner.cs) uses `EnemyPooler` for all enemy instantiations, significantly improving performance for waves.
- **Wave Config**: [WaveManager.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Gameplay/Enemies/WaveManager.cs) uses an array of `WaveData`, allowing unique configurations for Night 1, 2, and 3.
- **Telemetry**: [CombatTelemetry.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Gameplay/Combat/CombatTelemetry.cs) is fully integrated, logging kills, damage dealt, and shard gains.

---

## ✨ Pillar 3: Visual Feedback & UX

- **Popups & Pulses**: [EconomyFeedbackSystem.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Core/Systems/EconomyFeedbackSystem.cs) manages floating world popups with stacking logic. [ShardHUDPulse.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_UI/Scripts/RewardFeel/ShardHUDPulse.cs) handles the icon pulse.
- **Missing Features**:
    - No specific "Icon Outline" logic exists for rewards.
    - Polish on reward timing (Next Priority #3) is not yet implemented.

---

## 🔁 Pillar 4: Core Loop Completion

- **Functional Loop**: The transition between Day (Recruitment/Building) and Night (Defense) is handled via [DayNightSystem.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Core/Systems/DayNightSystem.cs).
- **Gaps**:
    - **Win/Loss Condition**: While [WaystoneBeaconController.cs](file:///c:/Users/riku2/Desktop/Wild/Wilderness%20-%20Copy%20-%20Copy/Assets/_Gameplay/Core/WaystoneBeaconController.cs) detects destruction, it does not trigger the `GameManager.TriggerGameOver` method or show a UI screen.
    - **Victory**: There is currently no logic for a "Victory" state after a set number of nights.

---

## 🚀 Recommendation & Next Steps

1.  **High Priority**: Link `WaystoneBeaconController.OnDestroyed` to `GameManager.TriggerGameOver` to complete the loss loop.
2.  **Polish**: Implement the "Icon Outline" and refine reward timing in the `RewardFeelSystem`.
3.  **UI**: Create Game Over and Victory screens to provide player closure.
