using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Characters;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Model.Factions
{
    public class WClan(Clan @base) : BaseFaction<WClan, Clan>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string Name => Base.Name.ToString();
        public override uint Color => Base.Color;
        public override uint Color2 => Base.Color2;

        public override Banner Banner => Base.Banner;
#if BL12
        public override BannerCode BannerCode => BannerCode.CreateFrom(Banner);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Characters                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Heroes ━━━━━━━━ */

        public override List<WHero> RosterHeroes =>
            [.. Base.Heroes.ToList().Select(hero => new WHero(hero))];

        /* ━━━━━━━━━ Roots ━━━━━━━━ */

        public override WCharacter RootBasic => WCharacter.Get(Base.BasicTroop);
    }
}
