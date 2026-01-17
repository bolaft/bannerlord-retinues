using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Utilities;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Troop Type                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public TroopSourceFlags SourceFlags => SourceFlagCache.Get(this);

        /// <summary>
        /// Invalidates all troop source caches.
        /// </summary>
        public static void InvalidateTroopSourceCaches()
        {
            FactionCache.Invalidate();
            SourceFlagCache.Invalidate();
            TreeFlagCache.Invalidate();

            // Also invalidate retinue conversion sources/targets caches.
            CacheRegistry.ClearGroup(ConversionCacheGroupKey);
        }

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Custom Tree Flag                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// True if this troop belongs to a custom map-faction tree (retinues or custom clan/kingdom roots).
        /// This is independent from IsCustom.
        /// </summary>
        public bool IsFactionTroop => TreeFlagCache.Get(this);
    }
}
