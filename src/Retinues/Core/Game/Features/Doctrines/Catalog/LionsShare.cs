using System.Linq;
using Retinues.Core.Game.Events;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class LionsShare : Doctrine
    {
        public override string Name => L.S("lions_share", "Lion's Share");
        public override string Description =>
            L.S("lions_share_description", "Hero kills count twice for unlocks.");
        public override int Column => 0;
        public override int Row => 0;

        public sealed class LS_25PersonalKills : Feat
        {
            public override string Description =>
                L.S("lions_share_25_personal_kills", "Personally defeat 25 enemies in one battle.");
            public override int Target => 25;

            public override void OnBattleEnd(Battle battle)
            {
                var playerKills = battle.Kills.Count(k => k.Killer.IsPlayer);

                if (playerKills > Progress)
                    SetProgress(playerKills);
            }
        }

        public sealed class LS_5Tier5Plus : Feat
        {
            public override string Description =>
                L.S(
                    "lions_share_5_tier_5_plus",
                    "Personally defeat 5 tier 5+ troops in one battle."
                );
            public override int Target => 5;

            public override void OnBattleEnd(Battle battle)
            {
                var playerKills = battle.Kills.Count(k =>
                    k.Killer.IsPlayer && k.Victim.Character.Tier >= 5
                );

                if (playerKills > Progress)
                    SetProgress(playerKills);
            }
        }

        public sealed class LS_KillEnemyLord : Feat
        {
            public override string Description =>
                L.S("lions_share_kill_enemy_lord", "Personally defeat an enemy lord in battle.");
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                var playerKills = battle.Kills.Count(k =>
                    k.Killer.IsPlayer && k.Victim.Character.IsHero
                );

                if (playerKills > Progress)
                    SetProgress(playerKills);
            }
        }
    }
}
