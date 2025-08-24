using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Drawing;
using MyPuzzleGame.Core;
using MyPuzzleGame.Entities;
using MyPuzzleGame.Graphics;

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

        public void RenderBlock(int gridX, int gridY, Core.BlockType blockType)
        {
            if (blockType == Core.BlockType.None) return;

            try
            {
                int pixelX = _field.X + gridX * Core.GameConfig.BlockSize;
                int pixelY = _field.Y + gridY * Core.GameConfig.BlockSize;

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
            int size = Core.GameConfig.BlockSize;
            int highlightSize = size / 4;
            int margin = 2;
            
            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(colors.Light);
            GL.Vertex2(x + margin, y + margin);
            GL.Vertex2(x + size - margin, y + margin);
            GL.Vertex2(x + margin, y + highlightSize);
            GL.End();

            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(colors.Light);
            GL.Vertex2(x + margin, y + margin);
            GL.Vertex2(x + highlightSize, y + size - margin);
            GL.Vertex2(x + margin, y + size - margin);
            GL.End();
        }

        private static void RenderBlockShadow(int x, int y, (Color Main, Color Light, Color Dark) colors)
        {
            int size = Core.GameConfig.BlockSize;
            int shadowSize = size / 4;
            int margin = 2;
            
            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(colors.Dark);
            GL.Vertex2(x + size - margin, y + size - margin);
            GL.Vertex2(x + size - shadowSize, y + margin);
            GL.Vertex2(x + size - margin, y + margin);
            GL.End();

            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(colors.Dark);
            GL.Vertex2(x + size - margin, y + size - margin);
            GL.Vertex2(x + margin, y + size - shadowSize);
            GL.Vertex2(x + margin, y + size - margin);
            GL.End();
        }

        private static void RenderBlockBorder(int x, int y, (Color Main, Color Light, Color Dark) colors)
        {
            int size = Core.GameConfig.BlockSize;
            
            GL.Color3(Color.FromArgb(100, 100, 100));
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

        public void RenderField()
        {
            try
            {
                // Only render background and grid occasionally to reduce CPU usage
                RenderBackground();
                RenderGrid();
                
                // Only render blocks that exist to minimize processing
                RenderBlocks();
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

                    // Only render the block if it's within the visible field area
                    if (pixelY >= _field.Y - Core.GameConfig.BlockSize) // Allow rendering one block above the field
                    {
                        if (_gpuRenderer != null && GPUBlockColors.Colors.TryGetValue(block.Type, out var gpuColors))
                        {
                            _gpuRenderer.RenderBlock(pixelX, (int)pixelY, Core.GameConfig.BlockSize, gpuColors.Main);
                        }
                        else
                        {
                            // Fallback for CPU rendering (will not be smooth)
                            _blockRenderer.RenderBlock(mino.X, (int)Math.Round(blockVisualY), block.Type);
                        }
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
            _gpuRenderer?.RenderQuad(_field.X, _field.Y, _field.Width, _field.Height, new Vector3(0.5f, 0.5f, 0.5f));
        }

        private void RenderGrid()
        {
            if (_gpuRenderer == null) return;

            var gridColor = new Vector3(0.3f, 0.3f, 0.3f);
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

        private void RenderBlocks()
        {
            foreach (var (x, y, block) in _field.GetAllBlocks())
            {
                _blockRenderer.RenderBlock(x, y, block.Type);
            }
        }
    }
}