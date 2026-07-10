using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Settings;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Retinues
{
    public partial class RetinuesBehavior
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("create_retinue", "retinues")]
        public static string CreateRetinueCommand(List<string> args)
        {
            if (args.Count < 2)
                return "Usage: create_retinue <culture_stringid> <retinue_name>";

            var cultureId = args[0];
            var retinueName = string.Join(" ", args.GetRange(1, args.Count - 1));

            var culture = WCulture.Get(cultureId);
            if (culture == null)
                return $"Error: Culture with stringid '{cultureId}' not found.";

            if (!TryGetInstance(out var behavior))
                return "Error: RetinuesBehavior is not registered in the current campaign.";

            var created = behavior.CreateRetinue(culture, retinueName);
            Player.Clan.AddRetinue(created);

            return $"Created new retinue '{retinueName}' for player clan based on culture '{culture.Name}'.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("unlock_retinue", "retinues")]
        public static string UnlockRetinueCommand(List<string> args)
        {
            if (args.Count < 1)
                return "Usage: unlock_retinue <culture_stringid>";

            var cultureId = args[0];

            var culture = WCulture.Get(cultureId);
            if (culture == null)
                return $"Error: Culture with stringid '{cultureId}' not found.";

            if (!Configuration.EnableRetinues)
                return "Error: retinues are disabled (EnableRetinues is off).";

            if (!TryGetInstance(out var behavior))
                return "Error: RetinuesBehavior is not registered in the current campaign.";

            // Flag the culture as unlocked so its unlock progress stops accruing (otherwise
            // workshops/fiefs in that culture keep counting toward a duplicate retinue), then
            // ensure the retinue exists (idempotent: returns the existing one if present).
            behavior.UnlockCulture(culture, showPopup: false);

            var retinue = behavior.EnsureRetinueExistsForCulture(culture);
            if (retinue?.Base == null)
                return $"Error: could not unlock a retinue for culture '{culture.Name}'.";

            return $"Unlocked retinue '{retinue.Name}' for player clan (culture '{culture.Name}').";
        }
    }
}
