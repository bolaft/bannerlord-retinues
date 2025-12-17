using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Characters;
using Retinues.Model.Parties;
using Retinues.Model.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model.Factions
{
    public abstract class BaseMapFaction<TWrapper, TFaction>(TFaction @base)
        : BaseFaction<TWrapper, TFaction>(@base)
        where TWrapper : BaseMapFaction<TWrapper, TFaction>
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
        //                         Faction                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IFaction MapFaction => Base.MapFaction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Heroes                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WHero Leader => WHero.Get(Base.Leader);

        public override List<WCharacter> RosterHeroes =>
            [.. Base.Heroes.Select(h => WHero.Get(h).Character).Where(c => c.Age >= 18)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Characters                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Cache culture roots for performance.
        static List<WCharacter> cultureRoots;

        /// <summary>
        /// Gets the custom root basic troop for this faction, if any.
        /// </summary>
        public override WCharacter RootBasic
        {
            get
            {
                var root = WCharacter.Get(Base.BasicTroop);
                if (root == null)
                    return null;

                // Cache culture roots.
                cultureRoots ??= [.. WCulture.All.Select(c => c.RootBasic).Where(r => r != null)];

                // A root shared with a culture is not a custom root.
                if (cultureRoots.Contains(root))
                    return null;

                return root;
            }
            // set
            // {
            //     // No set yet but remininder to invalidate caches if implemented.
            //     InvalidateTroopSourceFlagsCache();
            //     InvalidateTroopFactionsCache();
            // }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Territory                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IReadOnlyList<WSettlement> Settlements =>
            [.. Base.Settlements.Select(WSettlement.Get)];
        public IReadOnlyList<MTown> Fiefs => [.. Base.Fiefs.Select(f => new MTown(f))];

        public bool HasFiefs => Base.Fiefs != null && Base.Fiefs.Count > 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Parties                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IReadOnlyList<WParty> Parties =>
            [.. Base.WarPartyComponents.Select(c => WParty.Get(c.MobileParty))];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Flags & Stats                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsBanditFaction => Base.IsBanditFaction;
        public bool IsMinorFaction => Base.IsMinorFaction;
        public bool IsKingdomFaction => Base.IsKingdomFaction;
        public float TotalStrength =>
#if BL13
            Base.CurrentTotalStrength;
#else
            Base.TotalStrength;
#endif
        public bool IsEliminated => Base.IsEliminated;
    }
}
