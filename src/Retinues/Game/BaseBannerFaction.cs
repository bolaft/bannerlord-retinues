using Retinues.Utils;
using TaleWorlds.Core;
# if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Game
{
    /// <summary>
    /// Abstract base for factions that have banners.
    /// </summary>
    [SafeClass]
    public abstract class BaseBannerFaction : BaseFaction
    {
        /// <summary>
        /// The raw banner this faction exposes (may be null or "empty").
        /// </summary>
        public abstract Banner BaseBanner { get; }

        /// <summary>
        /// Returns a banner suitable for display.
        /// </summary>
        public virtual Banner Banner
        {
            get
            {
                var banner = BaseBanner;

                if (IsEmptyBanner(banner))
                    banner = CreateFallbackBanner(Culture.Base);

                return banner;
            }
        }

#if BL13
        /// <summary>
        /// Gets a banner image identifier VM for this faction's banner, scaled by the given factor.
        /// </summary>
        public BannerImageIdentifierVM GetBannerImage(float scale = 1.0f)
        {
            var banner = GetScaledBanner(scale);
            return banner != null ? new BannerImageIdentifierVM(banner, nineGrid: true) : null;
        }
#else
        /// <summary>
        /// Gets a banner image identifier VM for this faction's banner, scaled by the given factor.
        /// </summary>
        public ImageIdentifierVM GetBannerImage(float scale = 1.0f)
        {
            var banner = GetScaledBanner(scale);
            return banner != null
                ? new ImageIdentifierVM(BannerCode.CreateFrom(banner), nineGrid: true)
                : null;
        }
#endif

        /// <summary>
        /// Returns a banner suitable for display, with icons scaled by the given factor.
        /// </summary>
        public Banner GetScaledBanner(float scale)
        {
            if (scale == 1.0f)
                return Banner;

            return Banner != null ? ScaleBannerIcon(Banner, scale) : null;
        }

        /// <summary>
        /// Fallback banner when raw banner is null or empty.
        /// </summary>
        protected virtual Banner CreateFallbackBanner()
        {
            return Banner.CreateRandomClanBanner();
        }

        /// <summary>
        /// Determines if the given banner is empty or invalid.
        /// </summary>
        protected static bool IsEmptyBanner(Banner banner)
        {
            if (banner == null)
                return true;

            try
            {
                var list = banner.BannerDataList;
                if (list == null || list.Count == 0)
                    return true;

                // If the primary color is "invalid", Banner reports uint.MaxValue.
                if (banner.GetPrimaryColor() == uint.MaxValue)
                    return true;
            }
            catch
            {
                // If BannerDataList access explodes, just treat it as empty/suspect
                return true;
            }

            return false;
        }

        /// <summary>
        /// Scales the icons of the given banner by the specified factor.
        /// </summary>
        protected static Banner ScaleBannerIcon(Banner src, float scale)
        {
            if (src == null || scale == 1.0f)
                return src;

            // Safe copy (uses Banner(Banner) copy-ctor)
            var b = new Banner(src);

            // Index 0 is the background; scale the rest
            var list = b.BannerDataList;
            for (int i = 1; i < list.Count; i++)
            {
                var d = list[i];
                d.Size *= scale;
            }

            return b;
        }

        /// <summary>
        /// Culture-specific fallback banner when the raw banner is null/empty.
        /// Uses the culture's troop tree as a template and recolors with culture colors.
        /// </summary>
        protected Banner CreateFallbackBanner(BasicCultureObject basicCulture)
        {
            if (basicCulture == null)
                return Banner.CreateRandomClanBanner();

            Log.Debug($"Creating random banner for culture {StringId}");

            // Start from a guaranteed non-empty random banner.
            Banner banner = Banner.CreateRandomClanBanner();

            try
            {
                // Base banner on the culture's root troop banner if possible
                var root = RootBasic ?? RootElite;

                Log.Debug($"Using root troop {root?.StringId} for culture {StringId}");

                if (root != null)
                {
                    foreach (var troop in root.Tree)
                    {
#if BL13
                        var troopBanner = troop?.Culture?.Base?.Banner;
                        if (!IsEmptyBanner(troopBanner))
                        {
                            Log.Debug(
                                $"Found banner from troop {troop.StringId} for culture {StringId}"
                            );
                            // copy to avoid mutating the original
                            banner = new Banner(troopBanner);
                            break;
                        }
#else
                        var troopBannerKey = troop?.Culture?.Base?.BannerKey;
                        if (!string.IsNullOrEmpty(troopBannerKey))
                        {
                            Log.Debug(
                                $"Found banner key from troop {troop.StringId} for culture {StringId}"
                            );
                            banner = new Banner(troopBannerKey);
                            break;
                        }
#endif
                    }
                }

                if (!IsEmptyBanner(banner))
                {
                    try
                    {
                        // 1) Copy the design from the troop banner
                        var code = banner.Serialize();
                        banner = new Banner(code); // copy of design, original colors

                        // 2) Compute culture colors (swapped)
                        var (primary, secondary) = GetSafeCultureColors(basicCulture, banner);

                        // 3) Apply them explicitly:
                        //    - background = (primary, secondary)
                        //    - icons = secondary
                        if (
                            BannerManager.GetColorId(primary) >= 0
                            && BannerManager.GetColorId(secondary) >= 0
                        )
                        {
                            banner.ChangeBackgroundColor(primary, secondary);
                            banner.ChangeIconColors(secondary);
                        }
                    }
                    catch
                    {
                        // ignore recolor errors
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
            }

            return banner;
        }

        /// <summary>
        /// Get safe culture colors, falling back to banner colors if needed.
        /// </summary>
        private static (uint primary, uint secondary) GetSafeCultureColors(
            BasicCultureObject culture,
            Banner fallbackBanner
        )
        {
            // Prefer culture's main faction colors
            uint primary = culture.Color2; // swapped on purpose
            uint secondary = culture.Color;

            // If those are not valid palette colors, fall back to banner's own colors
            if (primary == uint.MaxValue || BannerManager.GetColorId(primary) < 0)
                primary =
                    fallbackBanner?.GetFirstIconColor()
                    ?? fallbackBanner?.GetPrimaryColor()
                    ?? uint.MaxValue;

            if (secondary == uint.MaxValue || BannerManager.GetColorId(secondary) < 0)
                secondary = fallbackBanner?.GetPrimaryColor() ?? primary;

            // Final guard: if we still have invalids, just keep using the banner's own colors
            if (primary == uint.MaxValue || BannerManager.GetColorId(primary) < 0)
                primary = fallbackBanner?.GetPrimaryColor() ?? 0;

            if (secondary == uint.MaxValue || BannerManager.GetColorId(secondary) < 0)
                secondary = fallbackBanner?.GetFirstIconColor() ?? primary;

            return (primary, secondary);
        }
    }
}
