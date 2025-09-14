using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers.Cache;
using System.Linq;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines
{
    /// Event router for feats.
    public sealed class FeatRuntimeBehavior : CampaignBehaviorBase
    {
        private readonly List<Feat> _activeFeats = [];

        public override void RegisterEvents()
        {
            // Build once session launches (after Doctrines are discovered)
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ =>
            {
                RefreshActiveFeats();
                // Keep list up to date when feats/doctrines complete
                DoctrineAPI.AddFeatCompletedListener(_ => RefreshActiveFeats());
                DoctrineAPI.AddDoctrineUnlockedListener(_ => RefreshActiveFeats());
            });

            // Daily
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);

            // Missions (player enters a scene)
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);

            // Fief captures
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);

            // Tournaments (special case, separate event)
            CampaignEvents.TournamentStarted.AddNonSerializedListener(this, OnTournamentStarted);
            CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);

            // Quests
            CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);

            // Recruitement
            CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);

            // Upgrading
            CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, PlayerUpgradedTroops); 
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state here — feat progress is persisted by DoctrineServiceBehavior.
        }

        // ---------------------------------------------------------------------
        // Hooks
        // ---------------------------------------------------------------------

        private void OnDailyTick()
        {
            // Call for all active feats (cheap, and feats can early-out themselves).
            foreach (var feat in _activeFeats)
                feat.OnDailyTick();
        }

        private void OnMissionStarted(IMission iMission)
        {
            if (iMission is not Mission mission) return;

            // Only care about player-involved map battles.
            var mapEvent = MobileParty.MainParty?.MapEvent;
            if (mapEvent == null || !mapEvent.IsPlayerMapEvent) return;

            // Attach your Battle behavior (captures kills etc.)
            var battle = mission.GetMissionBehavior<Battle>();
            if (battle == null)
            {
                battle = new Battle();
                mission.AddMissionBehavior(battle);
            }

            // Relay mission end back to us so we can call OnBattleEnd on feats.
            if (mission.GetMissionBehavior<MissionEndRelay>() == null)
                mission.AddMissionBehavior(new MissionEndRelay(this));

            // Notify feats: battle start
            foreach (var feat in _activeFeats)
                feat.OnBattleStart(battle);
        }

        internal void OnMissionEnded(Mission mission)
        {
            // At this point mission is ending; MapEvent may already be null in some cases,
            // so rely on the behavior we attached.
            var battle = mission?.GetMissionBehavior<Battle>();
            if (battle == null) return;

            foreach (var feat in _activeFeats)
                feat.OnBattleEnd(battle);

            // Log battle report
            battle.LogReport();
        }

        private Tournament _currentTournament;

        internal void OnTournamentStarted(Town town)
        {
            // Try to get the current mission
            var mission = Mission.Current;
            if (mission == null)
                return;

            // Check if Tournament behavior already exists
            var tournament = mission.GetMissionBehavior<Tournament>();
            if (tournament == null)
            {
                tournament = new Tournament(town);
                mission.AddMissionBehavior(tournament);
            }
            _currentTournament = tournament;

            // Notify feats
            foreach (var feat in _activeFeats)
                feat.OnTournamentStart(tournament);
        }

        internal void OnTournamentFinished(
            CharacterObject winner,
            MBReadOnlyList<CharacterObject> participants,
            Town town,
            ItemObject prize)
        {
            var mission = Mission.Current;
            Tournament tournament = null;
            if (mission != null)
                tournament = mission.GetMissionBehavior<Tournament>();
            tournament ??= _currentTournament; // fallback if mission is gone
            if (tournament == null)
                return;

            tournament.UpdateOnFinish(
                WCharacterCache.Wrap(winner),
                participants.ToList().Select(p => WCharacterCache.Wrap(p)).ToList()
            );

            // Notify feats
            foreach (var feat in _activeFeats)
                feat.OnTournamentFinished(tournament);
        }

        void OnSettlementOwnerChanged(Settlement s, bool _, Hero n, Hero o, Hero __, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail d)
        {
            foreach (var feat in _activeFeats)
            {
                feat.OnSettlementOwnerChanged(new SettlementOwnerChange(
                    s, d,
                    WCharacterCache.Wrap(o.CharacterObject),
                    WCharacterCache.Wrap(n.CharacterObject)
                ));
            }
        }

        void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
        {
            foreach (var feat in _activeFeats)
                feat.OnQuestCompleted(new Quest(quest, details is QuestBase.QuestCompleteDetails.Success));
        }

        void OnTroopRecruited(Hero recruiterHero, Settlement recruitmentSettlement, Hero recruitmentSource, CharacterObject troop, int amount)
        {
            // Only care about player clan recruitment
            if (recruiterHero.StringId != Player.Character.StringId) return;

            foreach (var feat in _activeFeats)
                feat.OnTroopRecruited(WCharacterCache.Wrap(troop), amount);
        }

        void PlayerUpgradedTroops(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
        {
            foreach (var feat in _activeFeats)
                feat.PlayerUpgradedTroops(WCharacterCache.Wrap(upgradeFromTroop), WCharacterCache.Wrap(upgradeToTroop), number);
        }

        // ---------------------------------------------------------------------
        // Active feats management
        // ---------------------------------------------------------------------

        private void RefreshActiveFeats()
        {
            _activeFeats.Clear();

            // Ask the service for the discovered doctrine defs; filter by status.
            var svcDoctrines = DoctrineAPI.AllDoctrines();
            if (svcDoctrines == null || svcDoctrines.Count == 0) return;

            foreach (var def in svcDoctrines)
            {
                var status = DoctrineAPI.GetDoctrineStatus(def.Key);
                // Track feats if the doctrine is not unlocked and not locked by prereq.
                if (status == DoctrineStatus.Unlocked || status == DoctrineStatus.Locked)
                    continue;

                // For each feat that isn't complete, instantiate and keep it live.
                foreach (var f in def.Feats)
                {
                    if (DoctrineAPI.IsFeatComplete(f.Key)) continue;

                    var featType = GetTypeByFullName(f.Key);
                    if (featType == null) continue;

                    if (Activator.CreateInstance(featType) is Feat feat) _activeFeats.Add(feat);
                }
            }

            Log.Debug($"Refreshed active feats; now tracking {_activeFeats.Count} active feats:");
            foreach (var feat in _activeFeats)
                Log.Debug($" - {feat.GetType().FullName}");
        }

        private static Type GetTypeByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            // Same-assembly fast path
            var t = Type.GetType(fullName, throwOnError: false);
            if (t != null) return t;

            // Search all loaded assemblies (useful if you split your mod assemblies)
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = a.GetType(fullName, throwOnError: false);
                    if (t != null) return t;
                }
                catch { /* ignore dynamic/ReflectionTypeLoadException cases */ }
            }
            return null;
        }

        // ---------------------------------------------------------------------
        // Mission relay
        // ---------------------------------------------------------------------
        private sealed class MissionEndRelay(FeatRuntimeBehavior host) : MissionBehavior
        {
            private readonly FeatRuntimeBehavior _host = host;

            public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

            protected override void OnEndMission()
            {
                _host?.OnMissionEnded(Mission);
            }
        }
    }
}
