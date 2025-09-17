using System.Linq;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Features.Doctrines.Model;
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class ID_Upgrade100BasicToMax : Feat
        {
            public override string Description =>
                L.S(
                    "iron_discipline_upgrade_100_basic_to_max",
                    "Upgrade 100 basic troops to max tier."
                );
            public override int Target => 100;

            public override void PlayerUpgradedTroops(
                WCharacter upgradeFromTroop,
                WCharacter upgradeToTroop,
                int number
            )
            {
                if (upgradeToTroop.IsElite)
                    return;
                if (!upgradeToTroop.IsMaxTier)
                    return;

                AdvanceProgress(number);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class ID_Troops50ArenaKOs : Feat
        {
            public override string Description =>
                L.S(
                    "iron_discipline_troops_50_arena_kos",
                    "Have your custom troops knock out 50 opponents in the arena."
                );
            public override int Target => 1;

            public override void OnArenaEnd(Combat combat)
            {
                int koCount = combat.Kills.Count(k => k.Killer.Character.IsCustom);
                Log.Info($"ID_Troops50ArenaKOs: counted {koCount} KOs from custom in arena match");
                AdvanceProgress(koCount);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class ID_DefeatTwiceSizeOnlyCustom : Feat
        {
            public override string Description =>
                L.S(
                    "iron_discipline_defeat_twice_size_only_custom",
                    "Defeat a party twice your size using only custom troops."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (battle.EnemyTroopCount < 2 * battle.FriendlyTroopCount)
                    return;
                if (Player.Party.MemberRoster.CustomRatio < 0.99f)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
