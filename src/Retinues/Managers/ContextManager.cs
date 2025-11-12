using Retinues.Configuration;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Managers
{
    /// <summary>
    /// Context constraints for editing troops (location, ownership, etc).
    /// </summary>
    [SafeClass]
    public static class ContextManager
    {
        /// <summary>
        /// Returns true if editing is allowed in the current context.
        /// </summary>
        public static bool IsAllowedInContext(WCharacter troop, string action)
        {
            return State.IsStudioMode || GetContextReason(troop, action) == null;
        }

        /// <summary>
        /// Returns the reason why editing is not allowed, or null if allowed.
        /// </summary>
        public static TextObject GetContextReason(WCharacter troop, string action)
        {
            if (troop == null)
                return null;

            var faction = troop.Faction;
            if (faction == null)
                return null;
            if (Config.RestrictEditingToFiefs == false)
                return null;

            var settlement = Player.CurrentSettlement;

            if (troop.IsRetinue == true && faction == Player.Clan)
            {
                if (settlement != null)
                    return null;
                return L.T(
                        "not_in_settlement_text",
                        "You must be in a settlement to {ACTION} this troop."
                    )
                    .SetTextVariable("ACTION", action);
            }

            if (faction.IsPlayerClan)
            {
                if (settlement?.Clan == Player.Clan)
                    return null;
                return L.T(
                        "not_in_clan_fief_text",
                        "You must be in one of your clan's fiefs to {ACTION} this troop."
                    )
                    .SetTextVariable("ACTION", action);
            }

            if (faction.IsPlayerKingdom)
            {
                if (settlement?.Kingdom == Player.Kingdom)
                    return null;
                return L.T(
                        "not_in_kingdom_fief_text",
                        "You must be in one of your kingdom's fiefs to {ACTION} this troop."
                    )
                    .SetTextVariable("ACTION", action);
            }

            return null;
        }

        /// <summary>
        /// Shows a popup if editing is not allowed. Returns true if allowed.
        /// </summary>
        public static bool IsAllowedInContextWithPopup(WCharacter troop, string action)
        {
            if (State.IsStudioMode)
                return true;

            var reason = GetContextReason(troop, action);
            if (reason == null)
                return true;

            var faction = troop.Faction;

            TextObject title = L.T("not_allowed_title", "Not Allowed");
            if (troop.IsRetinue == true && faction == Player.Clan)
                title = L.T("not_in_settlement", "Not in Settlement");
            else if (faction.IsPlayerClan)
                title = L.T("not_in_clan_fief", "Not in Clan Fief");
            else if (faction.IsPlayerKingdom)
                title = L.T("not_in_kingdom_fief", "Not in Kingdom Fief");

            Notifications.Popup(title, reason);
            return false;
        }
    }
}
