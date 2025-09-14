using TaleWorlds.Core;
using Retinues.Core.Game.Events;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class Ironclad : Doctrine
    {
        public override string Name => "Ironclad";
        public override string Description => "No tier restriction for arms and armor.";
        public override int Column => 1;
        public override int Row => 3;

        public sealed class IC_FullSetT5Plus100Kills : Feat
        {
            public override string Description => "Equip a full set of tier 5+ items on a custom troop and have them get 100 kills in battle.";
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
                        if (item.Tier < 5)
                        {
                            hasFullSet = false;
                            break; // Item does not meet tier requirement
                        }
                    }

                    if (hasFullSet)
                        AdvanceProgress(1);
                }
            }
        }

        public sealed class IC_10TroopsAthletics80 : Feat
        {
            public override string Description => "Have 10 custom troops reach 80 in Athletics skill.";
            public override int Target => 10;

            public override void OnDailyTick()
            {
                int count = 0;
                foreach (var troop in Player.Troops)
                    if (troop.GetSkill(DefaultSkills.Athletics) >= 80)
                        count++;
                SetProgress(count);
            }
        }

        public sealed class IC_100BattleOutnumberedLowTier : Feat
        {
            public override string Description => "Win a 100+ battle, while outnumbered, and fielding only custom troops of tier 1, 2, or 3.";
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost) return;
                if (battle.TotalTroopCount < 100) return;
                if (battle.FriendlyTroopCount >= battle.EnemyTroopCount) return;

                foreach (var troop in Player.Party.MemberRoster.Elements)
                {
                    if (!troop.Troop.IsCustom) return; // Not a custom troop
                    if (troop.Troop.Tier > 3) return; // Troop tier too high
                }

                AdvanceProgress(1);
            }
        }
    }
}
