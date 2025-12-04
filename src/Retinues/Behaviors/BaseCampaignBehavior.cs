using System;
using System.Reflection;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Behaviors
{
    /// <summary>
    /// Base class for Retinues campaign behaviors.
    /// Provides logging, enabled flag, and simple CampaignEvents hook helpers.
    /// </summary>
    public abstract class BaseCampaignBehavior : CampaignBehaviorBase
    {
        /// <summary>
        /// High-level hook IDs for common CampaignEvents.
        /// These wrap TaleWorlds.CampaignSystem.CampaignEvents for simple use.
        /// </summary>
        public enum BehaviorEvent
        {
            MissionStarted,
            MissionEnded,
            GameLoadFinished,
            SessionLaunched,
            HourlyTickParty,
        }

        /// <summary>
        /// Name of the behavior for logging.
        /// </summary>
        protected string Name => GetType().Name;

        /// <summary>
        /// Whether this behavior should be active.
        /// Recommended pattern in children:
        ///   public static bool Enabled => Config.EnableFoo;
        ///   public override bool IsEnabled => Enabled;
        /// </summary>
        public virtual bool IsEnabled => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  CampaignBehaviorBase                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// No-op by default so children can skip if they don't need events.
        /// </summary>
        public override void RegisterEvents() { }

        /// <summary>
        /// No-op by default so children can skip if they don't need persistence.
        /// </summary>
        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Hooking                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Hooks a simple Action into a common CampaignEvent.
        /// The action is only called when IsEnabled is true.
        /// Example:
        ///   Hook(BehaviorEvent.MissionStarted, () => _inMission = true);
        /// </summary>
        protected void Hook(BehaviorEvent evt, Action action)
        {
            if (action == null)
                return;

            // Wrap with IsEnabled guard so disabled behaviors bail early.
            void Wrapper()
            {
                if (!IsEnabled)
                    return;
                action();
            }

            switch (evt)
            {
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

                case BehaviorEvent.GameLoadFinished:
                    CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, Wrapper);
                    break;

                case BehaviorEvent.SessionLaunched:
                    CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                        this,
                        _ => Wrapper()
                    );
                    break;

                case BehaviorEvent.HourlyTickParty:
                    CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(
                        this,
                        _ => Wrapper()
                    );
                    break;

                default:
                    Log.Warn($"[{Name}] Unsupported BehaviorEvent: {evt}");
                    break;
            }
        }
    }
}
