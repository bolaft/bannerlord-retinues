using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Wrappers.Campaign;


namespace CustomClanTroops.Patches.Recruitment.Helpers
{
    public static class RecruitmentHelpers
    {
        public static CharacterObject GetFactionRootFor(CharacterObject vanilla, WFaction faction)
        {
            if (vanilla == null || faction == null || faction.RootBasic == null || faction.RootElite == null)
                return null;

            var rootId = IsEliteLine(vanilla) ? faction.RootElite.StringId : faction.RootBasic.StringId;
            return MBObjectManager.Instance.GetObject<CharacterObject>(rootId);
        }

        public static CharacterObject TryToLevel(CharacterObject root, int tier)
        {
            var cur = root;
            while (cur != null && cur.Tier < tier && cur.UpgradeTargets != null && cur.UpgradeTargets.Length > 0)
                cur = cur.UpgradeTargets[MBRandom.RandomInt(cur.UpgradeTargets.Length)];
            return cur ?? root;
        }

        public static bool IsEliteLine(CharacterObject unit)
        {
            var eliteRoot = unit?.Culture?.EliteBasicTroop;
            if (eliteRoot == null) return false;

            var seen = new HashSet<CharacterObject>();
            var stack = new Stack<CharacterObject>();
            stack.Push(eliteRoot);
            seen.Add(eliteRoot);

            while (stack.Count > 0)
            {
                var n = stack.Pop();
                if (ReferenceEquals(n, unit)) return true;
                var ups = n.UpgradeTargets;
                if (ups == null) continue;
                foreach (var v in ups)
                    if (v != null && seen.Add(v)) stack.Push(v);
            }
            return false;
        }

        public static bool IsFactionTroop(WFaction faction, CharacterObject c)
        {
            if (faction == null || c == null) return false;
            foreach (var t in faction.BasicTroops.Concat(faction.EliteTroops))
                if (t.StringId == c.StringId)
                    return true;
            // also consider roots
            if (faction.RootBasic?.StringId == c.StringId) return true;
            if (faction.RootElite?.StringId == c.StringId) return true;
            return false;
        }
    }
}