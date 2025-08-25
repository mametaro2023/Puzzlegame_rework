using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using MyPuzzleGame.Core;
using MyPuzzleGame.Entities;
using MyPuzzleGame.Logic;
using MyPuzzleGame.Rendering;
using MyPuzzleGame.SystemUtils;
using System.Linq;

namespace MyPuzzleGame
{
    public class Game : GameWindow
    {
        private GameField? _gameField;
        private FieldRenderer? _fieldRenderer;
        private GameLogic? _gameLogic;
        private GPURenderer? _gpuRenderer;
        private readonly object _renderLock = new();
        private bool _initialized = false;
        private InputHandler? _inputHandler;
        
        // FPS and frame limiting
        private int _frameCount = 0;
        private double _fpsUpdateTimer = 0.0;
        private double _currentFPS = 0.0;
        private readonly double _targetFrameTime = 1.0 / GameConfig.TargetFPS;
        
        // High precision frame limiting with minimal CPU usage
        private readonly Stopwatch _frameTimer = new();
        private long _lastFrameTick = 0;
        private readonly long _targetFrameTicks;
        // Removed unused field

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            _targetFrameTicks = (long)(_targetFrameTime * Stopwatch.Frequency);
        }

        protected override void OnLoad()
        {
            try
            {
                base.OnLoad();
                
                SystemUtils.NativeMethods.TimeBeginPeriod(1);
                _frameTimer.Start();
                _lastFrameTick = _frameTimer.ElapsedTicks;

                InitializeOpenGL();
                InitializeGame();
                
                // Initialize frame timer instead
                _initialized = true;
            }
            catch (Exception ex)
            {
                HandleError("initialization", ex);
                Close();
            }
        }

        protected override void OnUnload()
        {
            try
            {
                _gameLogic?.Dispose();
                _gpuRenderer?.Dispose();
                SystemUtils.NativeMethods.TimeEndPeriod(1);
                base.OnUnload();
            }
            catch (Exception ex)
            {
                HandleError("cleanup", ex);
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            try
            {
                base.OnResize(e);
                UpdateViewport();
                
                lock (_renderLock)
                {
                    _gameField?.UpdatePosition(ClientSize.X, ClientSize.Y);
                    _gpuRenderer?.UpdateProjection(ClientSize.X, ClientSize.Y);
                }
            }
            catch (Exception ex)
            {
                HandleError("resize", ex);
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        { 
            base.OnUpdateFrame(e);
            if (KeyboardState.IsKeyDown(Keys.Escape)) Close();

            // Update game logic here, driven by the main loop
            _gameLogic?.Update(e.Time * 1000.0); // e.Time is in seconds, logic uses milliseconds

            _inputHandler?.HandleInput(KeyboardState, e.Time * 1000.0);
            UpdateFPS(e.Time);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (!_initialized) return;

            try
            {
                base.OnRenderFrame(e);
                RenderGame();
                SwapBuffers();

                // Minimal CPU usage frame limiter
                MinimalCpuFrameLimit();
            }
            catch (Exception ex)
            {
                HandleError("rendering", ex);
            }
        }

        private void InitializeOpenGL()
        {
            GL.ClearColor(GameConfig.BackgroundColor);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            UpdateViewport();
        }

        private void UpdateViewport()
        {
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        }

        private void InitializeGame()
        {
            // Completely disable OpenTK's internal frame limiting
            VSync = VSyncMode.Off;
            UpdateFrequency = 0; // No internal update limiting

            _gpuRenderer = new GPURenderer();
            _gpuRenderer.Initialize(ClientSize.X, ClientSize.Y);

            _gameField = new GameField(ClientSize.X, ClientSize.Y);
            _fieldRenderer = new FieldRenderer(_gameField, _gpuRenderer);
            _gameLogic = new GameLogic(_gameField);
            
            _gameLogic.Start();
            _inputHandler = new InputHandler(_gameLogic);
            
            Console.WriteLine("High-performance GPU rendering initialized with custom frame limiter.");
        }

        private void RenderGame()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            lock (_renderLock)
            {
                var gameState = _gameLogic?.CurrentState ?? GameLogic.GameState.Spawning;
                // Get the list of falling blocks from the logic
                var fallingBlocks = _gameLogic?.GetFallingBlocks() ?? Enumerable.Empty<AnimatingBlock>();

                // Render the field and the falling blocks
                _fieldRenderer?.RenderField(fallingBlocks, gameState);
                
                // Render the currently controlled mino
                var currentMino = _gameLogic?.GetCurrentMino();
                if (currentMino != null)
                {
                    _fieldRenderer?.RenderMino(currentMino);
                }

                                var nextMinos = _gameLogic?.GetNextMinos();
                if (nextMinos != null)
                {
                    _fieldRenderer?.RenderNextMinos(nextMinos, gameState);
                }
            }
        }

        private void UpdateFPS(double deltaTime)
        {
            _frameCount++;
            _fpsUpdateTimer += deltaTime;
            
            if (_fpsUpdateTimer >= 1.0)
            {
                _currentFPS = _frameCount / _fpsUpdateTimer;
                Title = $"{GameConfig.WindowTitle} - {_currentFPS:F0} FPS";
                _frameCount = 0;
                _fpsUpdateTimer = 0.0;
            }
        }

        

        private void MinimalCpuFrameLimit()
        {
            long currentTick = _frameTimer.ElapsedTicks;
            long elapsed = currentTick - _lastFrameTick;
            long remaining = _targetFrameTicks - elapsed;
            
            if (remaining > 0)
            {
                double remainingMs = (double)remaining / Stopwatch.Frequency * 1000.0;
                
                if (remainingMs > 1.0)
                {
                    // Sleep for the bulk of the remaining time
                    Thread.Sleep((int)Math.Max(1, remainingMs - 0.5));
                }
                
                // Use SpinWait for the final precise timing - this is more CPU friendly
                SpinWait.SpinUntil(() => _frameTimer.ElapsedTicks - _lastFrameTick >= _targetFrameTicks);
            }
            
            _lastFrameTick = _frameTimer.ElapsedTicks;
        }

        private static void HandleError(string context, Exception ex)
        {
            Console.WriteLine($"Error during {context}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}