using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Retinues.Game.Missions
{
    /// <summary>
    /// MapEvent lifecycle hook.
    /// Sets MMapEvent.Current on map event start and clears it on map event end.
    /// Also clears on mission end to avoid stale current references.
    /// </summary>
    public sealed class MapEventBehavior : BaseCampaignBehavior
    {
        protected override void OnMapEventStarted(
            MapEvent mapEvent,
            PartyBase attackerParty,
            PartyBase defenderParty
        )
        {
            // Respect IsEnabled like the old behavior.
            if (!IsEnabled)
                return;

            MMapEvent.SetCurrent(mapEvent);

            var attacker = attackerParty?.Name?.ToString();
            var defender = defenderParty?.Name?.ToString();

            Log.Info(
                $"Map event started. Type='{mapEvent?.EventType}', Id='{mapEvent?.StringId}', Attacker='{attacker}', Defender='{defender}'."
            );
        }

        protected override void OnMapEventEnded(MapEvent mapEvent)
        {
            if (!IsEnabled)
                return;

            Log.Info($"Map event ended. Type='{mapEvent?.EventType}', Id='{mapEvent?.StringId}'.");

            // Only clear if we're still the current map event.
            MMapEvent.ClearCurrentIf(mapEvent);
        }

        protected override void OnMissionEnded(IMission mission)
        {
            // Spec: clear statics on mission end. This avoids stale state if MapEventEnded is skipped.
            // Intentionally not gated behind IsEnabled.
            MMapEvent.ClearCurrent();
        }
    }
}
