using System;

namespace AnimalsWearHats {
    internal static class FitMath {
        internal static float SafeScale(float value) => float.IsNaN(value) || float.IsInfinity(value)
            ? 0.30f : Math.Max(0.05f, Math.Min(2f, value));
        internal static float SafeOffset(float value) => float.IsNaN(value) || float.IsInfinity(value)
            ? 0f : Math.Max(-2f, Math.Min(2f, value));
    }
}
