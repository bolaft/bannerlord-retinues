using System.Collections.Generic;
using System.Text;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
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

            int parties = 0;
            int garrisons = 0;

            foreach (var party in WParty.All)
            {
                if (party.MemberRoster.CountOf(c) == 0)
                    continue;

                if (party.IsGarrison)
                    garrisons++;
                else
                    parties++;
            }

            sb.AppendLine(
                    L.T(
                            "statistics_parties_garrisons",
                            "Deployments: {TOTAL} ({PARTIES} parties, {GARRISONS} garrisons)"
                        )
                        .SetTextVariable("TOTAL", parties + garrisons)
                        .SetTextVariable("PARTIES", parties)
                        .SetTextVariable("GARRISONS", garrisons)
                        .ToString()
                )
                .AppendLine();

            var totalBattles = won + lost;

            sb.AppendLine(
                    L.T(
                            "statistics_battles_fought",
                            "Battles fought: {TOTAL} (won {WON}, lost {LOST})"
                        )
                        .SetTextVariable("TOTAL", totalBattles)
                        .SetTextVariable("WON", won)
                        .SetTextVariable("LOST", lost)
                        .ToString()
                )
                .AppendLine();

            // Battle type lines (only if > 0)
            if (field > 0)
                sb.AppendLine(FormatEntry(L.T("statistics_battles_field", "Field Battles"), field));
            if (siege > 0)
                sb.AppendLine(FormatEntry(L.T("statistics_battles_siege", "Siege Battles"), siege));
            if (naval > 0)
                sb.AppendLine(FormatEntry(L.T("statistics_battles_naval", "Naval Battles"), naval));
            if (raids > 0)
                sb.AppendLine(FormatEntry(L.T("statistics_battles_raids", "Raids"), raids));

            sb.AppendLine();

            AppendTopInlineTotal(sb, L.T("statistics_kills_title", "Kills"), kills);

            sb.AppendLine();

            AppendTopInlineTotal(sb, L.T("statistics_casualties_title", "Casualties"), casualties);
            Inquiries.Popup(
                title: L.T("statistics_popup_title", "Statistics"),
                description: new TextObject(sb.ToString())
            );
        }

        private static string FormatEntry(TextObject label, int count)
        {
            return L.T("statistics_line_entry", "- {LABEL}: {COUNT}")
                .SetTextVariable("LABEL", label.ToString())
                .SetTextVariable("COUNT", count)
                .ToString();
        }

        /// <summary>
        /// Appends a section in the format:
        /// Kills: 4
        /// - Looter: 4
        /// </summary>
        private static void AppendTopInlineTotal(
            StringBuilder sb,
            TextObject title,
            Dictionary<WCharacter, int> dict,
            int maxRows = 10
        )
        {
            int total = 0;

            if (dict != null && dict.Count > 0)
            {
                foreach (var kv in dict)
                    total += kv.Value;
            }

            sb.AppendLine(
                L.T("statistics_section_total", "{TITLE}: {TOTAL}")
                    .SetTextVariable("TITLE", title.ToString())
                    .SetTextVariable("TOTAL", total)
                    .ToString()
            );

            if (total <= 0 || dict == null || dict.Count == 0)
                return;

            sb.AppendLine();

            // Build list + sort desc
            var list = new List<KeyValuePair<WCharacter, int>>(dict.Count);
            foreach (var kv in dict)
                list.Add(kv);

            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            var rows = System.Math.Min(maxRows, list.Count);

            for (int i = 0; i < rows; i++)
            {
                var target = list[i].Key;
                var count = list[i].Value;

                sb.AppendLine(
                    L.T("statistics_entry", "- {NAME}: {COUNT}")
                        .SetTextVariable("NAME", target.Name)
                        .SetTextVariable("COUNT", count)
                        .ToString()
                );
            }

            if (list.Count > rows)
            {
                sb.AppendLine(
                    L.T("statistics_more", "({COUNT} more...)")
                        .SetTextVariable("COUNT", list.Count - rows)
                        .ToString()
                );
            }
        }
    }
}
