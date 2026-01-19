using System;
using System.Linq;
using Retinues.Domain.Characters.Services.Matching;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.Encounters;

namespace Retinues.Domain.Parties.Wrappers
{
    /// <summary>
    /// Troop swapping helpers for WParty.
    /// </summary>
    public partial class WParty
    {
        public void SwapTroops(IBaseFaction faction = null, Func<WCharacter, bool> filter = null)
        {
            faction ??= GetSwapTargetFaction();
            if (faction == null)
                return; // Nothing to swap with

            // Snapshot roster elements once so we can mutate the underlying roster safely.
            var elements = MemberRoster.Elements.ToList();
            if (elements == null || elements.Count == 0)
                return;

            // During battle simulation, Bannerlord avoids removing depleted non-hero entries.
            bool removeDepleted = !(PlayerEncounter.CurrentBattleSimulation != null);

            int swapped = 0;

            // Process each troop entry.
            foreach (var e in elements)
            {
                var troop = e.Troop;
                if (troop == null)
                    continue;

                if (troop.IsHero)
                    continue; // Do not swap heroes.

                if (filter != null && !filter(troop))
                    continue; // Does not match filter.

                // Find counterpart troop in the target faction.
                var match = CharacterMatcher.FindCounterpart(
                    troop,
                    faction,
                    strictTierMatch: false
                );

                if (match == null)
                    continue; // No counterpart found, ignore.

                if (match == troop)
                    continue; // Same troop, no swap needed.

                int number = e.Number;
                if (number <= 0)
                    continue;

                int available = MemberRoster.CountOf(troop);
                if (available <= 0)
                    continue;

                if (number > available)
                    number = available;

                int wounded = Math.Min(e.WoundedNumber, number);
                int xp = e.Xp;

                MemberRoster.RemoveTroop(troop, number, wounded, xp, removeDepleted);
                MemberRoster.AddTroop(match, number, wounded, xp);

                swapped += 1;
            }

            Log.Debug($"WParty.SwapTroops: swapped {swapped} troop entries in party {Name}.");
        }

        private IBaseFaction GetSwapTargetFaction()
        {
            if (Clan != null && Clan.Troops.Count() > 0)
                return Clan;

            if (Kingdom != null && Kingdom.Troops.Count() > 0)
                return Kingdom;

            return null;
        }
    }
}
