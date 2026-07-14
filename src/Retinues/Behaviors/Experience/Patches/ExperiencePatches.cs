using System;
using HarmonyLib;
using Helpers;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Experience.Patches
{
    /// <summary>
    /// Patches that ensure correct XP behavior and integrate skill point progress for faction troops.
    /// </summary>
    [HarmonyPatch]
    internal static class SkillPointExperiencePatches
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Vanilla XP Gate Fixes                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Postfix that fixes gainable XP limits for non-hero faction troops.
        /// </summary>
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

                if (!TryGetEligibleFactionTroop(character, out var wc))
                    return;

                int index = owner.MemberRoster.FindIndexOfTroop(character);
                if (index < 0)
                    return;

                int number = owner.MemberRoster.GetElementNumber(index);
                if (number <= 0)
                    return;

                int xpRequired = GetXpRequired(character, owner);
                int max = ComputeMaxXp(number, xpRequired);

                // Keep headroom fully open for the player's own custom troops, and for any max-rank
                // (no-upgrade-target) faction troop.
                //
                // Why the player-faction case matters for consistency: the game clamps the combat XP
                // it applies to a troop to CanTroopGainXp's gainableMaxXp (CommitXpGain does
                // Min(gainableMaxXp, earnedXp); the overflow goes to the leader's skill, not the
                // troop). If we only opened headroom for no-target troops, a mid-tier custom troop
                // pinned at its roster cap would get gainableMaxXp = 0 and earn ZERO skill-point XP
                // for the same kills that a max-rank retinue banks in full. Opening headroom for all
                // player custom troops makes skill-point gain track actual combat XP identically
                // regardless of tier or roster-XP state (the stored roster XP still caps via the
                // XP-change patches, so upgrade readiness is unchanged).
                if (wc.IsPlayerFactionTroop || HasNoUpgradeTargets(character))
                {
                    __result = true;
                    gainableMaxXp = max > 0 ? max : 1;
                    return;
                }

                int currentXp = owner.MemberRoster.GetElementXp(index);
                if (currentXp < max)
                {
                    __result = true;
                    gainableMaxXp = max - currentXp;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "MobilePartyHelper.CanTroopGainXp patch failed.");
            }
        }

#if BL12
        /// <summary>
        /// Prefix to clamp troop XP correctly for faction troops when upgrade targets are absent (BL1.2).
        /// </summary>
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

                // If the troop has targets, vanilla works.
                if (!HasNoUpgradeTargets(troop))
                    return true;

                if (!TryGetEligibleFactionTroop(troop, out _))
                    return true;

                int number = data[index].Number;
                if (number <= 0)
                {
                    data[index].Xp = 0;
                    return false;
                }

                var ownerParty = GetOwnerParty(__instance);
                int xpRequired = GetXpRequired(troop, ownerParty);

                data[index].Xp = ClampXp(data[index].Xp, number, xpRequired);

                // Skip vanilla (it would clamp to 0 because UpgradeTargets.Length == 0).
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "TroopRoster.ClampXp patch failed.");
                return true;
            }
        }

#endif

#if BL13 || BL14
        /// <summary>
        /// Prefix to clamp XP for faction troops when upgrade targets are absent (BL1.3).
        /// </summary>
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
                if (!HasNoUpgradeTargets(troop))
                    return true;

                if (!TryGetEligibleFactionTroop(troop, out _))
                    return true;

                int xpRequired = GetXpRequired(troop, __instance);
                element.Xp = ClampXp(element.Xp, element.Number, xpRequired);

                // Skip vanilla (it would compute num=0 then clamp to 0).
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "PartyBase.OnXpChanged patch failed.");
                return true;
            }
        }

