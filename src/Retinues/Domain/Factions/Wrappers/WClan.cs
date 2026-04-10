using System.Collections.Generic;
using Retinues.Domain.Factions.Base;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Domain.Factions.Wrappers
{
    public sealed class WClan(Clan @base) : BaseMapFaction<WClan, Clan>(@base)
    {
        public static new WClan Get(string stringId) => GetFromCampaign(stringId, () => Clan.All);

        public static new IEnumerable<WClan> All => AllFromCampaign(() => Clan.All);

        public WKingdom Kingdom => WKingdom.Get(Base.Kingdom);

        /// <summary>
        /// Gets the vanilla Bannerlord basic troop root for this clan, if it differs from the
        /// culture's own basic root. Used by the Universal Editor to expose vanilla clan-specific
        /// troop trees that are not tracked as mod-created custom roots.
        /// </summary>
        public WCharacter VanillaRootBasic
        {
            get
            {
                var co = Base.BasicTroop;
                if (co == null)
                    return null;

                var wc = WCharacter.Get(co);
                if (wc == null)
                    return null;

                // Exclude if it's the same troop as the culture's basic root.
                var cultureBasic = Culture?.RootBasic;
                if (cultureBasic != null && cultureBasic.StringId == wc.StringId)
                    return null;

                return wc;
            }
        }
    }
}
