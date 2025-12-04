using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class Ironclad : Doctrine
    {
        public override TextObject Name => L.T("ironclad", "Ironclad");
        public override TextObject Description =>
            L.T("ironclad_description", "No tier restriction for arms and armor.");
        public override int Column => 1;
        public override int Row => 3;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class IC_FullSetT6100Kills : Feat
        {
            public override TextObject Description =>
                L.T(
                    "ironclad_full_set_t6_100_kills",
                    "Get 100 kills in battle with troops wearing a tier 6 armor and helmet."
                );
            public override int Target => 100;

            public override void OnBattleEnd(Battle battle)
            {
                foreach (var kill in battle.Kills)
                {
                    if (!kill.KillerIsPlayerTroop)
                        continue; // Only consider player troops

                    var troop = kill.Killer;

                    if (!troop.IsCustom)
                        continue; // Only consider custom troops

                    var hasFullSet = true;

                    foreach (var equipment in troop.Loadout.Equipments)
                    {
                        if (equipment.Get(EquipmentIndex.Head).Tier < 6)
                        {
                            hasFullSet = false; // Helmet does not meet tier requirement
                            break;
                        }

                        if (equipment.Get(EquipmentIndex.Body).Tier < 6)
                        {
                            hasFullSet = false; // Body armor does not meet tier requirement
                            break;
                        }
                    }

                    if (hasFullSet)
                        AdvanceProgress(1);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class IC_12TroopsAthletics90 : Feat
        {
            public override TextObject Description =>
                L.T(
                    "ironclad_12_troops_athletics_90",
                    "Have 12 custom troops reach 90 in Athletics skill."
                );
            public override int Target => 12;

            public override void OnDailyTick()
            {
                int count = 0;
                foreach (var troop in Player.Troops)
                    if (troop.GetSkill(DefaultSkills.Athletics) >= 90)
                        count++;
                SetProgress(count);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class IC_100BattleOutnumberedLowTier : Feat
        {
            public override TextObject Description =>
                L.T(
                    "ironclad_100_battle_outnumbered_low_tier",
                    "Win a 100+ battle, while outnumbered, and fielding only custom troops of tier 1, 2, or 3."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (battle.TotalTroopCount < 100)
                    return;
                if (battle.FriendlyTroopCount >= battle.EnemyTroopCount)
                    return;

                foreach (var e in Player.Party.MemberRoster.Elements)
                {
                    if (e.Troop.IsHero)
                        continue; // Ignore heroes
                    if (!e.Troop.IsCustom)
                        return; // Not a custom troop
                    if (e.Troop.Tier > 3)
                        return; // Troop tier too high
                }

                AdvanceProgress(1);
            }
        }
    }
}
