using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using ActiveModules = Retinues.Framework.Modules.ModuleManager;

namespace Retinues.Compatibility
{
    /// <summary>
    /// Recruitment-only compatibility for Europe1700's shared regular/militia troops.
    /// Does not change global troop categories or actual settlement militia matching.
    /// </summary>
    internal static class Europe1700Recruitment
    {
        internal static bool IsActive => ActiveModules.GetActiveModules().Any(m =>
            string.Equals(m.Id, "Europe1700", StringComparison.OrdinalIgnoreCase));

        internal static bool TryGetTreeKind(CharacterObject troop, out bool elite)
        {
            elite = false;
            if (!IsActive || troop == null || troop.IsHero || troop.Culture == null)
                return false;

            var culture = troop.Culture;
            if (troop != culture.MeleeMilitiaTroop && troop != culture.RangedMilitiaTroop
                && troop != culture.MeleeEliteMilitiaTroop && troop != culture.RangedEliteMilitiaTroop)
                return false;

            // Elite trees can also be branches of the basic tree in 1700. Prefer the
            // elite pool, as normal recruitment does, irrespective of the militia slot.
            elite = Contains(culture.EliteBasicTroop, troop);
            return elite || Contains(culture.BasicTroop, troop);
        }

        private static bool Contains(CharacterObject root, CharacterObject troop)
        {
            if (root == null)
                return false;
            var seen = new HashSet<CharacterObject>();
            var pending = new Stack<CharacterObject>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current == null || !seen.Add(current))
                    continue;
                if (current == troop)
                    return true;
                foreach (var child in current.UpgradeTargets ?? Array.Empty<CharacterObject>())
                    pending.Push(child);
            }
            return false;
        }
    }
}
