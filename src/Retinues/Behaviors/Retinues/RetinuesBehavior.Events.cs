using System.Collections.Generic;
using System.Linq;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;

namespace Retinues.Behaviors.Retinues
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
            WCharacter winner,
            List<WCharacter> participants,
            WSettlement settlement,
            WItem prize
        )
        {
            if (!Configuration.EnableRetinues)
                return;

            if (winner?.Base == null || Hero.MainHero == null)
                return;

            if (winner.StringId != Hero.MainHero.StringId)
                return;

            var culture = settlement?.Culture;
            if (culture == null)
                return;

            AddProgress(
                culture,
                Progress_TournamentWin,
                RetinueProgressSource.TournamentWon,
                showProgressMessage: true
            );
        }

        /// <summary>
        /// Handles quest completions for retinue unlock progress.
        /// </summary>
        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!Configuration.EnableRetinues)
                return;

            if (!success)
                return;

            var culture = giver?.Culture;
            if (culture == null)
                return;

            AddProgress(
                culture,
                Progress_QuestCompleted,
                RetinueProgressSource.QuestCompleted,
                showProgressMessage: true
            );
        }

        /// <summary>
        /// Handles map events ending to add progress for battles won with allies.
        /// </summary>
        protected override void OnMapEventEnded(MMapEvent mapEvent)
        {
            if (!Configuration.EnableRetinues)
                return;

            TryAddBattleAlliesProgress(mapEvent);
        }

        private void TryAddBattleAlliesProgress(MMapEvent mapEvent)
        {
            if (mapEvent == null)
                return;

            if (!mapEvent.IsPlayerInvolved)
                return;

            if (mapEvent.IsLost)
                return;

            // Distinct cultures among allied parties (excluding main party).
            var cultures = new HashSet<WCulture>();

            foreach (var data in mapEvent.PlayerSide.PartyData)
            {
                var party = WParty.Get(data.PartyId);
                if (party == null)
                    continue;

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
            if (!Configuration.EnableRetinues)
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
            if (!Configuration.EnableRetinues)
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
