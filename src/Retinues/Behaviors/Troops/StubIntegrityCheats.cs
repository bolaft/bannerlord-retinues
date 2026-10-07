using System;
using System.Collections.Generic;
using System.Text;
using Retinues.Behaviors.Retinues;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Console commands for repairing troop state in a save. The stable branch has had
    /// retinues.scrub_save for a while; this is its counterpart for the current persistence
    /// model, so the documented command exists on both branches.
    /// </summary>
    [SafeClass]
    public static class StubIntegrityCheats
    {
        /// <summary>
        /// Repairs a save without reloading: re-points roster entries that reference a
        /// duplicate instance of a custom troop at the registered troop, and strips retinue
        /// units out of upgrade trees. Also lists roster entries whose troop cannot be
        /// repaired (no registered troop exists for the id — typically a removed mod), so the
        /// player knows what to remove by hand.
        /// Usage: retinues.scrub_save
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("scrub_save", "retinues")]
        public static string ScrubSave(List<string> args)
        {
            if (Campaign.Current == null)
                return "No campaign loaded.";

            var sb = new StringBuilder();

            // 1) Duplicate custom troop instances in rosters (the unloadable-save corruption).
            int healed = StubIntegrityBehavior.CanonicalizeAllRosters("scrub");
            sb.AppendLine(
                healed > 0
                    ? $"Repaired {healed} roster entr{(healed == 1 ? "y" : "ies")} referencing a duplicate custom troop."
                    : "No duplicate custom troop instances found in rosters."
            );

            // 2) Retinues leaked into upgrade trees (AI lords upgrading into player retinues).
            RetinuesBehavior.ScrubRetinueUpgradeTargets();
            sb.AppendLine("Checked upgrade trees for retinue links (details in the log).");

            // 2b) Items that no longer count as valid equipment (e.g. siege ammunition).
            StubIntegrityBehavior.ScrubInvalidEquipment();
            sb.AppendLine("Checked custom troops for invalid equipment (details in the log).");

            // 3) Read-only: troops nothing can repair, so the player can act on them.
            var broken = CollectUnrepairableEntries();
            if (broken.Count > 0)
            {
                sb.AppendLine(
                    $"{broken.Count} roster entr{(broken.Count == 1 ? "y" : "ies")} reference a troop with no registered definition (removed mod?):"
                );
                foreach (var line in broken)
                    sb.AppendLine("  " + line);
            }
            else
            {
                sb.AppendLine("No unrepairable troop entries found.");
            }

            if (healed > 0)
                sb.AppendLine("Save the game to persist the repair.");

            Log.Info($"Scrub: repaired {healed} roster entries, {broken.Count} unrepairable.");
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Lists roster entries whose character has no name and no registered instance for
        /// its id — nothing in the mod can rebuild those, so they are only reported.
        /// </summary>
        private static List<string> CollectUnrepairableEntries()
        {
            var result = new List<string>();
            var manager = MBObjectManager.Instance;
            if (manager == null)
                return result;

            void Scan(TroopRoster roster, string owner)
            {
                if (roster == null)
                    return;

                for (int i = 0; i < roster.Count; i++)
                {
                    var character = roster.GetElementCopyAtIndex(i).Character;
                    if (character == null)
                    {
                        result.Add($"{owner}: null troop at index {i}");
                        continue;
                    }

                    string name = null;
                    try
                    {
                        name = character.Name?.ToString();
                    }
                    catch (Exception)
                    {
                        // An uninitialized object can throw from Name; treat as nameless.
                    }

                    if (!string.IsNullOrEmpty(name))
                        continue;

                    var id = character.StringId ?? "<no id>";
                    var registered =
                        !string.IsNullOrEmpty(character.StringId)
                        && manager.GetObject<CharacterObject>(character.StringId) != null;

                    // Registered-but-nameless custom stubs were just repaired by the
                    // canonicalization pass; anything still nameless here is beyond repair.
                    if (!registered)
                        result.Add($"{owner}: '{id}' (index {i})");
                }
            }

            var parties = MobileParty.All;
            if (parties != null)
                foreach (var party in parties)
                {
                    var label = party?.Name?.ToString() ?? party?.StringId ?? "party";
                    Scan(party?.Party?.MemberRoster, label);
                    Scan(party?.Party?.PrisonRoster, label + " (prisoners)");
                }

            var settlements = Settlement.All;
            if (settlements != null)
                foreach (var settlement in settlements)
                {
                    var label =
                        settlement?.Name?.ToString() ?? settlement?.StringId ?? "settlement";
                    Scan(settlement?.Party?.MemberRoster, label);
                    Scan(settlement?.Party?.PrisonRoster, label + " (prisoners)");
                }

            return result;
        }
    }
}
