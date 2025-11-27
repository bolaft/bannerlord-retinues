using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class ArmedPeasantry : Doctrine
    {
        public override TextObject Name => L.T("armed_peasantry", "Armed Peasantry");
        public override TextObject Description =>
            L.T("armed_peasantry_description", "Unlocks villager troops.");
        public override int Column => 2;
        public override int Row => 2;
        public override bool IsDisabled => Config.NoDoctrineRequirements;
        public override TextObject DisabledMessage =>
            L.T(
                "armed_peasantry_disabled_message",
                "Disabled: special troops unlocked from config."
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AP_DefendVillageOnlyCustom : Feat
        {
            public override TextObject Description =>
                L.T(
                    "steadfast_soldiers_defend_village_only_custom",
                    "Defend a village from a raid using only custom troops."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (!battle.IsVillageRaid)
                    return;
                if (!battle.PlayerIsDefender)
                    return;
                if (Player.Party.MemberRoster.CustomRatio < 0.99f)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
