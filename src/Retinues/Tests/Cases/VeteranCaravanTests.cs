using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using HarmonyLib;
using Retinues.Compatibility;
using Retinues.Migration.Legacy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using StringReader = System.IO.StringReader;
using StringWriter = System.IO.StringWriter;

namespace Retinues.Tests.Cases
{
    public static class VeteranCaravanTests
    {
        private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));

        private static CharacterObject Troop(CultureObject culture, string id)
        {
            var troop = Empty<CharacterObject>();
            troop.StringId = id;
            AccessTools.PropertySetter(typeof(BasicCharacterObject), "Culture").Invoke(troop, new object[] { culture });
            AccessTools.Field(typeof(CharacterObject), "_occupation").SetValue(troop, Occupation.CaravanGuard);
            return troop;
        }

        private static void Set(CultureObject culture, string property, object value) =>
            AccessTools.PropertySetter(typeof(CultureObject), property).Invoke(culture, new[] { value });

        private static void Template(CultureObject culture, bool elite, params CharacterObject[] troops)
        {
            var template = Empty<PartyTemplateObject>();
            template.Stacks = new MBList<PartyTemplateStack>();
            foreach (var troop in troops)
                template.Stacks.Add(new PartyTemplateStack(troop, 1, 3));
#if BL12
            Set(culture, elite ? "EliteCaravanPartyTemplate" : "CaravanPartyTemplate", template);
#else
            Set(culture, elite ? "EliteCaravanPartyTemplates" : "CaravanPartyTemplates",
                new MBList<PartyTemplateObject> { template });
#endif
        }

        [GameTest("VeteranGuardsAreSeparateFromGuardsMastersAndTraders", "caravans", RequiresCampaign = false)]
        public static void VeteranGuardsAreSeparateFromGuardsMastersAndTraders()
        {
            foreach (var id in new[] { "aserai", "battania", "empire", "khuzait", "sturgia", "vlandia" })
            {
                var culture = Empty<CultureObject>();
                culture.StringId = id;
                var guard = Troop(culture, "caravan_guard_" + id);
                var master = Troop(culture, "caravan_master_" + id);
                var trader = Troop(culture, "armed_trader_" + id);
                var veteran = Troop(culture, "veteran_caravan_guard_" + id);
                Set(culture, "CaravanGuard", guard);
                Set(culture, "CaravanMaster", master);
                Template(culture, false, trader, guard, veteran);
                Template(culture, true, guard, veteran, master);
                Tests.AssertTrue(ReferenceEquals(veteran, CaravanTroopResolver.FindVeteran(culture)),
                    "Both caravan templates must resolve the same distinct veteran for " + id);
                // Duplicate appearances and another mod's specialist must not replace the veteran.
                Template(culture, true, veteran, veteran, Troop(culture, "extra_caravan_specialist"));
                Tests.AssertTrue(ReferenceEquals(veteran, CaravanTroopResolver.FindVeteran(culture)));
                Tests.AssertTrue(ReferenceEquals(guard, culture.CaravanGuard), "Discovery must not rewrite culture slots.");
            }
        }

        [GameTest("VeteranDiscoveryHandlesAbsentAndOverhaulTemplates", "caravans", RequiresCampaign = false)]
        public static void VeteranDiscoveryHandlesAbsentAndOverhaulTemplates()
        {
            Tests.AssertTrue(CaravanTroopResolver.FindVeteran(null) == null);
            var culture = Empty<CultureObject>();
            culture.StringId = "overhaul";
            Tests.AssertTrue(CaravanTroopResolver.FindVeteran(culture) == null);
            var guard = Troop(culture, "basic_guard");
            var master = Troop(culture, "merchant_leader");
            Set(culture, "CaravanGuard", guard);
            Set(culture, "CaravanMaster", master);
            Template(culture, false, guard, master, Troop(culture, "armed_trader_overhaul"), null);
            Tests.AssertTrue(CaravanTroopResolver.FindVeteran(culture) == null, "Do not clone an ordinary guard as a veteran.");
            var veteran = Troop(culture, "overhaul_senior_escort");
            Template(culture, true, veteran);
            Tests.AssertTrue(ReferenceEquals(veteran, CaravanTroopResolver.FindVeteran(culture)), "Elite-only template is supported.");
            Template(culture, true, veteran, Troop(culture, "another_specialist"));
            Tests.AssertTrue(CaravanTroopResolver.FindVeteran(culture) == null, "Ambiguous overhaul roles must not be guessed.");
            var foreign = Empty<CultureObject>();
            foreign.StringId = "foreign";
            var foreignGuard = Troop(foreign, "foreign_guard");
            var foreignMaster = Troop(foreign, "foreign_master");
            Set(foreign, "CaravanGuard", foreignGuard);
            Set(foreign, "CaravanMaster", foreignMaster);
            Template(culture, true, foreignGuard);
            Tests.AssertTrue(CaravanTroopResolver.FindVeteran(culture) == null, "A foreign ordinary guard is not a veteran.");
            Template(culture, true, foreignMaster);
            Tests.AssertTrue(CaravanTroopResolver.FindVeteran(culture) == null, "A foreign caravan master is not a veteran.");
        }

        [GameTest("VeteranCaravanRoleSurvivesCultureChanges", "caravans", RequiresCampaign = false)]
        public static void VeteranCaravanRoleSurvivesCultureChanges()
        {
            foreach (bool nativeId in new[] { true, false })
            {
                var owner = Empty<CultureObject>();
                owner.StringId = "caravan_owner";
                var destination = Empty<CultureObject>();
                destination.StringId = "caravan_destination";
                var guard = Troop(owner, "caravan_guard_owner");
                var master = Troop(owner, "caravan_master_owner");
                Set(owner, "CaravanGuard", guard);
                Set(owner, "CaravanMaster", master);
                var veteran = Troop(owner, nativeId ? "veteran_caravan_guard_caravan_owner" : "overhaul_senior_escort");
                Template(owner, false, guard, veteran);
                Template(owner, true, master, veteran);
                Tests.AssertTrue(ReferenceEquals(veteran, CaravanTroopResolver.FindVeteran(owner)));

                // This is the same CharacterObject culture setter used by the editor.
                AccessTools.PropertySetter(typeof(BasicCharacterObject), "Culture")
                    .Invoke(veteran, new object[] { destination });
                for (int reopen = 0; reopen < 3; reopen++)
                    Tests.AssertTrue(ReferenceEquals(veteran, CaravanTroopResolver.FindVeteran(owner)),
                        "Editing appearance culture cannot remove the assigned caravan veteran.");
                Tests.AssertTrue(CaravanTroopResolver.FindVeteran(destination) == null,
                    "Changing appearance culture must not assign the veteran to another culture's caravans.");
                Tests.AssertTrue(ReferenceEquals(destination, veteran.Culture), "Discovery must preserve the player's culture edit.");

                // Resolve a newly loaded identity without relying on a previous lookup cache.
                var loaded = Troop(destination, veteran.StringId);
                Template(owner, false, guard, loaded);
                Template(owner, true, master, loaded);
                Tests.AssertTrue(ReferenceEquals(loaded, CaravanTroopResolver.FindVeteran(owner)),
                    "An edited veteran must also resolve after save/load replaces its object.");
            }
        }

        [GameTest("VeteranSaveFieldIsAdditiveAndRoundTrips", "save-contracts", RequiresCampaign = false)]
        public static void VeteranSaveFieldIsAdditiveAndRoundTrips()
        {
            var field = typeof(FactionSaveData).GetField("VeteranCaravanGuard");
            var attr = field.GetCustomAttributesData().Single(a => a.AttributeType == typeof(SaveableFieldAttribute));
            Tests.AssertEqual(18, Convert.ToInt32(attr.ConstructorArguments[0].Value));
            var serializer = new XmlSerializer(typeof(FactionSaveData));
            using (var oldXml = new StringReader("<FactionSaveData><CaravanGuard><StringId>old_guard</StringId></CaravanGuard></FactionSaveData>"))
            {
                var old = (FactionSaveData)serializer.Deserialize(oldXml);
                Tests.AssertEqual("old_guard", old.CaravanGuard.StringId);
                Tests.AssertTrue(old.VeteranCaravanGuard == null, "Old data keeps its guard and leaves the new role unassigned.");
            }
            var saved = new FactionSaveData
            {
                CaravanGuard = new TroopSaveData { StringId = "existing_guard" },
                VeteranCaravanGuard = new TroopSaveData { StringId = "custom_veteran", VanillaStringId = "veteran_caravan_guard_empire" },
            };
            using (var xml = new StringWriter())
            {
                serializer.Serialize(xml, saved);
                using (var reader = new StringReader(xml.ToString()))
                {
                    var loaded = (FactionSaveData)serializer.Deserialize(reader);
                    Tests.AssertEqual("existing_guard", loaded.CaravanGuard.StringId);
                    Tests.AssertEqual("custom_veteran", loaded.VeteranCaravanGuard.StringId);
                    Tests.AssertEqual("veteran_caravan_guard_empire", loaded.VeteranCaravanGuard.VanillaStringId);
                }
            }
        }

        [GameTest("VeteranGuardsAppearInEditorAndMapToTheirOwnRole", "caravans")]
        public static void VeteranGuardsAppearInEditorAndMapToTheirOwnRole(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            foreach (var culture in Domain.Factions.Wrappers.WCulture.All)
            {
                var veteran = culture.VeteranCaravanGuard;
                if (veteran == null)
                    continue;
                Tests.AssertTrue(culture.RosterCaravan.Contains(veteran), "The Universal Editor must list veterans.");
                Tests.AssertTrue(veteran.IsCaravan, "Caravan spawn and daily swap filters must recognize veterans.");
                foreach (var faction in new Domain.Factions.IBaseFaction[] { Domain.Player.Clan, Domain.Player.Kingdom })
                {
                    if (faction?.VeteranCaravanGuard == null)
                        continue;
                    Tests.AssertTrue(faction.RosterCaravan.Contains(faction.VeteranCaravanGuard));
                    Tests.AssertEqual(faction.VeteranCaravanGuard,
                        Domain.Characters.Services.Matching.CharacterMatcher.FindCounterpart(veteran, faction));
                    if (culture.CaravanGuard != null && faction.CaravanGuard != null)
                        Tests.AssertEqual(faction.CaravanGuard,
                            Domain.Characters.Services.Matching.CharacterMatcher.FindCounterpart(culture.CaravanGuard, faction),
                            "Ordinary guards must not be promoted to veterans by the swap.");
                }
            }
        }
    }
}
