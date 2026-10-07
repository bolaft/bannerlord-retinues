#if BL12
using System;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Modules.Dependencies.Core;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Tests.Cases
{
    public static class LegacyMapEventTests
    {
        [GameTest("LegacyPartyAddedEventBindsAndResets", "community", RequiresCampaign = false)]
        public static void LegacyPartyAddedEventBindsAndResets()
        {
            var originalEvent = LegacyMapEventEvents.PartyAdded;
            var eventProperty = typeof(LegacyMapEventEvents).GetProperty("PartyAdded",
                BindingFlags.Static | BindingFlags.NonPublic);
            var harmony = new Harmony("retinues.tests.legacy-map-event." + Guid.NewGuid().ToString("N"));
            try
            {
                Tests.AssertTrue(HarmonyDependency.IsPatchContainer(typeof(LegacyMapEventEvents)),
                    "The legacy event adapter is included in normal Harmony discovery.");
                var patched = harmony.CreateClassProcessor(typeof(LegacyMapEventEvents)).Patch();
                Tests.AssertEqual(1, patched.Count, "The adapter binds to the real 1.2 party-add method.");

                LegacyMapEventEvents.Reset();
                var party = (PartyBase)FormatterServices.GetUninitializedObject(typeof(PartyBase));
                int calls = 0;
                LegacyMapEventEvents.PartyAdded.AddNonSerializedListener(new object(), added =>
                {
                    Tests.AssertTrue(ReferenceEquals(party, added), "Listeners receive the newly added party.");
                    calls++;
                });
                LegacyMapEventEvents.OnPartyAdded(null);
                Tests.AssertEqual(0, calls, "A missing party emits no notification.");
                LegacyMapEventEvents.OnPartyAdded(party);
                Tests.AssertEqual(1, calls, "A party addition emits one notification.");
                LegacyMapEventEvents.Reset();
                LegacyMapEventEvents.OnPartyAdded(party);
                Tests.AssertEqual(1, calls, "New campaigns cannot invoke listeners from the prior campaign.");
            }
            finally
            {
                eventProperty.SetValue(null, originalEvent);
                harmony.UnpatchAll(harmony.Id);
            }
        }
    }
}
#endif
