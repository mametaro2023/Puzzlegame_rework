using MyPuzzleGame.SystemUtils;

namespace MyPuzzleGame.Entities
{
    public class AnimatingBlock
    {
        public Block Block { get; }
        public int X { get; }
        public float StartY { get; }
        public float EndY { get; }
        public float VisualY { get; private set; }

        private readonly float _duration;
        private float _elapsedTime;

        public AnimatingBlock(Block block, int x, int startY, int endY, float duration)
        {
            Block = block;
            X = x;
            StartY = startY;
            EndY = endY;
            VisualY = startY;
            _duration = duration;
            _elapsedTime = 0f;
        }

        /// <summary>
        /// Updates the animation state.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last frame.</param>
        /// <returns>True if the animation is complete, otherwise false.</returns>
        public bool Update(float deltaTime)
        {
            _elapsedTime += deltaTime;
            if (_elapsedTime >= _duration)
            {
                VisualY = EndY;
                return true; // Animation completed
            }

            float t = _elapsedTime / _duration;
            float easedT = Easing.Bounce.Out(t);
            VisualY = StartY + (EndY - StartY) * easedT;

            return false; // Animation ongoing
        }
    }
}
