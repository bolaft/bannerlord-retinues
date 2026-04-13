using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Settlements.Models;
using Retinues.Framework.Model;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Domain.Settlements.Wrappers
{
    public partial class WSettlement(Settlement @base) : WBase<WSettlement, Settlement>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Identity                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => Base.Name?.ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCulture Culture => WCulture.Get(Base.Culture);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Town                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public MTown Town => Base.Town != null ? new(Base.Town) : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Heroes                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WHero Owner => WHero.Get(Base.Owner);
        public List<WHero> Notables => [.. Base.Notables.Select(WHero.Get)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Factions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WClan Clan => WClan.Get(Base.OwnerClan);

        public WKingdom Kingdom
        {
            get
            {
                var clan = Base.OwnerClan;
                if (clan == null)
                    return null;

                return WKingdom.Get(clan.Kingdom);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Kind / Type                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsTown => Base.IsTown;
        public bool IsCastle => Base.IsCastle;
        public bool IsVillage => Base.IsVillage;
        public bool IsHideout => Base.IsHideout;
        public bool IsFortification => Base.IsFortification;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Village Village => Base.Village;
        public Hideout Hideout => Base.Hideout;
    }
}
