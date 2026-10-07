using System.Collections.Generic;
using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using Retinues.Troops.Save;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Regression tests for the revolt save-corruption fix and the scrub recovery command.
    /// These run a real scrub against the loaded campaign, so they should be run on a
    /// disposable test save.
    /// </summary>
    public static class RegressionTests
    {
        /// <summary>
        /// scrub_save must release an orphaned custom stub (not reachable from the player's
        /// faction trees) while leaving live troops untouched.
        /// </summary>
        [GameTest(
            "ScrubReleasesOrphanKeepsLive",
            "regression",
            "scrub_save releases orphan stubs and keeps the player's live troops"
        )]
        public static void ScrubReleasesOrphanKeepsLive(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            // A live troop that must survive the scrub.
            var live = Player.Clan?.RetinueElite;

            using var sandbox = new TestSandbox();

            // Fabricate an orphan: a custom stub registered active but not in any faction tree.
            var orphan = sandbox.NewStub();
            var vanilla = Player.Clan?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "Player culture has a basic root troop.");
            orphan.FillFrom(
                vanilla,
                keepUpgrades: false,
                keepEquipment: false,
                keepSkills: false
            );
            var orphanId = orphan.StringId;

            Tests.AssertTrue(
                WCharacter.ActiveStubIds.Contains(orphanId),
                "Orphan stub is active before the scrub."
            );

            FactionCheats.ScrubSave(new List<string>());

            Tests.AssertFalse(
                WCharacter.ActiveStubIds.Contains(orphanId),
                "Scrub released the orphan stub."
            );

            if (live != null)
            {
                Tests.AssertTrue(
                    live.IsActive,
                    "Scrub kept the player's live elite retinue active."
                );
            }
        }

        /// <summary>
        /// ClearStaleKingdomData must never discard data while the player actually leads a
        /// kingdom (those troops are legitimate).
        /// </summary>
        [GameTest(
            "StaleKingdomClearRespectsLeadership",
            "regression",
            "ClearStaleKingdomData does not drop data while leading a kingdom"
        )]
        public static void StaleKingdomClearRespectsLeadership(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var behavior = Campaign.Current.GetCampaignBehavior<FactionBehavior>();
            Tests.AssertNotNull(behavior, "FactionBehavior is registered.");

            bool cleared = behavior.ClearStaleKingdomData();

            if (Player.Kingdom != null)
            {
                Tests.AssertFalse(
                    cleared,
                    "Must not clear kingdom troop data while the player leads a kingdom."
                );
            }
        }

        /// <summary>
        /// The exact fix-3 invariant: a stub that has been deserialized onto via the faction-less
        /// path must be re-registered as active so AllocateStub can never recycle it (which is how
        /// edited troops were silently overwritten).
        /// </summary>
        [GameTest(
            "StubRegisteredAfterFactionlessDeserialize",
            "regression",
            "Faction-less deserialize re-claims its stub so AllocateStub cannot recycle it"
        )]
        public static void StubRegisteredAfterFactionlessDeserialize(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var vanilla = sandbox.NewFaction()?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A vanilla template troop is available.");

            // Build a troop on a stub and capture its save data.
            var troop = sandbox.NewStub();
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: false, keepSkills: false);
            troop.Name = "Recycle Guard Troop";
            var id = troop.StringId;
            var data = new TroopSaveData(troop);

            // Simulate the orphan state: the stub holds data but is no longer registered active.
            WCharacter.ActiveStubIds.Remove(id);
            Tests.AssertFalse(
                WCharacter.ActiveStubIds.Contains(id),
                "Stub is free before deserialize."
            );

            // Faction-less deserialize must re-claim the stub.
            data.Deserialize();
            Tests.AssertTrue(
                WCharacter.ActiveStubIds.Contains(id),
                "Deserialize re-registered the stub."
            );

            // And AllocateStub must not hand it back out.
            var next = WCharacter.AllocateStub();
            Tests.AssertTrue(
                next.StringId != id,
                "AllocateStub did not recycle the re-registered stub."
            );
        }

        /// <summary>
        /// Vanilla kingdoms reuse their culture's id. A culture wrapper must never compare equal
        /// to a kingdom wrapper sharing that id, or a vanilla troop's culture reads as the
        /// player's inherited kingdom and its volunteers are never swapped for custom troops.
        /// </summary>
        [GameTest(
            "CultureNeverEqualsKingdomWithSameId",
            "regression",
            "A culture wrapper is not equal to a kingdom wrapper that shares its id"
        )]
        public static void CultureNeverEqualsKingdomWithSameId(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var kingdom = Kingdom.All.FirstOrDefault(k =>
                k?.Culture != null && k.StringId == k.Culture.StringId
            );
            if (kingdom == null)
                Tests.Skip("No colliding ids in this campaign (total conversion); nothing to verify.");

            var culture = new WCulture(kingdom.Culture);
            var faction = new WFaction(kingdom);

            Tests.AssertTrue(culture.StringId == faction.StringId, "The ids collide (precondition).");
            Tests.AssertFalse(
                culture == faction,
                "A culture and a kingdom sharing an id are not equal."
            );
            Tests.AssertTrue(
                faction == new WFaction(kingdom),
                "Two wrappers of the same kingdom are still equal."
            );
        }

        /// <summary>
        /// Copying a naval troop must set the engine's CharacterObject.IsMariner flag (read by the
        /// party screen and the encyclopedia), not only the trait the editor reads. On 1.4 the
        /// property is getter-only, so the flag must be written through its backing field.
        /// </summary>
