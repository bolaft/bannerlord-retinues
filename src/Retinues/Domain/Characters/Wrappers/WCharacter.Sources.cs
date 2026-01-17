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

        [Flags]
        public enum TroopSourceFlags
        {
            None = 0,
            Basic = 1 << 0,
            Elite = 1 << 1,
            Retinue = 1 << 2,
            Mercenary = 1 << 3,
            Bandit = 1 << 4,
            Militia = 1 << 5,
            Caravan = 1 << 6,
            Villager = 1 << 7,
            Civilian = 1 << 8,
        }

        public TroopSourceFlags SourceFlags => SourceFlagCache.Get(this);

        public bool IsRetinue => (SourceFlags & TroopSourceFlags.Retinue) != 0;
        public bool IsElite => (SourceFlags & TroopSourceFlags.Elite) != 0;
        public bool IsBasic => (SourceFlags & TroopSourceFlags.Basic) != 0;
        public bool IsMercenary => (SourceFlags & TroopSourceFlags.Mercenary) != 0;
        public bool IsBandit => (SourceFlags & TroopSourceFlags.Bandit) != 0;
        public bool IsMilitia => (SourceFlags & TroopSourceFlags.Militia) != 0;
        public bool IsCaravan => (SourceFlags & TroopSourceFlags.Caravan) != 0;
        public bool IsVillager => (SourceFlags & TroopSourceFlags.Villager) != 0;
        public bool IsCivilian => (SourceFlags & TroopSourceFlags.Civilian) != 0;

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
