using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Campaign.Retinues
{
    public class RetinuesBehavior : BaseCampaignBehavior
    {
        public override void RegisterEvents()
        {
            Hook(BehaviorEvent.GameLoadFinished, OnGameLoadFinished);
        }

        private void OnGameLoadFinished()
        {
            var hero = WHero.Get(Hero.MainHero);
            var clan = hero.Clan;

            if (clan.RosterRetinues.IsEmpty())
            {
                Log.Info("No retinues found for player clan; initializing default retinue.");
                clan.SetRetinues(
                    [
                        CreateRetinue(
                            clan.Culture,
                            L.T("retinue_default_name", "{CLAN} House Guard")
                                .SetTextVariable("CLAN", clan.Name)
                                .ToString()
                        ),
                    ]
                );
            }
        }

        private WCharacter CreateRetinue(WCulture culture, string name)
        {
            // Use the culture's root elite or basic troop as a template.
            var troop = culture.RootElite ?? culture.RootBasic;

            // Clone the troop, copying only the first of each equipment type.
            var retinue = troop.Clone(equipments: EquipmentCopyMode.FirstOfEach);

            // Rename
            retinue.Name = name;

            return retinue;
        }
    }
}