#endif

        /// <summary>
        /// Captures the XP the game grants to a troop stack and routes it to skill-point progress.
        /// We read the requested <paramref name="xpAmount"/> (not the accepted delta) so at-cap
        /// troops and top-of-tree retinues — whose roster XP the game would clamp to 0 — still earn.
        ///
        /// The XP source is classified by the battle window (<see cref="BattleXpWindow"/>): inside a
        /// player-involved map event it's battle XP (mission ticks plus aftermath), otherwise it's
        /// campaign-map daily party/garrison training. Each source is individually toggleable, so
        /// the actual credit decision (including double-count safety vs auto-resolve) is made in
        /// <see cref="SkillPointExperienceGain.ApplyXpToSkillPointProgress"/>.
        /// </summary>
        [HarmonyPatch(typeof(TroopRoster), nameof(TroopRoster.AddXpToTroopAtIndex))]
        [HarmonyPostfix]
        private static void Postfix_AddXpToTroopAtIndex(
            TroopRoster __instance,
            int xpAmount,
            int index
        )
        {
            try
            {
                if (xpAmount <= 0 || __instance == null)
                    return;

                // Skip party-screen transfers and donations. Moving a stack relocates its
                // accumulated roster XP — added as a positive amount on the destination roster via
                // AddXpToTroop — which is not earned XP. Without this, transferring troops to a
                // companion or garrison mints skill points (a stack of high-XP retinues is worth
                // many points per soldier). Combat runs in a mission and training on the map tick,
                // never while a party screen is open, so no legitimate XP is lost here.
                if (IsPartyScreenOpen())
                    return;

                if (index < 0 || index >= __instance.Count)
                    return;

                var troop = __instance.GetCharacterAtIndex(index);
                if (troop == null || troop.IsHero)
                    return;

                var ownerParty = GetOwnerParty(__instance);

                // Skip prisoners: their conformity XP must not feed skill-point progress.
                if (ownerParty != null && Equals(__instance, ownerParty.PrisonRoster))
                    return;

                if (!TryGetEligibleFactionTroop(troop, out var wc))
                    return;

                // Battle window open => battle XP; closed => campaign-map training XP.
                var source = BattleXpWindow.IsOpen
                    ? SkillXpSource.ManualBattle
                    : SkillXpSource.Training;

                // Training XP counts only for the player's own main party — not garrisons, AI
                // parties, or companion parties, whose bulk daily training would otherwise mint
                // skill points. Battle XP stays unrestricted so custom troops fighting in allied
                // parties still earn from combat.
                if (source == SkillXpSource.Training && ownerParty != PartyBase.MainParty)
                    return;

                // ApplyXpToSkillPointProgress self-gates on the source toggle, IsPlayerFactionTroop,
                // vanilla, and SkillPointsMustBeEarned, so enemy / AI-clan / disabled-source XP is
                // filtered there.
                ApplyXpToSkillPointProgress(
                    wc,
                    ownerParty ?? Player.Party?.PartyBase,
                    xpAmount,
                    source
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "AddXpToTroopAtIndex XP capture failed.");
            }
        }

        /// <summary>
        /// Returns true if this troop should be handled by our XP fixes (faction troop, non-hero).
        /// </summary>
        public static bool TryGetEligibleFactionTroop(CharacterObject troop, out WCharacter wc)
        {
            wc = null;

            if (troop == null || troop.IsHero)
                return false;

            wc = WCharacter.Get(troop);
            if (wc == null)
                return false;

            // Keep semantics aligned with SkillPointExperienceGain: faction troops only.
            return wc.IsFactionTroop;
        }

        /// <summary>
        /// Returns true if the troop has no upgrade targets (vanilla clamps to 0 in that case).
        /// </summary>
        public static bool HasNoUpgradeTargets(CharacterObject troop)
        {
            var targets = troop?.UpgradeTargets;
            return targets == null || targets.Length == 0;
        }

        /// <summary>
        /// True while a party management screen (troop transfer / donation) is open. XP applied in
        /// that state is a roster relocation, not earned XP, and must not feed skill-point progress.
        /// </summary>
        private static bool IsPartyScreenOpen()
        {
            try
            {
                return Game.Current?.GameStateManager?.ActiveState is PartyState;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reads TroopRoster.OwnerParty via reflection (stable across versions).
        /// </summary>
        public static PartyBase GetOwnerParty(TroopRoster roster)
        {
            return roster == null
                ? null
                : Reflection.GetPropertyValue<PartyBase>(roster, "OwnerParty");
        }

        /// <summary>
        /// Gets a stable XP required value for clamping, using the shared utility.
        /// </summary>
        public static int GetXpRequired(CharacterObject troop, PartyBase party)
        {
            int xpRequired = SkillPointExperienceGain.GetXpRequiredToUpgradeThisUnit(troop, party);
            if (xpRequired <= 0)
                xpRequired = 100;

            return xpRequired;
        }

        /// <summary>
        /// Computes the per-stack max XP cap (number * xpRequired), clamped to int range.
        /// </summary>
        public static int ComputeMaxXp(int number, int xpRequired)
        {
            long max = (long)number * xpRequired;
            if (max > int.MaxValue)
                max = int.MaxValue;

            if (max < 0)
                max = 0;

            return (int)max;
        }

        /// <summary>
        /// Clamps XP to [0..max] where max is number * xpRequired.
        /// </summary>
        public static int ClampXp(int xp, int number, int xpRequired)
        {
            int max = ComputeMaxXp(number, xpRequired);
            return MBMath.ClampInt(xp, 0, max);
        }

        /// <summary>
        /// Applies gained XP to skill point progress, respecting feature settings.
        /// </summary>
        public static void ApplyXpToSkillPointProgress(
            WCharacter wc,
            PartyBase party,
            int gainedXp,
            SkillXpSource source
        )
        {
            SkillPointExperienceGain.ApplyXpToSkillPointProgress(wc, party, gainedXp, source);
        }
    }
}
