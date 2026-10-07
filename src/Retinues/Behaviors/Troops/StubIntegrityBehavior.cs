using System;
using System.Reflection;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
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
        protected override void OnGameLoadFinished()
        {
            CanonicalizeAllRosters("load");
            ScrubInvalidEquipment();
        }

        /// <summary>
        /// Removes identified mission/siege ammunition from custom troops' saved and pending
        /// gear. Editor visibility alone is never a reason to discard existing equipment.
        /// </summary>
        internal static void ScrubInvalidEquipment()
        {
            int removed = 0;

            foreach (var troop in WCharacter.All)
            {
                if (troop?.Base == null || !troop.IsCustom || troop.IsHero)
                    continue;

                var sets = troop.Equipments;
                if (sets == null)
                    continue;

                foreach (var set in sets)
                {
                    if (set == null)
                        continue;

                    int count = set.ScrubInvalidItems();
                    removed += count;
                    if (count > 0)
                        Log.Warning(
                            $"Removed {count} invalid real/pending equipment item(s) from '{troop.StringId}'."
                        );
                }
            }

            if (removed > 0)
                Log.Warning($"Stub integrity (load): removed {removed} invalid equipment item(s).");
        }

        // ...and right before save (so a twin created mid-session is never persisted).
        protected override void OnBeforeSave() => CanonicalizeAllRosters("save");

        /// <summary>
        /// Repairs every party and settlement roster. Returns the number of repaired entries.
        /// Internal so the scrub_save console command can run it on demand.
        /// </summary>
        internal static int CanonicalizeAllRosters(string phase)
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

            return healed;
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

            return CanonicalizeRoster(roster, manager.GetObject<CharacterObject>);
        }

        internal static int CanonicalizeRoster(TroopRoster roster, Func<string, CharacterObject> resolve)
        {
            if (roster == null || resolve == null)
                return 0;

            // Plan against a copy. RemoveTroop/AddToCounts run campaign and XP callbacks,
            // can clamp saved XP, and can fail after the old row has already been removed.
            // These engine fields are checked before any mutation; a new engine layout
            // leaves the original roster intact instead of attempting a partial repair.
            var dataField = typeof(TroopRoster).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
            var countField = typeof(TroopRoster).GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic);
            if (dataField?.FieldType != typeof(TroopRosterElement[]) || countField?.FieldType != typeof(int))
                return 0;
            var original = dataField.GetValue(roster) as TroopRosterElement[];
            int count = roster.Count;
            if (original == null || count < 0 || count > original.Length)
                return 0;
            var repaired = (TroopRosterElement[])original.Clone();
            int healed = 0;

            for (int i = count - 1; i >= 0; i--)
            {
                var element = repaired[i];
                var character = element.Character;
                var id = character?.StringId;

                if (
                    string.IsNullOrEmpty(id)
                    || !id.StartsWith(WCharacter.CustomTroopPrefix, StringComparison.Ordinal)
                )
                    continue;

                var canonical = resolve(id);
                if (canonical == null || ReferenceEquals(canonical, character)
                    || canonical.StringId != id || canonical.IsHero || character.IsHero)
                    continue;

                int target = -1;
                for (int j = 0; j < count; j++)
                    if (ReferenceEquals(repaired[j].Character, canonical)) { target = j; break; }
                if (target < 0)
                    repaired[i].Character = canonical;
                else
                {
                    // Overflow aborts the entire plan before the original roster is touched.
                    repaired[target].Number = checked(repaired[target].Number + element.Number);
                    repaired[target].WoundedNumber = checked(repaired[target].WoundedNumber + element.WoundedNumber);
                    repaired[target].Xp = checked(repaired[target].Xp + element.Xp);
                    Array.Copy(repaired, i + 1, repaired, i, count - i - 1);
                    repaired[--count] = default;
                }
                healed++;
            }

            if (healed == 0)
                return 0;
            dataField.SetValue(roster, repaired);
            countField.SetValue(roster, count);
            // Only non-hero references/rows changed: total men and wounded stay unchanged.
            // Invalidate the engine's cached row list without running recruitment callbacks.
            roster.UpdateVersion();
            return healed;
        }
    }
}
