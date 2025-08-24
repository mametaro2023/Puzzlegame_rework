using System.Drawing;
using MyPuzzleGame.Core;

namespace MyPuzzleGame.Graphics
{
    public static class BlockColors
    {
        // New stylish, flat color palette
        public static readonly Dictionary<BlockType, (Color Main, Color Light, Color Dark)> Colors = new()
        {
            // Main, Light, and Dark are the same for a flat look.
            [BlockType.Red] = (Color.FromArgb(255, 59, 48), Color.FromArgb(255, 59, 48), Color.FromArgb(255, 59, 48)),
            [BlockType.Green] = (Color.FromArgb(52, 199, 89), Color.FromArgb(52, 199, 89), Color.FromArgb(52, 199, 89)),
            [BlockType.Blue] = (Color.FromArgb(0, 122, 255), Color.FromArgb(0, 122, 255), Color.FromArgb(0, 122, 255)),
            [BlockType.Yellow] = (Color.FromArgb(255, 204, 0), Color.FromArgb(255, 204, 0), Color.FromArgb(255, 204, 0)),
            [BlockType.Purple] = (Color.FromArgb(175, 82, 222), Color.FromArgb(175, 82, 222), Color.FromArgb(175, 82, 222)),
            [BlockType.Orange] = (Color.FromArgb(255, 149, 0), Color.FromArgb(255, 149, 0), Color.FromArgb(255, 149, 0))
        };
    }
}
