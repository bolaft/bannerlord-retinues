using Retinues.Core.Game.Events;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class Indomitable : Doctrine
    {
        public override string Name => L.S("indomitable", "Indomitable");
        public override string Description =>
            L.S("indomitable_description", "+20% retinue health.");
        public override int Column => 3;
        public override int Row => 0;

        public sealed class IND_25EquivNoCasualty : Feat
        {
            public override string Description =>
                L.S(
                    "indomitable_25_equiv_no_casualty",
                    "Have your retinues defeat 25 enemy troops of equivalent tier without a single casualty."
                );
            public override int Target => 25;

            public override void OnBattleEnd(Battle battle)
            {
                foreach (var kill in battle.Kills)
                {
                    if (kill.Victim.IsPlayerTroop)
                        if (kill.Victim.Character.IsRetinue)
                            SetProgress(0); // Reset progress on any retinue casualty
                        else if (kill.Killer.IsPlayerTroop)
                            if (kill.Killer.Character.IsRetinue)
                                if (kill.Victim.Character.Tier >= kill.Killer.Character.Tier)
                                    AdvanceProgress(1);
                }
            }
        }

        public sealed class IND_JoinSiegeDefenderFullStrength : Feat
        {
            public override string Description =>
                L.S(
                    "indomitable_join_siege_defender_full_strength",
                    "Fight a siege battle as a defender with at least 10 retinue troops and win."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (!battle.IsSiege)
                    return;
                if (!battle.PlayerIsDefender)
                    return;
                if (Player.Party.MemberRoster.RetinueCount < 10)
                    return;

                AdvanceProgress(1);
            }
        }

        public sealed class IND_RetinueOnly3DefWins : Feat
        {
            public override string Description =>
                L.S(
                    "indomitable_win_3_defensive_battles",
                    "Win 3 defensive battles in a row with a retinue-only party."
                );
            public override int Target => 3;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    SetProgress(0); // Reset progress on loss
                else if (Player.Party.MemberRoster.RetinueRatio < 1.0f)
                    SetProgress(0); // Reset progress if not retinue-only
                else if (battle.PlayerIsDefender)
                    AdvanceProgress(1); // Advance progress on win
            }
        }
    }
}
