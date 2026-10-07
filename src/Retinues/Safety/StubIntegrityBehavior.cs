using System;
using System.Reflection;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Retinues.Safety
{
    /// <summary>
    /// Repairs troop rosters that reference a non-canonical CharacterObject instance for a
    /// custom troop id, and drops retinue troops that leaked into upgrade trees.
    ///
    /// The duplicate-instance corruption: the save system stores CharacterObject records by
    /// value with only StringId/Id/IsRegistered (name, level and equipment are XML-domain and
    /// not saveable). If a custom stub's live instance was unregistered at save time (the
    /// engine's load-time UnregisterNonReadyObjects sweep can do this — see StubReadyPatch),
    /// the record is written with IsRegistered=false. On the next load it materializes as a
    /// floating skeleton (null name, level 0) that does NOT re-register, while the stub XML
    /// registers a fresh canonical instance — leaving rosters pointing at a half-dead twin
    /// that crashes wage/food/morale calculations on the campaign tick.
    /// </summary>
    [SafeClass]
    public class StubIntegrityBehavior : CampaignBehaviorBase
    {
        public override void SyncData(IDataStore dataStore) { }

        public override void RegisterEvents()
        {
            // Heal right after load (fixes saves that already carry a skeleton twin)...
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this,
                () =>
                {
                    CanonicalizeAllRosters("load");
                    ScrubRetinueUpgradeTargets();
                }
            );

            // ...and right before save (so a twin created mid-session is never persisted).
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(
                this,
                () => CanonicalizeAllRosters("save")
            );
        }

        private static bool IsCustomId(string id) =>
            !string.IsNullOrEmpty(id)
            && (
                id.StartsWith(WCharacter.CustomIdPrefix, StringComparison.Ordinal)
                || id.StartsWith(WCharacter.LegacyCustomIdPrefix, StringComparison.Ordinal)
            );

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
                Log.Warn(
                    $"Stub integrity ({phase}): repaired {healed} roster entr(y/ies) referencing "
                        + "a duplicate custom troop instance."
                );

            return healed;
        }

        /// <summary>
        /// Replaces roster entries whose Character is a custom-troop instance different from
        /// the object manager's registered instance for the same id, preserving count, wounded
        /// and xp. Internal for the test suite.
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

                if (!IsCustomId(id))
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
        /// <summary>
        /// Drops retinue troops from upgrade trees. Retinues rank up in place and must never
        /// be upgrade targets — when older data leaves them linked into a tree, they become
        /// reachable by the recruitment/garrison mapping (players found their retinues offered
        /// as village volunteers) and by the AI upgrader. Internal for the console command.
        /// </summary>
        internal static int ScrubRetinueUpgradeTargets()
        {
            int removed = 0;

            foreach (var faction in new[] { Player.Clan, Player.Kingdom })
            {
                if (faction == null)
                    continue;

                foreach (RootCategory category in Enum.GetValues(typeof(RootCategory)))
                {
                    var root = faction.GetRoot(category);
                    if (root == null)
                        continue;

                    var tree = root.Tree;
                    if (tree == null)
                        continue;

                    foreach (var troop in tree)
                    {
                        if (troop?.Base == null)
                            continue;

                        var targets = troop.UpgradeTargets;
                        if (targets == null || targets.Length == 0)
                            continue;

                        var kept = System.Linq.Enumerable.ToArray(
                            System.Linq.Enumerable.Where(
                                targets,
                                t => t?.Base != null && !faction.IsRetinueId(t.StringId)
                            )
                        );

                        if (kept.Length == targets.Length)
                            continue;

                        troop.UpgradeTargets = kept;
                        removed += targets.Length - kept.Length;
                        Log.Warn(
                            $"Scrubbed retinue upgrade link from '{troop.StringId}' (stale data)."
                        );
                    }
                }
            }

            return removed;
        }
    }
}
