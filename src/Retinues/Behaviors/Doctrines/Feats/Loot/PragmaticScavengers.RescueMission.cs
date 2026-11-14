using Retinues.Domain.Events.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Doctrines.Feats.Loot
{
    /// <summary>
    /// Rescue an allied lord through a battle victory or a successful prison break.
    /// </summary>
    public sealed class Feat_PragmaticScavengers_RescueMission : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.PR_RescueMission.Id;

        private MapEvent _battle;
        private bool _hasCaptiveAlliedLord;

        protected override void OnBattleStart(MMapEvent battle)
        {
            _battle = battle.Base;
            _hasCaptiveAlliedLord = false;

            if (battle.PlayerSideEnum == BattleSideEnum.None)
                return;

            // Defeated prisoners are released before MapEventEnded. Snapshot their presence
            // while rosters are intact, including when the player joins an ongoing battle.
            var enemySide = _battle.GetMapEventSide(
                battle.PlayerSideEnum == BattleSideEnum.Attacker
                    ? BattleSideEnum.Defender
                    : BattleSideEnum.Attacker
            );
            if (enemySide?.Parties == null)
                return;

            foreach (var party in enemySide.Parties)
            {
                var roster = party?.Party?.PrisonRoster;
                if (roster == null)
                    continue;

                for (int i = 0; i < roster.Count; i++)
                {
                    var entry = roster.GetElementCopyAtIndex(i);
                    if (entry.Number <= 0 || !IsAlliedLord(entry.Character?.HeroObject))
                        continue;

                    _hasCaptiveAlliedLord = true;
                    return;
                }
            }
        }

        protected override void OnMapEventEnded(MMapEvent end)
        {
            if (end?.IsPlayerInvolved != true || !ReferenceEquals(_battle, end.Base))
                return;

            // This feat needs no mission kills, so auto-resolved victories qualify too.
            // Consume only this battle's snapshot; stale or duplicate events cannot award it.
            bool rescued = end.IsWon && _hasCaptiveAlliedLord;
            _battle = null;
            _hasCaptiveAlliedLord = false;

            if (rescued)
                Feat.Add();
        }

        internal static bool IsAlliedLord(Hero hero)
        {
            var faction = Clan.PlayerClan?.MapFaction;
            return hero?.IsLord == true && faction != null && hero.Clan?.MapFaction == faction;
        }
    }
}
