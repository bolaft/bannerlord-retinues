using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Retinues.Behaviors.Presets;
using Retinues.Behaviors.Troops;
using Retinues.Framework.Behaviors;
using Retinues.Interface.Services.Popups;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Retinues.Tests.Cases
{
    public static class PresetPopupTests
    {
        [GameTest("CharacterCreationPhasesInitializeOnceAfterVanilla", "compatibility", RequiresCampaign = false)]
        public static void CharacterCreationPhasesInitializeOnceAfterVanilla()
        {
            var calls = 0;
            var phases = new MbEvent<int>();
            phases.AddNonSerializedListener(new object(),
                (Action<int>)CampaignEventsCompat.CreateCharacterCreationListener(typeof(Action<int>), () => calls++));
            var invoke = phases.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (var phase = 0; phase < 9; phase++)
                invoke.Invoke(phases, new object[] { phase });
            Tests.AssertEqual(0, calls, "The first nine phases must not open popups or bootstrap troops.");
            invoke.Invoke(phases, new object[] { 9 });
            Tests.AssertEqual(1, calls, "The complete beta character-creation sequence initializes once.");

            var legacy = new MbEvent();
            legacy.AddNonSerializedListener(new object(),
                (Action)CampaignEventsCompat.CreateCharacterCreationListener(typeof(Action), () => calls++));
            legacy.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(legacy, null);
            Tests.AssertEqual(2, calls, "Legacy parameterless completion remains supported.");
            Tests.AssertTrue(CampaignEventsCompat.CreateCharacterCreationListener(typeof(Action<string>), () => calls++) == null,
                "Unknown event payloads must not be treated as completion.");
        }

        [GameTest("PresetPopupAcceptsOnlyOneChoice", "ui", RequiresCampaign = false)]
        public static void PresetPopupAcceptsOnlyOneChoice()
        {
            foreach (var count in new[] { 3, 4 })
            for (var selected = 0; selected < count; selected++)
            {
                var actions = new List<int>();
                var closes = 0;
                var choices = new List<(TextObject, Action)>();
                for (var i = 0; i < count; i++)
                {
                    var index = i;
                    choices.Add((new TextObject("Choice " + i), () => actions.Add(index)));
                }
                MultiChoicePopupVM vm = null;
                vm = new MultiChoicePopupVM(new TextObject("Welcome"), new TextObject("Settings"), choices, () =>
                {
                    closes++;
                    Tests.AssertEqual(0, actions.Count, "Close before applying a preset that may open other inquiries.");
                    vm.Buttons[(selected + 1) % count].ExecuteAction();
                });
                vm.Buttons[selected].ExecuteAction();
                foreach (var button in vm.Buttons)
                    button.ExecuteAction();
                Tests.AssertEqual(1, closes, "Repeated or reentrant clicks cannot close another popup.");
                Tests.AssertEqual(1, actions.Count, "Only the selected callback runs.");
                Tests.AssertEqual(selected, actions[0]);
            }
        }

        private static int _bootstrapCalls;
        private static bool CaptureBootstrap(bool fromBootstrap)
        {
            Tests.AssertTrue(fromBootstrap, "Keeping settings initializes the campaign without unlock notifications.");
            _bootstrapCalls++;
            return false;
        }

        [GameTest("KeepCurrentSettingsInitializesCampaignTroops", "ui", RequiresCampaign = false)]
        public static void KeepCurrentSettingsInitializesCampaignTroops()
        {
            var previousSelection = PresetSelectionBehavior.IsPresetSelected;
            var harmony = new Harmony("retinues.tests.keep-current-settings");
            var target = AccessTools.Method(typeof(TroopUnlockerBehavior), nameof(TroopUnlockerBehavior.TryUnlockNow));
            try
            {
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(PresetPopupTests), nameof(CaptureBootstrap)));
                _bootstrapCalls = 0;
                PresetSelectionBehavior.ResetSelection();
                var behavior = new PresetSelectionBehavior();
                AccessTools.Method(typeof(PresetSelectionBehavior), "KeepCurrentSettings").Invoke(behavior, null);
                Tests.AssertTrue(PresetSelectionBehavior.IsPresetSelected);
                Tests.AssertEqual(1, _bootstrapCalls, "Keeping settings must run the previously deferred unlock checks.");
                PresetSelectionBehavior.ResetSelection();
                Tests.AssertFalse(PresetSelectionBehavior.IsPresetSelected,
                    "Starting another campaign must not retain the previous campaign's selection flag.");
            }
            finally
            {
                harmony.Unpatch(target, HarmonyPatchType.All, harmony.Id);
                typeof(PresetSelectionBehavior).GetProperty(nameof(PresetSelectionBehavior.IsPresetSelected))
                    .GetSetMethod(true).Invoke(null, new object[] { previousSelection });
            }
        }
    }
}
