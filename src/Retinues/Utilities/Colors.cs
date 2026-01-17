using TaleWorlds.Library;

namespace Retinues.Utilities
{
    /// <summary>
    /// Color utilities for UI elements.
    /// </summary>
    public static class Colors
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Constants                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // A warm neutral (slightly yellow) blends better with Bannerlord UI.
        // This avoids reds drifting toward pink/purple when tinted.
        public static readonly Color UiNeutral = new(0.92f, 0.90f, 0.84f);

        // Brighter, more "UI muted" endpoints.
        public static readonly Color MutedGreen = new(0.33f, 0.72f, 0.40f);

        // Warmer red (less blue influence) so tinting doesn't go magenta/purple.
        public static readonly Color MutedRed = new(0.86f, 0.36f, 0.22f);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Proximity Gradients                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns a color based on ratio proximity to a limit.
        /// - ratio: 0 far from limit, 1 at limit, >1 over limit.
        /// - start: where the color shift begins (default 0.70).
        /// - end: where the shift reaches the "near/at" color (default 1.00).
        /// </summary>
        public static Color ProximityLimitColor(
            float ratio,
            float start = 0.70f,
            float end = 1.00f,
            Color? far = null,
            Color? near = null,
            float saturation = 0.72f,
            float tintStrength = 0.90f
        )
        {
            if (end <= start)
                end = start + 0.0001f;

            float t = Clamp01((ratio - start) / (end - start));

            var c0 = far ?? MutedGreen;
            var c1 = near ?? MutedRed;

            var baseColor = Lerp(c0, c1, t);
            return UiMute(baseColor, saturation, tintStrength);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Color Utilities                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Converts a uint color to a hex string, optionally lightening it toward white.
        /// </summary>
        public static string UintColorToHex(uint color, float towardWhite = 0.0f)
        {
            // 0xAARRGGBB
            byte a = (byte)((color >> 24) & 0xFF);
            byte r = (byte)((color >> 16) & 0xFF);
            byte g = (byte)((color >> 8) & 0xFF);
            byte b = (byte)(color & 0xFF);

            // clamp 0..1
            if (towardWhite < 0f)
                towardWhite = 0f;
            if (towardWhite > 1f)
                towardWhite = 1f;

            r = (byte)(r + (255 - r) * towardWhite);
            g = (byte)(g + (255 - g) * towardWhite);
            b = (byte)(b + (255 - b) * towardWhite);

            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        /// <summary>
        /// Calculates the luma (perceived brightness) of a color.
        /// </summary>
        public static float Luma(Color c)
        {
            // Linear RGB luma (good enough for UI tinting).
            return (c.Red * 0.2126f) + (c.Green * 0.7152f) + (c.Blue * 0.0722f);
        }

        /// <summary>
        /// Converts a color to grayscale based on luma.
        /// </summary>
        public static Color ToGray(Color c)
        {
            float l = Luma(c);
            return new Color(l, l, l, c.Alpha);
        }

        /// <summary>
        /// Desaturates a color by lerping between grayscale and original.
        /// saturation=0 -> fully gray, saturation=1 -> original color.
        /// </summary>
        public static Color WithSaturation(Color c, float saturation)
        {
            saturation = Clamp01(saturation);
            return Lerp(ToGray(c), c, saturation);
        }

        /// <summary>
        /// Tints a color toward a neutral. strength=0 -> neutral, strength=1 -> original.
        /// </summary>
        public static Color TintFrom(Color neutral, Color c, float strength)
        {
            strength = Clamp01(strength);
            return Lerp(neutral.WithAlpha(c.Alpha), c, strength);
        }

        /// <summary>
        /// Produces a UI-friendly muted color by desaturating and tinting toward UiNeutral.
        /// </summary>
        public static Color UiMute(Color c, float saturation = 0.72f, float tintStrength = 0.90f)
        {
            var desat = WithSaturation(c, saturation);
            return TintFrom(UiNeutral, desat, tintStrength);
        }

        /// <summary>
        /// Returns a copy of the color with the given alpha.
        /// </summary>
        public static Color WithAlpha(this Color c, float alpha)
        {
            return new Color(c.Red, c.Green, c.Blue, alpha);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Math Helpers                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clamps a float value to the 0..1 range.
        /// </summary>
        public static float Clamp01(float v)
        {
            if (v < 0f)
                return 0f;
            if (v > 1f)
                return 1f;
            return v;
        }

        /// <summary>
        /// Linearly interpolates between two values by t (clamped 0..1).
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            t = Clamp01(t);
            return a + (b - a) * t;
        }

        /// <summary>
        /// Linearly interpolates between two colors by t (clamped 0..1).
        /// </summary>
        public static Color Lerp(Color a, Color b, float t)
        {
            t = Clamp01(t);
            return new Color(
                Lerp(a.Red, b.Red, t),
                Lerp(a.Green, b.Green, t),
                Lerp(a.Blue, b.Blue, t),
                Lerp(a.Alpha, b.Alpha, t)
            );
        }
    }
}
