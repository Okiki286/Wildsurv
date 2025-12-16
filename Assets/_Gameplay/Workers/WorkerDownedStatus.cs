using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Gestisce lo stato "Downed" (a terra) e "Injured" (debuff) dei worker.
    /// Attaccare al ROOT del prefab worker (stesso GO di WorkerController).
    ///
    /// Meccanica:
    /// - Quando HP <= 0 → Down() → worker va a terra (non morto)
    /// - Al mattino (OnDayStarted) → ReviveAtDawn() → si rialza con debuff Injured
    /// - Debuff dura 1-2 giorni e riduce WorkSpeed/MoveSpeed
    /// - Se viene downa di nuovo mentre Injured → durata = max(attuale, nuova)
    /// </summary>
    public class WorkerDownedStatus : MonoBehaviour
    {
        // ============================================
        // INJURY PRESETS
        // ============================================

        [System.Serializable]
        public struct InjuryPreset
        {
            [Range(0.5f, 1f)]
            public float workSpeedMultiplier;
            [Range(0.5f, 1f)]
            public float moveSpeedMultiplier;
            public int durationDays;

            public InjuryPreset(float work, float move, int days)
            {
                workSpeedMultiplier = work;
                moveSpeedMultiplier = move;
                durationDays = days;
            }
        }

        [TitleGroup("Injury Presets")]
        [InfoBox("Early Game = Night 1-3, Mid/Late = Night 4+")]
        [SerializeField]
        private InjuryPreset earlyGameInjury = new InjuryPreset(0.75f, 0.90f, 1);

        [SerializeField]
        private InjuryPreset midLateGameInjury = new InjuryPreset(0.65f, 0.85f, 2);

        [TitleGroup("Settings")]
        [Tooltip("Night number threshold: <= this uses early preset, > uses mid/late")]
        [SerializeField]
        private int earlyGameNightThreshold = 3;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector, ReadOnly]
        private bool isDowned = false;

        [ShowInInspector, ReadOnly]
        private bool isInjured = false;

        [ShowInInspector, ReadOnly]
        private int injuredDaysRemaining = 0;

        [ShowInInspector, ReadOnly]
        private float workSpeedMultiplier = 1f;

        [ShowInInspector, ReadOnly]
        private float moveSpeedMultiplier = 1f;

        // Flag per evitare che l'injury applicata al revive venga decrementata nello stesso DayStarted
        private bool injuryAppliedToday = false;

        // ============================================
        // REFERENCES (cached)
        // ============================================

        private WorkerController workerController;
        private WorkerInstance workerInstance;
        private NavMeshAgent navAgent;

        // ============================================
        // PROPERTIES
        // ============================================

        public bool IsDowned => isDowned;
        public bool IsInjured => isInjured;
        public int InjuredDaysRemaining => injuredDaysRemaining;

        /// <summary>
        /// True se il worker può ricevere ordini (assign, move, build).
        /// False quando è downed - AUTHORITATIVE GATE per tutti i sistemi.
        /// </summary>
        public bool CanReceiveOrders => !isDowned;

        /// <summary>
        /// True se il worker può lavorare (produce output).
        /// False quando downed. Injured può lavorare ma con malus.
        /// </summary>
        public bool CanWork => !isDowned;

        /// <summary>
        /// Moltiplicatore velocità lavoro (1.0 = normale, 0.75 = 75%)
        /// Usato da sistemi esterni per scalare la produttività.
        /// </summary>
        public float WorkSpeedMult => workSpeedMultiplier;

        /// <summary>
        /// Moltiplicatore velocità movimento (1.0 = normale, 0.90 = 90%)
        /// Applicato automaticamente al NavMeshAgent.
        /// </summary>
        public float MoveSpeedMult => moveSpeedMultiplier;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Cache references
            workerController = GetComponent<WorkerController>();
            navAgent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            // Get WorkerInstance from WorkerController
            if (workerController != null)
            {
                // Il WorkerInstance è linkato dopo Awake, quindi lo prendiamo in Start
                StartCoroutine(CacheWorkerInstanceDelayed());
            }
        }

        private System.Collections.IEnumerator CacheWorkerInstanceDelayed()
        {
            // Aspetta un frame per permettere il linking
            yield return null;

            // Usa reflection o trova altro modo per accedere a linkedInstance
            // Per ora usiamo il fatto che WorkerController.Data esiste
            // e possiamo trovare l'instance dal WorkerSystem
            if (WorkerSystem.Instance != null)
            {
                foreach (var instance in WorkerSystem.Instance.GetAvailableWorkers())
                {
                    if (instance.PhysicalWorker == workerController)
                    {
                        workerInstance = instance;
                        break;
                    }
                }

                if (workerInstance == null)
                {
                    foreach (var instance in WorkerSystem.Instance.GetAssignedWorkers())
                    {
                        if (instance.PhysicalWorker == workerController)
                        {
                            workerInstance = instance;
                            break;
                        }
                    }
                }
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Mette il worker a terra (downed). Chiamare invece di "morte" quando HP <= 0.
        /// </summary>
        public void Down()
        {
            if (isDowned) return; // Già a terra

            isDowned = true;

            // Ferma movimento
            StopWorkerMovement();

            // Disabilita lavoro/assegnazioni
            DisableWorkerFunctions();

            // Nome per log
            string workerName = GetWorkerName();
            Debug.Log($"<color=red>[WorkerDowned]</color> {workerName} DOWNED");
        }

        /// <summary>
        /// Rialza il worker all'alba. Chiamato da WorkerStatusSystem su OnDayStarted.
        /// </summary>
        public void ReviveAtDawn()
        {
            if (!isDowned) return;

            isDowned = false;

            // Determina il preset di injury basato sulla notte corrente
            int currentNight = GetCurrentNightNumber();
            InjuryPreset preset = currentNight <= earlyGameNightThreshold ? earlyGameInjury : midLateGameInjury;

            // Applica injury (o aggiorna se già injured)
            ApplyInjuryInternal(preset);

            // Setta flag per evitare decremento nello stesso DayStarted
            injuryAppliedToday = true;

            // Riabilita movimento
            EnableWorkerMovement();

            // Ripristina stato a Idle
            EnableWorkerFunctions();

            string workerName = GetWorkerName();
            Debug.Log($"<color=green>[WorkerDowned]</color> {workerName} REVIVED at dawn");
        }

        /// <summary>
        /// Applica un preset di injury. Se già injured, usa il max tra durata attuale e nuova.
        /// </summary>
        public void ApplyInjuryPreset(InjuryPreset preset)
        {
            ApplyInjuryInternal(preset);
        }

        /// <summary>
        /// Tick giornaliero: riduce giorni injury e rimuove debuff quando scade.
        /// Chiamato da WorkerStatusSystem su OnDayStarted.
        /// </summary>
        public void TickDay()
        {
            // Guard: se l'injury è stata applicata oggi (stesso DayStarted), skip decremento
            if (injuryAppliedToday)
            {
                injuryAppliedToday = false;
                return;
            }

            if (!isInjured) return;

            injuredDaysRemaining--;

            string workerName = GetWorkerName();

            if (injuredDaysRemaining <= 0)
            {
                // Debuff scaduto
                ClearInjury();
                Debug.Log($"<color=cyan>[WorkerDowned]</color> {workerName} INJURED expired - fully recovered");
            }
            else
            {
                Debug.Log($"<color=yellow>[WorkerDowned]</color> {workerName} INJURED days remaining: {injuredDaysRemaining}");
            }
        }

        /// <summary>
        /// Rimuove completamente lo stato injured.
        /// </summary>
        public void ClearInjury()
        {
            isInjured = false;
            injuredDaysRemaining = 0;
            workSpeedMultiplier = 1f;
            moveSpeedMultiplier = 1f;
            injuryAppliedToday = false;

            // Ripristina velocità NavMesh
            UpdateNavMeshSpeed();
        }

        /// <summary>
        /// Forza il clear di tutti gli stati (debug/cheat).
        /// </summary>
        public void ForceFullRecovery()
        {
            isDowned = false;
            ClearInjury();
            EnableWorkerMovement();
            EnableWorkerFunctions();

            string workerName = GetWorkerName();
            Debug.Log($"<color=lime>[WorkerDowned]</color> {workerName} FORCE RECOVERED");
        }

        // ============================================
        // INTERNAL HELPERS
        // ============================================

        private void ApplyInjuryInternal(InjuryPreset preset)
        {
            string workerName = GetWorkerName();

            if (isInjured)
            {
                // Già injured: usa max durata, e aggiorna multiplier se più severi
                int newDuration = Mathf.Max(injuredDaysRemaining, preset.durationDays);

                // Prendi i multiplier più severi (più bassi)
                float newWorkMult = Mathf.Min(workSpeedMultiplier, preset.workSpeedMultiplier);
                float newMoveMult = Mathf.Min(moveSpeedMultiplier, preset.moveSpeedMultiplier);

                injuredDaysRemaining = newDuration;
                workSpeedMultiplier = newWorkMult;
                moveSpeedMultiplier = newMoveMult;

                Debug.Log($"<color=orange>[WorkerDowned]</color> {workerName} INJURED updated: " +
                    $"days={injuredDaysRemaining}, work={workSpeedMultiplier:P0}, move={moveSpeedMultiplier:P0}");
            }
            else
            {
                // Prima injury
                isInjured = true;
                injuredDaysRemaining = preset.durationDays;
                workSpeedMultiplier = preset.workSpeedMultiplier;
                moveSpeedMultiplier = preset.moveSpeedMultiplier;

                Debug.Log($"<color=orange>[WorkerDowned]</color> {workerName} INJURED applied: " +
                    $"days={injuredDaysRemaining}, work={workSpeedMultiplier:P0}, move={moveSpeedMultiplier:P0}");
            }

            // Applica speed modifier al NavMesh
            UpdateNavMeshSpeed();
        }

        private void StopWorkerMovement()
        {
            if (workerController != null)
            {
                workerController.ForceIdle();
            }

            // Freeze completo del NavMeshAgent
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
                navAgent.velocity = Vector3.zero;
            }
        }

        private void EnableWorkerMovement()
        {
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
            }

            if (workerController != null)
            {
                workerController.Unlock();
            }
        }

        private void DisableWorkerFunctions()
        {
            // Unassign dal structure se assegnato
            if (workerInstance != null && workerInstance.IsAssigned)
            {
                WorkerSystem.Instance?.UnassignWorker(workerInstance);
            }

            // Imposta stato (non Dead, ma Downed - usiamo Dead temporaneamente)
            // In futuro potremmo aggiungere WorkerState.Downed
            if (workerInstance != null)
            {
                // Non impostiamo Dead, lasciamo che il sistema capisca da IsDowned
            }
        }

        private void EnableWorkerFunctions()
        {
            if (workerInstance != null)
            {
                workerInstance.SetState(WorkerState.Idle);
            }
        }

        private void UpdateNavMeshSpeed()
        {
            if (navAgent == null) return;

            // Ottieni la velocità base dal WorkerData
            float baseSpeed = 3.5f;
            if (workerInstance?.Data != null)
            {
                baseSpeed = workerInstance.Data.MovementSpeed;
            }
            else if (workerController?.Data != null)
            {
                baseSpeed = workerController.Data.MovementSpeed;
            }

            // Applica multiplier
            navAgent.speed = baseSpeed * moveSpeedMultiplier;
        }

        private int GetCurrentNightNumber()
        {
            if (WildernessSurvival.Core.Systems.DayNightSystem.Instance != null)
            {
                return WildernessSurvival.Core.Systems.DayNightSystem.Instance.CurrentDayNumber;
            }
            return 1;
        }

        private string GetWorkerName()
        {
            if (workerInstance != null)
            {
                return workerInstance.CustomName;
            }
            if (workerController?.Data != null)
            {
                return workerController.Data.DisplayName;
            }
            return gameObject.name;
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Force Down", ButtonSizes.Medium)]
        [GUIColor(1f, 0.4f, 0.4f)]
        private void DebugForceDown()
        {
            if (Application.isPlaying) Down();
        }

        [Button("Force Revive", ButtonSizes.Medium)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void DebugForceRevive()
        {
            if (Application.isPlaying) ReviveAtDawn();
        }

        [Button("Force Full Recovery", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void DebugForceFullRecovery()
        {
            if (Application.isPlaying) ForceFullRecovery();
        }

        [Button("Tick Day (simulate)", ButtonSizes.Medium)]
        private void DebugTickDay()
        {
            if (Application.isPlaying) TickDay();
        }

        [Button("Log Status", ButtonSizes.Medium)]
        private void DebugLogStatus()
        {
            Debug.Log($"[WorkerDownedStatus] {GetWorkerName()}\n" +
                $"  IsDowned: {isDowned}\n" +
                $"  IsInjured: {isInjured}\n" +
                $"  InjuredDaysRemaining: {injuredDaysRemaining}\n" +
                $"  WorkSpeedMult: {workSpeedMultiplier:P0}\n" +
                $"  MoveSpeedMult: {moveSpeedMultiplier:P0}");
        }
#endif
    }
}
