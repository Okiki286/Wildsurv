using UnityEngine;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.Core.Profiling
{
    /// <summary>
    /// Quick test script to verify pooling system is working.
    /// Attach to any GameObject and call methods via Inspector buttons.
    /// </summary>
    public class PoolingTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private int spawnCount = 10;
        [SerializeField] private float destroyDelay = 2f;

        private System.Collections.Generic.List<WorkerInstance> testWorkers = new System.Collections.Generic.List<WorkerInstance>();

        [ContextMenu("Test Spawn Workers")]
        public void TestSpawnWorkers()
        {
            if (WorkerSystem.Instance == null)
            {
                Debug.LogError("[PoolingTest] WorkerSystem.Instance is null! Make sure it's in the scene.");
                return;
            }

            var availableTypes = typeof(WorkerSystem)
                .GetField("availableWorkerTypes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(WorkerSystem.Instance) as System.Collections.Generic.List<WorkerData>;

            if (availableTypes == null || availableTypes.Count == 0)
            {
                Debug.LogError("[PoolingTest] No worker types configured!");
                return;
            }

            Debug.Log($"<color=cyan>[PoolingTest]</color> Spawning {spawnCount} workers...");
            for (int i = 0; i < spawnCount; i++)
            {
                var workerData = availableTypes[i % availableTypes.Count];
                var worker = WorkerSystem.Instance.CreateWorkerInstance(workerData);
                if (worker != null)
                {
                    testWorkers.Add(worker);
                }
            }

            Debug.Log($"<color=green>[PoolingTest]</color> Spawned {testWorkers.Count} workers. Check GCMonitor for allocation rate.");
        }

        [ContextMenu("Test Destroy Workers")]
        public void TestDestroyWorkers()
        {
            if (WorkerSystem.Instance == null)
            {
                Debug.LogError("[PoolingTest] WorkerSystem.Instance is null!");
                return;
            }

            Debug.Log($"<color=cyan>[PoolingTest]</color> Destroying {testWorkers.Count} workers...");
            foreach (var worker in testWorkers)
            {
                if (worker != null)
                {
                    WorkerSystem.Instance.DestroyWorkerInstance(worker);
                }
            }

            testWorkers.Clear();
            Debug.Log($"<color=green>[PoolingTest]</color> Workers destroyed and returned to pool. Check GCMonitor - should be <50KB/sec!");
        }

        [ContextMenu("Test Spawn-Destroy Cycle")]
        public void TestSpawnDestroyCycle()
        {
            TestSpawnWorkers();
            Invoke(nameof(TestDestroyWorkers), destroyDelay);
        }

        private void OnDestroy()
        {
            // Cleanup any remaining test workers
            if (WorkerSystem.Instance != null)
            {
                foreach (var worker in testWorkers)
                {
                    if (worker != null)
                    {
                        WorkerSystem.Instance.DestroyWorkerInstance(worker);
                    }
                }
            }
            testWorkers.Clear();
        }
    }
}
