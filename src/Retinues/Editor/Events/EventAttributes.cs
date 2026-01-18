using System;

namespace Retinues.Editor.Events
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                    Event Attributes                    //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Marks a property or method as listening to one or more UI events.
    /// When any of these events fire, the property will be scheduled
    /// for OnPropertyChanged in the current burst, and methods are
    /// invoked immediately.
    ///
    /// If Global=true, ListRowVMs will refresh this property even when
    /// the row is not selected.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        AllowMultiple = true,
        Inherited = true
    )]
    public sealed class EventListenerAttribute(params UIEvent[] events) : Attribute
    {
        public UIEvent[] Events { get; } = events ?? [];

        // Named attribute argument, default false:
        // [EventListener(UIEvent.Character, Global = true)]
        public bool Global { get; set; } = false;
    }
}
