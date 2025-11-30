namespace WildernessSurvival.Core.StateMachines
{
    /// <summary>
    /// Stati principali del ciclo di gioco
    /// </summary>
    public enum GameState
    {
        /// <summary>Caricamento iniziale</summary>
        Initializing,
        
        /// <summary>Menu principale</summary>
        MainMenu,
        
        /// <summary>Fase diurna - costruzione, raccolta, preparazione</summary>
        Day,
        
        /// <summary>Transizione giorno → notte</summary>
        DayToNight,
        
        /// <summary>Fase notturna - combattimento, difesa</summary>
        Night,
        
        /// <summary>Transizione notte → giorno</summary>
        NightToDay,
        
        /// <summary>Gioco in pausa</summary>
        Paused,
        
        /// <summary>Game Over - base distrutta</summary>
        GameOver,
        
        /// <summary>Vittoria - run completata</summary>
        Victory
    }

    /// <summary>
    /// Sottostati della fase diurna
    /// </summary>
    public enum DayPhase
    {
        /// <summary>Inizio giornata, briefing</summary>
        Dawn,
        
        /// <summary>Fase principale di lavoro</summary>
        Working,
        
        /// <summary>Preparazione alla notte</summary>
        Dusk
    }

    /// <summary>
    /// Sottostati della fase notturna
    /// </summary>
    public enum NightPhase
    {
        /// <summary>Preparazione difese</summary>
        Preparation,
        
        /// <summary>Ondata attiva</summary>
        WaveActive,
        
        /// <summary>Pausa tra ondate</summary>
        WaveIntermission,
        
        /// <summary>Tutte le ondate completate</summary>
        WavesComplete,
        
        /// <summary>Boss fight</summary>
        BossWave
    }
}
