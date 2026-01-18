using System.Collections.Generic;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Factions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<IBaseFaction> Factions => FactionCache.Get(this);

        /// <summary>
        /// Gets the map faction this troop is assigned to, if any.
        /// </summary>
        public IBaseFaction AssignedMapFaction
        {
            get
            {
                var list = Factions;
                if (list == null || list.Count == 0)
                    return null;

                for (int i = 0; i < list.Count; i++)
                    if (list[i] is WKingdom)
                        return list[i];

                for (int i = 0; i < list.Count; i++)
                    if (list[i] is WClan)
                        return list[i];

                return null;
            }
        }

        /// <summary>
        /// Determines if this troop belongs to the given faction.
        /// </summary>
        public bool BelongsTo(IBaseFaction faction)
        {
            if (faction == null)
                return false;

            var list = Factions;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Equals(faction))
                    return true;

            return false;
        }
    }
}
