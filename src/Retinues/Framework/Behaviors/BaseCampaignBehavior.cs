using System;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Retinues.Framework.Behaviors
{
    /// <summary>
    /// Base class for campaign behaviors providing automatic event wiring and lifecycle helpers.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract class BaseCampaignBehavior : CampaignBehaviorBase
    {
        protected string Name => GetType().Name;

        /// <summary>
        /// If false, the behavior is not registered.
        /// </summary>
        public virtual bool IsEnabled => true;

        /// <summary>
        /// If false, events are not invoked.
        /// </summary>
        public virtual bool IsActive => true;

        /// <summary>
        /// Register behavior events and custom events.
        /// </summary>
        public sealed override void RegisterEvents()
        {
            AutoRegisterEvents();
            AutoRegisterCustomEvents();
            RegisterCustomEvents();
        }

        /// <summary>
        /// Sync persisted data for the behavior.
        /// </summary>
        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Auto handlers                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called when a doctrine is acquired.
        /// </summary>
        protected virtual void OnDoctrineAcquired(Doctrine doctrine) { }

        /// <summary>
        /// Called after the game load finishes.
        /// </summary>
        protected virtual void OnGameLoadFinished() { }

        /// <summary>
        /// Called when character creation is finished.
        /// </summary>
        protected virtual void OnCharacterCreationIsOver() { }

        /// <summary>
        /// Called when the session is launched with the given starter.
        /// </summary>
        protected virtual void OnSessionLaunched(CampaignGameStarter starter) { }

        /// <summary>
        /// Called every tick.
        /// </summary>
        protected virtual void OnTick() { }

        /// <summary>
        /// Called on hourly tick.
        /// </summary>
        protected virtual void OnHourlyTick() { }

        /// <summary>
        /// Called on hourly tick for a party.
        /// </summary>
        protected virtual void OnHourlyTickParty(WParty party) { }

        /// <summary>
        /// Called on daily tick.
        /// </summary>
        protected virtual void OnDailyTick() { }

        /// <summary>
        /// Called on daily tick for a settlement.
        /// </summary>
        protected virtual void OnDailyTickSettlement(WSettlement settlement) { }

        /// <summary>
        /// Called on daily tick for a party.
        /// </summary>
        protected virtual void OnDailyTickParty(WParty party) { }

        /// <summary>
        /// Called when a mission starts.
        /// </summary>
        protected virtual void OnMissionStarted(MMission mission) { }

        /// <summary>
        /// Called when a mission ends.
        /// </summary>
        protected virtual void OnMissionEnded(MMission mission) { }

        /// <summary>
        /// Called when a map event starts.
        /// </summary>
        protected virtual void OnMapEventStarted(MMapEvent mapEvent) { }

        /// <summary>
        /// Called when a map event ends.
        /// </summary>
        protected virtual void OnMapEventEnded(MMapEvent mapEvent) { }

        /// <summary>
        /// Called when items are discarded by the player.
        /// </summary>
        protected virtual void OnItemsDiscardedByPlayer(ItemRoster roster) { }

        /// <summary>
        /// Called when a party leaves a settlement.
        /// </summary>
        protected virtual void OnSettlementLeft(WParty party, WSettlement settlement) { }

        /// <summary>
        /// Called before saving the game.
        /// </summary>
        protected virtual void OnBeforeSave() { }

        /// <summary>
        /// Called when a settlement's owner changes.
        /// </summary>
        protected virtual void OnSettlementOwnerChanged(
            WSettlement settlement,
            bool openToClaim,
            WHero newOwner,
            WHero oldOwner,
            WHero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail
        ) { }

        /// <summary>
        /// Called when a kingdom is created.
        /// </summary>
        protected virtual void OnKingdomCreated(Kingdom kingdom) { }

        /// <summary>
        /// Called when a tournament finishes.
        /// </summary>
        protected virtual void OnTournamentFinished(
            WCharacter winner,
            List<WCharacter> participants,
            WSettlement settlement,
            WItem prize
        ) { }

        /// <summary>
        /// Called when a quest completes.
        /// </summary>
        protected virtual void OnQuestCompleted(QuestBase quest, WHero giver, bool success) { }

        /// <summary>
        /// Called when a hideout battle completes.
        /// </summary>
        protected virtual void OnHideoutBattleCompleted(
            BattleSideEnum winnerSide,
            MMapEvent battle,
            HideoutEventComponent hideoutEventComponent
        ) { }

        /// <summary>
        /// Called when a troop is recruited.
        /// </summary>
        protected virtual void OnTroopRecruited(
            WHero recruiter,
            WSettlement settlement,
            WHero source,
            WCharacter troop,
            int amount
        ) { }

        /// <summary>
        /// Called when the player upgrades troops.
        /// </summary>
        protected virtual void OnPlayerUpgradedTroops(
            WCharacter source,
            WCharacter target,
            int number
        ) { }

        /// <summary>
        /// Register custom events for derived behaviors.
        /// </summary>
        protected virtual void RegisterCustomEvents() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Auto-register                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Automatically register overridden event handlers to campaign events.
        /// </summary>
        private void AutoRegisterEvents()
        {
            if (IsOverridden(nameof(OnGameLoadFinished)))
                CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                    this,
                    () => SafeInvoke(OnGameLoadFinished)
                );

            if (IsOverridden(nameof(OnCharacterCreationIsOver)))
                CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(
                    this,
                    () => SafeInvoke(OnCharacterCreationIsOver)
                );

            if (IsOverridden(nameof(OnSessionLaunched)))
                CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                    this,
                    starter => SafeInvoke(() => OnSessionLaunched(starter))
                );

            if (IsOverridden(nameof(OnTick)))
                CampaignEvents.TickEvent.AddNonSerializedListener(this, dt => SafeInvoke(OnTick));

            if (IsOverridden(nameof(OnHourlyTick)))
                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(
                    this,
                    () => SafeInvoke(OnHourlyTick)
                );

            if (IsOverridden(nameof(OnHourlyTickParty)))
                CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(
                    this,
                    party => SafeInvoke(() => OnHourlyTickParty(WParty.Get(party)))
                );

            if (IsOverridden(nameof(OnDailyTick)))
                CampaignEvents.DailyTickEvent.AddNonSerializedListener(
                    this,
                    () => SafeInvoke(OnDailyTick)
                );

            if (IsOverridden(nameof(OnDailyTickSettlement)))
                CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(
                    this,
                    settlement =>
                        SafeInvoke(() => OnDailyTickSettlement(WSettlement.Get(settlement)))
                );

            if (IsOverridden(nameof(OnDailyTickParty)))
                CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(
                    this,
                    party => SafeInvoke(() => OnDailyTickParty(WParty.Get(party)))
                );

            if (IsOverridden(nameof(OnMissionStarted)))
                CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(
                    this,
                    mission => SafeInvoke(() => OnMissionStarted(WrapMission(mission)))
                );

            if (IsOverridden(nameof(OnMissionEnded)))
                CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(
                    this,
                    mission => SafeInvoke(() => OnMissionEnded(WrapMission(mission)))
                );

            if (IsOverridden(nameof(OnMapEventStarted)))
                CampaignEvents.MapEventStarted.AddNonSerializedListener(
                    this,
                    (mapEvent, atk, def) =>
                        SafeInvoke(() => OnMapEventStarted(WrapMapEvent(mapEvent)))
                );

            if (IsOverridden(nameof(OnMapEventEnded)))
                CampaignEvents.MapEventEnded.AddNonSerializedListener(
                    this,
                    mapEvent => SafeInvoke(() => OnMapEventEnded(WrapMapEvent(mapEvent)))
                );

            if (IsOverridden(nameof(OnItemsDiscardedByPlayer)))
                CampaignEvents.OnItemsDiscardedByPlayerEvent.AddNonSerializedListener(
                    this,
                    roster => SafeInvoke(() => OnItemsDiscardedByPlayer(roster))
                );

            if (IsOverridden(nameof(OnSettlementLeft)))
                CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(
                    this,
                    (party, settlement) =>
                        SafeInvoke(() =>
                            OnSettlementLeft(WParty.Get(party), WSettlement.Get(settlement))
                        )
                );

            if (IsOverridden(nameof(OnBeforeSave)))
                CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(
                    this,
                    () => SafeInvoke(OnBeforeSave)
                );

            if (IsOverridden(nameof(OnSettlementOwnerChanged)))
                CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(
                    this,
                    (settlement, openToClaim, newOwner, oldOwner, capturerHero, detail) =>
                        SafeInvoke(() =>
                            OnSettlementOwnerChanged(
                                WSettlement.Get(settlement),
                                openToClaim,
                                WHero.Get(newOwner),
                                WHero.Get(oldOwner),
                                WHero.Get(capturerHero),
                                detail
                            )
                        )
                );

            if (IsOverridden(nameof(OnKingdomCreated)))
                CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(
                    this,
                    kingdom => SafeInvoke(() => OnKingdomCreated(kingdom))
                );

            if (IsOverridden(nameof(OnTournamentFinished)))
                CampaignEvents.TournamentFinished.AddNonSerializedListener(
                    this,
                    (winner, participants, town, prize) =>
                        SafeInvoke(() =>
                            OnTournamentFinished(
                                WCharacter.Get(winner),
                                WrapCharacters(participants),
                                WSettlement.Get(town.Settlement),
                                WItem.Get(prize)
                            )
                        )
                );

            if (IsOverridden(nameof(OnQuestCompleted)))
                CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(
                    this,
                    (quest, details) =>
                        SafeInvoke(() =>
                            OnQuestCompleted(
                                quest,
                                WHero.Get(quest.QuestGiver),
                                details is QuestBase.QuestCompleteDetails.Success
                            )
                        )
                );

            if (IsOverridden(nameof(OnHideoutBattleCompleted)))
                CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(
                    this,
                    (winnerSide, hideoutEventComponent) =>
                        SafeInvoke(() =>
                            OnHideoutBattleCompleted(
                                winnerSide,
                                WrapMapEvent(hideoutEventComponent.MapEvent),
                                hideoutEventComponent
                            )
                        )
                );

            if (IsOverridden(nameof(OnTroopRecruited)))
                CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(
                    this,
                    (recruiter, settlement, source, troop, amount) =>
                        SafeInvoke(() =>
                            OnTroopRecruited(
                                WHero.Get(recruiter),
                                WSettlement.Get(settlement),
                                WHero.Get(source),
                                WCharacter.Get(troop),
                                amount
                            )
                        )
                );

            if (IsOverridden(nameof(OnPlayerUpgradedTroops)))
                CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(
                    this,
                    (upgradeFromTroop, upgradeToTroop, number) =>
                        SafeInvoke(() =>
                            OnPlayerUpgradedTroops(
                                WCharacter.Get(upgradeFromTroop),
                                WCharacter.Get(upgradeToTroop),
                                number
                            )
                        )
                );
        }

        /// <summary>
        /// Automatically register overridden custom event handlers.
        /// </summary>
        private void AutoRegisterCustomEvents()
        {
            if (IsOverridden(nameof(OnDoctrineAcquired)))
                CustomEvents.DoctrineAcquiredEvent.AddNonSerializedListener(
                    this,
                    doctrine => SafeInvoke(() => OnDoctrineAcquired(doctrine))
                );
        }

        /// <summary>
        /// Wrap an IMission into an MMission wrapper.
        /// </summary>
        private static MMission WrapMission(IMission mission)
        {
            if (mission is not Mission m)
                return null;

            return new MMission(m);
        }

        /// <summary>
        /// Wrap a MapEvent into an MMapEvent wrapper.
        /// </summary>
        private static MMapEvent WrapMapEvent(MapEvent mapEvent)
        {
            if (mapEvent == null)
                return null;

            return new MMapEvent(mapEvent);
        }

        /// <summary>
        /// Convert a participant list to wrapped WCharacter list.
        /// </summary>
        private static List<WCharacter> WrapCharacters(MBReadOnlyList<CharacterObject> participants)
        {
            if (participants == null || participants.Count <= 0)
                return [];

            var list = new List<WCharacter>(participants.Count);

            for (int i = 0; i < participants.Count; i++)
            {
                var c = participants[i];
                if (c == null)
                    continue;

                var wc = WCharacter.Get(c);
                if (wc != null)
                    list.Add(wc);
            }

            return list;
        }

        /// <summary>
        /// Safely invoke an action if the behavior is enabled and active.
        /// </summary>
        private void SafeInvoke(Action action)
        {
            if (!IsEnabled)
                return;

            if (!IsActive)
                return;

            if (action == null)
                return;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// Check whether a method is overridden in a derived type.
        /// </summary>
        private bool IsOverridden(string methodName)
        {
            const BindingFlags Flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var method = GetType().GetMethod(methodName, Flags);
            if (method == null)
                return false;

            return method.DeclaringType != typeof(BaseCampaignBehavior);
        }
    }

    /// <summary>
    /// Generic typed base behavior exposing a convenient static instance accessor.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract class BaseCampaignBehavior<TSelf> : BaseCampaignBehavior
        where TSelf : CampaignBehaviorBase
    {
        /// <summary>
        /// Gets the static instance of the behavior.
        /// </summary>
        public static TSelf Instance
        {
            get
            {
                var campaign = Campaign.Current;
                if (campaign == null)
                    return null;

                return campaign.GetCampaignBehavior<TSelf>();
            }
        }

        /// <summary>
        /// Try to get the behavior instance safely.
        /// </summary>
        public static bool TryGetInstance(out TSelf behavior)
        {
            behavior = Instance;
            return behavior != null;
        }
    }
}
