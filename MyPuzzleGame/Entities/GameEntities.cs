using MyPuzzleGame.Core;

namespace MyPuzzleGame.Entities
{
    public class Block
    {
        public BlockType Type { get; set; }

        public Block(BlockType type)
        {
            Type = type;
        }
    }

    public class Mino
    {
        public int X { get; set; }
        public int LogicalY { get; set; }
        public float VisualY { get; set; }

        public Block[] Blocks { get; }

        public Mino(int x, int y, BlockType b1, BlockType b2, BlockType b3)
        {
            X = x;
            LogicalY = y;
            VisualY = y; // Initially, visual position matches logical position
            Blocks = new Block[3];
            Blocks[0] = new Block(b1); // Top
            Blocks[1] = new Block(b2); // Middle
            Blocks[2] = new Block(b3); // Bottom
        }

        public void RotateUp()
        {
            var topType = Blocks[0].Type;
            Blocks[0].Type = Blocks[1].Type;
            Blocks[1].Type = Blocks[2].Type;
            Blocks[2].Type = topType;
        }
    }

    public class GameField
    {
        private int _fieldX;
        private int _fieldY;
        private readonly Block?[,] _blocks;

        public GameField(int windowWidth, int windowHeight)
        {
            _blocks = new Block?[Core.GameConfig.FieldWidth, Core.GameConfig.FieldHeight];
            UpdatePosition(windowWidth, windowHeight);
        }

        public void UpdatePosition(int windowWidth, int windowHeight)
        {
            _fieldX = (windowWidth - Core.GameConfig.FieldPixelWidth) / 2;
            _fieldY = (windowHeight - Core.GameConfig.FieldPixelHeight) / 2;
        }

        public int X => _fieldX;
        public int Y => _fieldY;
        public int Width => Core.GameConfig.FieldPixelWidth;
        public int Height => Core.GameConfig.FieldPixelHeight;

        public Block? GetBlock(int x, int y)
        {
            if (!IsValidPosition(x, y))
                return null;
            return _blocks[x, y];
        }

        public void SetBlock(int x, int y, Block? block)
        {
            if (IsValidPosition(x, y))
            {
                _blocks[x, y] = block;
            }
        }
        
        public bool IsCollision(int x, int y)
        {
            if (x < 0 || x >= Core.GameConfig.FieldWidth || y >= Core.GameConfig.FieldHeight)
            {
                return true;
            }
            if (y < 0)
            {
                return false;
            }
            return _blocks[x, y] != null;
        }

        public IEnumerable<(int x, int y, Block block)> GetAllBlocks()
        {
            for (int x = 0; x < Core.GameConfig.FieldWidth; x++)
            {
                for (int y = 0; y < Core.GameConfig.FieldHeight; y++)
                {
                    if (_blocks[x, y] != null)
                    {
                        yield return (x, y, _blocks[x, y]!);
                    }
                }
            }
        }

        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Core.GameConfig.FieldWidth && y >= 0 && y < Core.GameConfig.FieldHeight;
        }
    }
}