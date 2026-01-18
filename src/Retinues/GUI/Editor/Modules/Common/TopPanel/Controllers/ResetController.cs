using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Common.TopPanel.Helpers;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.GUI.Editor.Modules.Common.TopPanel.Controllers
{
    /// <summary>
    /// Controller for resetting either the selected faction's troops or a specific troop of that faction,
    /// based on a user choice popup.
    /// </summary>
    public class ResetController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Reset                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Controller action to start the reset flow (choose faction or troop).
        /// </summary>
        public static ControllerAction<bool> Reset { get; } =
            Action<bool>("Reset")
                .DefaultTooltip(L.T("button_reset_tooltip", "Reset to defaults."))
                .AddCondition(
                    _ => State.Mode == EditorMode.Universal,
                    L.T("reset_default_universal_only", "Only available in the universal editor.")
                )
                .AddCondition(
                    _ => CanResetFaction() || CanResetAnyTroop(),
                    L.T("reset_none_available", "Nothing can be reset right now.")
                )
                .ExecuteWith(_ => ShowResetTargetPopup())
                .Fire(UIEvent.Faction);

        /// <summary>
        /// Returns true when the current state has a faction with resettable modified troops.
        /// </summary>
        private static bool CanResetFaction()
        {
            if (State.Faction == null)
                return false;

            return HasResettableDirtyTroops(State.Faction);
        }

        /// <summary>
        /// Returns true when the given troop can be reset.
        /// </summary>
        private static bool CanResetTroop(WCharacter c)
        {
            if (c == null)
                return false;

            if (c.IsHero)
                return false;

            if (!c.IsVanilla)
                return false;

            return c.IsDirty;
        }

        /// <summary>
        /// Returns true when any troop in the current faction can be reset.
        /// </summary>
        private static bool CanResetAnyTroop()
        {
            var faction = State.Faction;
            if (faction == null)
                return false;

            foreach (var wc in faction.Troops)
            {
                if (CanResetTroop(wc))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Show a popup prompting the user to choose reset target (faction or troop).
        /// </summary>
        private static void ShowResetTargetPopup()
        {
            var faction = State.Faction;
            if (faction == null)
                return;

            var elements = TargetsHelper.BuildFactionAndTroopsElements(
                faction: faction,
                isFactionEnabled: _ => CanResetFaction(),
                factionDisabledHint: L.S(
                    "reset_target_faction_disabled",
                    "No modified vanilla troops found to reset."
                ),
                isTroopEnabled: CanResetTroop,
                troopDisabledHint: _ =>
                    L.S(
                        "reset_target_troop_disabled",
                        "Only modified, non-hero vanilla troops can be reset."
                    )
            );

            Inquiries.SelectPopup(
                title: L.T("reset_choose_target_title", "Reset To Default"),
                description: L.T(
                    "reset_choose_target_body",
                    "Reset which of the following to default values?"
                ),
                elements: elements,
                onSelect: element =>
                {
                    if (element?.Identifier is not Target target)
                        return;

                    switch (target.Kind)
                    {
                        case TargetKind.Faction:
                            ResetFactionToDefaultImpl();
                            break;
                        case TargetKind.Troop:
                            ResetTroopToDefaultImpl(target.Troop);
                            break;
                    }

                    EventManager.Fire(UIEvent.Faction);
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Reset Faction                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Checks whether the faction contains any non-hero vanilla troops marked as dirty.
        /// </summary>
        private static bool HasResettableDirtyTroops(IBaseFaction faction)
        {
            if (faction == null)
                return false;

            foreach (var troop in faction.Troops)
            {
                if (!CanResetTroop(troop))
                    continue;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Schedule reset of modified vanilla troops in the selected faction.
        /// </summary>
        private static void ResetFactionToDefaultImpl()
        {
            var faction = State.Faction;
            if (faction == null)
                return;

            var toClean = new List<WCharacter>();

            foreach (var troop in faction.Troops)
            {
                if (!CanResetTroop(troop))
                    continue;

                toClean.Add(troop);
            }

            if (toClean.Count == 0)
                return;

            var title = L.T("reset_faction_title", "Reset To Default");
            var body = L.T(
                    "reset_faction_body",
                    "Reset all troops in {FACTION} to their default values?\n\nThis will undo all edits for {COUNT} troop(s).\n\nSave and restart the game for this action to take effect."
                )
                .SetTextVariable("FACTION", faction.Name)
                .SetTextVariable("COUNT", toClean.Count);

            Inquiries.Popup(
                title: title,
                description: body,
                confirmText: L.T("reset_confirm", "Reset"),
                cancelText: GameTexts.FindText("str_cancel"),
                onConfirm: () =>
                {
                    for (int i = 0; i < toClean.Count; i++)
                    {
                        try
                        {
                            toClean[i].MarkAllAttributesClean();
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(ex);
                        }
                    }

                    Inquiries.Popup(
                        title: L.T("reset_done_title", "Reset Scheduled"),
                        description: L.T(
                            "reset_done_body",
                            "Save and restart the game for this action to take effect."
                        )
                    );

                    EventManager.Fire(UIEvent.Faction);
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Reset Troop                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Schedule reset of the specified troop back to vanilla defaults.
        /// </summary>
        private static void ResetTroopToDefaultImpl(WCharacter c)
        {
            if (!CanResetTroop(c))
                return;

            var title = L.T("reset_character_title", "Reset To Default");
            var body = L.T(
                    "reset_character_body",
                    "Reset {NAME} to its default values?\n\nThis will undo all edits for this character.\n\nSave and restart the game for this action to take effect."
                )
                .SetTextVariable("NAME", c.Name);

            Inquiries.Popup(
                title: title,
                description: body,
                confirmText: L.T("reset_confirm", "Reset"),
                cancelText: GameTexts.FindText("str_cancel"),
                onConfirm: () =>
                {
                    try
                    {
                        c.MarkAllAttributesClean();
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }

                    Inquiries.Popup(
                        title: L.T("reset_done_title", "Reset Scheduled"),
                        description: L.T(
                            "reset_done_body",
                            "Save and restart the game for this action to take effect."
                        )
                    );

                    EventManager.Fire(UIEvent.Character);
                }
            );
        }
    }
}
