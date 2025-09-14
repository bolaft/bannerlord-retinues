using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Game.Helpers
{
    public static class CharacterObjectHelper
    {
        public static CharacterObject GetFactionRootFor(CharacterObject vanilla, WFaction faction)
        {
            if (
                vanilla == null
                || faction == null
                || faction.RootBasic == null
                || faction.RootElite == null
            )
                return null;

            var rootId = IsEliteLine(vanilla)
                ? faction.RootElite?.StringId
                : faction.RootBasic?.StringId;
            if (rootId == null)
                return null;
            return MBObjectManager.Instance.GetObject<CharacterObject>(rootId);
        }

        public static CharacterObject TryToLevel(CharacterObject root, int tier)
        {
            var cur = root;
            while (
                cur != null
                && cur.Tier < tier
                && cur.UpgradeTargets != null
                && cur.UpgradeTargets.Length > 0
            )
                cur = cur.UpgradeTargets[MBRandom.RandomInt(cur.UpgradeTargets.Length)];
            return cur ?? root;
        }

        public static bool IsEliteLine(CharacterObject unit)
        {
            var eliteRoot = unit?.Culture?.EliteBasicTroop;
            if (eliteRoot == null)
                return false;

            var seen = new HashSet<CharacterObject>();
            var stack = new Stack<CharacterObject>();
            stack.Push(eliteRoot);
            seen.Add(eliteRoot);

            while (stack.Count > 0)
            {
                var n = stack.Pop();
                if (ReferenceEquals(n, unit))
                    return true;
                var ups = n.UpgradeTargets;
                if (ups == null)
                    continue;
                foreach (var v in ups)
                    if (v != null && seen.Add(v))
                        stack.Push(v);
            }
            return false;
        }

        public static bool IsFactionTroop(WFaction faction, CharacterObject c)
        {
            if (faction == null || c == null)
                return false;
            foreach (var t in faction.BasicTroops.Concat(faction.EliteTroops))
                if (t.StringId == c.StringId)
                    return true;
            // also consider roots
            if (faction.RootBasic?.StringId == c.StringId)
                return true;
            if (faction.RootElite?.StringId == c.StringId)
                return true;
            return false;
        }

        public static void SetStringIdAndReregister(CharacterObject obj, string newId)
        {
            var manager = MBObjectManager.Instance;

            // 1) Unregister old (private API names differ between versions)
            //    Try UnregisterObject(obj) first, then fallback to removing from internal dictionaries.
            try
            {
                Reflector.InvokeMethod(manager, "UnregisterObject", [typeof(CharacterObject)], obj);
            }
            catch
            {
                // Fallback: try RemoveObject / RemoveObjectById if present
                try
                {
                    Reflector.InvokeMethod(manager, "RemoveObject", [typeof(CharacterObject)], obj);
                }
                catch
                { /* last resort: ignore, but you may hit dup-key when registering */
                }
            }

            // 2) Change backing field for StringId
            //    Your Reflector already tries common patterns, so this works across builds.
            Reflector.SetPropertyValue(obj, "StringId", newId);

            // 3) Re-register
            manager.RegisterObject(obj);
        }
    }
}