#if BL13 || BL14
        [GameTest(
            "ClonedNavalTroopKeepsEngineMarinerFlag",
            "regression",
            "A troop copied from a mariner keeps CharacterObject.IsMariner true, and clearing it works"
        )]
        public static void ClonedNavalTroopKeepsEngineMarinerFlag(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var mariner = CharacterObject.All.FirstOrDefault(c =>
                c != null && !c.IsHero && c.IsMariner
            );
            if (mariner == null)
                Tests.Skip("No naval troops in this game (no Naval DLC); nothing to verify.");

            using var sandbox = new TestSandbox();
            var troop = sandbox.NewStub();
            troop.FillFrom(
                new WCharacter(mariner),
                keepUpgrades: false,
                keepEquipment: false,
                keepSkills: false
            );

            Tests.AssertTrue(troop.IsMariner, "The copy carries the Mariner trait.");
            Tests.AssertTrue(
                troop.Base.IsMariner,
                "The copy carries the engine IsMariner flag the game UI reads."
            );

            troop.IsMariner = false;
            Tests.AssertFalse(troop.Base.IsMariner, "Clearing the trait clears the engine flag.");
        }
#endif

        /// <summary>
        /// Custom militia has been silently dead since game 1.3: the patch looked for a
        /// "ref int" overload of AddTroopToMilitiaParty that no longer exists. Pin the
        /// current overload so a future game change fails loudly here.
        /// </summary>
        [GameTest(
            "MilitiaSpawnHelperResolves",
            "regression",
            "The vanilla AddTroopToMilitiaParty overload used for custom militia resolves", RequiresCampaign = false)]
        public static void MilitiaSpawnHelperResolves(GameTestContext ctx)
        {
            Tests.AssertNotNull(
                Features.Swaps.Patches.PlayerMilitiaSpawnPatch.Helper_AddTroop,
                "Settlement.AddTroopToMilitiaParty resolved for this game version."
            );
            var countType = Features.Swaps.Patches.PlayerMilitiaSpawnPatch
                .Helper_AddTroop.GetParameters()[4].ParameterType;
#if BL12
            Tests.AssertTrue(countType == typeof(int).MakeByRefType(), "BL12 updates the remaining count by reference.");
#else
            Tests.AssertTrue(countType == typeof(int), "BL13+ uses independent lane counts.");
#endif
        }

        /// <summary>
        /// Rosters must never keep a non-canonical CharacterObject instance for a custom
        /// troop id: a save written in that state materializes a null-name skeleton on the
        /// next load and crashes wage/food/morale calculations.
        /// </summary>
        [GameTest(
            "RosterCanonicalizationHealsDuplicateInstance",
            "regression",
            "A roster entry referencing a duplicate custom troop instance is merged onto the registered troop"
        )]
        public static void RosterCanonicalizationHealsDuplicateInstance(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var canonical = sandbox.NewStub();
            Tests.AssertNotNull(canonical, "Allocated a stub as the canonical troop.");

            var twin = (CharacterObject)
                System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
                    typeof(CharacterObject)
                );
            twin.StringId = canonical.StringId;

            var roster = TroopRoster.CreateDummyTroopRoster();
            roster.AddToCounts(canonical.Base, 5, insertAtFront: false, woundedCount: 2);
            roster.AddToCounts(twin, 1, insertAtFront: false, woundedCount: 1);
            Tests.AssertEqual(2, roster.Count, "The twin occupies its own roster row.");

            var healed = Safety.StubIntegrityBehavior.CanonicalizeRoster(roster);

            Tests.AssertEqual(1, healed, "Exactly one entry was repaired.");
            Tests.AssertEqual(1, roster.Count, "The twin row was merged away.");

            var element = roster.GetElementCopyAtIndex(0);
            Tests.AssertTrue(
                ReferenceEquals(element.Character, canonical.Base),
                "The surviving row references the registered instance."
            );
            Tests.AssertEqual(6, element.Number, "Troop count preserved (5 + 1).");
        }

        /// <summary>
        /// Retinues are player-only: the AI upgrader must never be able to pick one even if
        /// stale data leaves a retinue reachable as an upgrade target.
        /// </summary>
        [GameTest(
            "AIUpgraderCannotPickRetinues",
            "regression",
            "The AI upgrade filter strips retinue targets for AI parties and leaves the player's untouched"
        )]
        public static void AIUpgraderCannotPickRetinues(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var retinue = Player.Clan?.RetinueElite ?? Player.Clan?.RetinueBasic;
            if (retinue?.Base == null || !retinue.IsActive)
                Tests.Skip("No retinues in this save; nothing to verify.");

            using var sandbox = new TestSandbox();
            var normal = sandbox.NewStub();

            var argsType = typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.PartyUpgraderCampaignBehavior).GetNestedType(
                "TroopUpgradeArgs",
                System.Reflection.BindingFlags.NonPublic
            );
            Tests.AssertNotNull(argsType, "The vanilla TroopUpgradeArgs struct exists.");

            var ctor = argsType
                .GetConstructors(
                    System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )
                .FirstOrDefault(c => c.GetParameters().Length == 6);
            Tests.AssertNotNull(ctor, "TroopUpgradeArgs has its 6-argument constructor.");

            System.Collections.IList Make(params CharacterObject[] targets)
            {
                var list = (System.Collections.IList)
                    System.Activator.CreateInstance(typeof(List<>).MakeGenericType(argsType));
                foreach (var target in targets)
                    list.Add(ctor.Invoke([target, target, 1, 0, 0, 1f]));
                return list;
            }

            var aiParty = MobileParty
                .All.FirstOrDefault(p => p?.Party != null && p.Party != PartyBase.MainParty)
                ?.Party;
            Tests.AssertNotNull(aiParty, "Found an AI party in the campaign.");

            var aiList = Make(retinue.Base, normal.Base);
            Features.Swaps.Patches.RetinueAIUpgradeFilterPatch.FilterForParty(aiParty, aiList);
            Tests.AssertEqual(1, aiList.Count, "AI party: the retinue entry was removed.");

            var playerList = Make(retinue.Base, normal.Base);
            Features.Swaps.Patches.RetinueAIUpgradeFilterPatch.FilterForParty(
                PartyBase.MainParty,
                playerList
            );
            Tests.AssertEqual(2, playerList.Count, "Main party keeps its retinue options.");
        }

        /// <summary>
        /// When Parameters/Version.xml is unreadable, the game generation is inferred from API
        /// markers; on a healthy install the two must agree, or broken installs would get the
        /// wrong (upside-down) editor layout.
        /// </summary>
        [GameTest(
            "VersionProbeMatchesVersionFile",
            "regression",
            "The API-marker version fallback agrees with the game's version file"
        )]
        public static void VersionProbeMatchesVersionFile(GameTestContext ctx)
        {
            var file = TaleWorlds.Library.ApplicationVersion.FromParametersFile();
            if (file.Major != 1)
                Tests.Skip("No readable version file here; nothing to compare against.");

            int probed;
            if (
                typeof(TaleWorlds.CampaignSystem.MapEvents.MapEvent).GetMethod(
                    "GetBattleRewards",
                    System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.Instance
                ) == null
            )
                probed = 4;
            else if (
                System.Type.GetType(
                    "TaleWorlds.CampaignSystem.Naval.Figurehead, TaleWorlds.CampaignSystem"
                ) != null
            )
                probed = 3;
            else
                probed = 2;

            var expected = file.Minor >= 4 ? 4 : file.Minor;
            Tests.AssertEqual(
                expected,
                probed,
                $"API probe (1.{probed}) matches the version file (1.{file.Minor})."
            );

            Tests.AssertTrue(
                Utils.BannerlordVersion.Version.Major >= 1,
                "The mod never reports a zero game version."
            );
        }

        /// <summary>
        /// The troop-copy path relies on FillFrom; game 1.4 moved it from CharacterObject to
        /// BasicCharacterObject. One of the two signatures must resolve on every game version.
        /// </summary>
        [GameTest(
            "CopyFillFromResolves",
            "regression",
            "A FillFrom overload (CharacterObject or BasicCharacterObject) exists for troop copying"
        )]
        public static void CopyFillFromResolves(GameTestContext ctx)
        {
            var method =
                HarmonyLib.AccessTools.Method(
                    typeof(CharacterObject),
                    "FillFrom",
                    [typeof(CharacterObject)]
                )
                ?? HarmonyLib.AccessTools.Method(
                    typeof(CharacterObject),
                    "FillFrom",
                    [typeof(BasicCharacterObject)]
                );

            Tests.AssertNotNull(method, "A FillFrom overload resolved on this game version.");
        }

        /// <summary>
        /// The appearance range copy needs either MBBodyProperty.CreateFrom (game 1.4+) or the
        /// legacy Clone; if both vanish the editor silently loses body-range isolation.
        /// </summary>
        [GameTest(
            "BodyRangeCloneSourceAvailable",
            "regression",
            "MBBodyProperty exposes CreateFrom or Clone for appearance-range copies"
        )]
        public static void BodyRangeCloneSourceAvailable(GameTestContext ctx)
        {
            var type = typeof(BodyProperties).Assembly.GetType("TaleWorlds.Core.MBBodyProperty");
            Tests.AssertNotNull(type, "The MBBodyProperty type exists.");

            var createFrom = type.GetMethod(
                "CreateFrom",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
            );
            var clone = type.GetMethod(
                "Clone",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
            );

            Tests.AssertTrue(
                createFrom != null || clone != null,
                "A body-range copy method (CreateFrom or Clone) exists."
            );
        }

        /// <summary>
        /// The naked-troops regression: on game 1.4 the copy chain aborted before installing the
        /// engine equipment roster, so troops created or converted after the game update spawned
        /// in battle with empty battle sets while the party screen still showed their gear.
        /// </summary>
        [GameTest(
            "CopiedTroopHasBattleEquipment",
            "regression",
            "A troop copied from a vanilla template carries non-empty battle equipment sets"
        )]
        public static void CopiedTroopHasBattleEquipment(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var vanilla = Player.Clan?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "Player culture has a basic root troop.");

            var stub = sandbox.NewStub();
            stub.FillFrom(vanilla, keepUpgrades: false, keepEquipment: true, keepSkills: false);

            var sets = stub.Base?.BattleEquipments?.ToList() ?? [];
            Tests.AssertTrue(sets.Count > 0, "The copied troop has at least one battle set.");

            bool anyItem = sets.Any(e =>
            {
                if (e == null)
                    return false;
                for (
                    var i = TaleWorlds.Core.EquipmentIndex.WeaponItemBeginSlot;
                    i < TaleWorlds.Core.EquipmentIndex.NumEquipmentSetSlots;
                    i++
                )
                {
                    if (e[i].Item != null)
                        return true;
                }
                return false;
            });
            Tests.AssertTrue(anyItem, "At least one battle set carries items (troop not naked).");
        }

        /// <summary>
        /// Smithed weapons must stay in the crafted equipment list across a restart. The engine's
        /// IsCraftedByPlayer flag is runtime-only and is not restored for weapons made through
        /// third-party smithing mods, so the persisted crafted-item id is accepted as well - but
        /// vanilla's XML pre-crafted weapons (which also carry a WeaponDesign) must stay out.
        /// </summary>
        [GameTest(
            "CraftedDetectionSurvivesRestart",
            "regression",
            "Crafted weapons are detected by their persisted id, and vanilla pre-crafted weapons are not"
        )]
        public static void CraftedDetectionSurvivesRestart(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var items = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObjectTypeList<
                TaleWorlds.Core.ItemObject
            >();
            Tests.AssertNotNull(items, "The item list is available.");

            foreach (var io in items)
            {
                if (io?.WeaponDesign == null)
                    continue;

                var item = new WItem(io);

                if (io.StringId?.StartsWith("crafted_item_") == true)
                {
                    // A smithed weapon: must read as crafted even when the runtime flag was lost
                    // (that is what happens to mod-made weapons after a reload).
                    Tests.AssertTrue(
                        item.IsCrafted,
                        $"Smithed weapon '{io.StringId}' is detected as crafted."
                    );
                }
                else if (!io.IsCraftedByPlayer)
                {
                    // Vanilla pre-crafted weapons from XML also carry a WeaponDesign but are not
                    // player work; they must never leak into the crafted list.
                    Tests.AssertFalse(
                        item.IsCrafted,
                        $"Vanilla pre-crafted weapon '{io.StringId}' is not treated as player-crafted."
                    );
                }
            }
        }
    }
}
