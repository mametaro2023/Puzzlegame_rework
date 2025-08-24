namespace MyPuzzleGame.Core
{
    public static class GameConfig
    {
        // Window Dimensions
        public const int WindowWidth = 1280;
        public const int WindowHeight = 720;
        
        // Field Dimensions
        public const int FieldWidth = 8;
        public const int FieldHeight = 15;
        public const int BlockSize = 40;

        public static int FieldPixelWidth => FieldWidth * BlockSize;
        public static int FieldPixelHeight => FieldHeight * BlockSize;

        // Timings (in milliseconds)
        public const double FallIntervalMs = 1000.0;
        public const double SoftDropIntervalMs = 50.0;
        public const double LockDelayMs = 500.0;
        
        // State Machine Timings
        public const double MinoLockDelay = 200.0;
        public const double GravityDelay = 500.0;
        public const double ChainDelay = 500.0;
        public const double SpawnDelayMs = 200.0;
        public const double HardDropSpawnDelayMs = 200.0;

        // Internal Game Loop Timings
        public const double GameTimerIntervalMs = 16.0;
    }
}
