using Retinues.Game.Wrappers;
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
            banner ??= CreateRandomBanner(culture);

            if (scale != 1.0f)
                banner = ScaleBannerIcon(banner, scale);

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
            Banner banner = null;

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
                return null; // no culture, no bannerbanner = Banner.CreateRandomClanBanner(Seed.FromString(culture.StringId ?? "null"));

            Banner banner = new(); // empty;

            try
            {
                // Base banner on the culture's root troop banner
                var root = culture.RootBasic ?? culture.RootElite;

                foreach (var troop in root.Tree)
                {
#if BL13
                    if (troop?.Culture?.Base?.Banner != null)
                    {
                        banner = troop.Culture.Base.Banner;
                        break;
                    }
#else
                    if (!string.IsNullOrEmpty(troop?.Culture?.Base?.BannerKey))
                    {
                        banner = new Banner(troop.Culture.Base.BannerKey);
                        break;
                    }
#endif
                }

                // Swap primary/secondary colors for culture banners
                banner.ChangePrimaryColor(banner.GetSecondaryColor());
                banner.ChangeBackgroundColor(banner.GetSecondaryColor(), banner.GetPrimaryColor());
            }
            catch
            { /* ignore */
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
    }
}
