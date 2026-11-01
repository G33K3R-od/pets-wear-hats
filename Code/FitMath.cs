using System;

namespace AnimalsWearHats {
    internal static class NativeHatIds {
        internal const string Definition = "Obj/Equipment/Hats/Hat01";
        internal const string Sprite = "Beings/Hats/Human/Hat01/Hat01";
        internal static bool Matches(string id) {
            if (string.IsNullOrEmpty(id)) return false;
            var key = id.Substring(id.LastIndexOf('/') + 1);
            return string.Equals(key, "Hat01", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "Hat", StringComparison.OrdinalIgnoreCase);
        }
    }
    internal static class HatFrames {
        internal static string[] Poses(bool vertical, bool alternate) => !vertical
            ? new[] { "_H", "" } : alternate ? new[] { "_1", "" } : new[] { "" };
    }
    internal static class SpeciesRules {
        internal static bool Supports(string species) => species == "Dog" || species == "Cat" || species == "Pig";
    }
    internal static class ColorHex {
        internal static string Normalize(string value) {
            if (value == null) return null;
            value = value.Trim();
            if (value.StartsWith("#", StringComparison.Ordinal)) value = value.Substring(1);
            if (value.Length != 6) return null;
            foreach (var c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return null;
            return "#" + value.ToUpperInvariant();
        }
    }
    internal static class FitMath {
        internal static float SafeScale(float value) => float.IsNaN(value) || float.IsInfinity(value)
            ? 0.30f : Math.Max(0.05f, Math.Min(2f, value));
        internal static float SafeOffset(float value) => float.IsNaN(value) || float.IsInfinity(value)
            ? 0f : Math.Max(-2f, Math.Min(2f, value));
    }
}
