using System.Collections.Generic;
using System.Text;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Pages.Character.Controllers
{
    /// <summary>
    /// Controller for showing character statistics and related UI actions.
    /// </summary>
    public class StatisticsController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Statistics                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shows the battle history popup for the selected character.
        /// </summary>
        public static ControllerAction<WCharacter> ShowStatistics { get; } =
            Action<WCharacter>("ShowStatistics")
                .AddCondition(
                    _ => State.Mode == EditorMode.Player,
                    L.T("statistics_player_only", "Not available in the Universal Editor")
                )
                .ExecuteWith(ShowStatisticsImpl);

        /// <summary>
        /// Show the battle history for the given character.
        /// </summary>
        private static void ShowStatisticsImpl(WCharacter c)
        {
            if (c == null)
                return;

            c.GetHistory(
                out int won,
                out int lost,
                out int field,
                out int siege,
                out int naval,
                out int raids,
                out Dictionary<WCharacter, int> kills,
                out Dictionary<WCharacter, int> casualties
            );

            var sb = new StringBuilder(2048);

            // ── In service ──
            if (c.IsCustom && c.CreationDay > 0)
            {
                double elapsedDays = CampaignTime.Now.ToDays - c.CreationDay;
                int years = (int)(elapsedDays / CampaignTime.DaysInYear);
                int remainingDays = (int)(elapsedDays % CampaignTime.DaysInYear);
                int seasons = remainingDays / CampaignTime.DaysInSeason;
                int days = remainingDays % CampaignTime.DaysInSeason;

                sb.AppendLine(
                    L.T(
                            "statistics_in_service",
                            "{NAME} has been in service for {YEARS} years, {SEASONS} seasons, {DAYS} days."
                        )
                        .SetTextVariable("NAME", c.Name)
                        .SetTextVariable("YEARS", years)
                        .SetTextVariable("SEASONS", seasons)
                        .SetTextVariable("DAYS", days)
                        .ToString()
                );
                sb.AppendLine();
            }

            // ── Deployments ──
            int parties = 0;
            int garrisons = 0;
            int totalTroops = 0;

            foreach (var party in WParty.All)
            {
                int count = party.MemberRoster.CountOf(c);
                if (count == 0)
                    continue;

                totalTroops += count;

                if (party.IsGarrison)
                    garrisons++;
                else
                    parties++;
            }

            if (totalTroops > 0)
            {
                sb.AppendLine(
                    L.T(
                            "statistics_deployments",
                            "{TOTAL} troops deployed across {PARTIES} parties and {GARRISONS} garrisons."
                        )
                        .SetTextVariable("TOTAL", totalTroops)
                        .SetTextVariable("PARTIES", parties)
                        .SetTextVariable("GARRISONS", garrisons)
                        .ToString()
                );
                sb.AppendLine();
            }

            // ── Battle types ──
            var battleParts = new List<string>();
            if (field > 0)
                battleParts.Add(
                    L.T("statistics_field_entry", "{COUNT} field battles")
                        .SetTextVariable("COUNT", field)
                        .ToString()
                );
            if (siege > 0)
                battleParts.Add(
                    L.T("statistics_siege_entry", "{COUNT} sieges")
                        .SetTextVariable("COUNT", siege)
                        .ToString()
                );
            if (naval > 0)
                battleParts.Add(
                    L.T("statistics_naval_entry", "{COUNT} naval battles")
                        .SetTextVariable("COUNT", naval)
                        .ToString()
                );
            if (raids > 0)
                battleParts.Add(
                    L.T("statistics_raid_entry", "{COUNT} raids")
                        .SetTextVariable("COUNT", raids)
                        .ToString()
                );

            if (battleParts.Count > 0)
            {
                sb.AppendLine(
                    L.T("statistics_fought_in", "Fought in {LIST}.")
                        .SetTextVariable("LIST", FormatList(battleParts))
                        .ToString()
                );
                sb.AppendLine();
            }

            // ── Victories / defeats ──
            int totalBattles = won + lost;
            if (totalBattles > 0)
            {
                sb.AppendLine(
                    L.T(
                            "statistics_victories_defeats",
                            "Achieved {WON} victories, suffered {LOST} defeats."
                        )
                        .SetTextVariable("WON", won)
                        .SetTextVariable("LOST", lost)
                        .ToString()
                );
                sb.AppendLine();
            }

            // ── Kills ──
            int totalKills = SumValues(kills);
            if (totalKills > 0)
            {
                sb.AppendLine(
                    L.T("statistics_kills_sentence", "Killed {TOTAL} enemies: {BREAKDOWN}.")
                        .SetTextVariable("TOTAL", totalKills)
                        .SetTextVariable("BREAKDOWN", BuildCultureBreakdown(kills))
                        .ToString()
                );
                sb.AppendLine();
            }

            // ── Casualties ──
            int totalCasualties = SumValues(casualties);
            if (totalCasualties > 0)
            {
                sb.AppendLine(
                    L.T(
                            "statistics_casualties_sentence",
                            "Suffered {TOTAL} casualties: {BREAKDOWN}."
                        )
                        .SetTextVariable("TOTAL", totalCasualties)
                        .SetTextVariable("BREAKDOWN", BuildCultureBreakdown(casualties))
                        .ToString()
                );
                sb.AppendLine();
            }

            // ── Most slain / most feared ──
            var topKill = GetTopTroop(kills);
            var topCasualty = GetTopTroop(casualties);

            if (topKill != null || topCasualty != null)
            {
                var mostParts = new List<string>();

                if (topKill != null)
                    mostParts.Add(
                        L.T("statistics_most_slain", "Most slain enemy: {NAME}.")
                            .SetTextVariable("NAME", topKill.Name)
                            .ToString()
                    );
                if (topCasualty != null)
                    mostParts.Add(
                        L.T("statistics_most_feared", "Most feared adversary: {NAME}.")
                            .SetTextVariable("NAME", topCasualty.Name)
                            .ToString()
                    );

                sb.AppendLine(string.Join(" ", mostParts));
            }

            Inquiries.Popup(
                title: L.T("statistics_popup_title", "Statistics"),
                description: new TextObject(sb.ToString())
            );
        }

        private static int SumValues(Dictionary<WCharacter, int> dict)
        {
            if (dict == null)
                return 0;

            int total = 0;
            foreach (var kv in dict)
                total += kv.Value;
            return total;
        }

        private static string BuildCultureBreakdown(
            Dictionary<WCharacter, int> dict,
            int maxCultures = 5
        )
        {
            var byCulture = new Dictionary<string, int>();
            foreach (var kv in dict)
            {
                var cultureName = kv.Key?.Culture?.Name ?? "Unknown";
                if (byCulture.TryGetValue(cultureName, out int existing))
                    byCulture[cultureName] = existing + kv.Value;
                else
                    byCulture[cultureName] = kv.Value;
            }

            var sorted = new List<KeyValuePair<string, int>>(byCulture);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

            var parts = new List<string>();
            int shown = System.Math.Min(maxCultures, sorted.Count);
            int remaining = sorted.Count - shown;

            for (int i = 0; i < shown; i++)
                parts.Add(
                    L.T("statistics_culture_entry", "{COUNT} against {CULTURE}")
                        .SetTextVariable("COUNT", sorted[i].Value)
                        .SetTextVariable("CULTURE", sorted[i].Key)
                        .ToString()
                );

            if (remaining > 0)
                parts.Add(
                    L.T("statistics_culture_more", "{COUNT} more")
                        .SetTextVariable("COUNT", remaining)
                        .ToString()
                );

            return FormatList(parts);
        }

        private static WCharacter GetTopTroop(Dictionary<WCharacter, int> dict)
        {
            if (dict == null || dict.Count == 0)
                return null;

            WCharacter top = null;
            int topCount = 0;

            foreach (var kv in dict)
            {
                if (top == null || kv.Value > topCount)
                {
                    top = kv.Key;
                    topCount = kv.Value;
                }
            }

            return top;
        }

        private static string FormatList(List<string> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            if (items.Count == 1)
                return items[0];

            if (items.Count == 2)
                return items[0] + " and " + items[1];

            var sb = new StringBuilder();
            for (int i = 0; i < items.Count - 1; i++)
            {
                sb.Append(items[i]);
                sb.Append(", ");
            }
            sb.Append("and ");
            sb.Append(items[items.Count - 1]);
            return sb.ToString();
        }
    }
}
