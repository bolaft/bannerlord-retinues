using System;
using HarmonyLib;
using Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace Retinues.Game.Experience.Patches
{
    [HarmonyPatch]
    internal static class SkillPointExperiencePatches
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Vanilla XP Gate Fixes                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [HarmonyPatch(typeof(MobilePartyHelper), nameof(MobilePartyHelper.CanTroopGainXp))]
        [HarmonyPostfix]
        private static void Postfix_CanTroopGainXp(
            PartyBase owner,
            CharacterObject character,
            ref int gainableMaxXp,
            ref bool __result
        )
        {
            try
            {
                if (__result)
                    return;

                if (owner == null || character == null || character.IsHero)
                    return;

                var wc = WCharacter.Get(character);
                if (wc == null || !wc.InCustomTree)
                    return;

                int index = owner.MemberRoster.FindIndexOfTroop(character);
                if (index < 0)
                    return;

                int number = owner.MemberRoster.GetElementNumber(index);
                if (number <= 0)
                    return;

                int currentXp = owner.MemberRoster.GetElementXp(index);

                int xpRequired = SkillPointExperienceGain.GetXpRequiredToUpgradeThisUnit(
                    character,
                    owner
                );
                if (xpRequired <= 0)
                    return;

                long max = (long)xpRequired * number;
                if (max > int.MaxValue)
                    max = int.MaxValue;

                if (currentXp < (int)max)
                {
                    __result = true;
                    gainableMaxXp = (int)max - currentXp;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "MobilePartyHelper.CanTroopGainXp patch failed.");
            }
        }

#if BL12
        // BL1.2.x: clamping is done in TroopRoster.ClampXp / IsPrisonRoster exists.
        [HarmonyPatch(typeof(TroopRoster), "ClampXp")]
        [HarmonyPrefix]
        private static bool Prefix_TroopRoster_ClampXp(TroopRoster __instance, int index)
        {
            try
            {
                if (__instance == null)
                    return true;

                if (__instance.IsPrisonRoster)
                    return true;

                var data = Reflection.GetFieldValue<TroopRosterElement[]>(__instance, "data");
                if (data == null || index < 0 || index >= data.Length)
                    return true;

                var troop = data[index].Character;
                if (troop == null || troop.IsHero)
                    return true;

                if (troop.UpgradeTargets != null && troop.UpgradeTargets.Length > 0)
                    return true;

                var wc = WCharacter.Get(troop);
                if (wc == null || !wc.InCustomTree)
                    return true;

                int number = data[index].Number;
                if (number <= 0)
                {
                    data[index].Xp = 0;
                    return false;
                }

                var ownerParty = Reflection.GetPropertyValue<PartyBase>(__instance, "OwnerParty");

                int xpRequired = SkillPointGain.GetXpRequiredToUpgradeThisUnit(ownerParty, troop);
                if (xpRequired <= 0)
                    xpRequired = 100;

                long max = (long)xpRequired * number;
                if (max > int.MaxValue)
                    max = int.MaxValue;

                data[index].Xp = MBMath.ClampInt(data[index].Xp, 0, (int)max);

                // Skip vanilla (it would clamp to 0 because UpgradeTargets.Length == 0).
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "TroopRoster.ClampXp patch failed.");
                return true;
            }
        }

        // BL1.2.x: AddXpToTroopAtIndex returns "actually applied XP".
        [HarmonyPatch(typeof(TroopRoster), nameof(TroopRoster.AddXpToTroopAtIndex))]
        [HarmonyPostfix]
        private static void Postfix_AddXpToTroopAtIndex_BL12(
            TroopRoster __instance,
            int xpAmount,
            int index,
            ref int __result
        )
        {
            try
            {
                int gained = __result;
                if (gained <= 0)
                    return;

                if (__instance == null || __instance.IsPrisonRoster)
                    return;

                var data = Reflection.GetFieldValue<TroopRosterElement[]>(__instance, "data");
                if (data == null || index < 0 || index >= data.Length)
                    return;

                var troop = data[index].Character;
                if (troop == null || troop.IsHero)
                    return;

                var wc = WCharacter.Get(troop);
                if (wc == null || !wc.InCustomTree)
                    return;

                var ownerParty = Reflection.GetPropertyValue<PartyBase>(__instance, "OwnerParty");
                SkillPointExperience.ApplyXpToSkillPointProgress(wc, ownerParty, gained);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "AddXpToTroopAtIndex (BL12) patch failed.");
            }
        }
