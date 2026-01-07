using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;

namespace Retinues.Game.Troops.Retinues.Patches
{
    internal static class RetinueUpgradeTargetsTempPatch
    {
        private static readonly object Sync = new();
        private static readonly Dictionary<string, CharacterObject[]> OriginalById = new(
            StringComparer.Ordinal
        );

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

            SetUpgradeTargets(source, list.ToArray());
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
        //          Intercept click on retinue "upgrade"          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [HarmonyPatch(typeof(PartyVM))]
        internal static class PartyVM_ExecuteUpgrade_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(PartyVM.ExecuteUpgrade))]
            private static bool Prefix(
                PartyVM __instance,
                PartyCharacterVM troop,
                int upgradeTargetType,
                int maxUpgradeCount
            )
            {
                try
                {
                    if (troop?.Character == null)
                        return true;

                    var targets = troop.Character.UpgradeTargets;
                    if (
                        targets == null
                        || upgradeTargetType < 0
                        || upgradeTargetType >= targets.Length
                    )
                        return true; // Invalid target, let vanilla handle it.

                    var target = targets[upgradeTargetType];
                    if (target == null)
                        return true; // No valid target, let vanilla handle it.

                    var wTarget = WCharacter.Get(target);
                    if (wTarget == null || !wTarget.IsRetinue)
                        return true; // Not a retinue upgrade, let vanilla handle it.

                    var party = Player.Party;
                    var cap = (int)Math.Floor(party.PartySizeLimit * Settings.MaxRetinueRatio);
                    var totalRetinues = party
                        .MemberRoster.Elements.Where(e => e.Troop.IsRetinue == true)
                        .Sum(e => e.Number);

                    if (totalRetinues < cap)
                        return true;
                    else
                    {
                        Inquiries.Popup(
                            L.T("retinue_upgrade_cap_reached_title", "Retinue Cap Reached"),
                            L.T(
                                    "retinue_upgrade_cap_reached_message",
                                    "You have reached the maximum allowed amount of retinues in your party ({CAP}).\n\nIncrease your max party size to allow for more retinues."
                                )
                                .SetTextVariable("CAP", cap.ToString())
                        );
                        return false; // Handled.
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "Retinue ExecuteUpgrade intercept failed.");
                    return true;
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
    }
}
