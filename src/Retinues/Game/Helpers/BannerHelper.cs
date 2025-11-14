using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
# if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Game.Helpers
{
    public static class BannerHelper
    {
#if BL13
        public static BannerImageIdentifierVM GetBannerImageFromCulture(
            WCulture culture,
            float scale = 1.0f
        )
        {
            if (culture == null || culture.Base == null)
                return null; // no culture, no banner

            Banner banner = culture.Base.Banner;
            Log.Debug($"Banner from culture {culture.StringId}: {banner}");

            if (IsEmptyBanner(banner))
            {
                Log.Debug($"Culture {culture.StringId} has no meaningful banner, generating one.");
                banner = CreateRandomBanner(culture) ?? Banner.CreateRandomClanBanner();
            }

            Log.Debug($"Using banner: {banner}");

            if (scale != 1.0f)
                banner = ScaleBannerIcon(banner, scale);

            Log.Debug($"Final banner after scaling: {banner}");

            return banner != null ? new BannerImageIdentifierVM(banner, nineGrid: true) : null;
        }
#else
        public static ImageIdentifierVM GetBannerImageFromCulture(
            WCulture culture,
            float scale = 1.0f
        )
        {
            if (culture == null || culture.Base == null)
                return null; // no culture, no banner

            // 1) Try CultureObject.BannerKey directly
            string bannerKey = culture.Base.BannerKey;

            // 2) Build the Banner
            Banner banner;

            if (!string.IsNullOrEmpty(bannerKey))
                banner = new Banner(bannerKey);
            else
                banner = CreateRandomBanner(culture);

            if (scale != 1.0f)
                banner = ScaleBannerIcon(banner, scale);

            return banner != null
                ? new ImageIdentifierVM(BannerCode.CreateFrom(banner), nineGrid: true)
                : null;
        }
#endif

        public static Banner CreateRandomBanner(WCulture culture)
        {
            if (culture == null || culture.Base == null)
                return null; // no culture, no banner

            Log.Debug($"Creating random banner for culture {culture.StringId}");

            // Start from a guaranteed non-empty random banner.
            Banner banner = Banner.CreateRandomClanBanner();

            try
            {
                // Base banner on the culture's root troop banner if possible
                var root = culture.RootBasic ?? culture.RootElite;

                Log.Debug($"Using root troop {root?.StringId} for culture {culture.StringId}");

                if (root != null)
                {
                    foreach (var troop in root.Tree)
                    {
#if BL13
                        var troopBanner = troop?.Culture?.Base?.Banner;
                        if (!IsEmptyBanner(troopBanner))
                        {
                            Log.Debug(
                                $"Found banner from troop {troop.StringId} for culture {culture.StringId}"
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
                                $"Found banner key from troop {troop.StringId} for culture {culture.StringId}"
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
                        // 1. Get the design as a code
                        var code = banner.Serialize(); // uses Banner.Serialize()

                        // 2. Grab the culture's faction colors and swap them
                        uint primary = culture.Base.Color2; // swapped
                        uint secondary = culture.Base.Color; // swapped

                        // 3. Rebuild a banner from the design, but with swapped colors
                        banner = new Banner(code, primary, secondary);
                    }
                    catch
                    { /* ignore recolor errors */
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
            }

            return banner;
        }

        public static Banner ScaleBannerIcon(Banner src, float scale)
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

        public static bool IsEmptyBanner(Banner banner)
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
    }
}
