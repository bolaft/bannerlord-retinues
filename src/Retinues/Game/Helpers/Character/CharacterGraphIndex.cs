using System.Collections.Generic;
using Retinues.Game.Wrappers;

namespace Retinues.Game.Helpers.Character
{
    public static class CharacterGraphIndex
    {
        // lookups
        private static readonly Dictionary<string, string> _parentOf = [];
        private static readonly Dictionary<string, WFaction> _factionOf = [];

        // membership sets
        private static readonly HashSet<string> _retinues = [];
        private static readonly HashSet<string> _retinuesElite = [];
        private static readonly HashSet<string> _retinuesBasic = [];
        private static readonly HashSet<string> _militiaMelee = [];
        private static readonly HashSet<string> _militiaRanged = [];
        private static readonly HashSet<string> _militiaMeleeElite = [];
        private static readonly HashSet<string> _militiaRangedElite = [];
        private static readonly HashSet<string> _eliteTree = [];
        private static readonly HashSet<string> _basicTree = [];

        public static void Clear()
        {
            _parentOf.Clear();
            _factionOf.Clear();
            _retinues.Clear();
            _retinuesElite.Clear();
            _retinuesBasic.Clear();
            _militiaMelee.Clear();
            _militiaRanged.Clear();
            _militiaMeleeElite.Clear();
            _militiaRangedElite.Clear();
            _eliteTree.Clear();
            _basicTree.Clear();
        }

        public static void RegisterFactionRoots(WFaction f)
        {
            // Mark faction for roots and all descendants (tree walk once per root)
            MarkTree(f, f.RootElite, _eliteTree);
            MarkTree(f, f.RootBasic, _basicTree);

            // Retinues: record membership and whether they are elite/basic
            MarkOne(f, f.RetinueElite, _retinues);
            if (f.RetinueElite != null && f.RetinueElite.IsActive)
                _retinuesElite.Add(f.RetinueElite.StringId);

            MarkOne(f, f.RetinueBasic, _retinues);
            if (f.RetinueBasic != null && f.RetinueBasic.IsActive)
                _retinuesBasic.Add(f.RetinueBasic.StringId);

            // Militias: record membership in overall militia sets and mark elite variants
            MarkOne(f, f.MilitiaMelee, _militiaMelee);
            MarkOne(f, f.MilitiaRanged, _militiaRanged);

            MarkOne(f, f.MilitiaMeleeElite, _militiaMelee);
            if (f.MilitiaMeleeElite != null && f.MilitiaMeleeElite.IsActive)
                _militiaMeleeElite.Add(f.MilitiaMeleeElite.StringId);

            MarkOne(f, f.MilitiaRangedElite, _militiaRanged);
            if (f.MilitiaRangedElite != null && f.MilitiaRangedElite.IsActive)
                _militiaRangedElite.Add(f.MilitiaRangedElite.StringId);
        }

        private static void MarkTree(WFaction f, WCharacter root, HashSet<string> bucket)
        {
            if (root == null)
                return;

            // uses current UpgradeTargets graph
            foreach (var c in root.Tree)
            {
                _factionOf[c.StringId] = f;
                bucket.Add(c.StringId);
                // parent edges: for children only; the first element of Tree is root itself
                foreach (var child in c.UpgradeTargets)
                    _parentOf[child.StringId] = c.StringId;
            }
        }

        private static void MarkOne(WFaction f, WCharacter c, HashSet<string> bucket)
        {
            if (c == null || !c.IsActive)
                return;
            _factionOf[c.StringId] = f;
            bucket.Add(c.StringId);
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

        // O(1) queries
        public static WFaction TryGetFaction(WCharacter c) =>
            (c != null && _factionOf.TryGetValue(c.StringId, out var f)) ? f : null;

        public static WCharacter TryGetParent(WCharacter c) =>
            (c != null && _parentOf.TryGetValue(c.StringId, out var pid))
                ? new WCharacter(pid)
                : null;

        public static bool IsRetinue(WCharacter c) => c != null && _retinues.Contains(c.StringId);

        public static bool IsMilitiaMelee(WCharacter c) =>
            c != null && _militiaMelee.Contains(c.StringId);

        public static bool IsMilitiaRanged(WCharacter c) =>
            c != null && _militiaRanged.Contains(c.StringId);

        public static bool IsElite(WCharacter c) =>
            c != null
            && (
                _eliteTree.Contains(c.StringId)
                || _retinuesElite.Contains(c.StringId)
                || _militiaMeleeElite.Contains(c.StringId)
                || _militiaRangedElite.Contains(c.StringId)
            );

        public static bool IsBasic(WCharacter c) =>
            c != null
            && (
                _basicTree.Contains(c.StringId)
                || (_retinues.Contains(c.StringId) && !_retinuesElite.Contains(c.StringId))
                || (_militiaMelee.Contains(c.StringId) && !_militiaMeleeElite.Contains(c.StringId))
                || (
                    _militiaRanged.Contains(c.StringId) && !_militiaRangedElite.Contains(c.StringId)
                )
            );
    }
}
