#if BL12
using System;
using HarmonyLib;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace Retinues.Framework.Modules.Dependencies.Core.Patches
{
    /// <summary>
    /// BL12's <see cref="WidgetExtensions.SetWidgetAttributeFromString"/> has no
    /// try-catch guard around its body.  In BL13+ TaleWorlds wrapped the whole
    /// method in a try/catch so that null-Brush traversal (Brush.FontSize etc.)
    /// and other widget-attribute errors are silently swallowed.
    ///
    /// This Harmony Finalizer patch restores that behaviour for BL12 builds:
    /// any exception thrown inside the method is suppressed, which prevents
    /// NullReferenceException crashes when Brush sub-properties are set on a
    /// widget whose Brush has not yet been resolved.
    /// </summary>
    [HarmonyPatch(typeof(WidgetExtensions), nameof(WidgetExtensions.SetWidgetAttributeFromString))]
    internal static class WidgetExtensionsAttributePatch
    {
        // A Harmony Finalizer that returns null suppresses the exception entirely,
        // matching the silent catch(Exception) behaviour introduced in BL13.
        public static Exception Finalizer(Exception __exception) => null;
    }
}
#endif
