using System.Linq;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Features.Doctrines.Model;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class MastersAtArms : Doctrine
    {
        public override string Name => L.S("masters_at_arms", "Masters-At-Arms");
        public override string Description =>
            L.S("masters_at_arms_description", "+1 upgrade branch for elite troops.");
        public override int Column => 2;
        public override int Row => 2;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class MAA_Upgrade100EliteToMax : Feat
        {
            public override string Description =>
                L.S(
                    "masters_at_arms_upgrade_100_elite_to_max",
                    "Upgrade 100 elite troops to max tier."
                );
            public override int Target => 100;

            public override void PlayerUpgradedTroops(
                WCharacter upgradeFromTroop,
                WCharacter upgradeToTroop,
                int number
            )
            {
                if (!upgradeToTroop.IsElite)
                    return;
                if (!upgradeToTroop.IsMaxTier)
                    return;

                AdvanceProgress(number);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class MAA_KO50Opponents : Feat
        {
            public override string Description =>
                L.S("masters_at_arms_ko_50_opponents", "Knock out 50 opponents in the arena.");
            public override int Target => 50;

            public override void OnArenaEnd(Combat combat)
            {
                int koCount = combat.Kills.Count(k => k.Killer.IsPlayer);
                Log.Info($"MAA_KO50Opponents: counted {koCount} KOs in arena match");
                AdvanceProgress(koCount);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class MAA_1000EliteKills : Feat
        {
            public override string Description =>
                L.S("masters_at_arms_1000_elite_kills", "Get 1000 kills with elite troops.");
            public override int Target => 1000;

            public override void OnBattleEnd(Battle battle)
            {
                int killsByElites = battle.Kills.Count(k =>
                    k.Killer.IsPlayerTroop && k.Killer.Character.IsElite
                );
                AdvanceProgress(killsByElites);
            }
        }
    }
}
