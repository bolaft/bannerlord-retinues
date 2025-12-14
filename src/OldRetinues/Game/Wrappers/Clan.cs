using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
#endif

namespace OldRetinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for CultureObject, exposing troop roots and militia for custom logic.
    /// </summary>
    [SafeClass]
    public class WClan(Clan clan) : BaseBannerFaction
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IEnumerable<WClan> All
        {
            get
            {
                foreach (
                    var clan in Clan
                        .All.OrderBy(c => c.Culture.ToString())
                        .ThenBy(c => c.Name.ToString())
                )
                    if (clan != null)
                        yield return new WClan(clan);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Clan _clan = clan;
        public Clan Base => _clan;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string Name => Base?.Name?.ToString();
        public override string StringId => Base?.StringId ?? Name; // Some cultures have no StringId?
        public override uint Color => Base?.Color ?? 0;
        public override uint Color2 => Base?.Color2 ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsPlayerClan => Base != null && Base.StringId == Clan.PlayerClan.StringId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Banner                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override Banner BaseBanner => Base?.Banner;

#if BL13
        public BannerImageIdentifier Image =>
            Base.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
        public ImageIdentifier ImageIdentifier =>
            Base.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
#else
        public BannerCode BannerCode => BannerCode.CreateFrom(Base.Banner);
        public ImageIdentifierVM Image => new(BannerCode);
        public ImageIdentifier ImageIdentifier => new(BannerCode);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override WCharacter RootBasic
        {
            get
            {
                if (IsPlayerClan)
                    return null; // Player clan is handled differently

                var co = Base.BasicTroop;

                if (co.StringId == Culture.RootBasic.StringId)
                    return null;

                if (co == null)
                    return null;

                return new WCharacter(co);
            }
        }

        public override List<WHero> Heroes
        {
            get
            {
                var heroes = new List<WHero>();
                foreach (var hero in Base.Heroes)
                {
                    if (hero == null)
                        continue;

                    var wc = new WHero(hero);

                    if (!wc.IsValid)
                        continue;

                    if (wc.HiddenInEncyclopedia)
                        continue; // Skip hidden heroes

                    if (wc.Skills.Sum(kv => kv.Value) == 0)
                        continue; // Skip unskilled heroes (probably not fully initialized)

                    heroes.Add(wc);
                }
                return heroes;
            }
        }
    }
}
