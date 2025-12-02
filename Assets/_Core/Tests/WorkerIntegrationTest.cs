using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Tests
{
    /// <summary>
    /// Test di integrazione per validare il sistema Worker Mobile:
    /// - NavMesh pathfinding
    /// - WorkerInstance assignment
    /// - Production calculation
    /// </summary>
    public class WorkerIntegrationTest : MonoBehaviour
    {
        // ============================================
        // SETUP DATA
        // ============================================

        [TitleGroup("Test Configuration")]
        [BoxGroup("Test Configuration/Profiles")]
        [Required, AssetsOnly]
        [LabelText("Worker Profile")]
        [Tooltip("Il profilo del worker da testare (ScriptableObject)")]
        public WorkerData testWorkerProfile;

        [BoxGroup("Test Configuration/Profiles")]
        [Required, AssetsOnly]
        [LabelText("Structure Profile")]
        [Tooltip("Il profilo della struttura da testare (ScriptableObject)")]
        public StructureData testStructureProfile;

        [BoxGroup("Test Configuration/Scene")]
        [SceneObjectsOnly]
        [LabelText("Target Structure (Optional)")]
        [InfoBox("Se vuoto, verrà creata una struttura temporanea", InfoMessageType.None)]
        [Tooltip("Riferimento a struttura già in scena. Se null, ne viene creata una nuova.")]
        public StructureController targetStructure;

        [BoxGroup("Test Configuration/Scene")]
        [LabelText("Destroy On Complete")]
        [Tooltip("Se true, distrugge gli oggetti di test alla fine della sequenza. Se false, lascia gli oggetti nella scena per ispezione.")]
        [SerializeField]
        private bool destroyOnComplete = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime State")]
        [BoxGroup("Runtime State/Worker")]
        [ShowInInspector, ReadOnly]
        [LabelText("Worker Instance")]
        private WorkerInstance spawnedWorker;

        [BoxGroup("Runtime State/Worker")]
        [ShowInInspector, ReadOnly]
        [LabelText("Worker State")]
        private WorkerState workerState => spawnedWorker?.CurrentState ?? WorkerState.Idle;

        [BoxGroup("Runtime State/Worker")]
        [ShowInInspector, ReadOnly]
        [LabelText("Distance to Target")]
        [ProgressBar(0, 50, ColorGetter = "GetDistanceBarColor")]
        private float distanceToTarget
        {
            get
            {
                if (spawnedWorker?.PhysicalWorker == null) return 0f;
                var agent = spawnedWorker.PhysicalWorker.GetComponent<UnityEngine.AI.NavMeshAgent>();
                return agent != null && agent.hasPath ? agent.remainingDistance : 0f;
            }
        }

        [BoxGroup("Runtime State/Structure")]
        [ShowInInspector, ReadOnly]
        [LabelText("Active Structure")]
        private StructureController activeStructure;

        [BoxGroup("Runtime State/Structure")]
        [ShowInInspector, ReadOnly]
        [LabelText("Production Rate")]
        [SuffixLabel("per min", Overlay = true)]
        private float productionRate
        {
            get
            {
                // Access private field via reflection since there's no public property
                if (activeStructure == null) return 0f;
                var field = typeof(StructureController).GetField("currentProductionRate", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return field != null ? (float)field.GetValue(activeStructure) : 0f;
            }
        }

        [BoxGroup("Runtime State/Structure")]
        [ShowInInspector, ReadOnly]
        [LabelText("Worker Bonus")]
        [SuffixLabel("%", Overlay = true)]
        private float workerBonus
        {
            get
            {
                if (activeStructure == null) return 0f;
                var prodRate = productionRate;
                if (prodRate == 0f) return 0f;
                var baseRate = activeStructure.Data.BaseProductionRate;
                return baseRate > 0 ? (prodRate / baseRate - 1f) * 100f : 0f;
            }
        }

        // ============================================
        // TEST ACTIONS
        // ============================================

        [TitleGroup("Test Actions")]
        [BoxGroup("Test Actions/Step by Step")]
        [Button("1️⃣ Spawn & Initialize", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.8f)]
        private void Test_SpawnAndInitialize()
        {
            if (!ValidateSetup()) return;

            // Setup Structure
            if (targetStructure == null)
            {
                GameObject structObj = new GameObject($"TestStructure_{testStructureProfile.DisplayName}");
                structObj.transform.position = new Vector3(10, 0, 10);
                activeStructure = structObj.AddComponent<StructureController>();
                activeStructure.Initialize(testStructureProfile);
                Debug.Log($"<color=cyan>[Test]</color> Created temporary structure at {structObj.transform.position}");
            }
            else
            {
                activeStructure = targetStructure;
                Debug.Log($"<color=cyan>[Test]</color> Using existing structure: {activeStructure.name}");
            }

            // Spawn Worker
            if (WorkerSystem.Instance == null)
            {
                Debug.LogError("<color=red>[Test]</color> WorkerSystem.Instance is NULL! Add WorkerSystem to scene.");
                return;
            }

            spawnedWorker = WorkerSystem.Instance.CreateWorkerInstance(testWorkerProfile);
            
            if (spawnedWorker != null)
            {
                Debug.Log($"<color=green>✅ [Test]</color> Environment Setup Complete!\n" +
                    $"Worker: {spawnedWorker.Data.DisplayName}\n" +
                    $"Structure: {activeStructure.Data.DisplayName}\n" +
                    $"Position: {activeStructure.transform.position}");
            }
            else
            {
                Debug.LogError("<color=red>[Test]</color> Failed to spawn worker!");
            }
        }

        [BoxGroup("Test Actions/Step by Step")]
        [Button("2️⃣ Assign & Move", ButtonSizes.Large)]
        [GUIColor(1f, 0.8f, 0.4f)]
        private void Test_AssignAndMove()
        {
            if (spawnedWorker == null || activeStructure == null)
            {
                Debug.LogError("<color=red>[Test]</color> Run Step 1 first!");
                return;
            }

            if (WorkerSystem.Instance == null)
            {
                Debug.LogError("<color=red>[Test]</color> WorkerSystem.Instance is NULL!");
                return;
            }

            bool success = WorkerSystem.Instance.AssignWorker(spawnedWorker, activeStructure);

            if (success)
            {
                Vector3 targetPos = activeStructure.transform.position;
                Debug.Log($"<color=yellow>➡️ [Test]</color> Worker Assigned. Moving to {targetPos}...\n" +
                    $"Current Distance: {distanceToTarget:F2}m");
            }
            else
            {
                Debug.LogError("<color=red>[Test]</color> Failed to assign worker!");
            }
        }

        [BoxGroup("Test Actions/Step by Step")]
        [Button("3️⃣ Force Production Tick", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.4f, 1f)]
        private void Test_ForceProductionTick()
        {
            if (activeStructure == null)
            {
                Debug.LogError("<color=red>[Test]</color> No active structure!");
                return;
            }

            float deltaTime = 60f; // Simulate 1 minute
            float productionBefore = productionRate;
            
            // Force production tick
            activeStructure.TickProduction(deltaTime);

            // Get assigned workers count via reflection
            var field = typeof(StructureController).GetField("assignedWorkerInstances",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int assignedCount = 0;
            if (field != null)
            {
                var list = field.GetValue(activeStructure) as System.Collections.IList;
                assignedCount = list != null ? list.Count : 0;
            }

            Debug.Log($"<color=magenta>⚡ [Test]</color> Production Tick Forced (60s simulation)\n" +
                $"Production Rate: {productionRate:F2}/min\n" +
                $"Worker Bonus: +{workerBonus:F1}%\n" +
                $"Assigned Workers: {assignedCount}");
        }

        [TitleGroup("Full Test")]
        [BoxGroup("Full Test/Automated")]
        [Button("▶️ RUN FULL TEST SEQUENCE", ButtonSizes.Gigantic)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void Test_RunFullSequence()
        {
            if (!ValidateSetup()) return;

            StopAllCoroutines();
            StartCoroutine(FullTestSequenceCoroutine());
        }

        // ============================================
        // COROUTINE SEQUENCE
        // ============================================

        private IEnumerator FullTestSequenceCoroutine()
        {
            Debug.Log("<color=cyan>╔════════════════════════════════════╗</color>");
            Debug.Log("<color=cyan>║   FULL TEST SEQUENCE STARTED      ║</color>");
            Debug.Log("<color=cyan>╚════════════════════════════════════╝</color>");

            // Step 1: Spawn & Initialize
            Debug.Log("\n<color=yellow>═══ STEP 1: SPAWN & INITIALIZE ═══</color>");
            Test_SpawnAndInitialize();
            
            // Visual delay after spawn
            yield return new WaitForSeconds(1.0f);

            if (spawnedWorker == null || activeStructure == null)
            {
                Debug.LogError("<color=red>[Test]</color> Sequence aborted: Setup failed!");
                yield break;
            }

            // Step 2: Assign & Move
            Debug.Log("\n<color=yellow>═══ STEP 2: ASSIGN & MOVE ═══</color>");
            Test_AssignAndMove();
            
            // Give physics engine time to process
            yield return null;
            yield return null;

            // Get NavMeshAgent reference
            var agent = spawnedWorker?.PhysicalWorker?.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError("<color=red>[Test]</color> NavMeshAgent not found on worker!");
                yield break;
            }

            // Wait for path calculation
            Debug.Log("<color=cyan>[Test]</color> Waiting for NavMesh path calculation...");
            float pathTimeout = 5f;
            float pathElapsed = 0f;
            while (agent.pathPending && pathElapsed < pathTimeout)
            {
                yield return null;
                pathElapsed += Time.deltaTime;
            }

            if (agent.pathPending)
            {
                Debug.LogWarning("<color=orange>[Test]</color> Path calculation timeout!");
            }
            else
            {
                Debug.Log($"<color=green>[Test]</color> Path calculated in {pathElapsed:F2}s. Distance: {agent.remainingDistance:F2}m");
            }

            // Wait for worker to arrive
            Debug.Log("<color=cyan>[Test]</color> Waiting for worker to reach destination...");
            float timeout = 30f;
            float elapsed = 0f;

            while (agent.remainingDistance > agent.stoppingDistance && elapsed < timeout)
            {
                if (spawnedWorker?.PhysicalWorker == null) break;
                
                Debug.Log($"<color=gray>[Test]</color> Distance remaining: {agent.remainingDistance:F2}m");
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (elapsed >= timeout)
            {
                Debug.LogWarning("<color=orange>[Test]</color> Timeout waiting for worker arrival. Continuing anyway...");
            }
            else
            {
                Debug.Log($"<color=green>✅ [Test]</color> Worker arrived! (took {elapsed:F1}s)");
            }

            // Visual delay before production tick
            yield return new WaitForSeconds(1.0f);

            // Step 3: Production Tick
            Debug.Log("\n<color=yellow>═══ STEP 3: PRODUCTION TICK ═══</color>");
            Test_ForceProductionTick();
            yield return new WaitForSeconds(1f);

            // Final Report
            Debug.Log("\n<color=green>╔════════════════════════════════════╗</color>");
            Debug.Log("<color=green>║   TEST SEQUENCE COMPLETED ✅       ║</color>");
            Debug.Log("<color=green>╚════════════════════════════════════╝</color>");
            Debug.Log($"<color=cyan>[Final Report]</color>\n" +
                $"Worker State: {workerState}\n" +
                $"Production Rate: {productionRate:F2}/min\n" +
                $"Worker Bonus: +{workerBonus:F1}%\n" +
                $"Structure: {activeStructure.Data.DisplayName}");

            // Cleanup based on setting
            if (destroyOnComplete)
            {
                Debug.Log("\n<color=yellow>[Test]</color> Auto-cleanup enabled, destroying test objects...");
                yield return new WaitForSeconds(2f);
                CleanupTestObjects();
            }
            else
            {
                Debug.Log("\n<color=cyan>[Test]</color> Objects left in scene for inspection (destroyOnComplete = false)");
            }
        }

        // ============================================
        // UTILITIES
        // ============================================

        [BoxGroup("Full Test/Automated")]
        [Button("🗑️ Cleanup Test Objects", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void CleanupTestObjects()
        {
            if (destroyOnComplete)
            {
                if (spawnedWorker?.PhysicalWorker != null)
                {
                    Destroy(spawnedWorker.PhysicalWorker.gameObject);
                    Debug.Log("<color=yellow>[Test]</color> Destroyed spawned worker");
                }

                if (activeStructure != null && activeStructure != targetStructure)
                {
                    Destroy(activeStructure.gameObject);
                    Debug.Log("<color=yellow>[Test]</color> Destroyed temporary structure");
                }

                spawnedWorker = null;
                activeStructure = null;

                Debug.Log("<color=green>[Test]</color> Cleanup complete!");
            }
            else
            {
                Debug.Log("<color=cyan>[Test]</color> Objects left in scene for inspection (destroyOnComplete = false)");
            }
        }

        private bool ValidateSetup()
        {
            if (testWorkerProfile == null)
            {
                Debug.LogError("<color=red>[Test]</color> Test Worker Profile is NULL! Assign in Inspector.");
                return false;
            }

            if (testStructureProfile == null)
            {
                Debug.LogError("<color=red>[Test]</color> Test Structure Profile is NULL! Assign in Inspector.");
                return false;
            }

            if (WorkerSystem.Instance == null)
            {
                Debug.LogError("<color=red>[Test]</color> WorkerSystem.Instance is NULL! Add WorkerSystem GameObject to scene.");
                return false;
            }

            return true;
        }

        private Color GetDistanceBarColor()
        {
            float dist = distanceToTarget;
            if (dist < 1f) return Color.green;
            if (dist < 10f) return Color.yellow;
            return Color.red;
        }

        // ============================================
        // GIZMOS
        // ============================================

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (spawnedWorker?.PhysicalWorker != null && activeStructure != null)
            {
                // Draw line from worker to structure
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(
                    spawnedWorker.PhysicalWorker.transform.position,
                    activeStructure.transform.position
                );

                // Draw sphere at structure
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(activeStructure.transform.position, 1f);
            }
        }
#endif
    }
}
