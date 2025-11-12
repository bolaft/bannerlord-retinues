using System.Collections.Generic;
using Retinues.Game.Wrappers;

namespace Retinues.Game.Helpers.Character
{
    public static class CharacterIndexer
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Lookups                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Dictionary<string, string> _parentOf = [];
        private static readonly Dictionary<string, WFaction> _factionOf = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Trees                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly HashSet<string> _eliteTree = [];
        private static readonly HashSet<string> _basicTree = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Per-faction singletons                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Dictionary<string, string> _retinueEliteOf = [];
        private static readonly Dictionary<string, string> _retinueBasicOf = [];

        private static readonly Dictionary<string, string> _militiaMeleeOf = [];
        private static readonly Dictionary<string, string> _militiaRangedOf = [];
        private static readonly Dictionary<string, string> _militiaMeleeEliteOf = [];
        private static readonly Dictionary<string, string> _militiaRangedEliteOf = [];

        private static readonly Dictionary<string, string> _caravanGuardOf = [];
        private static readonly Dictionary<string, string> _caravanMasterOf = [];
        private static readonly Dictionary<string, string> _villagerOf = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //              Incremental maintenance hooks             //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void Clear()
        {
            _parentOf.Clear();
            _factionOf.Clear();

            _eliteTree.Clear();
            _basicTree.Clear();

            _retinueEliteOf.Clear();
            _retinueBasicOf.Clear();

            _militiaMeleeOf.Clear();
            _militiaRangedOf.Clear();
            _militiaMeleeEliteOf.Clear();
            _militiaRangedEliteOf.Clear();

            _caravanGuardOf.Clear();
            _caravanMasterOf.Clear();
            _villagerOf.Clear();
        }

        /// <summary>
        /// Register all graph memberships for a faction: trees and per-faction singletons.
        /// </summary>
        public static void RegisterFactionRoots(WFaction f)
        {
            // Trees: walk once per root and fill parent edges
            MarkTree(f, f.RootElite, _eliteTree);
            MarkTree(f, f.RootBasic, _basicTree);

            // Retinues
            SetSingleton(_retinueEliteOf, f, f.RetinueElite);
            SetSingleton(_retinueBasicOf, f, f.RetinueBasic);

            // Militias
            SetSingleton(_militiaMeleeOf, f, f.MilitiaMelee);
            SetSingleton(_militiaRangedOf, f, f.MilitiaRanged);
            SetSingleton(_militiaMeleeEliteOf, f, f.MilitiaMeleeElite);
            SetSingleton(_militiaRangedEliteOf, f, f.MilitiaRangedElite);

            // Caravan
            SetSingleton(_caravanGuardOf, f, f.CaravanGuard);
            SetSingleton(_caravanMasterOf, f, f.CaravanMaster);

            // Villager
            SetSingleton(_villagerOf, f, f.Villager);
        }

        private static void MarkTree(WFaction f, WCharacter root, HashSet<string> bucket)
        {
            if (root == null)
                return;

            foreach (var c in root.Tree)
            {
                _factionOf[c.StringId] = f;
                bucket.Add(c.StringId);

                // Parent edges: for children only; the first element of Tree is the root itself
                foreach (var child in c.UpgradeTargets)
                    _parentOf[child.StringId] = c.StringId;
            }
        }

        private static void SetSingleton(Dictionary<string, string> dict, WFaction f, WCharacter c)
        {
            if (f == null)
                return;

            if (c == null || !c.IsActive)
            {
                dict.Remove(f.StringId);
                return;
            }

            _factionOf[c.StringId] = f;
            dict[f.StringId] = c.StringId;
        }

        // Incremental maintenance hooks

        public static void SetParent(WCharacter parent, WCharacter child)
        {
            if (child == null)
                return;

            if (parent == null)
                _parentOf.Remove(child.StringId);
            else
                _parentOf[child.StringId] = parent.StringId;
        }

        public static void SetFaction(WFaction f, WCharacter c)
        {
            if (c == null)
                return;

            if (f == null)
                _factionOf.Remove(c.StringId);
            else
                _factionOf[c.StringId] = f;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      O(1) queries                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WFaction TryGetFaction(WCharacter c)
        {
            if (c == null)
                return null;
            return _factionOf.TryGetValue(c.StringId, out WFaction f) ? f : null;
        }

        public static WCharacter TryGetParent(WCharacter c)
        {
            if (c == null)
                return null;
            return _parentOf.TryGetValue(c.StringId, out string pid) ? new WCharacter(pid) : null;
        }

        public static bool IsRetinue(WCharacter c)
        {
            var f = TryGetFaction(c);
            if (c == null || f == null)
                return false;

            return (
                    _retinueEliteOf.TryGetValue(f.StringId, out string eliteId)
                    && eliteId == c.StringId
                )
                || (
                    _retinueBasicOf.TryGetValue(f.StringId, out string basicId)
                    && basicId == c.StringId
                );
        }

        public static bool IsMilitiaMelee(WCharacter c)
        {
            var f = TryGetFaction(c);
            if (c == null || f == null)
                return false;

            return (_militiaMeleeOf.TryGetValue(f.StringId, out string id) && id == c.StringId)
                || (
                    _militiaMeleeEliteOf.TryGetValue(f.StringId, out string eliteId)
                    && eliteId == c.StringId
                );
        }

        public static bool IsMilitiaRanged(WCharacter c)
        {
            var f = TryGetFaction(c);
            if (c == null || f == null)
                return false;

            return (_militiaRangedOf.TryGetValue(f.StringId, out string id) && id == c.StringId)
                || (
                    _militiaRangedEliteOf.TryGetValue(f.StringId, out string eliteId)
                    && eliteId == c.StringId
                );
        }

        public static bool IsCaravanGuard(WCharacter c)
        {
            var f = TryGetFaction(c);
            if (c == null || f == null)
                return false;

            return _caravanGuardOf.TryGetValue(f.StringId, out string id) && id == c.StringId;
        }

        public static bool IsCaravanMaster(WCharacter c)
        {
            var f = TryGetFaction(c);
            if (c == null || f == null)
                return false;

            return _caravanMasterOf.TryGetValue(f.StringId, out string id) && id == c.StringId;
        }

        public static bool IsVillager(WCharacter c)
        {
            var f = TryGetFaction(c);
            if (c == null || f == null)
                return false;

            return _villagerOf.TryGetValue(f.StringId, out string id) && id == c.StringId;
        }

        public static bool IsElite(WCharacter c)
        {
            if (c == null)
                return false;

            if (_eliteTree.Contains(c.StringId))
                return true;

            var f = TryGetFaction(c);
            if (f == null)
                return false;

            return (_retinueEliteOf.TryGetValue(f.StringId, out string re) && re == c.StringId)
                || (
                    _militiaMeleeEliteOf.TryGetValue(f.StringId, out string mme)
                    && mme == c.StringId
                )
                || (
                    _militiaRangedEliteOf.TryGetValue(f.StringId, out string mre)
                    && mre == c.StringId
                );
        }
    }
}
