using System;
using System.Collections.Generic;
using Retinues.Model.Factions;
using TaleWorlds.CampaignSystem;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
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

        public TroopSourceFlags SourceFlags => TroopSourceFlagCache.Get(this);

        public bool IsRetinue => (SourceFlags & TroopSourceFlags.Retinue) != 0;
        public bool IsElite => (SourceFlags & TroopSourceFlags.Elite) != 0;
        public bool IsBasic => (SourceFlags & TroopSourceFlags.Basic) != 0;
        public bool IsMercenary => (SourceFlags & TroopSourceFlags.Mercenary) != 0;
        public bool IsBandit => (SourceFlags & TroopSourceFlags.Bandit) != 0;
        public bool IsMilitia => (SourceFlags & TroopSourceFlags.Militia) != 0;
        public bool IsCaravan => (SourceFlags & TroopSourceFlags.Caravan) != 0;
        public bool IsVillager => (SourceFlags & TroopSourceFlags.Villager) != 0;
        public bool IsCivilian => (SourceFlags & TroopSourceFlags.Civilian) != 0;

        public static void InvalidateTroopSourceFlagsCache() => TroopSourceFlagCache.Invalidate();

        private static class TroopSourceFlagCache
        {
            private static readonly object Sync = new();

            private static bool _built;
            private static readonly Dictionary<string, TroopSourceFlags> ById = [];

            public static void Invalidate()
            {
                lock (Sync)
                {
                    ById.Clear();
                    _built = false;
                }
            }

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

            private static void EnsureBuilt()
            {
                if (_built)
                    return;

                lock (Sync)
                {
                    if (_built)
                        return;

                    ById.Clear();

                    // Culture rosters are already your canonical classification layer.
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

                    _built = true;
                }
            }

            private static void MarkMany(IEnumerable<WCharacter> list, TroopSourceFlags flags)
            {
                if (list == null)
                    return;

                foreach (var wc in list)
                    Mark(wc, flags);
            }

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Factions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<IBaseFaction> Factions => TroopFactionCache.Get(this);

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

        public static void InvalidateTroopFactionsCache() => TroopFactionCache.Invalidate();

        private static class TroopFactionCache
        {
            private static readonly object Sync = new();

            private static bool _built;
            private static readonly Dictionary<string, List<IBaseFaction>> ByTroopId = [];

            public static void Invalidate()
            {
                lock (Sync)
                {
                    ByTroopId.Clear();
                    _built = false;
                }
            }

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

            private static void BuildLocked()
            {
                ByTroopId.Clear();

                IndexMany(WCulture.All);
                IndexMany(WKingdom.All);
                IndexMany(WClan.All);
            }

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

            private static void AddUnique(List<IBaseFaction> list, IBaseFaction faction)
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Equals(faction))
                        return;

                list.Add(faction);
            }
        }
    }
}
