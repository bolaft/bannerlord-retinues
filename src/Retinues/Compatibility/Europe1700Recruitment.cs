using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using ActiveModules = Retinues.Utils.ModuleChecker;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;

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

        internal static WCharacter GetAffectedSource(WCharacter troop, out bool elite)
        {
            elite = false;
            if (!IsActive || troop == null || !troop.IsValid || troop.IsHero)
                return null;
            var source = troop;
            if (troop.IsCustom)
            {
                // Repair only a known faction militia slot with a recorded original.
                // Never reinterpret ordinary custom troops or guess from their names.
                var faction = troop.Faction;
                if (faction == null || (troop != faction.MilitiaMelee && troop != faction.MilitiaMeleeElite
                    && troop != faction.MilitiaRanged && troop != faction.MilitiaRangedElite))
                    return null;
                var culture = troop.Base.Culture;
                if (culture == null || troop.VanillaStringId == troop.StringId)
                    return null;
                var original = new[] { culture.MeleeMilitiaTroop, culture.RangedMilitiaTroop,
                    culture.MeleeEliteMilitiaTroop, culture.RangedEliteMilitiaTroop }
                    .FirstOrDefault(t => t != null && t.StringId == troop.VanillaStringId);
                if (original == null)
                    return null;
                source = new WCharacter(original);
            }
            return TryGetTreeKind(source.Base, out elite) ? source : null;
        }

        internal static WCharacter MatchVolunteer(BaseFaction faction, WCharacter source, bool elite)
        {
            var root = elite ? faction?.RootElite : faction?.RootBasic;
            var replacement = TroopMatcher.PickBestFromTree(root, source, sameTierOnly: false);
            // A locked/missing custom tree must leave the original recruit available,
            // rather than fall back to a custom militia slot again.
            return replacement?.IsCustom == true ? replacement : source;
        }

    }
}
