namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Stati possibili per un worker.
    /// </summary>
    public enum WorkerState
    {
        Idle,
        Moving,
        MovingToWork,
        Working,
        Resting,
        Retreating,
        Sheltered,  // Worker is inside a shelter (night retreat) - non-targetable
        Combat,
        Dead
    }
}
