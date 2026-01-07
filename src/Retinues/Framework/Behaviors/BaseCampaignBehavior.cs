using System;
using System.Reflection;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
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

            HourlyTick,
            HourlyTickParty,

            MissionStarted,
            MissionEnded,

            MapEventStarted,
            MapEventEnded,
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

        protected virtual void RegisterCustomEvents() { }

        private void AutoRegisterEvents()
        {
            if (IsOverridden(nameof(OnGameLoadFinished)))
                Hook(BehaviorEvent.GameLoadFinished, OnGameLoadFinished);

            if (IsOverridden(nameof(OnCharacterCreationIsOver)))
                Hook(BehaviorEvent.CharacterCreationIsOver, OnCharacterCreationIsOver);

            if (IsOverridden(nameof(OnSessionLaunched)))
                Hook(BehaviorEvent.SessionLaunched, OnSessionLaunched);

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

        // No-arg hook (kept for convenience)
        protected void Hook(BehaviorEvent evt, Action action)
        {
            if (action == null)
                return;

            void Wrapper()
            {
                if (!IsEnabled)
                    return;

                action();
            }

            switch (evt)
            {
                case BehaviorEvent.GameLoadFinished:
                    CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, Wrapper);
                    break;

                case BehaviorEvent.CharacterCreationIsOver:
                    CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(
                        this,
                        Wrapper
                    );
                    break;

                case BehaviorEvent.SessionLaunched:
                    CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                        this,
                        _ => Wrapper()
                    );
                    break;

                case BehaviorEvent.HourlyTick:
                    CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, Wrapper);
                    break;

                case BehaviorEvent.HourlyTickParty:
                    CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(
                        this,
                        _ => Wrapper()
                    );
                    break;

                case BehaviorEvent.MissionStarted:
                    CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(
                        this,
                        _ => Wrapper()
                    );
                    break;

                case BehaviorEvent.MissionEnded:
                    CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(
                        this,
                        _ => Wrapper()
                    );
                    break;

                case BehaviorEvent.MapEventStarted:
                    CampaignEvents.MapEventStarted.AddNonSerializedListener(
                        this,
                        (_, __, ___) => Wrapper()
                    );
                    break;

                case BehaviorEvent.MapEventEnded:
                    CampaignEvents.MapEventEnded.AddNonSerializedListener(this, _ => Wrapper());
                    break;

                default:
                    Log.Warn($"[{Name}] Unsupported BehaviorEvent (Action): {evt}");
                    break;
            }
        }

        protected void Hook(BehaviorEvent evt, Action<CampaignGameStarter> action)
        {
            if (action == null)
                return;

            void Wrapper(CampaignGameStarter starter)
            {
                if (!IsEnabled)
                    return;

                action(starter);
            }

            switch (evt)
            {
                case BehaviorEvent.SessionLaunched:
                    CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Wrapper);
                    break;

                default:
                    Log.Warn(
                        $"[{Name}] Unsupported BehaviorEvent (Action<CampaignGameStarter>): {evt}"
                    );
                    break;
            }
        }

        protected void Hook(BehaviorEvent evt, Action<MobileParty> action)
        {
            if (action == null)
                return;

            void Wrapper(MobileParty party)
            {
                if (!IsEnabled)
                    return;

                action(party);
            }

            switch (evt)
            {
                case BehaviorEvent.HourlyTickParty:
                    CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, Wrapper);
                    break;

                default:
                    Log.Warn($"[{Name}] Unsupported BehaviorEvent (Action<MobileParty>): {evt}");
                    break;
            }
        }

        protected void Hook(BehaviorEvent evt, Action<IMission> action)
        {
            if (action == null)
                return;

            void Wrapper(IMission mission)
            {
                if (!IsEnabled)
                    return;

                action(mission);
            }

            switch (evt)
            {
                case BehaviorEvent.MissionStarted:
                    CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, Wrapper);
                    break;

                case BehaviorEvent.MissionEnded:
                    CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, Wrapper);
                    break;

                default:
                    Log.Warn($"[{Name}] Unsupported BehaviorEvent (Action<IMission>): {evt}");
                    break;
            }
        }

        protected void Hook(BehaviorEvent evt, Action<MapEvent, PartyBase, PartyBase> action)
        {
            if (action == null)
                return;

            void Wrapper(MapEvent me, PartyBase atk, PartyBase def)
            {
                if (!IsEnabled)
                    return;

                action(me, atk, def);
            }

            switch (evt)
            {
                case BehaviorEvent.MapEventStarted:
                    CampaignEvents.MapEventStarted.AddNonSerializedListener(this, Wrapper);
                    break;

                default:
                    Log.Warn(
                        $"[{Name}] Unsupported BehaviorEvent (Action<MapEvent,PartyBase,PartyBase>): {evt}"
                    );
                    break;
            }
        }

        protected void Hook(BehaviorEvent evt, Action<MapEvent> action)
        {
            if (action == null)
                return;

            void Wrapper(MapEvent me)
            {
                if (!IsEnabled)
                    return;

                action(me);
            }

            switch (evt)
            {
                case BehaviorEvent.MapEventEnded:
                    CampaignEvents.MapEventEnded.AddNonSerializedListener(this, Wrapper);
                    break;

                default:
                    Log.Warn($"[{Name}] Unsupported BehaviorEvent (Action<MapEvent>): {evt}");
                    break;
            }
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
