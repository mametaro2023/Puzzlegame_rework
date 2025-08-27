using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using System.IO;
using MyPuzzleGame.Core;
using MyPuzzleGame.Entities;
using MyPuzzleGame.Logic;
using MyPuzzleGame.Rendering;
using MyPuzzleGame.SystemUtils;
using System.Linq;
using System.Drawing;
using OpenTK.Mathematics;

namespace MyPuzzleGame
{
    public class Game : GameWindow
    {
        private GameField? _gameField;
        private FieldRenderer? _fieldRenderer;
        private GameLogic? _gameLogic;
        private GPURenderer? _gpuRenderer;
        private SoundManager? _soundManager;
        private readonly object _renderLock = new();
        private bool _initialized = false;
        private InputHandler? _inputHandler;

        // UI State
        private bool _isSettingsOpen = false;
        private Rectangle _settingsButtonRect;
        private Rectangle _muteButtonRect;
        private Rectangle _volumeSliderRect;
        private Rectangle _volumeSliderHandleRect;
        private Rectangle _gaugeRect;
        private bool _isDraggingVolume = false;
        private UIRenderer? _uiRenderer;
        
        private readonly Stopwatch _frameTimer = new();
        private long _lastFrameTick = 0;
        private readonly long _targetFrameTicks;

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            _targetFrameTicks = (long)((1.0 / GameConfig.TargetFPS) * Stopwatch.Frequency);
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
                _soundManager?.Dispose();
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
                    UpdateUIElementPositions();
                }
            }
            catch (Exception ex)
            {
                HandleError("resize", ex);
            }
        }

        private void UpdateUIElementPositions()
        {
            if (_gameField == null) return;

            int settingsButtonSize = 40;
            _settingsButtonRect = new Rectangle(ClientSize.X - settingsButtonSize - 10, 10, settingsButtonSize, settingsButtonSize);
            _muteButtonRect = new Rectangle(ClientSize.X - 60, 60, 50, 40);
            _volumeSliderRect = new Rectangle(ClientSize.X - 30, 125, 20, 150);
            UpdateVolumeSliderHandle();

            // Position the gauge to the right of the field
            _gaugeRect = new Rectangle(_gameField.X + _gameField.Width + 10, _gameField.Y, 30, _gameField.Height);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButton.Left)
            {
                var mousePosition = new Point((int)MouseState.X, (int)MouseState.Y);

                if (_settingsButtonRect.Contains(mousePosition))
                {
                    _isSettingsOpen = !_isSettingsOpen;
                }
                else if (_isSettingsOpen)
                {
                    if (_muteButtonRect.Contains(mousePosition))
                    {
                        _soundManager?.ToggleMute();
                    }
                    else if (_volumeSliderRect.Contains(mousePosition) || _volumeSliderHandleRect.Contains(mousePosition))
                    {
                        _isDraggingVolume = true;
                        UpdateVolumeFromMouse(MouseState.Y);
                    }
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButton.Left)
            {
                _isDraggingVolume = false;
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDraggingVolume)
            {
                UpdateVolumeFromMouse(e.Position.Y);
            }
        }

        private void UpdateVolumeFromMouse(float mouseY)
        {
            float volume = 1.0f - ((mouseY - _volumeSliderRect.Y) / (float)_volumeSliderRect.Height);
            volume = Math.Clamp(volume, 0.0f, 1.0f);
            _soundManager?.SetVolume(volume);
            UpdateVolumeSliderHandle();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        { 
            base.OnUpdateFrame(e);
            if (KeyboardState.IsKeyDown(Keys.Escape)) Close();

            _gameLogic?.Update(e.Time * 1000.0);
            _inputHandler?.HandleInput(KeyboardState, e.Time * 1000.0);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (!_initialized) return;

            try
            {
                base.OnRenderFrame(e);
                RenderGame();
                SwapBuffers();
                MinimalCpuFrameLimit();
            }
            catch (Exception ex)
            {
                HandleError("rendering", ex);
            }
        }

        private void InitializeOpenGL()
        {
            GL.ClearColor(GameConfig.BackgroundColor.R / 255f, GameConfig.BackgroundColor.G / 255f, GameConfig.BackgroundColor.B / 255f, 1.0f);
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
            VSync = VSyncMode.Off;
            UpdateFrequency = 0;

            _gpuRenderer = new GPURenderer();
            _gpuRenderer.Initialize(ClientSize.X, ClientSize.Y);

            _uiRenderer = new UIRenderer(_gpuRenderer);

            _soundManager = new SoundManager();
            try
            {
                string soundDir = Path.Combine(AppContext.BaseDirectory, "Sounds");
                _soundManager.LoadSound("move", Path.Combine(soundDir, "move.wav"));
                _soundManager.LoadSound("lock", Path.Combine(soundDir, "drop.wav"));
                _soundManager.LoadSound("clear", Path.Combine(soundDir, "1combo.wav"));
            }
            catch (Exception ex)
            {
                HandleError("loading sounds", ex);
            }

            _gameField = new GameField(ClientSize.X, ClientSize.Y);
            _fieldRenderer = new FieldRenderer(_gameField, _gpuRenderer);
            _gameLogic = new GameLogic(_gameField, _soundManager);
            
            _gameLogic.Start();
            _inputHandler = new InputHandler(_gameLogic);
            
            UpdateUIElementPositions();
            Console.WriteLine("High-performance GPU rendering initialized with custom frame limiter.");
        }

        private void RenderGame()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            lock (_renderLock)
            {
                if (_gameLogic == null || _fieldRenderer == null || _uiRenderer == null) return;

                var gameState = _gameLogic.CurrentState;
                var fallingBlocks = _gameLogic.GetFallingBlocks() ?? Enumerable.Empty<AnimatingBlock>();

                _fieldRenderer.RenderField(fallingBlocks, gameState, ClientSize);
                
                var currentMino = _gameLogic.GetCurrentMino();
                if (currentMino != null)
                {
                    _fieldRenderer.RenderMino(currentMino);
                }

                var nextMinos = _gameLogic.GetNextMinos();
                if (nextMinos != null)
                {
                    _fieldRenderer.RenderNextMinos(nextMinos, gameState);
                }

                // Render UI
                _uiRenderer.RenderSettingsIcon(_settingsButtonRect);
                if (_isSettingsOpen)
                {
                    _uiRenderer.RenderButton(_muteButtonRect, "Mute", _soundManager?.IsMuted() ?? false);
                    _uiRenderer.RenderSlider(_volumeSliderRect, _volumeSliderHandleRect);
                }

                // Render Gauge
                float gaugeValue = _gameLogic.GetGaugeValue();
                _uiRenderer.RenderGauge(_gaugeRect, gaugeValue);
            }
        }

        private void UpdateVolumeSliderHandle()
        {
            if (_soundManager != null)
            {
                int handleWidth = 20;
                int handleHeight = 30;
                float sliderPos = (_volumeSliderRect.Width - handleWidth) * _soundManager.GetVolume();
                _volumeSliderHandleRect = new Rectangle(_volumeSliderRect.X + (int)sliderPos, _volumeSliderRect.Y + (_volumeSliderRect.Height - handleHeight) / 2, handleWidth, handleHeight);
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
                    Thread.Sleep((int)Math.Max(1, remainingMs - 0.5));
                }
                
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