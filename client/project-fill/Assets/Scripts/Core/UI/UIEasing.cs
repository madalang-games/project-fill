using UnityEngine;

namespace Game.Core.UI
{
    public static class UIEasing
    {
        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseIn(float t) => t * t;
        public static float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float Sine(float t) => Mathf.Sin(t * Mathf.PI * 2f);
    }
}
