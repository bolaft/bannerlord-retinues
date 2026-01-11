using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Factions.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Game.Retinues
{
    public partial class RetinuesBehavior
    {
        /// <summary>
        /// Ensures the player has a default retinue and unlocks their culture's retinue on game start.
        /// </summary>
        protected override void OnGameLoadFinished() => EnsureDefaultRetinueForPlayer();

        /// <summary>
        /// Ensures the player has a default retinue and unlocks their culture's retinue after character creation.
        /// </summary>
        protected override void OnCharacterCreationIsOver() => EnsureDefaultRetinueForPlayer();

        /// <summary>
        /// Adds daily progress for owned fiefs and workshops toward retinue unlocks.
        /// </summary>
        protected override void OnDailyTick()
        {
            AddDailyOwnedFiefsProgress();
            AddDailyOwnedWorkshopsProgress();
        }

        /// <summary>
        /// Handles tournament wins for retinue unlock progress.
        /// </summary>
        protected override void OnTournamentFinished(
            CharacterObject winner,
            MBReadOnlyList<CharacterObject> participants,
            Town town,
            ItemObject prize
        )
        {
            if (!Settings.EnableRetinues)
                return;

            if (winner == null || Hero.MainHero == null)
                return;

            if (winner.StringId != Hero.MainHero.StringId)
                return;

            var culture = town?.Settlement?.Culture;
            if (culture == null)
                return;

            AddProgress(
                WCulture.Get(culture),
                Progress_TournamentWin,
                RetinueProgressSource.TournamentWon,
                showProgressMessage: true
            );
        }

        /// <summary>
        /// Handles quest completions for retinue unlock progress.
        /// </summary>
        protected override void OnQuestCompleted(
            QuestBase quest,
            QuestBase.QuestCompleteDetails details
        )
        {
            if (!Settings.EnableRetinues)
                return;

            if (details != QuestBase.QuestCompleteDetails.Success)
                return;

            var giver = quest?.QuestGiver;
            var culture = giver?.Culture;
            if (culture == null)
                return;

            AddProgress(
                WCulture.Get(culture),
                Progress_QuestCompleted,
                RetinueProgressSource.QuestCompleted,
                showProgressMessage: true
            );
        }

        /// <summary>
        /// Handles map events ending to add progress for battles won with allies.
        /// </summary>
        protected override void OnMapEventEnded(MapEvent mapEvent)
        {
            if (!Settings.EnableRetinues)
                return;

            TryAddBattleAlliesProgress(new MMapEvent(mapEvent));
        }

        private void TryAddBattleAlliesProgress(MMapEvent mapEvent)
        {
            if (!mapEvent.IsPlayerInvolved)
                return;

            if (mapEvent.IsLost)
                return;

            // Distinct cultures among allied parties (excluding main party).
            var cultures = new HashSet<WCulture>();

            foreach (var party in mapEvent.PlayerSideParties)
            {
                if (party.IsMainParty)
                    continue;

                cultures.Add(party.Culture);
            }

            foreach (var culture in cultures)
            {
                AddProgress(
                    culture,
                    Progress_WonBattleWithAllies,
                    RetinueProgressSource.WonBattleWithAllies,
                    showProgressMessage: true
                );
            }
        }

        /// <summary>
        /// Adds daily progress for each owned fief toward retinue unlocks.
        /// Aggregated per culture to avoid spam.
        /// </summary>
        private void AddDailyOwnedFiefsProgress()
        {
            if (!Settings.EnableRetinues)
                return;

            var byCulture = new Dictionary<string, int>();

            foreach (var fief in Player.Clan.Settlements)
            {
                var culture = fief.Culture;
                if (culture == null)
                    continue;

                byCulture.TryGetValue(culture.StringId, out var cur);
                byCulture[culture.StringId] = cur + Progress_DailyPerOwnedFief;
            }

            foreach (var kv in byCulture)
            {
                AddProgress(
                    WCulture.Get(kv.Key),
                    kv.Value,
                    RetinueProgressSource.OwnedFiefsDaily,
                    showProgressMessage: true
                );
            }
        }

        /// <summary>
        /// Adds daily progress for owned workshops toward retinue unlocks.
        /// Aggregated per culture to avoid spam.
        /// </summary>
        private void AddDailyOwnedWorkshopsProgress()
        {
            if (!Settings.EnableRetinues)
                return;

            var byCulture = new Dictionary<string, int>();

            foreach (var fief in Player.Clan.Settlements)
            {
                var culture = fief.Culture;
                if (culture == null)
                    continue;

                int workshopCount = fief.Town?.Workshops.Count() ?? 0;

                byCulture.TryGetValue(culture.StringId, out var cur);
                byCulture[culture.StringId] =
                    cur + (Progress_DailyPerOwnedWorkshop * workshopCount);
            }

            foreach (var kv in byCulture)
            {
                AddProgress(
                    WCulture.Get(kv.Key),
                    kv.Value,
                    RetinueProgressSource.OwnedWorkshopsDaily,
                    showProgressMessage: true
                );
            }
        }
    }
}
