using Retinues.Core.Game.Events;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class CulturalPride : Doctrine
    {
        public override string Name => L.S("cultural_pride", "Cultural Pride");
        public override string Description =>
            L.S("cultural_pride_description", "10% rebate on items of the troop's culture.");
        public override int Column => 1;
        public override int Row => 0;

        public sealed class CP_TournamentOwnCultureGear : Feat
        {
            public override string Description =>
                L.S(
                    "cultural_pride_tournament_own_culture_gear",
                    "Win a tournament wearing only items of your own culture."
                );
            public override int Target => 1;

            public override void OnTournamentFinished(Tournament tournament)
            {
                if (tournament.Winner?.StringId != Player.Character.StringId)
                    return;

                foreach (var item in Player.Character.Equipment.Items)
                {
                    if (item.Culture?.StringId != Player.Culture.StringId)
                        return; // Item does not match player culture
                }

                AdvanceProgress(1);
            }
        }

        public sealed class CP_FullSet100Kills : Feat
        {
            public override string Description =>
                L.S(
                    "cultural_pride_full_set_100_kills",
                    "Have a troop type wearing a full set of items of their culture get 100 kills in battle."
                );
            public override int Target => 100;

            public override void OnBattleEnd(Battle battle)
            {
                foreach (var kill in battle.Kills)
                {
                    if (!kill.Killer.IsPlayerTroop)
                        continue; // Only consider player troops

                    var troop = kill.Killer.Character;

                    if (!troop.IsCustom)
                        continue; // Only consider custom troops

                    foreach (var item in troop.Equipment.Items)
                    {
                        if (item.Culture?.StringId != troop.Culture.StringId)
                            break; // Item does not match troop culture

                        AdvanceProgress(1);
                        break;
                    }
                }
            }
        }

        public sealed class CP_DefeatForeignMonarch : Feat
        {
            public override string Description =>
                L.S(
                    "cultural_pride_defeat_foreign_monarch",
                    "Defeat a monarch of a different culture in battle."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;

                foreach (var leader in battle.EnemyLeaders)
                {
                    if (!leader?.IsRuler ?? true)
                        continue; // Not a monarch

                    if (leader?.Culture.StringId == Player.Culture.StringId)
                        continue; // Same culture

                    AdvanceProgress(1);
                    break;
                }
            }
        }
    }
}
