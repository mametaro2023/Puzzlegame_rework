using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Drawing;
using MyPuzzleGame.Core;
using MyPuzzleGame.Entities;
using MyPuzzleGame.Graphics;
using MyPuzzleGame.Logic;

namespace MyPuzzleGame.Rendering
{
    public class BlockRenderer
    {
        private readonly GameField _field;
        private GPURenderer? _gpuRenderer;

        public BlockRenderer(GameField field, GPURenderer? gpuRenderer = null)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _gpuRenderer = gpuRenderer;
        }

        public void SetGPURenderer(GPURenderer gpuRenderer)
        {
            _gpuRenderer = gpuRenderer;
        }

        public void RenderBlock(int gridX, int gridY, Core.BlockType blockType, bool isGameOver = false)
        {
            if (blockType == Core.BlockType.None) return;

            try
            {
                int pixelX = _field.X + gridX * Core.GameConfig.BlockSize;
                int pixelY = _field.Y + gridY * Core.GameConfig.BlockSize;

                if (isGameOver)
                {
                    var greyColor = new Vector3(0.3f, 0.3f, 0.3f);
                    if (_gpuRenderer != null)
                    {
                        _gpuRenderer.RenderBlock(pixelX, pixelY, Core.GameConfig.BlockSize, greyColor);
                    }
                    else
                    {
                        // CPU fallback
                        RenderBlockBackground(pixelX, pixelY, (Color.FromArgb(76, 76, 76), Color.FromArgb(76, 76, 76), Color.FromArgb(76, 76, 76)));
                        RenderBlockBorder(pixelX, pixelY, (Color.Gray, Color.LightGray, Color.DarkGray));
                    }
                    return;
                }


                if (_gpuRenderer != null && GPUBlockColors.Colors.TryGetValue(blockType, out var gpuColors))
                {
                    _gpuRenderer.RenderBlock(pixelX, pixelY, Core.GameConfig.BlockSize, gpuColors.Main);
                }
                else if (BlockColors.Colors.TryGetValue(blockType, out var colors))
                {
                    // Fallback to CPU rendering
                    RenderBlockBackground(pixelX, pixelY, colors);
                    RenderBlockHighlight(pixelX, pixelY, colors);
                    RenderBlockShadow(pixelX, pixelY, colors);
                    RenderBlockBorder(pixelX, pixelY, colors);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering block at ({gridX}, {gridY}): {ex.Message}");
            }
        }

        public void RenderFloatingBlock(int gridX, float visualY, Core.BlockType blockType)
        {
            if (blockType == Core.BlockType.None) return;

            try
            {
                int pixelX = _field.X + gridX * Core.GameConfig.BlockSize;
                int pixelY = (int)Math.Round(_field.Y + visualY * Core.GameConfig.BlockSize);

                if (_gpuRenderer != null && GPUBlockColors.Colors.TryGetValue(blockType, out var gpuColors))
                {
                    _gpuRenderer.RenderBlock(pixelX, pixelY, Core.GameConfig.BlockSize, gpuColors.Main);
                }
                else if (BlockColors.Colors.TryGetValue(blockType, out var colors))
                {
                    // Fallback to CPU rendering
                    RenderBlockBackground(pixelX, pixelY, colors);
                    RenderBlockHighlight(pixelX, pixelY, colors);
                    RenderBlockShadow(pixelX, pixelY, colors);
                    RenderBlockBorder(pixelX, pixelY, colors);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering floating block at ({gridX}, {visualY}): {ex.Message}");
            }
        }

        private static void RenderBlockBackground(int x, int y, (Color Main, Color Light, Color Dark) colors)
        {
            int size = Core.GameConfig.BlockSize;
            int margin = 1;
            
            GL.Begin(PrimitiveType.Quads);
            GL.Color3(colors.Main);
            GL.Vertex2(x + margin, y + margin);
            GL.Vertex2(x + size - margin, y + margin);
            GL.Vertex2(x + size - margin, y + size - margin);
            GL.Vertex2(x + margin, y + size - margin);
            GL.End();
        }

        private static void RenderBlockHighlight(int x, int y, (Color Main, Color Light, Color Dark) colors)
        {
            // Disabled for flat design
        }

        private static void RenderBlockShadow(int x, int y, (Color Main, Color Light, Color Dark) colors)
        {
            // Disabled for flat design
        }

        private static void RenderBlockBorder(int x, int y, (Color Main, Color Light, Color Dark) colors)
        {
            int size = Core.GameConfig.BlockSize;
            
            GL.Color3(Color.FromArgb(96, 96, 96));
            GL.LineWidth(1.0f);
            GL.Begin(PrimitiveType.LineLoop);
            GL.Vertex2(x + 1, y + 1);
            GL.Vertex2(x + size - 1, y + 1);
            GL.Vertex2(x + size - 1, y + size - 1);
            GL.Vertex2(x + 1, y + size - 1);
            GL.End();
        }
    }

    public class FieldRenderer
    {
        private readonly GameField _field;
        private readonly BlockRenderer _blockRenderer;
        private GPURenderer? _gpuRenderer;

        public FieldRenderer(GameField field, GPURenderer? gpuRenderer = null)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _blockRenderer = new BlockRenderer(field, gpuRenderer);
            _gpuRenderer = gpuRenderer;
        }

        public void SetGPURenderer(GPURenderer gpuRenderer)
        {
            _gpuRenderer = gpuRenderer;
            _blockRenderer.SetGPURenderer(gpuRenderer);
        }

        public void RenderField(IEnumerable<AnimatingBlock> fallingBlocks, GameLogic.GameState gameState)
        {
            try
            {
                // Only render background and grid occasionally to reduce CPU usage
                RenderBackground();
                RenderGrid();
                
                // Only render blocks that exist to minimize processing
                RenderBlocks(gameState);

                // Render animating blocks
                foreach (var block in fallingBlocks)
                {
                    _blockRenderer.RenderFloatingBlock(block.X, block.VisualY, block.Block.Type);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering field: {ex.Message}");
            }
        }
        
        public void RenderMino(Mino mino)
        {
            if (mino == null) return;

            try
            {
                // The renderer is now "dumb". It simply draws the mino at the VisualY
                // coordinate that is calculated and updated by the GameLogic.
                for (int i = 0; i < mino.Blocks.Length; i++)
                {
                    Block block = mino.Blocks[i];
                    if (block.Type == Core.BlockType.None) continue;

                    int pixelX = _field.X + mino.X * Core.GameConfig.BlockSize;
                    
                    // Calculate pixel Y based on the VisualY calculated in GameLogic
                    float blockVisualY = mino.VisualY - (mino.Blocks.Length - 1 - i);
                    float pixelY = _field.Y + blockVisualY * Core.GameConfig.BlockSize;

                    if (_gpuRenderer != null && GPUBlockColors.Colors.TryGetValue(block.Type, out var gpuColors))
                    {
                        _gpuRenderer.RenderBlock(pixelX, (int)Math.Round(pixelY), Core.GameConfig.BlockSize, gpuColors.Main);
                    }
                    else
                    {
                        // Fallback for CPU rendering (will not be smooth)
                        _blockRenderer.RenderBlock(mino.X, (int)Math.Round(blockVisualY), block.Type);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering mino: {ex.Message}");
            }
        }

        private void RenderBackground()
        {
            _gpuRenderer?.RenderQuad(_field.X, _field.Y, _field.Width, _field.Height, new Vector3(44f / 255f, 44f / 255f, 44f / 255f));
        }

        private void RenderGrid()
        {
            if (_gpuRenderer == null) return;

            var gridColor = new Vector3(64f / 255f, 64f / 255f, 64f / 255f);
            const int lineWidth = 1;

            // Vertical lines
            for (int i = 0; i <= Core.GameConfig.FieldWidth; i++)
            {
                int x = _field.X + i * Core.GameConfig.BlockSize;
                _gpuRenderer.RenderQuad(x, _field.Y, lineWidth, _field.Height, gridColor);
            }

            // Horizontal lines
            for (int i = 0; i <= Core.GameConfig.FieldHeight; i++)
            {
                int y = _field.Y + i * Core.GameConfig.BlockSize;
                _gpuRenderer.RenderQuad(_field.X, y, _field.Width, lineWidth, gridColor);
            }
        }

        private void RenderBlocks(GameLogic.GameState gameState)
        {
            bool isGameOver = gameState == GameLogic.GameState.GameOver;
            foreach (var (x, y, block) in _field.GetAllBlocks())
            {
                _blockRenderer.RenderBlock(x, y, block.Type, isGameOver);
            }
        }

        public void RenderNextMinos(List<Mino> nextMinos, GameLogic.GameState gameState)
        {
            if (_gpuRenderer == null || nextMinos == null || nextMinos.Count == 0) return;

            bool isGameOver = gameState == GameLogic.GameState.GameOver;
            var gameOverGreyColor = new Vector3(0.3f, 0.3f, 0.3f);

            // --- Next 1 (Main Display) ---
            Mino next1 = nextMinos[0];
            if (next1 == null) return;

            int blockSize = Core.GameConfig.BlockSize;
            int boxWidth = (int)(blockSize * 3.5);
            int boxHeight = (int)(blockSize * 4.5);
            int boxX = _field.X - boxWidth - 20; // 20px margin from field
            int boxY = _field.Y;

            // Draw border and background for Next1
            _gpuRenderer.RenderQuad(boxX - 2, boxY - 2, boxWidth + 4, boxHeight + 4, new Vector3(0.1f, 0.1f, 0.1f)); // Border
            _gpuRenderer.RenderQuad(boxX, boxY, boxWidth, boxHeight, new Vector3(0.2f, 0.2f, 0.2f)); // Background

            // Center the mino inside the box
            int minoX = boxX + (boxWidth - blockSize) / 2;
            int minoY = boxY + (boxHeight - blockSize * 3) / 2;

            // Draw the Next1 mino
            for (int j = 0; j < next1.Blocks.Length; j++)
            {
                Block block = next1.Blocks[j];
                if (block.Type == Core.BlockType.None) continue;

                int blockPixelY = minoY + j * blockSize;
                if (isGameOver)
                {
                    _gpuRenderer.RenderBlock(minoX, blockPixelY, blockSize, gameOverGreyColor);
                }
                else if (GPUBlockColors.Colors.TryGetValue(block.Type, out var gpuColors))
                {
                    _gpuRenderer.RenderBlock(minoX, blockPixelY, blockSize, gpuColors.Main);
                }
            }

            // --- Next 2 (Smaller, Greyscale Display) ---
            if (nextMinos.Count > 1)
            {
                Mino next2 = nextMinos[1];
                if (next2 == null) return;

                int smallBlockSize = (int)(blockSize * 0.6f);
                int smallMinoX = boxX + 5; // Inside the main box, left corner
                int smallMinoY = boxY + boxHeight - (smallBlockSize * 3) - 5; // Inside the main box, bottom corner
                var greyColor = new Vector3(0.5f, 0.5f, 0.5f); // Grey color for Next2

                for (int j = 0; j < next2.Blocks.Length; j++)
                {
                    Block block = next2.Blocks[j];
                    if (block.Type == Core.BlockType.None) continue;

                    int blockPixelY = smallMinoY + j * smallBlockSize;
                    _gpuRenderer.RenderBlock(smallMinoX, blockPixelY, smallBlockSize, isGameOver ? gameOverGreyColor : greyColor);
                }
            }
        }
    }
}