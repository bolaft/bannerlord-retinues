using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Services.Matching;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Settings.Controllers
{
    /// <summary>
    /// Controller for destructive purge actions.
    /// </summary>
    public sealed class PurgeController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Purge All                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Purges all custom troops from all party rosters, either by removing or replacing them.
        /// </summary>
        public static ControllerAction<object> PurgeAll { get; } =
            Action<object>("PurgeAll")
                .DefaultTooltip(
                    L.T("purge_all_tooltip", "Purge all custom troop data from the game")
                )
                .ExecuteWith(_ => PurgeAllImpl());

        private static void PurgeAllImpl()
        {
            if (Campaign.Current == null)
            {
                Inquiries.Popup(
                    L.T("purge_title", "Purge Custom Troops"),
                    L.T("purge_need_campaign_reason", "No running campaign. Load a save first.")
                );
                return;
            }

            Inquiries.Popup(
                title: L.T("purge_confirm_title", "Purge Custom Troops"),
                onConfirm: () => ShowPurgeModePopup(),
                description: L.T(
                    "purge_confirm_body",
                    "This action is meant to help you uninstall Retinues safely.\n\nIt will scan every party roster in the current campaign and remove or replace ALL custom troops created by Retinues.\n\nThis action is irreversible. Make a backup save first. Continue?"
                ),
                confirmText: L.T("purge_continue", "Continue"),
                cancelText: GameTexts.FindText("str_cancel"),
                pauseGame: true
            );
        }

        private static void ShowPurgeModePopup()
        {
            Inquiries.Popup(
                title: L.T("purge_mode_title", "Purge Mode"),
                onChoice1: () => ExecutePurge(removeOnly: true),
                onChoice2: () => ExecutePurge(removeOnly: false),
                choice1Text: L.T("purge_mode_remove", "Remove"),
                choice2Text: L.T("purge_mode_replace", "Replace"),
                description: L.T(
                    "purge_mode_body",
                    "Do you want to simply remove custom troops from rosters, or try to replace them with their closest vanilla equivalents when possible?"
                ),
                pauseGame: true
            );
        }

        private sealed class PurgeStats
        {
            public int PartiesVisited;
            public int RosterEntriesProcessed;
            public int CustomEntriesFound;
            public int RemovedEntries;
            public int ReplacedEntries;
            public int ReplaceMisses;
        }

        private static void ExecutePurge(bool removeOnly)
        {
            var stats = new PurgeStats();

            try
            {
                foreach (var party in WParty.All)
                {
                    if (party == null)
                        continue;

                    stats.PartiesVisited += 1;

                    PurgeRoster(party, party.MemberRoster, removeOnly, stats);
                    PurgeRoster(party, party.PrisonRoster, removeOnly, stats);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "PurgeController.ExecutePurge failed.");
                Inquiries.Popup(
                    L.T("purge_failed_title", "Purge Failed"),
                    L.T(
                            "purge_failed_body",
                            "Something went wrong while purging. Your save may be partially modified. Do not save, reload your game and try again, or report the issue."
                        )
                        .SetTextVariable("ERROR", e.Message ?? "Unknown error")
                );
                return;
            }

            var advice =
                (removeOnly || stats.ReplaceMisses == 0)
                    ? L.T(
                        "purge_result_advice_ok",
                        "Purge complete. Save the game into a new file and you will be able to remove Retinues safely from your game."
                    )
                    : L.T(
                            "purge_result_advice_partial",
                            "Purge finished but some troops could not be replaced ({MISSES}) and were removed instead.\n\nConsider running again in Remove mode, or report this issue."
                        )
                        .SetTextVariable("MISSES", stats.ReplaceMisses);

            Inquiries.Popup(L.T("purge_done_title", "Purge Complete"), advice);
        }

        private static void PurgeRoster(
            WParty party,
            Domain.Parties.Models.MRoster roster,
            bool removeOnly,
            PurgeStats stats
        )
        {
            if (party == null || roster == null)
                return;

            // Snapshot to allow safe mutation.
            var elements = roster.Elements;
            if (elements == null || elements.Count == 0)
                return;

            for (int i = 0; i < elements.Count; i++)
            {
                var e = elements[i];
                if (e == null)
                    continue;

                stats.RosterEntriesProcessed += 1;

                var troop = e.Troop;
                if (troop == null)
                    continue;

                if (!troop.IsCustom)
                    continue;

                // Extra safety: never touch heroes.
                if (troop.IsHero)
                    continue;

                stats.CustomEntriesFound += 1;

                var number = e.Number;
                if (number <= 0)
                    continue;

                var wounded = Math.Min(e.WoundedNumber, number);
                var xp = e.Xp;

                if (removeOnly)
                {
                    roster.RemoveTroop(troop, number, wounded, xp, removeDepleted: true);
                    stats.RemovedEntries += 1;
                    continue;
                }

                var replacement = FindCultureVanillaReplacement(troop, party);

                roster.RemoveTroop(troop, number, wounded, xp, removeDepleted: true);
                stats.RemovedEntries += 1;

                if (replacement == null)
                {
                    stats.ReplaceMisses += 1;
                    continue;
                }

                roster.AddTroop(replacement, number, wounded, xp);
                stats.ReplacedEntries += 1;
            }
        }

        private static WCharacter FindCultureVanillaReplacement(WCharacter troop, WParty party)
        {
            if (troop == null)
                return null;

            var culture = troop.Culture ?? party?.Culture;
            if (culture == null)
                return null;

            // Materialize once; custom troops often have incomplete SourceFlags, so strict category
            // matching can filter everything out. We'll try strict first, then relax.
            var candidates = new List<WCharacter>();
            void AddRoster(List<WCharacter> roster)
            {
                if (roster == null)
                    return;

                for (int i = 0; i < roster.Count; i++)
                {
                    var c = roster[i];
                    if (c != null && c.IsVanilla && !c.IsHero)
                        candidates.Add(c);
                }
            }

            // "Culture tree" = the culture's two upgrade trees (basic+elite).
            AddRoster(culture.RosterBasic);
            AddRoster(culture.RosterElite);

            if (candidates.Count == 0)
                return null;

            // FindBest already considers tier/mounted/ranged/weapons/skills similarity.
            var strict = CharacterMatcher.FindBest(
                troop,
                candidates,
                strictTierMatch: false,
                strictCategoryMatch: true,
                fallback: null
            );

            if (strict != null)
                return strict;

            return CharacterMatcher.FindBest(
                troop,
                candidates,
                strictTierMatch: false,
                strictCategoryMatch: false,
                fallback: candidates[0]
            );
        }
    }
}
