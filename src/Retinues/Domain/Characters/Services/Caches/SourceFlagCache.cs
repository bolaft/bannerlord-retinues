using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;

namespace Retinues.Domain.Characters.Services.Caches
{
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

    /// <summary>
    /// Caches the source flags for troops based on their presence in faction rosters.
    /// </summary>
    [SafeClass]
    public static class SourceFlagCache
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Troop Source Flag Cache                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly object Sync = new();

        private static bool _built;
        private static readonly Dictionary<string, TroopSourceFlags> ById = [];

        /// <summary>
        /// Invalidates the cache.
        /// </summary>
        [StaticClearAction]
        public static void Invalidate()
        {
            lock (Sync)
            {
                ById.Clear();
                _built = false;
            }
        }

        /// <summary>
        /// Gets the source flags for the given wrapped character.
        /// </summary>
        public static TroopSourceFlags Get(WCharacter wc)
        {
            if (wc == null)
                return TroopSourceFlags.None;

            EnsureBuilt();

            var id = wc.StringId;
            if (string.IsNullOrEmpty(id))
                return TroopSourceFlags.None;

            lock (Sync)
            {
                return ById.TryGetValue(id, out var flags) ? flags : TroopSourceFlags.None;
            }
        }

        /// <summary>
        /// Ensures the cache is built.
        /// </summary>
        private static void EnsureBuilt()
        {
            if (_built)
                return;

            lock (Sync)
            {
                if (_built)
                    return;

                ById.Clear();

                // Culture rosters are the canonical classification layer.
                foreach (var culture in WCulture.All)
                {
                    MarkMany(culture.RosterBasic, TroopSourceFlags.Basic);
                    MarkMany(culture.RosterElite, TroopSourceFlags.Elite);

                    MarkMany(culture.RosterMercenary, TroopSourceFlags.Mercenary);
                    MarkMany(culture.RosterBandit, TroopSourceFlags.Bandit);
                    MarkMany(culture.RosterMilitia, TroopSourceFlags.Militia);
                    MarkMany(culture.RosterCaravan, TroopSourceFlags.Caravan);
                    MarkMany(culture.RosterVillager, TroopSourceFlags.Villager);
                    MarkMany(culture.RosterCivilian, TroopSourceFlags.Civilian);
                }

                // Same for clan rosters.
                foreach (var clan in WClan.All)
                {
                    MarkMany(clan.RosterBasic, TroopSourceFlags.Basic);
                    MarkMany(clan.RosterElite, TroopSourceFlags.Elite);
                }

                // Same for kingdom rosters.
                foreach (var kingdom in WKingdom.All)
                {
                    MarkMany(kingdom.RosterBasic, TroopSourceFlags.Basic);
                    MarkMany(kingdom.RosterElite, TroopSourceFlags.Elite);
                }

                // Retinues live on map-factions (clans/kingdoms).
                foreach (var clan in WClan.All)
                    MarkMany(clan.RosterRetinues, TroopSourceFlags.Retinue);

                foreach (var kingdom in WKingdom.All)
                    MarkMany(kingdom.RosterRetinues, TroopSourceFlags.Retinue);

                _built = true;
            }
        }

        /// <summary>
        /// Marks many wrapped characters with the given flags.
        /// </summary>
        private static void MarkMany(IEnumerable<WCharacter> list, TroopSourceFlags flags)
        {
            if (list == null)
                return;

            foreach (var wc in list)
                Mark(wc, flags);
        }

        /// <summary>
        /// Marks a wrapped character with the given flags.
        /// </summary>
        private static void Mark(WCharacter wc, TroopSourceFlags flags)
        {
            if (wc == null)
                return;

            var id = wc.StringId;
            if (string.IsNullOrEmpty(id))
                return;

            if (ById.TryGetValue(id, out var existing))
                ById[id] = existing | flags;
            else
                ById.Add(id, flags);
        }
    }
}
