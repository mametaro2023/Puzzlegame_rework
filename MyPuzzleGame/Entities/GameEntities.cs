using MyPuzzleGame.Core;
using System.Collections.Generic;

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
            VisualY = y;
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

        public void RotateDown()
        {
            var bottomType = Blocks[2].Type;
            Blocks[2].Type = Blocks[1].Type;
            Blocks[1].Type = Blocks[0].Type;
            Blocks[0].Type = bottomType;
        }
    }

    public class GameField
    {
        private int _fieldX;
        private int _fieldY;
        private readonly Block?[,] _blocks;
        private const int TotalFieldHeight = Core.GameConfig.FieldHeight + Core.GameConfig.VanishingZoneHeight;

        public GameField(int windowWidth, int windowHeight)
        {
            _blocks = new Block?[Core.GameConfig.FieldWidth, TotalFieldHeight];
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

        private int ToGridY(int y) => y + Core.GameConfig.VanishingZoneHeight;

        public Block? GetBlock(int x, int y)
        {
            int gridY = ToGridY(y);
            if (!IsInternalValidPosition(x, gridY))
                return null;
            return _blocks[x, gridY];
        }

        public void SetBlock(int x, int y, Block? block)
        {
            int gridY = ToGridY(y);
            if (IsInternalValidPosition(x, gridY))
            {
                _blocks[x, gridY] = block;
            }
        }
        
        public bool IsCollision(int x, int y)
        {
            if (x < 0 || x >= Core.GameConfig.FieldWidth || y >= Core.GameConfig.FieldHeight)
            {
                return true;
            }
            
            int gridY = ToGridY(y);
            if (!IsInternalValidPosition(x, gridY))
            {
                // This case should ideally not be hit if the above check is correct
                return true;
            }

            return _blocks[x, gridY] != null;
        }

        public IEnumerable<(int x, int y, Block block)> GetAllBlocks()
        {
            for (int x = 0; x < Core.GameConfig.FieldWidth; x++)
            {
                for (int y = 0; y < TotalFieldHeight; y++)
                {
                    if (_blocks[x, y] != null)
                    {
                        // Convert internal grid Y back to logical Y
                        yield return (x, y - Core.GameConfig.VanishingZoneHeight, _blocks[x, y]!);
                    }
                }
            }
        }

        private bool IsInternalValidPosition(int x, int internalY)
        {
            return x >= 0 && x < Core.GameConfig.FieldWidth && internalY >= 0 && internalY < TotalFieldHeight;
        }
    }
}