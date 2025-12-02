using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures;
using System.Linq;
using System.Collections.Generic;

public class WorkerDebugSpy : MonoBehaviour
{
    [Button("🕵️ Spy Status (Fixed)", ButtonSizes.Large)]
    [GUIColor(0.2f, 1f, 0.2f)]
    private void Spy()
    {
        var sys = WorkerSystem.Instance;
        if (sys == null)
        {
            Debug.LogError("❌ WorkerSystem not found!");
            return;
        }

        // 1. Recuperiamo TUTTE le istanze logiche (Disponibili + Assegnate)
        var available = sys.GetAvailableWorkers();
        var assigned = sys.GetAssignedWorkers();
        var allWorkers = new List<WorkerInstance>();
        allWorkers.AddRange(available);
        allWorkers.AddRange(assigned);

        // Filtriamo solo i Builder
        var builders = allWorkers
            .Where(w => w.Data.DefaultRole == WorkerRole.Builder)
            .ToList();

        // 2. Troviamo le Farm fisiche
        var farms = FindObjectsByType<StructureController>(FindObjectsSortMode.None)
            .Where(s => s.Data != null && s.Data.DisplayName.Contains("Farm")) // O controlla il nome del file
            .ToList();

        Debug.Log($"<color=yellow>--- 🕵️ SPY REPORT ---</color>");

        // REPORT WORKER
        Debug.Log($"<b>Builders Logici Trovati: {builders.Count}</b>");
        foreach (var b in builders)
        {
            string status = b.AssignedStructure != null
                ? $"<color=green>WORKING @ {b.AssignedStructure.Data.DisplayName}</color>"
                : "<color=red>IDLE (Waiting)</color>";

            Debug.Log($" > <b>{b.CustomName}</b>: {status}");
        }

        // REPORT STRUTTURE
        Debug.Log($"<b>Farms Fisiche in Scena: {farms.Count}</b>");
        foreach (var f in farms)
        {
            bool isBuilding = f.State == StructureState.Building;
            bool hasSlots = f.HasFreeWorkerSlot();
            bool needsBuilder = f.Data.RequiresBuilder;

            string stateColor = isBuilding ? "green" : "white";
            string slotColor = hasSlots ? "green" : "red";

            Debug.Log($" > <b>{f.name}</b>:\n" +
                      $"   State: <color={stateColor}>{f.State}</color>\n" +
                      $"   HasSlots: <color={slotColor}>{hasSlots}</color> ({f.AssignedWorkerInstanceCount}/{f.Data.WorkerSlots})\n" +
                      $"   NeedsBuilder: {needsBuilder}");
        }

        Debug.Log($"<color=yellow>--- END REPORT ---</color>");
    }
}