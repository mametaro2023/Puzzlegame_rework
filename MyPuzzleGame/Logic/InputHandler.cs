using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MyPuzzleGame.Logic
{
    public class InputHandler
    {
        private readonly GameLogic _gameLogic;
        private bool _wasDownPressed = false;

        private const double DasDelay = 150.0; // ms
        private const double DasInterval = 50.0; // ms

        private bool _isLeftHeld = false;
        private bool _isRightHeld = false;
        private double _leftDasTimer = 0.0;
        private double _rightDasTimer = 0.0;

        public InputHandler(GameLogic gameLogic)
        {
            _gameLogic = gameLogic;
        }

        public void HandleInput(KeyboardState keyboardState, double deltaTime)
        {
            // Up key for rotation
            if (keyboardState.IsKeyPressed(Keys.Up))
            {
                _gameLogic.RotateMino();
            }

            // Space for hard drop
            if (keyboardState.IsKeyPressed(Keys.Space))
            {
                _gameLogic.HardDrop();
            }

            // Down key for soft drop
            bool isDownPressed = keyboardState.IsKeyDown(Keys.Down);
            if (isDownPressed && !_wasDownPressed)
            {
                _gameLogic.StartSoftDrop();
            }
            else if (!isDownPressed && _wasDownPressed)
            {
                _gameLogic.StopSoftDrop();
            }
            _wasDownPressed = isDownPressed;

            // Left key with DAS
            if (keyboardState.IsKeyPressed(Keys.Left))
            {
                _gameLogic.MoveMino(-1, 0);
                _isLeftHeld = true;
                _leftDasTimer = DasDelay;
            }
            else if (keyboardState.IsKeyDown(Keys.Left) && _isLeftHeld)
            {
                _leftDasTimer -= deltaTime;
                if (_leftDasTimer <= 0)
                {
                    _gameLogic.MoveMino(-1, 0);
                    _leftDasTimer = DasInterval;
                }
            }
            else
            {
                _isLeftHeld = false;
            }

            // Right key with DAS
            if (keyboardState.IsKeyPressed(Keys.Right))
            {
                _gameLogic.MoveMino(1, 0);
                _isRightHeld = true;
                _rightDasTimer = DasDelay;
            }
            else if (keyboardState.IsKeyDown(Keys.Right) && _isRightHeld)
            {
                _rightDasTimer -= deltaTime;
                if (_rightDasTimer <= 0)
                {
                    _gameLogic.MoveMino(1, 0);
                    _rightDasTimer = DasInterval;
                }
            }
            else
            {
                _isRightHeld = false;
            }
        }
    }
}