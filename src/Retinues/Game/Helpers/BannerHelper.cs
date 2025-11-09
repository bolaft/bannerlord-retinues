using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;

namespace Retinues.Game.Helpers
{
    public static class BannerHelper
    {
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
            {
                banner = new Banner(bannerKey);
            }
            else
            {
                banner = Banner.CreateRandomClanBanner(Seed.FromString(culture.StringId ?? "null"));

                try
                {
                    // Base banner on the culture's root troop banner
                    var root = culture.RootBasic ?? culture.RootElite;
                    banner = new Banner(); // empty

                    foreach (var troop in root.Tree)
                    {
                        if (!string.IsNullOrEmpty(troop?.Culture?.Base?.BannerKey))
                        {
                            banner = new Banner(troop.Culture.Base.BannerKey);
                            break;
                        }
                    }

                    // Swap primary/secondary colors for culture banners
                    banner.ChangePrimaryColor(banner.GetSecondaryColor());
                    banner.ChangeBackgroundColor(
                        banner.GetSecondaryColor(),
                        banner.GetPrimaryColor()
                    );
                }
                catch
                { /* ignore */
                }
            }

            if (scale != 1.0f)
                banner = ScaleBannerIcon(banner, scale);

            return banner != null
                ? new ImageIdentifierVM(BannerCode.CreateFrom(banner), nineGrid: true)
                : null;
        }

        public static Banner ScaleBannerIcon(Banner src, float scale)
        {
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
