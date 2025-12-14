using TaleWorlds.Core;
using TaleWorlds.Library;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
#else
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

            // Safe copy (uses Banner(Banner) copy-ctor)
            var b = new Banner(src);

            // Index 0 is the background; scale the rest
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
                return bannerCode != null ? new ImageIdentifier(bannerCode) : null;

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
