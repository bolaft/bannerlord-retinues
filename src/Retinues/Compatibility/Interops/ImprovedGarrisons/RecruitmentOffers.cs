using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Volunteers;
using Retinues.Domain.Characters.Services.Matching;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Compatibility.Interops.ImprovedGarrisons
{
    /// <summary>Recruiter-specific offers. Discovery never changes the world's volunteer arrays.</summary>
    internal static class RecruitmentOffers
    {
        internal static CharacterObject Resolve(Settlement settlement, Hero owner, Hero notable,
            int slot, CharacterObject original, Func<CharacterObject, bool> matchesTemplate)
        {
            if (original == null || original.IsHero || owner?.Clan == null || settlement == null)
                return original;

            var source = WCharacter.Get(original);
            var ws = WSettlement.Get(settlement);
            if (source == null || ws == null)
                return original;

            var candidates = new List<WCharacter>();
            if (owner.Clan == Clan.PlayerClan && Configuration.AllowedRecruiters.Value != Configuration.AllowedRecruitersMode.PlayerOnly)
            {
                // Use the same availability, mixing, tier and category rules as the player.
                // A template may choose among the eligible clan/kingdom trees, but cannot
                // introduce an unavailable tree or promote a low-tier volunteer for free.
                foreach (var root in ws.GetPlayerRoots(source.IsElite))
                {
                    var eligible = root.Tree.Where(t => CanOffer(t, ws, owner)
                        && (matchesTemplate == null || matchesTemplate(t.Base)));
                    var match = CharacterMatcher.FindBestVolunteer(source, eligible);
                    if (match != null && !candidates.Contains(match))
                        candidates.Add(match);
                }
                if (candidates.Count > 0)
                    return PlayerVolunteerSwapState.ChooseRootDeterministic(ws, notable?.StringId, slot, candidates)?.Base;
                // No matching eligible troop: do not fall back to an unavailable custom tree.
                if (matchesTemplate != null)
                    return null;
            }
            else if (CanOffer(source, ws, owner))
                return original; // Non-player garrisons retain their existing troop pool.

            // Respect restrictions on automation even for IG's direct roster-transfer path.
            var vanillaRoot = source.IsElite ? ws.Culture?.RootElite : ws.Culture?.RootBasic;
            var fallback = CharacterMatcher.FindBestVolunteer(source, vanillaRoot?.Tree)?.Base;
            return fallback ?? (source.IsFactionTroop ? null : original);
        }

        private static bool CanOffer(WCharacter troop, WSettlement settlement, Hero owner)
        {
            if (troop == null || troop.IsHero)
                return false;
            if (!troop.IsFactionTroop)
                return true;
            bool sameFaction = troop.Factions.Any(f =>
                f is WClan clan && clan.Base == owner.Clan
                || f is WKingdom kingdom && kingdom.Base == owner.Clan?.Kingdom);
            bool sameCulture = troop.Base.Culture == null || settlement.Base.Culture == null
                || troop.Base.Culture == settlement.Base.Culture;
            return AllowsCustom(Configuration.AllowedRecruiters.Value, sameFaction,
                Configuration.SameCultureOnly.Value, sameCulture) && !troop.IsRetinue;
        }

        internal static bool AllowsCustom(Configuration.AllowedRecruitersMode mode,
            bool sameFaction, bool sameCultureOnly, bool sameCulture) =>
            (mode == Configuration.AllowedRecruitersMode.Everyone
                || mode == Configuration.AllowedRecruitersMode.FactionOnly && sameFaction)
            && (!sameCultureOnly || sameCulture);

        internal static bool IsElite(CharacterObject troop)
        {
            if (troop == null || troop.IsHero)
                return false;
            var wrapped = WCharacter.Get(troop);
            return wrapped != null && wrapped.IsFactionTroop
                ? wrapped.IsElite && !wrapped.IsRetinue
                : Reaches(troop.Culture?.EliteBasicTroop, troop);
        }

        internal static bool Reaches(CharacterObject root, CharacterObject target)
        {
            if (root == null || target == null)
                return false;
            var seen = new HashSet<CharacterObject>();
            var pending = new Stack<CharacterObject>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var troop = pending.Pop();
                if (troop == null || !seen.Add(troop))
                    continue;
                if (troop == target)
                    return true;
                foreach (var child in troop.UpgradeTargets ?? Array.Empty<CharacterObject>())
                    pending.Push(child);
            }
            return false;
        }

        internal sealed class Offer
        {
            internal readonly Hero Owner;
            internal readonly Hero Notable;
            internal readonly int Slot;
            internal readonly CharacterObject Original;
            internal readonly CharacterObject Troop;
            internal Offer(Hero owner, Hero notable, int slot, CharacterObject original, CharacterObject troop)
            { Owner = owner; Notable = notable; Slot = slot; Original = original; Troop = troop; }

            internal bool IsCurrent => Notable?.VolunteerTypes != null && Slot >= 0
                && Slot < Notable.VolunteerTypes.Length && Notable.VolunteerTypes[Slot] == Original;

            internal SlotSwap Apply()
            {
                if (!IsCurrent)
                    return null;
                return new SlotSwap(Notable.VolunteerTypes, Slot, Original, Troop);
            }
        }

        /// <summary>IG's regional path reads the slot again. Change only that slot during transfer.</summary>
        internal sealed class SlotSwap : IDisposable
        {
            private readonly CharacterObject[] _slots;
            private readonly int _slot;
            private readonly CharacterObject _original;
            private readonly CharacterObject _offered;
            internal SlotSwap(CharacterObject[] slots, int slot, CharacterObject original, CharacterObject offered)
            {
                _slots = slots; _slot = slot; _original = original; _offered = offered;
                slots[slot] = offered;
            }
            public void Dispose()
            {
                // Successful recruitment clears the slot. Never replenish it, or overwrite
                // another mod's change. Failed transfers restore the original troop.
                if (_slots[_slot] == _offered)
                    _slots[_slot] = _original;
            }
        }
    }
}
