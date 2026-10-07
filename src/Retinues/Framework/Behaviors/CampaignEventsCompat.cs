using System;
using System.Reflection;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Framework.Behaviors
{
    /// <summary>
    /// Version-tolerant subscriptions for campaign events whose signatures differ across game
    /// versions. Game 1.5 changed OnCharacterCreationIsOverEvent from IMbEvent to
    /// IMbEvent&lt;int&gt;; a compiled reference to the property binds its return type at JIT
    /// time and throws MissingMethodException on the other version, so one binary can only
    /// serve both by resolving the event and its listener registration reflectively.
    /// </summary>
    public static class CampaignEventsCompat
    {
        /// <summary>
        /// Subscribes to CampaignEvents.OnCharacterCreationIsOverEvent on any game version,
        /// adapting to whatever listener delegate the running game expects.
        /// </summary>
        public static void SubscribeCharacterCreationIsOver(object owner, Action callback)
        {
            try
            {
                var property = typeof(CampaignEvents).GetProperty(
                    "OnCharacterCreationIsOverEvent",
                    BindingFlags.Public | BindingFlags.Static
                );
                var mbEvent = property?.GetValue(null);
                var add = mbEvent?.GetType().GetMethod("AddNonSerializedListener");
                if (add == null)
                {
                    Log.Warning(
                        "OnCharacterCreationIsOverEvent unavailable; character-creation hook not registered."
                    );
                    return;
                }

                var delegateType = add.GetParameters()[1].ParameterType;
                var listener = CreateCharacterCreationListener(delegateType, callback);
                if (listener == null)
                {
                    Log.Warning(
                        $"OnCharacterCreationIsOverEvent has unsupported listener type '{delegateType}'; hook not registered."
                    );
                    return;
                }

                add.Invoke(mbEvent, [owner, listener]);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to subscribe to OnCharacterCreationIsOverEvent.");
            }
        }

        internal static Delegate CreateCharacterCreationListener(Type delegateType, Action callback)
        {
            if (delegateType == typeof(Action))
                return callback;

            if (delegateType != typeof(Action<int>))
                return null;

            // 1.5 invokes this event for phases 0..9, not ten completed creations.
            // Wait for the final phase so earlier vanilla initialization has finished.
            var phaseCount = typeof(CampaignEvents).GetField(
                "OnCharacterCreationIsOverEventIndexMax",
                BindingFlags.NonPublic | BindingFlags.Static
            )?.GetRawConstantValue() as int? ?? 10;
            return new Action<int>(phase =>
            {
                if (phase == phaseCount - 1)
                    callback();
            });
        }
    }
}
