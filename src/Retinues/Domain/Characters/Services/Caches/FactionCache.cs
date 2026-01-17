using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;

namespace Retinues.Domain.Characters.Services.Caches
{
    /// <summary>
    /// Caches the factions for troops based on their presence in faction rosters.
    /// </summary>
    [SafeClass]
    public static class FactionCache
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Troop Faction Cache                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly object Sync = new();

        private static bool _built;
        private static readonly Dictionary<string, List<IBaseFaction>> ByTroopId = [];

        /// <summary>
        /// Invalidates the cache.
        /// </summary>
        [StaticClearAction]
        public static void Invalidate()
        {
            lock (Sync)
            {
                ByTroopId.Clear();
                _built = false;
            }
        }

        /// <summary>
        /// Gets the factions for the given wrapped character.
        /// </summary>
        public static List<IBaseFaction> Get(WCharacter wc)
        {
            if (wc == null)
                return [];

            EnsureBuilt();

            var id = wc.StringId;
            if (string.IsNullOrEmpty(id))
                return [];

            lock (Sync)
            {
                return ByTroopId.TryGetValue(id, out var list) ? [.. list] : [];
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

                BuildLocked();
                _built = true;
            }
        }

        /// <summary>
        /// Builds the cache.
        /// </summary>
        private static void BuildLocked()
        {
            ByTroopId.Clear();

            IndexMany(WCulture.All);
            IndexMany(WKingdom.All);
            IndexMany(WClan.All);
        }

        /// <summary>
        /// Indexes many factions into the cache.
        /// </summary>
        private static void IndexMany<TFaction>(IEnumerable<TFaction> factions)
            where TFaction : IBaseFaction
        {
            if (factions == null)
                return;

            foreach (var faction in factions)
            {
                if (faction == null)
                    continue;

                foreach (var troop in faction.Troops)
                {
                    if (troop == null)
                        continue;

                    var id = troop.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (!ByTroopId.TryGetValue(id, out var list))
                    {
                        list = [];
                        ByTroopId.Add(id, list);
                    }

                    AddUnique(list, faction);
                }
            }
        }

        /// <summary>
        /// Adds a faction to the list if not already present.
        /// </summary>
        private static void AddUnique(List<IBaseFaction> list, IBaseFaction faction)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Equals(faction))
                    return;

            list.Add(faction);
        }
    }
}
