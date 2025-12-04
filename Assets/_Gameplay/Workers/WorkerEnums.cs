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
        Combat,
        Dead
    }
}
