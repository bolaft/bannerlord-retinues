using Retinues.Model.Characters;
using TaleWorlds.Core;
using TaleWorlds.Library;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
#endif

namespace Retinues.Helpers
{
    public static class Banners
    {
        private static object _emptyVm;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Placeholder                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static object EmptyVm => _emptyVm ??= SolidColorVm(0xFFFFFFFFu);

#if BL13
        public static BannerImageIdentifierVM EmptyImage => (BannerImageIdentifierVM)EmptyVm;
#else
        public static ImageIdentifierVM EmptyImage => (ImageIdentifierVM)EmptyVm;
#endif

        public static object SolidColorVm(uint argb, bool nineGrid = true)
        {
            var banner = SolidBackgroundBanner(argb, argb);
            return GetBannerImage(banner, scale: 1.0f, nineGrid: nineGrid) ?? EmptyVm;
        }

        public static Banner SolidBackgroundBanner(uint argb1, uint argb2)
        {
            EnsureBannerManager();

            int meshId = GetAnyBackgroundMeshIdSafe();
            int c1 = GetClosestBackgroundColorIdSafe(argb1);
            int c2 = GetClosestBackgroundColorIdSafe(argb2);

            var banner = new Banner();
            banner.AddIconData(
                new BannerData(
                    meshId,
                    c1,
                    c2,
                    new Vec2(Banner.BannerFullSize, Banner.BannerFullSize),
                    new Vec2(Banner.BannerFullSize / 2f, Banner.BannerFullSize / 2f),
                    drawStroke: false,
                    mirror: false,
                    rotationValue: 0f
                )
            );

            return banner;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Fallback Banners                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns banner if valid, otherwise returns a culture-based fallback banner.
        /// Colors are inverted (swapped) from the given troop culture.
        /// </summary>
        public static Banner GetOrFallbackBanner(
            Banner banner,
            WCharacter firstTroop,
            BasicCultureObject fallbackCulture = null
        )
        {
            if (!IsEmptyBanner(banner))
                return banner;

            BasicCultureObject firstTroopCulture = null;

            foreach (var troop in firstTroop.Tree)
            {
                if (!IsEmptyBanner(troop.Culture.Base.Banner))
                {
                    firstTroopCulture = troop.Culture?.Base;
                    break;
                }
            }

            var culture = firstTroopCulture ?? fallbackCulture;
            return CreateFallbackBannerFromCulture(culture);
        }

        /// <summary>
        /// Creates a non-empty banner based on the culture banner (if available),
        /// recolored using inverted culture colors: (Color2, Color).
        /// </summary>
        public static Banner CreateFallbackBannerFromCulture(BasicCultureObject culture)
        {
            EnsureBannerManager();

            // Guaranteed non-empty seed.
            Banner banner = Banner.CreateRandomClanBanner();

            try
            {
                // Try to use the culture's own banner design as template.
                var template = TryGetCultureTemplateBanner(culture);
                if (!IsEmptyBanner(template))
                    banner = new Banner(template);

                if (!IsEmptyBanner(banner))
                {
                    // Copy design again via serialize to avoid any accidental shared refs.
                    try
                    {
                        var code = banner.Serialize();
                        banner = new Banner(code);
                    }
                    catch
                    {
                        // ignore
                    }

                    // Invert colors (swap) using troop culture.
                    var (primary, secondary) = GetSafeInvertedCultureColors(culture, banner);

                    // Only apply if BannerManager recognizes them.
                    if (
                        BannerManager.GetColorId(primary) >= 0
                        && BannerManager.GetColorId(secondary) >= 0
                    )
                    {
                        try
                        {
                            // Background uses (primary, secondary), icons use secondary.
                            banner.ChangeBackgroundColor(primary, secondary);
                            banner.ChangeIconColors(secondary);
                        }
                        catch
                        {
                            // ignore recolor errors
                        }
                    }
                }
            }
            catch
            {
                // ignore, keep the random banner
            }

            return banner;
        }

        private static Banner TryGetCultureTemplateBanner(BasicCultureObject culture)
        {
            if (culture == null)
                return null;

#if BL13
            try
            {
                return culture.Banner;
            }
            catch
            {
                return null;
            }
#else
            try
            {
                var key = culture.BannerKey;
                return !string.IsNullOrEmpty(key) ? new Banner(key) : null;
            }
            catch
            {
                return null;
            }
#endif
        }

        private static bool IsEmptyBanner(Banner banner)
        {
            if (banner == null)
                return true;

            try
            {
                var list = banner.BannerDataList;
                if (list == null || list.Count == 0)
                    return true;

                // Banner uses uint.MaxValue as "invalid" in several cases.
                if (banner.GetPrimaryColor() == uint.MaxValue)
                    return true;
            }
            catch
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns (Color2, Color), falling back to banner colors if needed.
        /// </summary>
        private static (uint primary, uint secondary) GetSafeInvertedCultureColors(
            BasicCultureObject culture,
            Banner fallbackBanner
        )
        {
            // Inversion here means swap (same as old BaseBannerFaction).
            uint primary = culture != null ? culture.Color2 : uint.MaxValue;
            uint secondary = culture != null ? culture.Color : uint.MaxValue;

            if (primary == uint.MaxValue || BannerManager.GetColorId(primary) < 0)
                primary =
                    fallbackBanner?.GetFirstIconColor()
                    ?? fallbackBanner?.GetPrimaryColor()
                    ?? uint.MaxValue;

            if (secondary == uint.MaxValue || BannerManager.GetColorId(secondary) < 0)
                secondary = fallbackBanner?.GetPrimaryColor() ?? primary;

            if (primary == uint.MaxValue || BannerManager.GetColorId(primary) < 0)
                primary = fallbackBanner?.GetPrimaryColor() ?? 0;

            if (secondary == uint.MaxValue || BannerManager.GetColorId(secondary) < 0)
                secondary = fallbackBanner?.GetFirstIconColor() ?? primary;

            return (primary, secondary);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Scaling                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static Banner GetScaledBanner(Banner banner, float scale = 1.0f)
        {
            if (scale == 1.0f)
                return banner;

            return banner != null ? ScaleBannerIcons(banner, scale) : null;
        }

        public static Banner ScaleBannerIcons(Banner src, float scale)
        {
            if (src == null || scale == 1.0f)
                return src;

            var b = new Banner(src);

            var list = b.BannerDataList;
            for (int i = 1; i < list.Count; i++)
                list[i].Size *= scale;

            return b;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //               Image / Identifier Helpers               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        public static BannerImageIdentifierVM GetBannerImage(
            Banner banner,
            float scale = 1.0f,
            bool nineGrid = true
        )
        {
            var scaled = GetScaledBanner(banner, scale);
            return scaled != null ? new BannerImageIdentifierVM(scaled, nineGrid: nineGrid) : null;
        }

        public static ImageIdentifier GetImageIdentifier(
            Banner banner,
            float scale = 1.0f,
            bool nineGrid = false
        )
        {
            var scaled = GetScaledBanner(banner, scale);
            return scaled != null ? new BannerImageIdentifier(scaled, nineGrid: nineGrid) : null;
        }
#else
        public static ImageIdentifierVM GetBannerImage(
            Banner banner,
            float scale = 1.0f,
            bool nineGrid = true
        )
        {
            if (banner == null)
                return null;

            if (scale == 1.0f)
                return new ImageIdentifierVM(BannerCode.CreateFrom(banner), nineGrid: nineGrid);

            var scaled = GetScaledBanner(banner, scale);
            return scaled != null
                ? new ImageIdentifierVM(BannerCode.CreateFrom(scaled), nineGrid: nineGrid)
                : null;
        }

        public static ImageIdentifier GetImageIdentifier(
            BannerCode bannerCode,
            Banner banner = null,
            float scale = 1.0f,
            bool nineGrid = false
        )
        {
            if (scale == 1.0f)
                return bannerCode != null
                    ? new ImageIdentifier(bannerCode, nineGrid: nineGrid)
                    : null;

            var scaled = GetScaledBanner(banner, scale);
            return scaled != null
                ? new ImageIdentifier(BannerCode.CreateFrom(scaled), nineGrid: nineGrid)
                : null;
        }
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void EnsureBannerManager()
        {
            if (BannerManager.Instance == null)
                BannerManager.Initialize();
        }

        private static int GetAnyBackgroundMeshIdSafe()
        {
            var mgr = BannerManager.Instance;
            if (mgr == null)
                return 0;

            if (mgr.BaseBackgroundId > 0)
                return mgr.BaseBackgroundId;

            foreach (var group in mgr.BannerIconGroups)
            {
                if (!group.IsPattern)
                    continue;

                foreach (var kv in group.AllBackgrounds)
                    return kv.Key;
            }

            return 0;
        }

        private static int GetClosestBackgroundColorIdSafe(uint argb)
        {
            var palette = BannerManager.Instance.ReadOnlyColorPalette;
            if (palette == null || palette.Count == 0)
                return 0;

            int tr = (int)((argb >> 16) & 0xFF);
            int tg = (int)((argb >> 8) & 0xFF);
            int tb = (int)(argb & 0xFF);

            int bestId = 0;
            int bestDist = int.MaxValue;

            foreach (var kv in palette)
            {
                var entry = kv.Value;
                if (!entry.PlayerCanChooseForBackground)
                    continue;

                uint col = entry.Color;
                int r = (int)((col >> 16) & 0xFF);
                int g = (int)((col >> 8) & 0xFF);
                int b = (int)(col & 0xFF);

                int dr = r - tr;
                int dg = g - tg;
                int db = b - tb;
                int dist = (dr * dr) + (dg * dg) + (db * db);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestId = kv.Key;
                }
            }

            return bestId;
        }
    }
}
