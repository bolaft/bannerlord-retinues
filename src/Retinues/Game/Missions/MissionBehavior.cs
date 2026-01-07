using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;

namespace Retinues.Game.Missions
{
    /// <summary>
    /// Mission lifecycle hook.
    /// Sets MMission.Current on mission start and clears it on mission end.
    /// </summary>
    public sealed class MissionBehavior : BaseMissionBehavior
    {
        private bool _started;
        private bool _ended;

        public override void AfterStart()
        {
            base.AfterStart();

            if (_started)
                return;

            _started = true;
            MMission.SetCurrent(Mission);

            Log.Info($"Mission started. Scene='{Mission?.SceneName}'.");
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            End();
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            End();
        }

        private void End()
        {
            if (_ended)
                return;

            _ended = true;

            Log.Info($"Mission ended. Scene='{Mission?.SceneName}'.");

            // Only clear if we're still the current mission.
            MMission.ClearCurrentIf(Mission);
        }
    }
}