#endif

#if BL13
        // BL1.3.x: clamping is done in PartyBase.OnXpChanged(roster, ref element).
        // This is where "no upgrade targets => XP clamps to 0" happens.
        [HarmonyPatch(typeof(PartyBase), "OnXpChanged")]
        [HarmonyPrefix]
        private static bool Prefix_PartyBase_OnXpChanged(
            PartyBase __instance,
            TroopRoster roster,
            ref TroopRosterElement element
        )
        {
            try
            {
                var troop = element.Character;
                if (troop == null)
                    return true;

                if (troop.IsHero)
                {
                    // Keep vanilla hero behavior.
                    element.Xp = MathF.Max(element.Xp, 0);
                    return false;
                }

                if (__instance == null || roster == null)
                    return true;

                // Keep vanilla prisoner behavior (conformity clamp).
                if (Equals(roster, __instance.PrisonRoster))
                    return true;

                // If the troop has targets, vanilla works.
                if (troop.UpgradeTargets != null && troop.UpgradeTargets.Length > 0)
                    return true;

                var wc = WCharacter.Get(troop);
                if (wc == null || !wc.InCustomTree)
                    return true;

                int xpRequired = SkillPointExperienceGain.GetXpRequiredToUpgradeThisUnit(
                    troop,
                    __instance
                );
                if (xpRequired <= 0)
                    xpRequired = 100;

                long max = (long)element.Number * xpRequired;
                if (max > int.MaxValue)
                    max = int.MaxValue;

                element.Xp = MBMath.ClampInt(element.Xp, 0, (int)max);

                // Skip vanilla (it would compute num=0 then clamp to 0).
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "PartyBase.OnXpChanged patch failed.");
                return true;
            }
        }

        // BL1.3.x: AddXpToTroopAtIndex is void; compute gained XP via prefix/postfix.
        [HarmonyPatch(typeof(TroopRoster), nameof(TroopRoster.AddXpToTroopAtIndex))]
        [HarmonyPrefix]
        private static void Prefix_AddXpToTroopAtIndex_BL13(
            TroopRoster __instance,
            int index,
            ref int __state
        )
        {
            __state = 0;

            try
            {
                if (__instance == null || index < 0 || index >= __instance.Count)
                    return;

                __state = __instance.GetElementXp(index);
            }
            catch
            {
                __state = 0;
            }
        }

        [HarmonyPatch(typeof(TroopRoster), nameof(TroopRoster.AddXpToTroopAtIndex))]
        [HarmonyPostfix]
        private static void Postfix_AddXpToTroopAtIndex_BL13(
            TroopRoster __instance,
            int index,
            int __state
        )
        {
            try
            {
                if (__instance == null || index < 0 || index >= __instance.Count)
                    return;

                // Skip prison roster: in 1.3 we can identify it through OwnerParty.PrisonRoster.
                var ownerParty = Reflection.GetPropertyValue<PartyBase>(__instance, "OwnerParty");
                if (ownerParty != null && Equals(__instance, ownerParty.PrisonRoster))
                    return;

                var troop = __instance.GetCharacterAtIndex(index);
                if (troop == null || troop.IsHero)
                    return;

                int after = __instance.GetElementXp(index);
                int gained = after - __state;
                if (gained <= 0)
                    return;

                var wc = WCharacter.Get(troop);
                if (wc == null || !wc.InCustomTree)
                    return;

                SkillPointExperienceGain.ApplyXpToSkillPointProgress(wc, ownerParty, gained);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "AddXpToTroopAtIndex (BL13) patch failed.");
            }
        }
#endif
    }
}
