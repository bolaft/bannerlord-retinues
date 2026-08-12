using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Behaviors.Retinues;
using Retinues.Behaviors.Retinues.Patches;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Retinues are player-only. They must never survive as permanent upgrade targets (the
    /// vanilla AI upgrader would convert AI lords' troops into them), and the AI upgrader's
    /// choices are filtered as a structural guarantee.
    /// </summary>
    public static class RetinueExclusivityTests
    {
        /// <summary>
        /// Returns an existing retinue troop, or creates one in the sandbox when the campaign has
        /// none yet. Null when retinues are unavailable in this session.
        /// </summary>
        private static WCharacter FindOrCreateRetinue(TestSandbox sandbox)
        {
            var retinue = WCharacter.All.FirstOrDefault(c => c?.Base != null && c.IsRetinue);
            if (retinue != null)
                return retinue;

            if (!Configuration.EnableRetinues)
                return null;

            var behavior = RetinuesBehavior.Instance;
            var culture = WCulture.All.FirstOrDefault(c =>
                c?.Base != null && (c.RootElite ?? c.RootBasic)?.Base != null
            );
            if (behavior == null || culture == null)
                return null;

            var created = sandbox.Track(
                behavior.CreateRetinue(culture, "Test Retinue", notifyUnlocks: false)
            );
            WCharacter.InvalidateTroopSourceCaches();
            return created?.IsRetinue == true ? created : null;
        }

        [GameTest(
            "ScrubRemovesRetinueUpgradeTargets",
            "retinues",
            "The load-time scrub drops retinue upgrade links but keeps legitimate ones"
        )]
        public static void ScrubRemovesRetinueUpgradeTargets(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var retinue = FindOrCreateRetinue(sandbox);
            Tests.AssertNotNull(retinue, "Found or created a retinue troop.");

            // Simulate the stale data older versions could persist: a troop whose upgrade
            // targets include a retinue alongside a legitimate custom target.
            var troop = sandbox.NewStub();
            var legit = sandbox.NewStub();
            troop.AddUpgradeTarget(legit);
            troop.AddUpgradeTarget(retinue);
            Tests.AssertEqual(2, troop.UpgradeTargets.Count, "The stale retinue link is in place.");

            RetinuesBehavior.ScrubRetinueUpgradeTargets();

            var after = troop.UpgradeTargets;
            Tests.AssertEqual(1, after.Count, "Exactly one upgrade target survives the scrub.");
            Tests.AssertEqual(
                legit.StringId,
                after[0].StringId,
                "The legitimate target survives; the retinue link is gone."
            );
        }

        [GameTest(
            "AIUpgraderCannotPickRetinues",
            "retinues",
            "The AI upgrade filter strips retinue targets for AI parties and leaves the player's untouched"
        )]
        public static void AIUpgraderCannotPickRetinues(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var retinue = FindOrCreateRetinue(sandbox);
            Tests.AssertNotNull(retinue, "Found or created a retinue troop.");

            var normal = sandbox.NewStub();

            // Build the game's private List<TroopUpgradeArgs> the same way the AI upgrader does.
            var argsType = typeof(PartyUpgraderCampaignBehavior).GetNestedType(
                "TroopUpgradeArgs",
                BindingFlags.NonPublic
            );
            Tests.AssertNotNull(argsType, "The vanilla TroopUpgradeArgs struct exists.");

            var ctor = argsType
                .GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                )
                .FirstOrDefault(c => c.GetParameters().Length == 6);
            Tests.AssertNotNull(ctor, "TroopUpgradeArgs has its 6-argument constructor.");

            IList MakeList(params CharacterObject[] targets)
            {
                var list = (IList)
                    Activator.CreateInstance(typeof(List<>).MakeGenericType(argsType));
                foreach (var t in targets)
                    list.Add(ctor.Invoke([t, t, 1, 0, 0, 1f]));
                return list;
            }

            var aiParty = MobileParty
                .All?.FirstOrDefault(p => p?.Party != null && p.Party != PartyBase.MainParty)
                ?.Party;
            Tests.AssertNotNull(aiParty, "Found an AI party in the campaign.");

            // AI party: the retinue entry is removed, the normal one is kept.
            var aiList = MakeList(retinue.Base, normal.Base);
            RetinueAIUpgradeFilterPatch.FilterForParty(aiParty, aiList);
            Tests.AssertEqual(1, aiList.Count, "AI party: the retinue entry was removed.");

            var upgradeTargetField = argsType.GetField("UpgradeTarget");
            Tests.AssertNotNull(upgradeTargetField, "TroopUpgradeArgs exposes UpgradeTarget.");
            Tests.AssertEqual(
                normal.StringId,
                ((CharacterObject)upgradeTargetField.GetValue(aiList[0])).StringId,
                "AI party: the normal upgrade entry survives."
            );

            // Player party: nothing is filtered.
            var playerList = MakeList(retinue.Base, normal.Base);
            RetinueAIUpgradeFilterPatch.FilterForParty(PartyBase.MainParty, playerList);
            Tests.AssertEqual(2, playerList.Count, "Main party keeps its retinue upgrade options.");
        }
    }
}
