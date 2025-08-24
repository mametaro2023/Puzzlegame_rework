using System;

namespace MyPuzzleGame.SystemUtils
{
    public static class Easing
    {
        public static float Linear(float t) => t;

        // Quadratic easing functions
        public static class Quad
        {
            public static float In(float t) => t * t;
            public static float Out(float t) => 1f - In(1f - t);
            public static float InOut(float t) => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
        }

        // Cubic easing functions
        public static class Cubic
        {
            public static float In(float t) => t * t * t;
            public static float Out(float t) => 1f - In(1f - t);
            public static float InOut(float t) => t < 0.5f ? 4f * t * t * t : 1f - 4f * (1f - t) * (1f - t) * (1f - t);
        }

        // Quartic easing functions
        public static class Quart
        {
            public static float In(float t) => t * t * t * t;
            public static float Out(float t) => 1f - In(1f - t);
            public static float InOut(float t) => t < 0.5f ? 8f * t * t * t * t : 1f - 8f * (1f - t) * (1f - t) * (1f - t) * (1f - t);
        }

        // Quintic easing functions
        public static class Quint
        {
            public static float In(float t) => t * t * t * t * t;
            public static float Out(float t) => 1f - In(1f - t);
            public static float InOut(float t) => t < 0.5f ? 16f * t * t * t * t * t : 1f - 16f * (1f - t) * (1f - t) * (1f - t) * (1f - t) * (1f - t);
        }

        // Sine easing functions
        public static class Sine
        {
            public static float In(float t) => 1f - (float)Math.Cos(t * Math.PI / 2f);
            public static float Out(float t) => (float)Math.Sin(t * Math.PI / 2f);
            public static float InOut(float t) => 0.5f * (1f - (float)Math.Cos(t * Math.PI));
        }

        // Exponential easing functions
        public static class Expo
        {
            public static float In(float t) => t == 0f ? 0f : (float)Math.Pow(2f, 10f * (t - 1f));
            public static float Out(float t) => t == 1f ? 1f : 1f - (float)Math.Pow(2f, -10f * t);
            public static float InOut(float t)
            {
                if (t == 0f) return 0f;
                if (t == 1f) return 1f;
                return t < 0.5f ? 0.5f * (float)Math.Pow(2f, 20f * t - 10f) : 1f - 0.5f * (float)Math.Pow(2f, -20f * t + 10f);
            }
        }

        // Circular easing functions
        public static class Circ
        {
            public static float In(float t) => 1f - (float)Math.Sqrt(1f - t * t);
            public static float Out(float t) => (float)Math.Sqrt(1f - (1f - t) * (1f - t));
            public static float InOut(float t) => t < 0.5f ? 0.5f * (1f - (float)Math.Sqrt(1f - 4f * t * t)) : 0.5f * ((float)Math.Sqrt(1f - 4f * (1f - t) * (1f - t)) + 1f);
        }

        // Back easing functions (overshoot)
        public static class Back
        {
            private const float c1 = 1.70158f;
            private const float c2 = c1 * 1.525f;
            private const float c3 = c1 + 1f;

            public static float In(float t) => c3 * t * t * t - c1 * t * t;
            public static float Out(float t) => 1f + c3 * (t - 1f) * (t - 1f) * (t - 1f) + c1 * (t - 1f) * (t - 1f);
            public static float InOut(float t) => t < 0.5f ? 2f * t * t * ((c2 + 1f) * 2f * t - c2) : 1f + 2f * (t - 1f) * (t - 1f) * ((c2 + 1f) * 2f * (t - 1f) + c2);
        }

        // Elastic easing functions
        public static class Elastic
        {
            private const float c4 = (2f * (float)Math.PI) / 3f;
            private const float c5 = (2f * (float)Math.PI) / 4.5f;

            public static float In(float t) => t == 0f ? 0f : t == 1f ? 1f : -(float)Math.Pow(2f, 10f * t - 10f) * (float)Math.Sin((t * 10f - 10.75f) * c4);
            public static float Out(float t) => t == 0f ? 0f : t == 1f ? 1f : (float)Math.Pow(2f, -10f * t) * (float)Math.Sin((t * 10f - 0.75f) * c4) + 1f;
            public static float InOut(float t)
            {
                if (t == 0f) return 0f;
                if (t == 1f) return 1f;
                return t < 0.5f 
                    ? -0.5f * (float)Math.Pow(2f, 20f * t - 10f) * (float)Math.Sin((20f * t - 11.125f) * c5)
                    : 0.5f * (float)Math.Pow(2f, -20f * t + 10f) * (float)Math.Sin((20f * t - 11.125f) * c5) + 1f;
            }
        }

        // Bounce easing functions
        public static class Bounce
        {
            public static float In(float t) => 1f - Out(1f - t);
            
            public static float Out(float t)
            {
                const float n1 = 7.5625f;
                const float d1 = 2.75f;

                if (t < 1f / d1) return n1 * t * t;
                if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
                if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
            
            public static float InOut(float t) => t < 0.5f ? 0.5f * In(2f * t) : 0.5f * Out(2f * t - 1f) + 0.5f;
        }
    }

    // Animation helper class
    public class Animator
    {
        public float Duration { get; set; }
        public float StartValue { get; set; }
        public float EndValue { get; set; }
        public Func<float, float> EasingFunction { get; set; }
        
        private float _elapsedTime = 0f;
        private bool _isCompleted = false;

        public Animator(float duration, float startValue, float endValue, Func<float, float>? easingFunction = null)
        {
            Duration = duration;
            StartValue = startValue;
            EndValue = endValue;
            EasingFunction = easingFunction ?? Easing.Linear;
        }

        public float Update(float deltaTime)
        {
            if (_isCompleted) return EndValue;

            _elapsedTime += deltaTime;
            
            if (_elapsedTime >= Duration)
            {
                _elapsedTime = Duration;
                _isCompleted = true;
            }

            float t = Duration > 0f ? _elapsedTime / Duration : 1f;
            float easedT = EasingFunction(t);
            
            return StartValue + (EndValue - StartValue) * easedT;
        }

        public bool IsCompleted => _isCompleted;
        
        public void Reset()
        {
            _elapsedTime = 0f;
            _isCompleted = false;
        }

        public void Reset(float startValue, float endValue)
        {
            StartValue = startValue;
            EndValue = endValue;
            Reset();
        }
    }

    // Interpolation utilities
    public static class Interpolation
    {
        public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);
        
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        
        public static float InverseLerp(float a, float b, float value)
        {
            if (Math.Abs(a - b) < float.Epsilon) return 0f;
            return Math.Clamp((value - a) / (b - a), 0f, 1f);
        }

        public static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        public static float SmootherStep(float edge0, float edge1, float x)
        {
            float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }
    }
}