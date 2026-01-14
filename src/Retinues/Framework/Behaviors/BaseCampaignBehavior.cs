using System;
using System.Collections.Generic;
using System.Reflection;
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
    [SafeClass(IncludeDerived = true)]
    public abstract class BaseCampaignBehavior : CampaignBehaviorBase
    {
        protected string Name => GetType().Name;

        public virtual bool IsEnabled => true;
        public virtual bool IsActive => true;

        public sealed override void RegisterEvents()
        {
            AutoRegisterEvents();
            RegisterCustomEvents();
        }

        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Auto handlers                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected virtual void OnGameLoadFinished() { }

        protected virtual void OnCharacterCreationIsOver() { }

        protected virtual void OnSessionLaunched(CampaignGameStarter starter) { }

        protected virtual void OnTick() { }

        protected virtual void OnHourlyTick() { }

        protected virtual void OnHourlyTickParty(WParty party) { }

        protected virtual void OnDailyTick() { }

        protected virtual void OnMissionStarted(MMission mission) { }

        protected virtual void OnMissionEnded(MMission mission) { }

        protected virtual void OnMapEventStarted(MMapEvent mapEvent) { }

        protected virtual void OnMapEventEnded(MMapEvent mapEvent) { }

        protected virtual void OnItemsDiscardedByPlayer(ItemRoster roster) { }

        protected virtual void OnSettlementLeft(WParty party, WSettlement settlement) { }

        protected virtual void OnBeforeSave() { }

        protected virtual void OnSettlementOwnerChanged(
            WSettlement settlement,
            bool openToClaim,
            WHero newOwner,
            WHero oldOwner,
            WHero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail
        ) { }

        protected virtual void OnKingdomCreated(Kingdom kingdom) { }

        protected virtual void OnTournamentFinished(
            WCharacter winner,
            List<WCharacter> participants,
            WSettlement settlement,
            WItem prize
        ) { }

        protected virtual void OnQuestCompleted(QuestBase quest, WHero giver, bool success) { }

        protected virtual void OnHideoutBattleCompleted(
            BattleSideEnum winnerSide,
            MMapEvent battle,
            HideoutEventComponent hideoutEventComponent
        ) { }

        protected virtual void OnTroopRecruited(
            WHero recruiter,
            WSettlement settlement,
            WHero source,
            WCharacter troop,
            int amount
        ) { }

        protected virtual void OnPlayerUpgradedTroops(
            WCharacter source,
            WCharacter target,
            int number
        ) { }

        protected virtual void RegisterCustomEvents() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Auto-register                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        private static MMission WrapMission(IMission mission)
        {
            if (mission is not Mission m)
                return null;

            return new MMission(m);
        }

        private static MMapEvent WrapMapEvent(MapEvent mapEvent)
        {
            if (mapEvent == null)
                return null;

            return new MMapEvent(mapEvent);
        }

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

    [SafeClass(IncludeDerived = true)]
    public abstract class BaseCampaignBehavior<TSelf> : BaseCampaignBehavior
        where TSelf : CampaignBehaviorBase
    {
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

        public static bool TryGetInstance(out TSelf behavior)
        {
            behavior = Instance;
            return behavior != null;
        }
    }
}
