using System.Linq;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class MastersAtArms : Doctrine
    {
        public override string Name => "Masters-At-Arms";
        public override string Description => "+1 upgrade branch for elite troops.";
        public override int Column => 2;
        public override int Row => 2;

        public sealed class MAA_Upgrade100EliteToMax : Feat
        {
            public override string Description => "Upgrade 100 elite troops to max tier.";
            public override int Target => 100;

            public override void PlayerUpgradedTroops(WCharacter upgradeFromTroop, WCharacter upgradeToTroop, int number)
            {
                if (!upgradeToTroop.IsElite) return;
                if (!upgradeToTroop.IsMaxTier) return;

                AdvanceProgress(number);
            }
        }

        public sealed class MAA_DefeatLordOnlyElite : Feat
        {
            public override string Description => "Defeat an enemy lord using only elite troops.";
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost) return;
                if (Player.Party.MemberRoster.EliteRatio < 1.0f) return;

                foreach (var leader in battle.EnemyLeaders)
                {
                    if (leader.IsHero)
                    {
                        AdvanceProgress(1);
                        return;
                    }
                }
            }
        }

        public sealed class MAA_1000EliteKills : Feat
        {
            public override string Description => "Get 1000 kills with elite troops.";
            public override int Target => 1000;

            public override void OnBattleEnd(Battle battle)
            {
                int killsByElites = battle.Kills.Count(k => k.Killer.IsPlayerTroop && k.Killer.Character.IsElite);
                AdvanceProgress(killsByElites);
            }
        }
    }
}
