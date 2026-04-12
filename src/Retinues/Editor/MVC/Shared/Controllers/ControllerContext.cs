using Retinues.Domain;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Interface.Services;
using Retinues.Settings;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Shared.Controllers
{
    /// <summary>
    /// Convenience extensions to attach the restriction guard to ControllerActions.
    /// </summary>
    [SafeClass]
    public static class ContextRestrictionControllerActionExtensions
    {
        /// <summary>
        /// Adds a centralized condition that blocks the action when the current troop
        /// is not editable under the configured Player mode restriction.
        /// </summary>
        public static ControllerAction<TArg> RequireValidEditingContext<TArg>(
            this ControllerAction<TArg> action
        )
        {
            return action.AddCondition(
                applies: _ => ContextRestrictionService.AppliesToCurrentSelection(),
                test: _ => ContextRestrictionService.GetCharacterEditingBlockReason() == null,
                reasonFactory: _ => ContextRestrictionService.GetCharacterEditingBlockReason()
            );
        }
    }

    /// <summary>
    /// Service for checking context restrictions in the editor.
    /// </summary>
    [SafeClass]
    public static class ContextRestrictionService
    {
        /// <summary>
        /// Determines if the current selection is a valid character for editing restrictions.
        /// </summary>
        public static bool AppliesToCurrentSelection()
        {
            var state = EditorState.Instance;

            if (state.Mode != EditorMode.Player)
                return false;

            var c = state.Character;
            if (c == null)
                return false;

            if (c.IsHero)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a compact reason for row-level display (e.g. equipment list rows), or null if editing is allowed.
        /// </summary>
        public static TextObject GetCharacterEditingBlockReasonShort()
        {
            if (!AppliesToCurrentSelection())
                return null;

            var restriction = Configuration.EditingRestriction;
            if (restriction == Configuration.EditingRestrictionMode.None)
                return null;

            var settlement = Player.CurrentSettlement;
            if (settlement == null)
                return L.T("editing_restriction_need_settlement_reason_short", "Not in settlement");

            var character = EditorState.Instance.Character;
            if (
                restriction == Configuration.EditingRestrictionMode.InSettlement
                || character.IsRetinue
            )
                return null;

            var faction = character.AssignedMapFaction;
            if (faction == null)
                return L.T("editing_restriction_unknown_faction_reason_short", "Unknown faction");

            if (IsCharacterFactionOwnedFief(settlement, faction))
                return null;

            return L.T("editing_restriction_need_faction_fief_reason_short", "Not in fief");
        }

        /// <summary>
        /// Gets the reason why the current character cannot be edited under the configured restrictions, or null
        /// if editing is allowed.
        /// </summary>
        public static TextObject GetCharacterEditingBlockReason()
        {
            if (!AppliesToCurrentSelection())
                return null;

            var restriction = Configuration.EditingRestriction;
            if (restriction == Configuration.EditingRestrictionMode.None)
                return null;

            var character = EditorState.Instance.Character;
            var faction = character.AssignedMapFaction;
            var settlement = Player.CurrentSettlement;

            if (settlement == null)
            {
                return restriction == Configuration.EditingRestrictionMode.InFief && faction != null
                    ? L.T(
                            "editing_restriction_need_faction_fief_reason",
                            "You can only edit this unit in a fief owned by {FACTION}."
                        )
                        .SetTextVariable("FACTION", faction.Name)
                    : L.T(
                        "editing_restriction_need_settlement_reason",
                        "You can only edit units while inside a settlement."
                    );
            }

            if (
                restriction == Configuration.EditingRestrictionMode.InSettlement
                || character.IsRetinue
            )
                return null; // Any settlement is fine for retinues or InSettlement mode.

            // InFief: require a town/castle/village owned by the troop's clan/kingdom.
            if (faction == null)
            {
                return L.T(
                    "editing_restriction_unknown_faction_reason",
                    "Cannot determine this unit's faction for editing restrictions."
                );
            }

            if (IsCharacterFactionOwnedFief(settlement, faction))
                return null;

            return L.T(
                    "editing_restriction_need_faction_fief_reason",
                    "You can only edit this unit in a fief owned by {FACTION}."
                )
                .SetTextVariable("FACTION", faction.Name);
        }

        /// <summary>
        /// Determines if the given settlement is a fief owned by the given troop faction.
        /// </summary>
        private static bool IsCharacterFactionOwnedFief(
            WSettlement settlement,
            IBaseFaction troopFaction
        )
        {
            if (settlement == null || troopFaction == null)
                return false;

            // Treat "fief" as town/castle/village (exclude hideouts etc).
            if (!settlement.IsTown && !settlement.IsCastle && !settlement.IsVillage)
                return false;

            var ownerClan = settlement.Clan;
            if (ownerClan == null)
                return false;

            // Clan troop: must be in a fief owned by that clan.
            if (troopFaction is WClan clan)
                return ownerClan.StringId == clan.StringId;

            // Kingdom troop: must be in a fief whose owner clan belongs to that kingdom.
            if (troopFaction is WKingdom kingdom)
            {
                var ownerKingdomBase = ownerClan.Base?.Kingdom;
                if (ownerKingdomBase == null)
                    return false;

                var ownerKingdom = WKingdom.Get(ownerKingdomBase);
                return ownerKingdom != null && ownerKingdom.StringId == kingdom.StringId;
            }

            return false;
        }
    }
}
