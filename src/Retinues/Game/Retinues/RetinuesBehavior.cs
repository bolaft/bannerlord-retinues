using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Game.Troops;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Game.Retinues
{
    /// <summary>
    /// Retinue management behavior.
    /// Keeps the retinue creation entrypoint but does not auto-run on campaign start.
    /// </summary>
    public class RetinuesBehavior : BaseCampaignBehavior<RetinuesBehavior>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Retinue Creation                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter EnsureDefaultRetinue(WClan clan, string name, bool notifyUnlocks = true)
        {
            if (!Settings.EnableRetinues)
                return null;

            if (clan?.Base == null)
                return null;

            if (!clan.RosterRetinues.IsEmpty())
                return null;

            var retinue = CreateRetinue(clan.Culture, name, notifyUnlocks);
            if (retinue?.Base == null)
                return null;

            clan.AddRetinue(retinue);
            return retinue;
        }

        public WCharacter CreateRetinue(WCulture culture, string name, bool notifyUnlocks = true)
        {
            if (!Settings.EnableRetinues)
                return null;

            var template = culture?.RootElite ?? culture?.RootBasic;
            if (template?.Base == null)
                return null;

            return TroopCloner.BuildFromTemplate(
                template,
                new TroopCloner.TroopBuildRequest
                {
                    Name = name,
                    CultureContext = culture,
                    CopySkills = true,
                    CreateCivilianSet = true,
                    NotifyUnlocks = notifyUnlocks,
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if DEBUG
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

            // Create + equip (settings-driven by TroopBuilder).
            var created = behavior.CreateRetinue(culture, retinueName);

            Player.Clan.AddRetinue(created);

            return $"Created new retinue '{retinueName}' for player clan based on culture '{culture.Name}'.";
        }
#endif
    }
}
