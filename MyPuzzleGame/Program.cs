using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using MyPuzzleGame.Core;

namespace MyPuzzleGame
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var gameWindowSettings = new GameWindowSettings()
                {
                    UpdateFrequency = 0  // Uncapped
                };

                var nativeWindowSettings = new NativeWindowSettings()
                {
                    ClientSize = (Core.GameConfig.WindowWidth, Core.GameConfig.WindowHeight),
                    Title = Core.GameConfig.WindowTitle,
                    Profile = Core.GameConfig.GLProfile,
                    Vsync = Core.GameConfig.DefaultVSyncMode
                };

                using (var game = new Game(gameWindowSettings, nativeWindowSettings))
                {
                    game.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}