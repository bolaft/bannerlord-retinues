using System.Linq;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class IronDiscipline : Doctrine
    {
        public override string Name => L.S("iron_discipline", "Iron Discipline");
        public override string Description => L.S("iron_discipline_description", "+5% skill cap.");
        public override int Column => 2;
        public override int Row => 0;

        public sealed class ID_Upgrade100BasicToMax : Feat
        {
            public override string Description => L.S("iron_discipline_upgrade_100_basic_to_max", "Upgrade 100 basic troops to max tier.");
            public override int Target => 100;

            public override void PlayerUpgradedTroops(WCharacter upgradeFromTroop, WCharacter upgradeToTroop, int number)
            {
                if (upgradeToTroop.IsElite) return;
                if (!upgradeToTroop.IsMaxTier) return;

                AdvanceProgress(number);
            }
        }

        public sealed class ID_HeadshotEnemyLord : Feat
        {
            public override string Description => L.S("iron_discipline_headshot_enemy_lord", "Headshot an enemy lord with a ranged weapon.");
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.Kills.Select(k =>
                    k.Killer.IsPlayer
                    && k.Victim.Character.IsHero
                    && k.Blow.IsMissile
                    && k.Blow.IsHeadShot()).Count() > 0)
                {
                    AdvanceProgress(1);
                }
            }
        }

        public sealed class ID_DefeatTwiceSizeMostlyCustom : Feat
        {
            public override string Description => L.S("iron_discipline_defeat_twice_size_mostly_custom", "Defeat a party twice your size using mostly custom troops.");
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost) return;
                if (battle.EnemyTroopCount < 2 * battle.FriendlyTroopCount) return;
                if (Player.Party.MemberRoster.CustomRatio < 0.75f) return;

                AdvanceProgress(1);
            }
        }
    }
}
