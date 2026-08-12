using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers
{
    /// <summary>
    /// Discovers troop trees that exist outside every culture's canonical rosters.
    ///
    /// Overhaul mods (e.g. Realm of Thrones' Household Troops) ship secondary soldier trees for a
    /// culture that none of its fields (basic, elite, militia, caravan, villager, mercenary,
    /// bandit, civilian) reference, making them invisible to the editor. A troop qualifies as an
    /// extra root when it is a non-hero, non-custom Soldier with a culture, is shown in the
    /// encyclopedia (filters out quest/template troops), is not reachable from any canonical
    /// group, and no other troop upgrades into it.
    /// </summary>
    [SafeClass]
    public static class ExtraTroopHelper
    {
        private static readonly object Sync = new();

        private static bool _built;
        private static readonly Dictionary<string, List<WCharacter>> RootsByCulture = new(
            StringComparer.Ordinal
        );

        /// <summary>
        /// Clears the cache (called from the SubModule static reset).
        /// </summary>
        public static void Clear()
        {
            lock (Sync)
            {
                RootsByCulture.Clear();
                _built = false;
            }
        }

        /// <summary>
        /// Roots of the extra trees for the given culture, or an empty list.
        /// </summary>
        public static List<WCharacter> GetRoots(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId))
                return [];

            EnsureBuilt();

            lock (Sync)
            {
                return RootsByCulture.TryGetValue(cultureId, out var roots) ? roots : [];
            }
        }

        /// <summary>
        /// Every troop of the given culture's extra trees, flattened (roots plus descendants).
        /// </summary>
        public static List<WCharacter> GetTroops(string cultureId)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var troops = new List<WCharacter>();

            foreach (var root in GetRoots(cultureId))
                CollectTree(root, seen, troops);

            return troops;
        }

        /// <summary>
        /// True if any troop in the root's tree has been edited and must be persisted.
        /// </summary>
        public static bool TreeNeedsPersistence(WCharacter root)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var troops = new List<WCharacter>();
            CollectTree(root, seen, troops);

            foreach (var troop in troops)
                if (troop.NeedsPersistence)
                    return true;

            return false;
        }

        /// <summary>
        /// Depth-first walk over a troop's upgrade tree (deduplicated).
        /// </summary>
        private static void CollectTree(WCharacter root, HashSet<string> seen, List<WCharacter> into)
        {
            if (root?.Base == null || string.IsNullOrEmpty(root.StringId))
                return;

            if (!seen.Add(root.StringId))
                return;

            into.Add(root);

            var targets = root.Base.UpgradeTargets;
            if (targets == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                var t = targets[i];
                if (t != null)
                    CollectTree(new WCharacter(t), seen, into);
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

                RootsByCulture.Clear();

                // Everything reachable from any culture's canonical groups is "claimed".
                var claimed = new HashSet<string>(StringComparer.Ordinal);

                void Claim(WCharacter troop)
                {
                    if (troop?.Base == null || string.IsNullOrEmpty(troop.StringId))
                        return;

                    if (!claimed.Add(troop.StringId))
                        return;

                    var targets = troop.Base.UpgradeTargets;
                    if (targets == null)
                        return;

                    for (int i = 0; i < targets.Length; i++)
                        if (targets[i] != null)
                            Claim(new WCharacter(targets[i]));
                }

                var cultures =
                    MBObjectManager.Instance?.GetObjectTypeList<CultureObject>() ?? [];

                foreach (var cultureObject in cultures)
                {
                    if (cultureObject == null)
                        continue;

                    var culture = new WCulture(cultureObject);

                    foreach (var t in culture.EliteTroops)
                        Claim(t);
                    foreach (var t in culture.BasicTroops)
                        Claim(t);
                    foreach (var t in culture.MilitiaTroops)
                        Claim(t);
                    foreach (var t in culture.CaravanTroops)
                        Claim(t);
                    foreach (var t in culture.VillagerTroops)
                        Claim(t);
                    foreach (var t in culture.MercenaryTroops)
                        Claim(t);
                    foreach (var t in culture.BanditTroops)
                        Claim(t);
                    foreach (var t in culture.CivilianTroops)
                        Claim(t);
                }

                // Candidates + the set of ids some soldier upgrades into.
                var characters =
                    MBObjectManager.Instance?.GetObjectTypeList<CharacterObject>() ?? [];

                var candidates = new List<WCharacter>();
                var upgradedInto = new HashSet<string>(StringComparer.Ordinal);

                foreach (var co in characters)
                {
                    if (co == null || co.IsHero || co.Occupation != Occupation.Soldier)
                        continue;

                    var wc = new WCharacter(co);
                    if (wc.IsCustom || wc.IsLegacyCustom)
                        continue;

                    candidates.Add(wc);

                    var targets = co.UpgradeTargets;
                    if (targets == null)
                        continue;

                    for (int i = 0; i < targets.Length; i++)
                    {
                        var id = targets[i]?.StringId;
                        if (!string.IsNullOrEmpty(id))
                            upgradedInto.Add(id);
                    }
                }

                foreach (var wc in candidates)
                {
                    if (claimed.Contains(wc.StringId))
                        continue;

                    if (upgradedInto.Contains(wc.StringId))
                        continue; // not a root

                    if (wc.HiddenInEncyclopedia)
                        continue; // quest/template troops

                    var cultureId = wc.Culture?.StringId;
                    if (string.IsNullOrEmpty(cultureId))
                        continue;

                    if (!RootsByCulture.TryGetValue(cultureId, out var list))
                        RootsByCulture[cultureId] = list = [];

                    list.Add(wc);
                }

                _built = true;
            }
        }
    }
}
