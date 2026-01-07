using System;
using Retinues.Framework.Model;
using Retinues.Framework.Runtime;
using TaleWorlds.CampaignSystem.MapEvents;

namespace Retinues.Domain.Events.Models
{
    /// <summary>
    /// Wrapper for MapEvent.
    /// Holds a static Current for the most recently started map event.
    /// </summary>
    public sealed class MMapEvent(MapEvent @base) : MBase<MapEvent>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Current                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static MMapEvent Current { get; private set; }

        internal static void SetCurrent(MapEvent mapEvent)
        {
            Current = mapEvent != null ? new MMapEvent(mapEvent) : null;
        }

        [StaticClearAction]
        public static void ClearCurrent() => Current = null;

        internal static void ClearCurrentIf(MapEvent mapEvent)
        {
            if (Current == null)
                return;

            if (mapEvent != null && !ReferenceEquals(Current.Base, mapEvent))
                return;

            Current = null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string StringId => Base.StringId;

        public MapEvent.BattleTypes EventType => Base.EventType;
    }
}
