using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Characters;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model.Factions
{
    public abstract class BaseFactionWrapper<TWrapper, TFaction>(TFaction @base)
        : BaseFaction<TWrapper, TFaction>(@base)
        where TWrapper : BaseFactionWrapper<TWrapper, TFaction>
        where TFaction : MBObjectBase, IFaction
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string Name => Base.Name?.ToString();
        public override uint Color => Base.Color;
        public override uint Color2 => Base.Color2;
        public override Banner Banner => Base.Banner;

#if BL12
        public override BannerCode BannerCode => BannerCode.CreateFrom(Banner);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCulture Culture => WCulture.Get(Base.Culture);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Heroes                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WHero Leader => WHero.Get(Base.Leader);

        public override List<WHero> RosterHeroes => [.. Base.Heroes.Select(WHero.Get)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Characters                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Roots ━━━━━━━━ */

        public override WCharacter RootBasic => WCharacter.Get(Base.BasicTroop);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Territory                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IReadOnlyList<Settlement> Settlements => Base.Settlements;
        public IReadOnlyList<Town> Fiefs => Base.Fiefs;

        /// <summary>
        /// Convenience flag for "owns at least one fief".
        /// </summary>
        public bool HasFiefs => Base.Fiefs != null && Base.Fiefs.Count > 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       War Parties                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IReadOnlyList<WarPartyComponent> WarPartyComponents =>
            Base.WarPartyComponents;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Flags & Stats                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsBanditFaction => Base.IsBanditFaction;
        public bool IsMinorFaction => Base.IsMinorFaction;
        public bool IsKingdomFaction => Base.IsKingdomFaction;
        public bool IsClan => Base.IsClan;
        public bool IsOutlaw => Base.IsOutlaw;
        public bool IsMapFaction => Base.IsMapFaction;
        public float TotalStrength =>
#if BL13
            Base.CurrentTotalStrength;
#else
            Base.TotalStrength;
#endif
        public bool IsEliminated => Base.IsEliminated;
        public float Aggressiveness => Base.Aggressiveness;
    }
}
