using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace OldRetinues.Doctrines.Catalog
{
    public sealed class IronDiscipline : Doctrine
    {
        public override TextObject Name => L.T("iron_discipline", "Iron Discipline");
        public override TextObject Description =>
            L.T("iron_discipline_description", "+5 to skill caps.");
        public override int Column => 3;
        public override int Row => 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class ID_Upgrade100BasicToMax : Feat
        {
            public override TextObject Description =>
                L.T(
                    "iron_discipline_upgrade_100_basic_to_max",
                    "Upgrade 100 basic custom troops to max tier."
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
                if (!upgradeToTroop.IsCustom)
                    return;
                if (!upgradeToTroop.IsMaxTier)
                    return;

                AdvanceProgress(number);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class ID_LeadArmy10Days : Feat
        {
            public override TextObject Description =>
                L.T("iron_discipline_lead_army_10_days", "Lead an army for 10 days in a row.");
            public override int Target => 10;

            public override void OnDailyTick()
            {
                if (Player.IsArmyLeader)
                    AdvanceProgress(1);
                else
                    SetProgress(0);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class ID_DefeatTwiceSizeOnlyCustom : Feat
        {
            public override TextObject Description =>
                L.T(
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
                if (Player.Party.MemberRoster.CustomCount <= 0)
                    return; // Avoid trivial cases

                AdvanceProgress(1);
            }
        }
    }
}
