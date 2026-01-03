using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Campaign.Retinues
{
    public class RetinuesBehavior : BaseCampaignBehavior<RetinuesBehavior>
    {
        public override void RegisterEvents()
        {
            // New game: after character creation, first time you hit the campaign map.
            Hook(BehaviorEvent.CharacterCreationIsOver, EnsureDefaultRetinue);

            // Loaded save: when the campaign has finished loading.
            Hook(BehaviorEvent.GameLoadFinished, EnsureDefaultRetinue);
        }

        private void EnsureDefaultRetinue()
        {
            var hero = WHero.Get(Hero.MainHero);
            var clan = hero.Clan;

            if (clan.RosterRetinues.IsEmpty())
            {
                Log.Info("No retinues found for player clan; initializing default retinue.");
                clan.AddRetinue(
                    CreateRetinue(
                        clan.Culture,
                        L.T("retinue_default_name", "{CLAN} House Guard")
                            .SetTextVariable("CLAN", clan.Name)
                            .ToString()
                    )
                );
            }
        }

        private WCharacter CreateRetinue(WCulture culture, string name)
        {
            // Use the culture's root elite or basic troop as a template.
            var troop = culture.RootElite ?? culture.RootBasic;

            // Create the retinue clone.
            var retinue = TroopBuilder.CloneVanilla(troop, skills: true, equipments: true);

            // Rename
            retinue.Name = name;

            return retinue;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if DEBUG
        /// <summary>
        /// Create a new retinue for the player clan based on the specific culture StringId.
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("create_retinue", "retinues")]
        public static string CreateRetinue(List<string> args)
        {
            if (args.Count < 2)
                return "Usage: create_retinue <culture_stringid> <retinue_name>";

            var cultureId = args[0];
            var retinueName = string.Join(" ", args.GetRange(1, args.Count - 1));

            var culture = WCulture.Get(cultureId);
            if (culture == null)
                return $"Error: Culture with stringid '{cultureId}' not found.";

            var clan = WHero.Get(Hero.MainHero).Clan;
            var newRetinue = Instance.CreateRetinue(culture, retinueName);

            clan.AddRetinue(newRetinue);

            return $"Created new retinue '{retinueName}' for player clan based on culture '{culture.Name}'.";
        }
#endif
    }
}
