using OpenTK.Windowing.Common;
using System.Drawing;

namespace MyPuzzleGame.Core
{
    public static class GameConfig
    {
        // Window Settings
        public const string WindowTitle = "My Puzzle Game";
        public const int WindowWidth = 1280;
        public const int WindowHeight = 720;
        public const int TargetFPS = 1000;
        public static readonly Color BackgroundColor = Color.CornflowerBlue;
        public const VSyncMode DefaultVSyncMode = VSyncMode.Off;
        public const ContextProfile GLProfile = ContextProfile.Compatability;
        
        // Field Dimensions
        public const int FieldWidth = 8;
        public const int FieldHeight = 15;
        public const int VanishingZoneHeight = 4; // Extra space above the visible field
        public const int BlockSize = 40;

        public static int FieldPixelWidth => FieldWidth * BlockSize;
        public static int FieldPixelHeight => FieldHeight * BlockSize;

        // Timings (in milliseconds)
        public const double FallIntervalMs = 1000.0;
        public const double SoftDropIntervalMs = 50.0;
        public const double LockDelayMs = 500.0;

        // Dynamic Fall Speed Settings (blocks per second)
        public const float BaseFallSpeed = 0.5f;
        public const float GaugeBonusSpeed = 3.0f; // Max speed bonus from gauge
        public const float SoftDropBonusSpeed = 6.0f;
        public const float TimeBonusSpeedPerMinute = 1.0f;
        
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
