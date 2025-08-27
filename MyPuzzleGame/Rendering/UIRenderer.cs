using OpenTK.Mathematics;
using System.Drawing;

namespace MyPuzzleGame.Rendering
{
    public class UIRenderer
    {
        private readonly GPURenderer _gpuRenderer;

        public UIRenderer(GPURenderer gpuRenderer)
        {
            _gpuRenderer = gpuRenderer;
        }

        public void RenderButton(Rectangle rect, string text, bool isToggled)
        {
            var color = isToggled ? new Vector3(0.6f, 0.8f, 0.2f) : new Vector3(0.2f, 0.2f, 0.2f);
            _gpuRenderer.RenderQuad(rect.X, rect.Y, rect.Width, rect.Height, color);
            RenderMuteButtonIcon(rect, isToggled);
        }

        private void RenderMuteButtonIcon(Rectangle rect, bool isMuted)
        {
            var iconColor = new Vector3(0.8f, 0.8f, 0.8f);
            if (isMuted) // If muted, the icon should be more prominent
            {
                iconColor = new Vector3(0.9f, 0.9f, 0.9f);
            }

            // Speaker body (rectangle)
            int speakerBodyWidth = rect.Width / 3;
            int speakerBodyHeight = rect.Height / 2;
            int speakerBodyX = rect.X + rect.Width / 4;
            int speakerBodyY = rect.Y + rect.Height / 4;
            _gpuRenderer.RenderQuad(speakerBodyX, speakerBodyY, speakerBodyWidth, speakerBodyHeight, iconColor);

            // Speaker cone (triangle) - simplified as a smaller rectangle for now
            int speakerConeWidth = rect.Width / 4;
            int speakerConeHeight = rect.Height / 4;
            int speakerConeX = speakerBodyX + speakerBodyWidth;
            int speakerConeY = rect.Y + rect.Height / 2 - speakerConeHeight / 2;
            _gpuRenderer.RenderQuad(speakerConeX, speakerConeY, speakerConeWidth, speakerConeHeight, iconColor);

            if (isMuted)
            {
                // Slash (line) - simplified as a thin rectangle
                int slashThickness = 2;
                int slashLength = (int)(rect.Width * 0.7);
                int slashX = rect.X + (rect.Width - slashLength) / 2;
                int slashY = rect.Y + rect.Height / 2 - slashThickness / 2;
                _gpuRenderer.RenderQuad(slashX, slashY, slashLength, slashThickness, iconColor);
            }
        }

        public void RenderSlider(Rectangle rect, Rectangle handleRect)
        {
            // Render slider background (vertical)
            _gpuRenderer.RenderQuad(rect.X, rect.Y, rect.Width, rect.Height, new Vector3(0.2f, 0.2f, 0.2f));
            // Render slider handle
            _gpuRenderer.RenderQuad(handleRect.X, handleRect.Y, handleRect.Width, handleRect.Height, new Vector3(0.6f, 0.6f, 0.6f));

            // Render '+' and '-' indicators
            RenderPlusMinusIndicators(rect);
        }

        private void RenderPlusMinusIndicators(Rectangle sliderRect)
        {
            var indicatorColor = new Vector3(0.8f, 0.8f, 0.8f);
            int indicatorSize = 10;
            int indicatorThickness = 2;

            // '+' at the top
            int plusX = sliderRect.X + (sliderRect.Width - indicatorSize) / 2;
            int plusY = sliderRect.Y - indicatorSize - 5; // 5 pixels above the slider
            _gpuRenderer.RenderQuad(plusX, plusY + (indicatorSize - indicatorThickness) / 2, indicatorSize, indicatorThickness, indicatorColor); // Horizontal bar
            _gpuRenderer.RenderQuad(plusX + (indicatorSize - indicatorThickness) / 2, plusY, indicatorThickness, indicatorSize, indicatorColor); // Vertical bar

            // '-' at the bottom
            int minusX = sliderRect.X + (sliderRect.Width - indicatorSize) / 2;
            int minusY = sliderRect.Y + sliderRect.Height + 5; // 5 pixels below the slider
            _gpuRenderer.RenderQuad(minusX, minusY + (indicatorSize - indicatorThickness) / 2, indicatorSize, indicatorThickness, indicatorColor); // Horizontal bar
        }

        public void RenderSettingsIcon(Rectangle rect)
        {
            // Simple gear icon
            int centerX = rect.X + rect.Width / 2;
            int centerY = rect.Y + rect.Height / 2;
            float radius = rect.Width / 3.0f;

            _gpuRenderer.RenderQuad(rect.X, rect.Y, rect.Width, rect.Height, new Vector3(0.2f, 0.2f, 0.2f));

            // A simple plus sign to represent settings for now
            _gpuRenderer.RenderQuad(centerX - rect.Width / 4, centerY - 1, rect.Width / 2, 2, new Vector3(0.8f, 0.8f, 0.8f));
            _gpuRenderer.RenderQuad(centerX - 1, centerY - rect.Height / 4, 2, rect.Height / 2, new Vector3(0.8f, 0.8f, 0.8f));
        }
    }
}
