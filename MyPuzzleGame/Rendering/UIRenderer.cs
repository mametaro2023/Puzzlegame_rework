using OpenTK.Mathematics;
using System.Drawing;
using System;

namespace MyPuzzleGame.Rendering
{
    public class UIRenderer
    {
        private readonly GPURenderer _gpuRenderer;
        private int _speakerOnTexture = -1;
        private int _speakerOffTexture = -1;

        public UIRenderer(GPURenderer gpuRenderer)
        {
            _gpuRenderer = gpuRenderer;
            try
            {
                _speakerOnTexture = _gpuRenderer.LoadTexture("Assets/Icons/speaker_on.png");
                _speakerOffTexture = _gpuRenderer.LoadTexture("Assets/Icons/speaker_off.png");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load UI textures: {ex.Message}");
            }
        }

        public void RenderButton(Rectangle rect, string text, bool isToggled)
        {
            var color = isToggled ? new Vector3(0.6f, 0.8f, 0.2f) : new Vector3(0.2f, 0.2f, 0.2f);
            _gpuRenderer.RenderQuad(rect.X, rect.Y, rect.Width, rect.Height, color);
            RenderMuteButtonIcon(rect, isToggled);
        }

        private void RenderMuteButtonIcon(Rectangle rect, bool isMuted)
        {
            int textureId = isMuted ? _speakerOffTexture : _speakerOnTexture;
            if (textureId != -1)
            {
                int padding = 5;
                int iconSize = Math.Min(rect.Width, rect.Height) - padding * 2;
                int iconX = rect.X + (rect.Width - iconSize) / 2;
                int iconY = rect.Y + (rect.Height - iconSize) / 2;
                _gpuRenderer.RenderTexturedQuad(iconX, iconY, iconSize, iconSize, textureId);
            }
            // Fallback if textures are not loaded
            else
            {
                var iconColor = new Vector3(0.8f, 0.8f, 0.8f);
                // Original simple shape drawing as a fallback
                int speakerBodyWidth = rect.Width / 3;
                int speakerBodyHeight = rect.Height / 2;
                int speakerBodyX = rect.X + rect.Width / 4;
                int speakerBodyY = rect.Y + rect.Height / 4;
                _gpuRenderer.RenderQuad(speakerBodyX, speakerBodyY, speakerBodyWidth, speakerBodyHeight, iconColor);

                if (isMuted)
                {
                    var slashColor = new Vector3(0.9f, 0.2f, 0.2f);
                    int slashThickness = 2;
                    _gpuRenderer.RenderQuad(rect.X + 5, rect.Y + rect.Height/2 - slashThickness/2, rect.Width - 10, slashThickness, slashColor);
                }
            }
        }

        public void RenderSlider(Rectangle rect, Rectangle handleRect)
        {
            _gpuRenderer.RenderQuad(rect.X, rect.Y, rect.Width, rect.Height, new Vector3(0.2f, 0.2f, 0.2f));
            _gpuRenderer.RenderQuad(handleRect.X, handleRect.Y, handleRect.Width, handleRect.Height, new Vector3(0.6f, 0.6f, 0.6f));
            RenderPlusMinusIndicators(rect);
        }

        private void RenderPlusMinusIndicators(Rectangle sliderRect)
        {
            var indicatorColor = new Vector3(0.8f, 0.8f, 0.8f);
            int indicatorSize = 10;
            int indicatorThickness = 2;

            int plusX = sliderRect.X + (sliderRect.Width - indicatorSize) / 2;
            int plusY = sliderRect.Y - indicatorSize - 5;
            _gpuRenderer.RenderQuad(plusX, plusY + (indicatorSize - indicatorThickness) / 2, indicatorSize, indicatorThickness, indicatorColor);
            _gpuRenderer.RenderQuad(plusX + (indicatorSize - indicatorThickness) / 2, plusY, indicatorThickness, indicatorSize, indicatorColor);

            int minusX = sliderRect.X + (sliderRect.Width - indicatorSize) / 2;
            int minusY = sliderRect.Y + sliderRect.Height + 5;
            _gpuRenderer.RenderQuad(minusX, minusY + (indicatorSize - indicatorThickness) / 2, indicatorSize, indicatorThickness, indicatorColor);
        }

        public void RenderSettingsIcon(Rectangle rect)
        {
            int centerX = rect.X + rect.Width / 2;
            int centerY = rect.Y + rect.Height / 2;
            _gpuRenderer.RenderQuad(rect.X, rect.Y, rect.Width, rect.Height, new Vector3(0.2f, 0.2f, 0.2f));
            _gpuRenderer.RenderQuad(centerX - rect.Width / 4, centerY - 1, rect.Width / 2, 2, new Vector3(0.8f, 0.8f, 0.8f));
            _gpuRenderer.RenderQuad(centerX - 1, centerY - rect.Height / 4, 2, rect.Height / 2, new Vector3(0.8f, 0.8f, 0.8f));
        }

        public void RenderGauge(Rectangle rect, float percentage)
        {
            // Background
            _gpuRenderer.RenderQuad(rect.X, rect.Y, rect.Width, rect.Height, new Vector3(0.2f, 0.2f, 0.2f));

            // Foreground (filled part)
            float fillHeight = rect.Height * (percentage / 100.0f);
            int fillY = rect.Y + rect.Height - (int)fillHeight;

            // Interpolate color from blue to red based on percentage
            var color = Vector3.Lerp(new Vector3(0, 0.5f, 1), new Vector3(1, 0.2f, 0.2f), percentage / 100.0f);

            _gpuRenderer.RenderQuad(rect.X, fillY, rect.Width, (int)fillHeight, color);

            // Border
            _gpuRenderer.RenderQuad(rect.X, rect.Y, 1, rect.Height, new Vector3(0.1f, 0.1f, 0.1f)); // Left
            _gpuRenderer.RenderQuad(rect.X + rect.Width - 1, rect.Y, 1, rect.Height, new Vector3(0.1f, 0.1f, 0.1f)); // Right
            _gpuRenderer.RenderQuad(rect.X, rect.Y, rect.Width, 1, new Vector3(0.1f, 0.1f, 0.1f)); // Top
            _gpuRenderer.RenderQuad(rect.X, rect.Y + rect.Height - 1, rect.Width, 1, new Vector3(0.1f, 0.1f, 0.1f)); // Bottom
        }
    }
}