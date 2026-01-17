using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.GUI.Services;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Shared.Controllers
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

            if (c.IsRetinue)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the reason why the current character cannot be edited under the configured restrictions, or null
        /// if editing is allowed.
        /// </summary>
        public static TextObject GetCharacterEditingBlockReason()
        {
            if (!AppliesToCurrentSelection())
                return null;

            var restriction = Settings.EditingRestriction;
            if (restriction == Settings.EditingRestrictionMode.None)
                return null;

            var troopFaction = EditorState.Instance.Character?.AssignedMapFaction;
            var settlement = Player.CurrentSettlement;

            if (settlement == null)
            {
                return restriction == Settings.EditingRestrictionMode.InFief && troopFaction != null
                    ? L.T(
                            "editing_restriction_need_faction_fief_reason",
                            "You can only edit this unit in a fief owned by {FACTION}."
                        )
                        .SetTextVariable("FACTION", troopFaction.Name)
                    : L.T(
                        "editing_restriction_need_settlement_reason",
                        "You can only edit units while inside a settlement."
                    );
            }

            if (restriction == Settings.EditingRestrictionMode.InSettlement)
                return null;

            // InFief: require a town/castle/village owned by the troop's clan/kingdom.
            if (troopFaction == null)
            {
                return L.T(
                    "editing_restriction_unknown_faction_reason",
                    "Cannot determine this unit's faction for editing restrictions."
                );
            }

            if (IsCharacterFactionOwnedFief(settlement, troopFaction))
                return null;

            return L.T(
                    "editing_restriction_need_faction_fief_reason",
                    "You can only edit this unit in a fief owned by {FACTION}."
                )
                .SetTextVariable("FACTION", troopFaction.Name);
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
