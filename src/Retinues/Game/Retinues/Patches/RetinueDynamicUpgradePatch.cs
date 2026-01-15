using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.UI.Services;
using Retinues.UI.VM;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace Retinues.Game.Retinues.Patches
{
    internal static class RetinueDynamicUpgradePatch
    {
        private static readonly object Sync = new();
        private static readonly Dictionary<string, CharacterObject[]> OriginalById = new(
            StringComparer.Ordinal
        );

        private sealed class SavedUpgradeState(
            int availableUpgrades,
            bool isAvailable,
            bool isInsufficient,
            BasicTooltipViewModel hint
        )
        {
            public readonly int AvailableUpgrades = availableUpgrades;
            public readonly bool IsAvailable = isAvailable;
            public readonly bool IsInsufficient = isInsufficient;
            public readonly BasicTooltipViewModel Hint = hint;
        }

        private static readonly Dictionary<object, SavedUpgradeState> SavedUpgradeByVm = [];

        private static void ApplyRetinueCapRule(PartyCharacterVM vm)
        {
            if (vm?.Character == null)
                return;

            // Only for right-side member troops (player party side).
            if (vm.Side != PartyScreenLogic.PartyRosterSide.Right)
                return;
            if (vm.Type != PartyScreenLogic.TroopType.Member)
                return;
            if (vm.IsHero)
                return;

            var upgrades = vm.Upgrades;
            var targets = vm.Character.UpgradeTargets;

            if (upgrades == null || targets == null || upgrades.Count == 0 || targets.Length == 0)
                return;

            var party = Player.Party;
            if (party == null)
                return;

            var cap = (int)Math.Floor(party.PartySizeLimit * Settings.MaxRetinueRatio);

            var totalRetinues = party
                .MemberRoster.Elements.Where(e => e.Troop.IsRetinue == true)
                .Sum(e => e.Number);

            var atCap = totalRetinues >= cap;

            var n = Math.Min(upgrades.Count, targets.Length);

            for (int i = 0; i < n; i++)
            {
                var target = targets[i];
                if (target == null)
                    continue;

                var wTarget = WCharacter.Get(target);
                if (wTarget == null || !wTarget.IsRetinue)
                    continue;

                var u = upgrades[i];
                if (u == null)
                    continue;

                // Restore path
                if (!atCap)
                {
                    if (SavedUpgradeByVm.TryGetValue(u, out var saved))
                    {
                        u.Hint = saved.Hint;
                        u.AvailableUpgrades = saved.AvailableUpgrades;
                        u.IsAvailable = saved.IsAvailable;
                        u.IsInsufficient = saved.IsInsufficient;

                        SavedUpgradeByVm.Remove(u);
                    }

                    continue;
                }

                // Disable path (save once, then override)
                if (!SavedUpgradeByVm.ContainsKey(u))
                {
                    SavedUpgradeByVm[u] = new SavedUpgradeState(
                        u.AvailableUpgrades,
                        u.IsAvailable,
                        u.IsInsufficient,
                        u.Hint
                    );
                }

                var capLine = L.T(
                        "retinue_upgrade_cap_reached_hint",
                        "Upgrade to {RETINUE}\nMax retinue cap reached: {CAP}\nIncrease your party size limit to allow for more retinues"
                    )
                    .SetTextVariable("RETINUE", target.Name)
                    .SetTextVariable("CAP", cap.ToString())
                    .ToString();

                u.Hint = new Tooltip(capLine);

                // Grey out: Unavailable brush
                u.IsAvailable = false;
                u.IsInsufficient = false;

                // IMPORTANT: do NOT zero this, otherwise you have nothing to restore.
                // u.AvailableUpgrades stays whatever vanilla computed.
            }
        }

        private static CharacterObject[] GetUpgradeTargets(CharacterObject c)
        {
            return c?.UpgradeTargets ?? [];
        }

        private static void SetUpgradeTargets(CharacterObject c, CharacterObject[] targets)
        {
            if (c == null)
                return;

            Reflection.SetPropertyValue(c, "UpgradeTargets", targets ?? []);
        }

        private static void EnsureInjected(CharacterObject source)
        {
            if (source == null)
                return;

            var sid = source.StringId;
            if (string.IsNullOrEmpty(sid))
                return;

            // Only inject once per screen session.
            lock (Sync)
                if (OriginalById.ContainsKey(sid))
                    return;

            var wsource = WCharacter.Get(source);
            if (wsource?.Base == null)
                return;

            // Find player-owned retinues that can convert from this source.
            var retinues = WCharacter.GetPlayerRetinuesForSource(wsource);
            if (retinues == null || retinues.Count == 0)
                return;

            var original = GetUpgradeTargets(source);

            var list = new List<CharacterObject>(original.Length + retinues.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // Keep original targets first.
            for (int i = 0; i < original.Length; i++)
            {
                var t = original[i];
                if (t == null)
                    continue;

                var id = t.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (seen.Add(id))
                    list.Add(t);
            }

            // Append retinue targets.
            for (int i = 0; i < retinues.Count; i++)
            {
                var r = retinues[i];
                if (r?.Base == null)
                    continue;

                var id = r.Base.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (seen.Add(id))
                    list.Add(r.Base);
            }

            if (list.Count == original.Length)
                return;

            lock (Sync)
                OriginalById[sid] = original;

            SetUpgradeTargets(source, [.. list]);
        }

        private static void RestoreAll()
        {
            lock (Sync)
            {
                foreach (var kv in OriginalById)
                {
                    var id = kv.Key;
                    var original = kv.Value;

                    var c = CharacterObject.Find(id);
                    if (c == null)
                        continue;

                    SetUpgradeTargets(c, original);
                }

                OriginalById.Clear();
                SavedUpgradeByVm.Clear();
            }
        }

        [HarmonyPatch(typeof(PartyCharacterVM))]
        internal static class PartyCharacterVM_SetCharacter_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch("set_Character")]
            private static void Prefix(PartyCharacterVM __instance, CharacterObject value)
            {
                try
                {
                    if (value == null)
                        return;

                    // Only for right-side member troops (player party side).
                    if (__instance.Side != PartyScreenLogic.PartyRosterSide.Right)
                        return;
                    if (__instance.Type != PartyScreenLogic.TroopType.Member)
                        return;
                    if (__instance.IsHero)
                        return;

                    EnsureInjected(value);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "Retinue upgrade injection failed.");
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //         Disable retinue upgrade when cap reached       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [HarmonyPatch(typeof(PartyCharacterVM))]
        internal static class PartyCharacterVM_InitializeUpgrades_RetinueCap_Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(PartyCharacterVM.InitializeUpgrades))]
            private static void Postfix(PartyCharacterVM __instance)
            {
                try
                {
                    ApplyRetinueCapRule(__instance);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "Retinue cap upgrade disable failed.");
                }
            }
        }

        [HarmonyPatch(typeof(PartyCharacterVM))]
        internal static class PartyCharacterVM_RefreshValues_RetinueCap_Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(PartyCharacterVM.RefreshValues))]
            private static void Postfix(PartyCharacterVM __instance)
            {
                try
                {
                    ApplyRetinueCapRule(__instance);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "Retinue cap refresh disable failed.");
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Restore Screen Close                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [HarmonyPatch]
        internal static class PartyScreen_Close_Patch
        {
            private static MethodBase TargetMethod()
            {
                // Try multiple known party screen type names.
                var t =
                    AccessTools.TypeByName("SandBox.GauntletUI.GauntletPartyScreen")
                    ?? AccessTools.TypeByName("SandBox.GauntletUI.MenuScreens.GauntletPartyScreen")
                    ?? AccessTools.TypeByName("SandBox.GauntletUI.Party.GauntletPartyScreen");

                if (t == null)
                    return null;

                return AccessTools.Method(t, "ClosePartyPresentation")
                    ?? AccessTools.Method(t, "OnFinalize")
                    ?? AccessTools.Method(t, "OnDeactivate");
            }

            [HarmonyPostfix]
            private static void Postfix()
            {
                RestoreAll();
            }
        }

        [HarmonyPatch(typeof(PartyVM))]
        internal static class PartyVM_Update_RetinueCapRefresh_Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch("Update")] // private method
            private static void Postfix(PartyVM __instance, PartyScreenLogic.PartyCommand command)
            {
                try
                {
                    if (__instance == null || command == null)
                        return;

                    // Only after actions that can change retinue count / eligibility.
                    switch (command.Code)
                    {
                        case PartyScreenLogic.PartyCommandCode.ExecuteTroop: // dismiss
                        case PartyScreenLogic.PartyCommandCode.TransferTroop:
                        case PartyScreenLogic.PartyCommandCode.TransferPartyLeaderTroop:
                        case PartyScreenLogic.PartyCommandCode.TransferTroopToLeaderSlot:
                        case PartyScreenLogic.PartyCommandCode.TransferAllTroops:
                        case PartyScreenLogic.PartyCommandCode.RecruitTroop:
                            break;

                        default:
                            return;
                    }

                    // Refresh upgrades for ALL main-party troops (vanilla does this only on UpgradeTroop).
                    var troops = __instance.MainPartyTroops;
                    if (troops == null)
                        return;

                    for (int i = 0; i < troops.Count; i++)
                        troops[i]?.InitializeUpgrades();

                    // No need to call ApplyRetinueCapRule here: the InitializeUpgrades postfix handles it.
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "Retinue cap refresh (PartyVM.Update) failed.");
                }
            }
        }
    }
}
