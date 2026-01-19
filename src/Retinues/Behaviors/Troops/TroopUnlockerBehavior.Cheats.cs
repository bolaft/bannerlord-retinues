using System.Collections.Generic;
using Retinues.Domain;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Troops
{
    public sealed partial class TroopUnlockerBehavior
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("unlock_clan_troops", "retinues")]
        public static string UnlockClanTroopsCommand(List<string> args)
        {
            var clan = Player.Clan;
            if (clan?.Base == null)
                return "Player clan not found.";

            if (clan.RootBasic != null && clan.RootElite != null)
                return "Player clan troops are already unlocked.";

            UnlockFactionTroops(clan, label: "clan", fromBootstrap: false);
            return "Player clan troops unlocked.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("unlock_kingdom_troops", "retinues")]
        public static string UnlockKingdomTroopsCommand(List<string> args)
        {
            var kingdom = Player.Kingdom;
            if (kingdom?.Base == null)
                return "Player kingdom not found.";

            if (kingdom.RootBasic != null && kingdom.RootElite != null)
                return "Player kingdom troops are already unlocked.";

            UnlockFactionTroops(kingdom, label: "kingdom", fromBootstrap: false);
            return "Player kingdom troops unlocked.";
        }
    }
}
