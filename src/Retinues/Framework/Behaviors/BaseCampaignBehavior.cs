using System;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Models;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
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

        protected virtual void OnDailyTick() { }

        protected virtual void OnHourlyTick() { }

        protected virtual void OnHourlyTickParty(WParty party) { }

        protected virtual void OnMissionStarted(MMission mission) { }

        protected virtual void OnMissionEnded(MMission mission) { }

        protected virtual void OnMapEventStarted(
            MMapEvent mapEvent,
            WParty attackerParty,
            WParty defenderParty
        ) { }

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
            MTown town,
            ItemObject prize
        ) { }

        protected virtual void OnQuestCompleted(
            QuestBase quest,
            QuestBase.QuestCompleteDetails details
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

            if (IsOverridden(nameof(OnDailyTick)))
                CampaignEvents.DailyTickEvent.AddNonSerializedListener(
                    this,
                    () => SafeInvoke(OnDailyTick)
                );

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
                        SafeInvoke(() =>
                            OnMapEventStarted(
                                WrapMapEvent(mapEvent),
                                WParty.Get(atk.MobileParty),
                                WParty.Get(def.MobileParty)
                            )
                        )
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
                                town != null ? new MTown(town) : null,
                                prize
                            )
                        )
                );

            if (IsOverridden(nameof(OnQuestCompleted)))
                CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(
                    this,
                    (quest, details) => SafeInvoke(() => OnQuestCompleted(quest, details))
                );
        }

        private static MMission WrapMission(IMission mission)
        {
            var m = mission as Mission;
            if (m == null)
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
                return new List<WCharacter>();

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
