using System;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Repairs troop rosters that reference a non-canonical CharacterObject instance for a custom
    /// troop id, and prevents such references from being written into saves.
    ///
    /// How the corruption happens: the save system stores CharacterObject records by value with
    /// only StringId/Id/IsRegistered (name, level, equipment are XML-domain and not saveable). If
    /// a custom stub's live instance was unregistered at save time (the engine's load-time
    /// UnregisterNonReadyObjects sweep can do this — see StubReadyPatch), the record is written
    /// with IsRegistered=false. On the next load that record materializes as a floating skeleton
    /// (null name, level 0) that does NOT re-register, while stubs.xml registers a fresh canonical
    /// instance — leaving rosters pointing at a half-dead twin. Wage/food/perk calculations then
    /// crash on the null name and the save becomes unloadable.
    /// </summary>
    [SafeClass]
    public sealed class StubIntegrityBehavior : BaseCampaignBehavior
    {
        // Heal rosters right after load (fixes saves that already carry a skeleton twin)...
        protected override void OnGameLoadFinished() => CanonicalizeAllRosters("load");

        // ...and right before save (so a twin created mid-session is never persisted).
        protected override void OnBeforeSave() => CanonicalizeAllRosters("save");

        private static void CanonicalizeAllRosters(string phase)
        {
            int healed = 0;

            var parties = MobileParty.All;
            if (parties != null)
                foreach (var party in parties)
                {
                    healed += CanonicalizeRoster(party?.Party?.MemberRoster);
                    healed += CanonicalizeRoster(party?.Party?.PrisonRoster);
                }

            var settlements = Settlement.All;
            if (settlements != null)
                foreach (var settlement in settlements)
                {
                    healed += CanonicalizeRoster(settlement?.Party?.MemberRoster);
                    healed += CanonicalizeRoster(settlement?.Party?.PrisonRoster);
                }

            if (healed > 0)
                Log.Warning(
                    $"Stub integrity ({phase}): repaired {healed} roster entr(y/ies) referencing "
                        + "a duplicate custom troop instance."
                );
        }

        /// <summary>
        /// Replaces roster entries whose Character is a custom-troop instance different from the
        /// object manager's registered instance for the same id, preserving count, wounded and
        /// xp. Returns the number of repaired entries. Internal for the test suite.
        /// </summary>
        internal static int CanonicalizeRoster(TroopRoster roster)
        {
            if (roster == null)
                return 0;

            var manager = MBObjectManager.Instance;
            if (manager == null)
                return 0;

            int healed = 0;

            for (int i = roster.Count - 1; i >= 0; i--)
            {
                var element = roster.GetElementCopyAtIndex(i);
                var character = element.Character;
                var id = character?.StringId;

                if (
                    string.IsNullOrEmpty(id)
                    || !id.StartsWith(WCharacter.CustomTroopPrefix, StringComparison.Ordinal)
                )
                    continue;

                var canonical = manager.GetObject<CharacterObject>(id);
                if (canonical == null || ReferenceEquals(canonical, character))
                    continue;

                // RemoveTroop/AddToCounts match rows by reference (CharacterObject has no
                // equality overloads), so this removes exactly the bad row and merges its
                // numbers into the canonical row if one already exists.
                int number = element.Number;
                int wounded = element.WoundedNumber;
                int xp = element.Xp;

                roster.RemoveTroop(character, number);
                roster.AddToCounts(
                    canonical,
                    number,
                    insertAtFront: false,
                    woundedCount: wounded,
                    xpChange: xp
                );

                healed++;
                Log.Warning(
                    $"Replaced duplicate instance of '{id}' in a roster "
                        + $"(count {number}, wounded {wounded}) with the registered troop."
                );
            }

            return healed;
        }
    }
}
