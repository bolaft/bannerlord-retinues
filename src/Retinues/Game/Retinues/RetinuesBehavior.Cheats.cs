using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Factions.Wrappers;
using TaleWorlds.Library;

namespace Retinues.Game.Retinues
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
    }
}
