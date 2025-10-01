using System;
using System.Reflection;
using HarmonyLib;
using Helpers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Core.Safety.Patches
{
    [HarmonyPatch]
    internal static class GetMoraleEffectsFromSkill_SafePatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(DefaultPartyMoraleModel),
                "GetMoraleEffectsFromSkill",
                [typeof(MobileParty), typeof(ExplainedNumber).MakeByRefType()]
            );
        }

        [HarmonyPrefix]
        [SafeMethod]
        private static bool Prefix(MobileParty party, ref ExplainedNumber bonus)
        {
            try
            {
                if (party == null || party.Party == null)
                {
                    Log.Error(
                        "[MoraleDiag] GetMoraleEffectsFromSkill: party/party.Party is null - skipping."
                    );
                    return false; // skip original
                }

                CharacterObject leader = null;
                try
                {
                    leader = SkillHelper.GetEffectivePartyLeaderForSkill(party.Party);
                }
                catch (Exception exLeader)
                {
                    Log.Error("[MoraleDiag] GetEffectivePartyLeaderForSkill failed: " + exLeader);
                    MoralePatchesShared.DumpPartySnapshot(
                        "GetMoraleEffectsFromSkill:LeaderErr",
                        party
                    );
                    return false; // skip original
                }

                if (leader == null)
                {
                    // No leader (e.g., odd template parties) - just skip without adding bonus
                    return false;
                }

                int leadership = 0;
                try
                {
                    leadership = leader.GetSkillValue(DefaultSkills.Leadership);
                }
                catch (Exception exSkill)
                {
                    Log.Error("[MoraleDiag] leader.GetSkillValue(Leadership) threw: " + exSkill);
                    MoralePatchesShared.DumpPartySnapshot(
                        "GetMoraleEffectsFromSkill:SkillErr",
                        party
                    );
                    return false; // skip original
                }

                if (leadership > 0)
                {
                    try
                    {
                        SkillHelper.AddSkillBonusForCharacter(
                            DefaultSkills.Leadership,
                            DefaultSkillEffects.LeadershipMoraleBonus,
                            leader,
                            ref bonus
                        );
                    }
                    catch (Exception exAdd)
                    {
                        Log.Error("[MoraleDiag] AddSkillBonusForCharacter threw: " + exAdd);
                        // swallow, still skip original
                    }
                }

                return false; // handled safely, don't run original
            }
            catch (Exception ex)
            {
                Log.Error("[MoraleDiag] SafePrefix outer catch: " + ex);
                MoralePatchesShared.DumpPartySnapshot("GetMoraleEffectsFromSkill:Outer", party);
                return false; // skip original on any unexpected issue
            }
        }
    }

    [HarmonyPatch(
        typeof(DefaultPartyMoraleModel),
        nameof(DefaultPartyMoraleModel.GetEffectivePartyMorale)
    )]
    internal static class GetEffectivePartyMorale_FailSafe
    {
        [HarmonyFinalizer]
        private static void Finalizer(
            DefaultPartyMoraleModel __instance,
            MobileParty mobileParty,
            bool includeDescription,
            ref ExplainedNumber __result,
            Exception __exception
        )
        {
            if (__exception == null)
                return;

            try
            {
                Log.Error("[MoraleDiag] GetEffectivePartyMorale threw: " + __exception);
                MoralePatchesShared.DumpPartySnapshot("GetEffectivePartyMorale", mobileParty);

                __result = new ExplainedNumber(50f, includeDescription);
                __result.Add(0f, new TextObject("Retinues: fail-safe morale"));
                Log.Error("[MoraleDiag] Returned safe default morale = 50.");
            }
            catch (Exception dumpEx)
            {
                __result = new ExplainedNumber(50f, includeDescription);
                Log.Error(
                    "[MoraleDiag] Dump failed ("
                        + dumpEx.GetType().Name
                        + "). Returned default morale = 50."
                );
            }
        }
    }

    internal static class MoralePatchesShared
    {
        public static void DumpPartySnapshot(string where, MobileParty p)
        {
            try
            {
                if (p == null)
                {
                    Log.Error($"[MoraleDiag] {where}: party = <null>");
                    return;
                }

                var partyBase = p.Party;
                string pid = Safe(() => partyBase?.Id.ToString(), "<no-id>");
                string pname = Safe(() => partyBase?.Name?.ToString(), "<no-name>");
                string owner = Safe(() => p.LeaderHero?.Name?.ToString(), "<no-leader>");
                string clan = Safe(() => p.LeaderHero?.Clan?.Name?.ToString(), "<no-clan>");
                string settlement = Safe(() => p.CurrentSettlement?.Name?.ToString(), "<none>");

                Log.Error(
                    $"[MoraleDiag] {where}: PartyId={pid} Name={pname} Owner={owner} Clan={clan}"
                );
                Log.Error(
                    $"[MoraleDiag] Flags: IsMain={p.IsMainParty}, IsMilitia={p.IsMilitia}, IsGarrison={p.IsGarrison}, IsStarving={Safe(() => p.Party?.IsStarving, false)}"
                );
                Log.Error(
                    $"[MoraleDiag] Wages: HasUnpaid={Safe(() => p.HasUnpaidWages, 0f)}  Loc={settlement}"
                );

                var r = p.MemberRoster;
                int count = Safe(() => (int)(r?.Count ?? -1), -1);
                Log.Error($"[MoraleDiag] Roster count={count}");
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        string slotMsg;
                        try
                        {
                            var el = r.GetElementCopyAtIndex(i);
                            var ch = el.Character;

                            string cid = Safe(() => ch?.StringId, "<null-char>");
                            string cname = Safe(() => ch?.Name?.ToString(), "<null-name>");
                            int num = el.Number;
                            int wounded = Safe(() => el.WoundedNumber, 0);
                            int xp = el.Xp;
                            bool hero = Safe(() => ch?.IsHero ?? false, false);
                            int tier = Safe(() => ch?.Tier ?? -1, -1);

                            slotMsg =
                                $"[{i}] CharId={cid} Name={cname} Num={num} Wounded={wounded} Xp={xp} Hero={hero} Tier={tier}";
                        }
                        catch (Exception exSlot)
                        {
                            slotMsg =
                                $"[{i}] <slot read failed: {exSlot.GetType().Name}: {exSlot.Message}>";
                        }
                        Log.Error("[MoraleDiag] " + slotMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MoraleDiag] DumpPartySnapshot failed: " + ex);
            }
        }

        private static T Safe<T>(Func<T> f, T fallback)
        {
            try
            {
                return f();
            }
            catch
            {
                return fallback;
            }
        }
    }
}
