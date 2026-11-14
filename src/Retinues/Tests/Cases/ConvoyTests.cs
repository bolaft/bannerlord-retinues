using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Compatibility;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class ConvoyTests
    {
        private static CharacterObject Troop(string id, CultureObject culture = null)
        {
            var troop = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
            troop.StringId = id;
            AccessTools.PropertySetter(typeof(BasicCharacterObject), "Culture")
                .Invoke(troop, new object[] { culture });
            AccessTools.Field(typeof(CharacterObject), "_occupation").SetValue(troop, Occupation.CaravanGuard);
            return troop;
        }

        [GameTest("ConvoysAreOptionalWithoutWarSails", "caravans", RequiresCampaign = false)]
        public static void ConvoysAreOptionalWithoutWarSails()
        {
            Tests.AssertEqual(0, ConvoyTroopResolver.Find((CultureObject)null).Count);
            Tests.AssertEqual(0, ConvoyTroopResolver.Find("empire", _ => null).Count);
            Tests.AssertEqual(0, ConvoyTroopResolver.Find("", _ => throw new Exception("No lookup without a culture")).Count);
            Tests.AssertFalse(typeof(ConvoyTroopResolver).Assembly.GetReferencedAssemblies()
                .Any(a => a.Name.StartsWith("NavalDLC", StringComparison.Ordinal)),
                "The editor must load without the optional War Sails assemblies.");
        }

        [GameTest("ConvoysRemainDiscoverableAfterEditingAndReload", "caravans", RequiresCampaign = false)]
        public static void ConvoysRemainDiscoverableAfterEditingAndReload()
        {
            var appearance = (CultureObject)FormatterServices.GetUninitializedObject(typeof(CultureObject));
            appearance.StringId = "changed_appearance";
            foreach (var culture in new[] { "aserai", "battania", "empire", "khuzait", "sturgia", "vlandia", "nord" })
            {
                var ids = new[] { "naval_caravan_guard_", "naval_veteran_caravan_guard_",
                    "naval_caravan_master_", "naval_armed_trader_" }.Select(p => p + culture).ToArray();
                var database = ids.ToDictionary(id => id, id => Troop(id));
                CharacterObject Lookup(string id) => database.TryGetValue(id, out var troop) ? troop : null;
                Tests.AssertEqual(4, ConvoyTroopResolver.Find(culture, Lookup).Count);
                foreach (var troop in database.Values)
                    AccessTools.PropertySetter(typeof(BasicCharacterObject), "Culture")
                        .Invoke(troop, new object[] { appearance });
                Tests.AssertTrue(ConvoyTroopResolver.Find(culture, Lookup).Select(t => t.StringId).SequenceEqual(ids),
                    "Edited appearance culture must not move convoy troops out of their original editor section.");
                Tests.AssertEqual(0, ConvoyTroopResolver.Find(appearance.StringId, Lookup).Count);

                var previous = database[ids[1]];
                database[ids[1]] = Troop(ids[1], appearance);
                Tests.AssertTrue(ReferenceEquals(database[ids[1]], ConvoyTroopResolver.Find(culture, Lookup)[1]),
                    "A reload must resolve the current character rather than retaining a stale object.");
                Tests.AssertFalse(ReferenceEquals(previous, ConvoyTroopResolver.Find(culture, Lookup)[1]));
            }
        }

        [GameTest("PartialConvoyDataNeverSubstitutesLandGuards", "caravans", RequiresCampaign = false)]
        public static void PartialConvoyDataNeverSubstitutesLandGuards()
        {
            var guard = Troop("naval_caravan_guard_empire");
            var landGuard = Troop("caravan_guard_empire");
            var database = new Dictionary<string, CharacterObject>
            {
                [guard.StringId] = guard,
                [landGuard.StringId] = landGuard,
            };
            CharacterObject Lookup(string id) => database.TryGetValue(id, out var troop) ? troop : null;
            var convoy = ConvoyTroopResolver.Find("empire", Lookup);
            Tests.AssertEqual(1, convoy.Count);
            Tests.AssertTrue(ReferenceEquals(guard, convoy[0]));
            Tests.AssertFalse(convoy.Contains(landGuard));
            Tests.AssertEqual(0, ConvoyTroopResolver.Find("empire", _ => landGuard).Count,
                "A missing naval role must never fall back to a land troop.");
            database.Remove(guard.StringId);
            Tests.AssertEqual(0, ConvoyTroopResolver.Find("empire", Lookup).Count,
                "Removing optional data must hide the section instead of leaving cached troops.");
        }

        [GameTest("ConvoyEditorIncludesExistingNativeTroops", "caravans")]
        public static void ConvoyEditorIncludesExistingNativeTroops(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            foreach (var culture in Game.Wrappers.WCulture.All)
            {
                var native = ConvoyTroopResolver.Find(culture.Base);
                var listed = culture.ConvoyTroops;
                Tests.AssertTrue(native.Select(t => t.StringId).SequenceEqual(listed.Select(t => t.StringId)));
                // Exercise the actual save collector without altering troop data.
                var editedBefore = Game.Wrappers.WCharacter.EditedVanillaRootIds.ToArray();
                try
                {
                    foreach (var convoy in listed)
                        convoy.NeedsPersistence = true;
                    var saved = new Troops.Save.FactionSaveData(culture);
                    foreach (var convoy in listed)
                        Tests.AssertTrue(saved.Extras != null && saved.Extras.Any(e => e.StringId == convoy.StringId),
                            "Convoy edits must enter the existing extra-troop save records.");
                }
                finally
                {
                    Game.Wrappers.WCharacter.EditedVanillaRootIds.Clear();
                    Game.Wrappers.WCharacter.EditedVanillaRootIds.UnionWith(editedBefore);
                }
                foreach (var troop in listed)
                {
                    Tests.AssertTrue(troop.IsVanilla, "Editing a convoy must not allocate a custom replacement.");
                    Tests.AssertTrue(culture.Troops.Contains(troop), "Faction operations must include convoy troops.");
                    Tests.AssertFalse(culture.CaravanTroops.Contains(troop), "Keep sea and land roles separate.");
                }
            }
        }
    }
}
