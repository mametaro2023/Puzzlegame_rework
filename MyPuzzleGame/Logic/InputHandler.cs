using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MyPuzzleGame.Logic
{
    public class InputHandler
    {
        private readonly GameLogic _gameLogic;
        private bool _wasDownPressed = false;

        public InputHandler(GameLogic gameLogic)
        {
            _gameLogic = gameLogic;
        }

        public void HandleInput(KeyboardState keyboardState)
        {
            if (keyboardState.IsKeyPressed(Keys.Left))
            {
                _gameLogic.MoveMino(-1, 0);
            }
            if (keyboardState.IsKeyPressed(Keys.Right))
            {
                _gameLogic.MoveMino(1, 0);
            }
            if (keyboardState.IsKeyPressed(Keys.Up))
            {
                _gameLogic.RotateMino();
            }
            if (keyboardState.IsKeyPressed(Keys.Space))
            {
                _gameLogic.HardDrop();
            }

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
        }
    }
}
