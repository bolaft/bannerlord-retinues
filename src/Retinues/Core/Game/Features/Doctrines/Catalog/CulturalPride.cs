using Retinues.Core.Game.Events;
using Retinues.Core.Game.Features.Doctrines.Model;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class CulturalPride : Doctrine
    {
        public override string Name => L.S("cultural_pride", "Cultural Pride");
        public override string Description =>
            L.S("cultural_pride_description", "10% rebate on items of the troop's culture.");
        public override int Column => 1;
        public override int Row => 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CP_TournamentOwnCultureGear : Feat
        {
            public override string Description =>
                L.S(
                    "cultural_pride_tournament_own_culture_gear",
                    "Win a tournament wearing only armor of your own culture."
                );
            public override int Target => 1;

            public override void OnTournamentFinished(Tournament tournament)
            {
                if (tournament.Winner?.StringId != Player.Character.StringId)
                    return;

                foreach (var item in Player.Character.Equipment.Items)
                {
                    if (!item.IsArmor)
                        continue; // Ignore non-armor items
                    if (item.Culture?.StringId != Player.Culture.StringId)
                        return; // Item does not match player culture
                }

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CP_FullSet100Kills : Feat
        {
            public override string Description =>
                L.S(
                    "cultural_pride_full_set_100_kills",
                    "Get 100 kills in battle with troops wearing no foreign gear."
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

                    bool hasFullSet = true;
                    foreach (var item in troop.Equipment.Items)
                    {
                        if (item.Culture == null)
                            continue; // Ignore culture-less items
                        if (item.Culture?.StringId != troop.Culture.StringId)
                        {
                            hasFullSet = false;
                            break; // Item does not match troop culture
                        }
                    }

                    if (hasFullSet)
                        AdvanceProgress(1);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CP_DefeatForeignRuler : Feat
        {
            public override string Description =>
                L.S(
                    "cultural_pride_defeat_foreign_ruler",
                    "Defeat a ruler of a different culture in battle."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;

                foreach (var leader in battle.EnemyLeaders)
                {
                    if (!leader?.IsRuler ?? true)
                        continue; // Not a ruler

                    if (leader?.Culture.StringId == Player.Culture.StringId)
                        continue; // Same culture

                    AdvanceProgress(1);
                    break;
                }
            }
        }
    }
}
