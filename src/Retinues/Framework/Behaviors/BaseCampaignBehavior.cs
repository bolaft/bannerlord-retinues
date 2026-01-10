using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Retinues.Framework.Behaviors
{
    [SafeClass(IncludeDerived = true)]
    public abstract class BaseCampaignBehavior : CampaignBehaviorBase
    {
        public enum BehaviorEvent
        {
            GameLoadFinished,
            CharacterCreationIsOver,
            SessionLaunched,

            DailyTick,
            HourlyTick,
            HourlyTickParty,

            MissionStarted,
            MissionEnded,

            MapEventStarted,
            MapEventEnded,

            ItemsDiscardedByPlayer,

            SettlementLeft,
            BeforeSave,

            SettlementOwnerChanged,
            KingdomCreated,
        }

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

        protected virtual void OnHourlyTickParty(MobileParty party) { }

        protected virtual void OnMissionStarted(IMission mission) { }

        protected virtual void OnMissionEnded(IMission mission) { }

        protected virtual void OnMapEventStarted(
            MapEvent mapEvent,
            PartyBase attackerParty,
            PartyBase defenderParty
        ) { }

        protected virtual void OnMapEventEnded(MapEvent mapEvent) { }

        protected virtual void OnItemsDiscardedByPlayer(ItemRoster roster) { }

        protected virtual void OnSettlementLeft(MobileParty party, Settlement settlement) { }

        protected virtual void OnBeforeSave() { }

        protected virtual void OnSettlementOwnerChanged(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail
        ) { }

        protected virtual void OnKingdomCreated(Kingdom kingdom) { }

        protected virtual void RegisterCustomEvents() { }

        private void AutoRegisterEvents()
        {
            if (IsOverridden(nameof(OnGameLoadFinished)))
                Hook(BehaviorEvent.GameLoadFinished, OnGameLoadFinished);

            if (IsOverridden(nameof(OnCharacterCreationIsOver)))
                Hook(BehaviorEvent.CharacterCreationIsOver, OnCharacterCreationIsOver);

            if (IsOverridden(nameof(OnSessionLaunched)))
                Hook(BehaviorEvent.SessionLaunched, OnSessionLaunched);

            if (IsOverridden(nameof(OnDailyTick)))
                Hook(BehaviorEvent.DailyTick, OnDailyTick);

            if (IsOverridden(nameof(OnHourlyTick)))
                Hook(BehaviorEvent.HourlyTick, OnHourlyTick);

            if (IsOverridden(nameof(OnHourlyTickParty)))
                Hook(BehaviorEvent.HourlyTickParty, OnHourlyTickParty);

            if (IsOverridden(nameof(OnMissionStarted)))
                Hook(BehaviorEvent.MissionStarted, OnMissionStarted);

            if (IsOverridden(nameof(OnMissionEnded)))
                Hook(BehaviorEvent.MissionEnded, OnMissionEnded);

            if (IsOverridden(nameof(OnMapEventStarted)))
                Hook(BehaviorEvent.MapEventStarted, OnMapEventStarted);

            if (IsOverridden(nameof(OnMapEventEnded)))
                Hook(BehaviorEvent.MapEventEnded, OnMapEventEnded);

            if (IsOverridden(nameof(OnItemsDiscardedByPlayer)))
                Hook(BehaviorEvent.ItemsDiscardedByPlayer, OnItemsDiscardedByPlayer);

            if (IsOverridden(nameof(OnSettlementLeft)))
                Hook(BehaviorEvent.SettlementLeft, OnSettlementLeft);

            if (IsOverridden(nameof(OnBeforeSave)))
                Hook(BehaviorEvent.BeforeSave, OnBeforeSave);

            if (IsOverridden(nameof(OnSettlementOwnerChanged)))
                Hook(BehaviorEvent.SettlementOwnerChanged, OnSettlementOwnerChanged);

            if (IsOverridden(nameof(OnKingdomCreated)))
                Hook(BehaviorEvent.KingdomCreated, OnKingdomCreated);
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Hooks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected void Hook<THandler>(BehaviorEvent evt, THandler handler)
            where THandler : Delegate
        {
            HookInternal(evt, handler, null);
        }

        protected void Hook<THandler, TNormalize>(
            BehaviorEvent evt,
            THandler handler,
            TNormalize normalize
        )
            where THandler : Delegate
            where TNormalize : Delegate
        {
            HookInternal(evt, handler, normalize);
        }

        private void HookInternal(BehaviorEvent evt, Delegate handler, Delegate normalize)
        {
            if (handler == null)
                return;

            switch (evt)
            {
                case BehaviorEvent.GameLoadFinished:
                    CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                        this,
                        () => InvokeHook(evt, handler, normalize)
                    );
                    break;

                case BehaviorEvent.CharacterCreationIsOver:
                    CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(
                        this,
                        () => InvokeHook(evt, handler, normalize)
                    );
                    break;

                case BehaviorEvent.SessionLaunched:
                    CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                        this,
                        starter => InvokeHook(evt, handler, normalize, starter)
                    );
                    break;

                case BehaviorEvent.DailyTick:
                    CampaignEvents.DailyTickEvent.AddNonSerializedListener(
                        this,
                        () => InvokeHook(evt, handler, normalize)
                    );
                    break;

                case BehaviorEvent.HourlyTick:
                    CampaignEvents.HourlyTickEvent.AddNonSerializedListener(
                        this,
                        () => InvokeHook(evt, handler, normalize)
                    );
                    break;

                case BehaviorEvent.HourlyTickParty:
                    CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(
                        this,
                        party => InvokeHook(evt, handler, normalize, party)
                    );
                    break;

                case BehaviorEvent.MissionStarted:
                    CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(
                        this,
                        mission => InvokeHook(evt, handler, normalize, mission)
                    );
                    break;

                case BehaviorEvent.MissionEnded:
                    CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(
                        this,
                        mission => InvokeHook(evt, handler, normalize, mission)
                    );
                    break;

                case BehaviorEvent.MapEventStarted:
                    CampaignEvents.MapEventStarted.AddNonSerializedListener(
                        this,
                        (me, atk, def) => InvokeHook(evt, handler, normalize, me, atk, def)
                    );
                    break;

                case BehaviorEvent.MapEventEnded:
                    CampaignEvents.MapEventEnded.AddNonSerializedListener(
                        this,
                        me => InvokeHook(evt, handler, normalize, me)
                    );
                    break;

                case BehaviorEvent.SettlementLeft:
                    CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(
                        this,
                        (party, settlement) =>
                            InvokeHook(evt, handler, normalize, party, settlement)
                    );
                    break;

                case BehaviorEvent.BeforeSave:
                    CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(
                        this,
                        () => InvokeHook(evt, handler, normalize)
                    );
                    break;

                case BehaviorEvent.ItemsDiscardedByPlayer:
                    CampaignEvents.OnItemsDiscardedByPlayerEvent.AddNonSerializedListener(
                        this,
                        roster => InvokeHook(evt, handler, normalize, roster)
                    );
                    break;

                case BehaviorEvent.SettlementOwnerChanged:
                    CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(
                        this,
                        (settlement, openToClaim, newOwner, oldOwner, capturerHero, detail) =>
                            InvokeHook(
                                evt,
                                handler,
                                normalize,
                                settlement,
                                openToClaim,
                                newOwner,
                                oldOwner,
                                capturerHero,
                                detail
                            )
                    );
                    break;

                case BehaviorEvent.KingdomCreated:
                    CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(
                        this,
                        kingdom => InvokeHook(evt, handler, normalize, kingdom)
                    );
                    break;

                default:
                    Log.Warn($"Unsupported BehaviorEvent {evt}.");
                    break;
            }
        }

        private void InvokeHook(
            BehaviorEvent evt,
            Delegate handler,
            Delegate normalize,
            params object[] vanillaArgs
        )
        {
            if (!IsEnabled)
                return;

            try
            {
                var handlerArgs = vanillaArgs ?? new object[0];

                if (normalize != null)
                    handlerArgs = NormalizeArgs(evt, handler, normalize, handlerArgs);

                InvokeHandler(evt, handler, handlerArgs);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private object[] NormalizeArgs(
            BehaviorEvent evt,
            Delegate handler,
            Delegate normalize,
            object[] vanillaArgs
        )
        {
            var normParams = normalize.Method.GetParameters();
            if (normParams.Length != (vanillaArgs?.Length ?? 0))
            {
                Log.Warn($"Normalizer arg count mismatch for {evt}.");
                return vanillaArgs ?? new object[0];
            }

            var result = normalize.DynamicInvoke(vanillaArgs ?? new object[0]);
            if (result == null)
                return vanillaArgs ?? new object[0];

            if (result is object[] arr)
                return arr;

            if (result is ITuple tuple)
            {
                var items = new object[tuple.Length];
                for (var i = 0; i < tuple.Length; i++)
                    items[i] = tuple[i];
                return items;
            }

            return new[] { result };
        }

        private void InvokeHandler(BehaviorEvent evt, Delegate handler, object[] args)
        {
            var expected = handler.Method.GetParameters().Length;

            if (expected == 0)
            {
                handler.DynamicInvoke();
                return;
            }

            if (args == null)
                args = new object[0];

            if (args.Length < expected)
            {
                Log.Warn($"Handler arg count mismatch for {evt}.");
                return;
            }

            if (args.Length == expected)
            {
                handler.DynamicInvoke(args);
                return;
            }

            var trimmed = new object[expected];
            Array.Copy(args, trimmed, expected);
            handler.DynamicInvoke(trimmed);
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
