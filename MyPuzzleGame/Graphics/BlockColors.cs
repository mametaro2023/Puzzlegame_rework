using System.Drawing;
using MyPuzzleGame.Core;

namespace MyPuzzleGame.Graphics
{
    public static class BlockColors
    {
        public static readonly Dictionary<BlockType, (Color Main, Color Light, Color Dark)> Colors = new()
        {
            [BlockType.Red] = (Color.FromArgb(220, 20, 60), Color.FromArgb(255, 99, 132), Color.FromArgb(139, 0, 0)),
            [BlockType.Green] = (Color.FromArgb(34, 139, 34), Color.FromArgb(144, 238, 144), Color.FromArgb(0, 100, 0)),
            [BlockType.Blue] = (Color.FromArgb(30, 144, 255), Color.FromArgb(135, 206, 250), Color.FromArgb(0, 0, 139)),
            [BlockType.Yellow] = (Color.FromArgb(255, 215, 0), Color.FromArgb(255, 255, 224), Color.FromArgb(184, 134, 11)),
            [BlockType.Purple] = (Color.FromArgb(147, 112, 219), Color.FromArgb(221, 160, 221), Color.FromArgb(75, 0, 130)),
            [BlockType.Orange] = (Color.FromArgb(255, 140, 0), Color.FromArgb(255, 218, 185), Color.FromArgb(205, 92, 92))
        };
    }
}