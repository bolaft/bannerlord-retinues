using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Retinues.Doctrines
{
    /// <summary>
    /// Campaign behavior for doctrine feat event management.
    /// Tracks active feats, relays game events to feat hooks, and manages feat lifecycle.
    /// </summary>
    [SafeClass]
    public sealed class FeatServiceBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            Log.Debug("Registering FeatServiceBehavior events.");

            // Build once session launches (after Doctrines are discovered)
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this,
                _ =>
                {
                    // Keep list up to date when feats/doctrines complete
                    DoctrineAPI.AddCatalogBuiltListener(RefreshActiveFeats);
                    DoctrineAPI.AddFeatCompletedListener(_ => RefreshActiveFeats());
                    DoctrineAPI.AddDoctrineUnlockedListener(_ => RefreshActiveFeats());

                    // Subscribe to config changes
                    Config.OptionChanged += OnConfigOptionChanged;
                }
            );

            // Daily
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);

            // Missions (player enters a scene)
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);

            // Fief captures
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(
                this,
                OnSettlementOwnerChanged
            );

            // Tournaments
            CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);

            // Quests
            CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);

            // Recruitement
            CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, OnUnitRecruited);

            // Upgrading
            CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(
                this,
                PlayerUpgradedTroops
            );
        }

        /// <summary>
        /// Handler for config option changes.
        /// </summary>
        private void OnConfigOptionChanged(string key, object value) => RefreshActiveFeats();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Daily Tick ━━━━━━ */

        private void OnDailyTick()
        {
            NotifyFeats((feat, args) => feat.OnDailyTick());
        }

        /* ━━━━━━━ Missions ━━━━━━━ */

        private void OnMissionStarted(IMission iMission)
        {
            if (iMission is not Mission mission)
                return; // Not a battle or a tournament

            // Only care about player-involved map battles.
            var mapEvent = MobileParty.MainParty?.MapEvent;
            if (mapEvent != null && mapEvent.IsPlayerMapEvent)
            {
                Log.Info("Mission Type: Battle");

                // Attach Battle behavior (captures kills etc.)
                var battle = mission.GetMissionBehavior<Battle>();
                if (battle == null)
                {
                    battle = new Battle();
                    mission.AddMissionBehavior(battle);
                }

                // Relay mission end back so we can call OnBattleEnd on feats.
                if (mission.GetMissionBehavior<MissionEndRelay>() == null)
                    mission.AddMissionBehavior(new MissionEndRelay(this));

                // Notify feats: battle start
                NotifyFeats((feat, args) => feat.OnBattleStart((Battle)args[0]), battle);
            }
            else if (
                mission.CombatType == Mission.MissionCombatType.Combat
                && mission.Mode == MissionMode.StartUp
            )
            {
                Log.Info("Mission Type: Arena");

                // Attach Combat behavior (captures kills etc.)
                var combat = mission.GetMissionBehavior<Combat>();
                if (combat == null)
                {
                    combat = new Combat();
                    mission.AddMissionBehavior(combat);
                }

                // Relay mission end back so we can call OnTournamentMatchEnd on feats.
                if (mission.GetMissionBehavior<MissionEndRelay>() == null)
                    mission.AddMissionBehavior(new MissionEndRelay(this));

                // Notify feats: combat start
                NotifyFeats((feat, args) => feat.OnArenaStart((Combat)args[0]), combat);
            }
        }

        internal void OnMissionEnded(Mission mission)
        {
            var battle = mission?.GetMissionBehavior<Battle>();
            if (battle != null)
            {
                // Notify feats: battle end
                NotifyFeats((feat, args) => feat.OnBattleEnd((Battle)args[0]), battle);

                // Battle report
                battle.LogReport();

                return; // Don't also process as arena
            }

            var combat = mission?.GetMissionBehavior<Combat>();
            if (combat != null)
                NotifyFeats((feat, args) => feat.OnArenaEnd((Combat)args[0]), combat);

            // Display feat unlock popups if any
            Campaign.Current?.GetCampaignBehavior<FeatNotificationBehavior>()?.TryFlush();
        }

        /* ━━━━━━ Tournaments ━━━━━ */

        internal void OnTournamentFinished(
            CharacterObject winner,
            MBReadOnlyList<CharacterObject> participants,
            Town town,
            ItemObject prize
        )
        {
            var tournament = new Tournament(
                new WSettlement(town?.Settlement),
                new WCharacter(winner),
                [.. participants.ToList().Select(p => new WCharacter(p))]
            );

            // Notify feats
            NotifyFeats((feat, args) => feat.OnTournamentFinished((Tournament)args[0]), tournament);

            // Display feat unlock popups if any
            Campaign.Current?.GetCampaignBehavior<FeatNotificationBehavior>()?.TryFlush();
        }

        /* ━━━━━ Fief Captures ━━━━ */

        void OnSettlementOwnerChanged(
            Settlement s,
            bool _,
            Hero n,
            Hero o,
            Hero __,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail d
        )
        {
            if (s == null)
                return; // No settlement

            if (new WFaction(n?.Clan).IsPlayerClan == false)
                return; // Not player clan gaining a fief

            NotifyFeats(
                (feat, args) => feat.OnSettlementOwnerChanged((SettlementOwnerChange)args[0]),
                new SettlementOwnerChange(new WSettlement(s), d, new WHero(o), new WHero(n))
            );
        }

        /* ━━━━━━━━ Quests ━━━━━━━━ */

        void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
        {
            NotifyFeats(
                (feat, args) => feat.OnQuestCompleted((Quest)args[0]),
                new Quest(quest, details is QuestBase.QuestCompleteDetails.Success)
            );
        }

        /* ━━━━━ Recruitement ━━━━━ */

        void OnUnitRecruited(CharacterObject troop, int amount)
        {
            NotifyFeats(
                (feat, args) => feat.OnTroopRecruited((WCharacter)args[0], (int)args[1]),
                new WCharacter(troop),
                amount
            );
        }

        /* ━━━━━━━ Upgrading ━━━━━━ */

        void PlayerUpgradedTroops(
            CharacterObject upgradeFromTroop,
            CharacterObject upgradeToTroop,
            int number
        )
        {
            NotifyFeats(
                (feat, args) =>
                    feat.PlayerUpgradedTroops(
                        (WCharacter)args[0],
                        (WCharacter)args[1],
                        (int)args[2]
                    ),
                new WCharacter(upgradeFromTroop),
                new WCharacter(upgradeToTroop),
                number
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Active Feats Management                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly List<Feat> _activeFeats = [];

        /// <summary>
        /// Refreshes the list of active feats based on doctrine status and feat completion.
        /// </summary>
        private void RefreshActiveFeats()
        {
            _activeFeats.Clear();

            if (Config.EnableFeatRequirements == false)
                return;

            // Ask the service for the discovered doctrine defs; filter by status.
            var svcDoctrines = DoctrineAPI.AllDoctrines();
            if (svcDoctrines == null || svcDoctrines.Count == 0)
                return;

            foreach (var def in svcDoctrines)
            {
                var status = DoctrineAPI.GetDoctrineStatus(def.Key);
                // Track feats if the doctrine is not unlocked and not locked by prereq.
                if (status == DoctrineStatus.Unlocked || status == DoctrineStatus.Locked)
                    continue;

                // For each feat that isn't complete, instantiate and keep it live.
                foreach (var f in def.Feats)
                {
                    if (DoctrineAPI.IsFeatComplete(f.Key))
                        continue;

                    var featType = GetTypeByFullName(f.Key);
                    if (featType == null)
                        continue;

                    if (Activator.CreateInstance(featType) is Feat feat)
                        _activeFeats.Add(feat);
                }
            }

            string featList = string.Join(", ", _activeFeats.Select(f => f.GetType().FullName));
            Log.Info($"Active feats refreshed: {featList}.");
        }

        private static Type GetTypeByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;
            // Same-assembly fast path
            var t = Type.GetType(fullName, throwOnError: false);
            if (t != null)
                return t;

            // Search all loaded assemblies
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = a.GetType(fullName, throwOnError: false);
                    if (t != null)
                        return t;
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Notifies all active feats of a game event by invoking the given action.
        /// </summary>
        private void NotifyFeats(Action<Feat, object[]> action, params object[] args)
        {
            if (Config.EnableFeatRequirements == false)
                return; // No-op if feats are disabled

            foreach (var feat in _activeFeats.ToList())
            {
                try
                {
                    action(feat, args);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        /// <summary>
        /// Mission behavior that relays mission end to the host FeatServiceBehavior.
        /// </summary>
        private sealed class MissionEndRelay(FeatServiceBehavior host) : MissionBehavior
        {
            private readonly FeatServiceBehavior _host = host;

            public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

            protected override void OnEndMission()
            {
                _host?.OnMissionEnded(Mission);
            }
        }
    }
}
