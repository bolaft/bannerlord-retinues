using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

// Isolated host only: discard the exporting campaign's object registry, then load
// the generated NPC XML through Bannerlord's actual BasicCharacterObject reader.
internal static class NpcModuleExportProbe
{
    private static bool RaceNames(ref string[] __result)
    {
        __result = new[] { "human", "test_race" };
        return false;
    }

    private static bool RaceId(string raceId, ref int __result)
    {
        __result = raceId == "test_race" ? 1 : 0;
        return false;
    }

    internal static void Run(Assembly module)
    {
        var cultureBefore = Thread.CurrentThread.CurrentCulture;
        var harmony = new Harmony("retinues.headless.npc-module-export");
        var scopes = new List<IDisposable>();
        var bodyHelper = module.GetType("Retinues.Domain.Characters.Helpers.BodyHelper", true);
        try
        {
            harmony.Patch(AccessTools.Method(typeof(FaceGen), "GetRaceNames"),
                prefix: new HarmonyMethod(typeof(NpcModuleExportProbe), nameof(RaceNames)));
            harmony.Patch(AccessTools.Method(typeof(FaceGen), "GetRaceOrDefault"),
                prefix: new HarmonyMethod(typeof(NpcModuleExportProbe), nameof(RaceId)));
            SetOption(module, scopes, false);
            Initialize();
            module.GetType("Retinues.Domain.Characters.Services.Skills.SkillCatalog", true)
                .GetMethod("ClearSkillCaches").Invoke(null, null);
            var manager = MBObjectManager.Instance;
            var culture = Empty<CultureObject>();
            culture.StringId = "npc_export_culture";
            var source = Empty<CharacterObject>();
            source.StringId = "npc_export_recruit";
            ((BasicCharacterObject)source).Culture = culture;
            source.Age = 33.75f;
            source.Level = 6;
            Set(source, typeof(BasicCharacterObject), "IsFemale", true);
            var name = AccessTools.Field(typeof(BasicCharacterObject), "_basicName");
            name.SetValue(source, Activator.CreateInstance(name.FieldType, new object[] { "Export test", null }));
            var template = Empty<CharacterObject>();
            template.StringId = "npc_export_template";
            Set(culture, typeof(CultureObject), "BasicTroop", template);
            Set(culture, typeof(CultureObject), "VillageWoman", template);
            var min = new BodyProperties(new DynamicBodyProperties(21.29f, .25f, .35f),
                new StaticBodyProperties(1, 2, 3, 4, 5, 6, 7, 8));
            var max = new BodyProperties(new DynamicBodyProperties(40f, .5f, .6f),
                new StaticBodyProperties(8, 7, 6, 5, 4, 3, 2, 1));
            var original = new MBBodyProperty("npc_export_original");
            original.Init(min, max);
            manager.RegisterObject(original);
            // This is the operation used when Retinues edits a shared appearance.
            var copy = typeof(MBBodyProperty).GetMethod("CreateFrom");
            var runtime = copy != null ? (MBBodyProperty)copy.Invoke(null, new object[] { original })
                : manager.CreateObject<MBBodyProperty>("MBBodyProperty_1");
            runtime.Init(min, max);
            Set(source, typeof(BasicCharacterObject), "BodyPropertyRange", runtime);
            var otherBody = new MBBodyProperty("npc_export_culture_template");
            otherBody.Init(new BodyProperties(new DynamicBodyProperties(50f, .1f, .2f), default(StaticBodyProperties)),
                new BodyProperties(new DynamicBodyProperties(60f, .8f, .9f), default(StaticBodyProperties)));
            Set(template, typeof(BasicCharacterObject), "BodyPropertyRange", otherBody);
            Set(template, typeof(BasicCharacterObject), "IsFemale", true);
            var tagOwner = TagOwner(source);
            var tagType = tagOwner is MBBodyProperty ? typeof(MBBodyProperty) : typeof(BasicCharacterObject);
            Set(tagOwner, tagType, "HairTags", "short, ponytail,");
            Set(tagOwner, tagType, "BeardTags", "bald,");
            Set(tagOwner, tagType, "TattooTags", "scar&mark,");
            manager.RegisterObject(culture);
            manager.RegisterObject(source);
            var wrapper = Activator.CreateInstance(
                module.GetType("Retinues.Domain.Characters.Wrappers.WCharacter", true), source);
            var export = module.GetType("Retinues.Exports.NPCCharacters.NPCCharacterExporter", true)
                .GetMethod("ExportAsNPC");
            Func<string, XElement> generate = id => XElement.Parse(
                (string)export.Invoke(null, new object[] { wrapper, id }));

            // A comma-decimal OS must still produce portable XML.
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var existing = generate(source.StringId);
            var newTroop = generate("npc_export_new");
            Require(XNode.DeepEquals(existing.Element("face"), newTroop.Element("face")),
                "Existing and new IDs must preserve the same body range.");
            ((BasicCharacterObject)source).Culture = null;
            var withoutCulture = generate("npc_export_no_culture");
            Set(source, typeof(BasicCharacterObject), "Race", 1);
            var customRace = generate("npc_export_custom_race");
            Require((string)customRace.Attribute("race") == "test_race",
                "Custom race must use its raw XML identifier.");
            Set(source, typeof(BasicCharacterObject), "Race", 0);

            // Body-mod compatibility has a rendering substitute, but exports must retain
            // the original stored appearance, just like regular Retinues exports.
            var cache = (IDictionary<string, string>)bodyHelper.GetField("_compatBodyCache",
                BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            cache[source.StringId] = min + "|<MIN-MAX>|" + max;
            Set(source, typeof(BasicCharacterObject), "BodyPropertyRange", otherBody);
            SetOption(module, scopes, true);
            var compatibility = generate("npc_export_compatibility");
            Require(XNode.DeepEquals(existing.Element("face").Element("BodyProperties"),
                compatibility.Element("face").Element("BodyProperties")),
                "Rendering compatibility replaced the stored export appearance.");
            SetOption(module, scopes, false);
            Set(source, typeof(BasicCharacterObject), "BodyPropertyRange", null);
            try
            {
                generate("npc_export_missing_body");
                throw new Exception("Missing body properties silently produced a broken module.");
            }
            catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException)
            {
                Require(e.InnerException.Message.Contains(source.StringId), "Export failure must identify the troop.");
            }
            Set(source, typeof(BasicCharacterObject), "BodyPropertyRange", runtime);
            Require(runtime.BodyPropertyMin.Equals(min) && runtime.BodyPropertyMax.Equals(max),
                "Export modified the source appearance.");

            Initialize(); // No old campaign or runtime body definitions remain.
            Require(MBObjectManager.Instance.GetObject<MBBodyProperty>(runtime.StringId) == null,
                "The old runtime template leaked into the fresh registry.");
            // Bannerlord loads XML under the invariant culture.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            foreach (var npc in new[] { existing, newTroop, withoutCulture, customRace, compatibility })
            {
                Require(npc.Element("face").Element("face_key_template") == null,
                    "Generated module depends on an external body template.");
                var xml = new XmlDocument();
                xml.LoadXml(npc.ToString(SaveOptions.DisableFormatting));
                var loaded = new BasicCharacterObject { StringId = (string)npc.Attribute("id") };
                loaded.Deserialize(MBObjectManager.Instance, xml.DocumentElement);
                Require(loaded.BodyPropertyRange.BodyPropertyMin.Equals(min) && loaded.BodyPropertyRange.BodyPropertyMax.Equals(max),
                    "Fresh module loading changed the age, weight, build or face keys for " + loaded.StringId);
                Require(loaded.Age == 34f && loaded.IsFemale, "NPC age/gender failed to round-trip.");
                Require(loaded.Race == (npc == customRace ? 1 : 0), "NPC race failed to round-trip.");
                if (npc == compatibility) continue; // Its live rendering substitute had no tag restrictions.
                var loadedTags = TagOwner(loaded);
                Require((string)AccessTools.Property(loadedTags.GetType(), "HairTags").GetValue(loadedTags) == "short,ponytail,",
                    "Hair selection tags were lost.");
                Require((string)AccessTools.Property(loadedTags.GetType(), "BeardTags").GetValue(loadedTags) == "bald,",
                    "Beard selection tags were lost.");
                Require((string)AccessTools.Property(loadedTags.GetType(), "TattooTags").GetValue(loadedTags) == "scar&mark,",
                    "Tattoo selection tags were lost or escaped incorrectly.");
            }
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = cultureBefore;
            bodyHelper.GetMethod("ClearCompatBodyCache").Invoke(null, null);
            for (int i = scopes.Count - 1; i >= 0; i--) scopes[i].Dispose();
            harmony.UnpatchAll(harmony.Id);
        }
    }

    private static void Initialize()
    {
        MBObjectManager.Init();
        var manager = MBObjectManager.Instance;
        manager.RegisterType<CharacterObject>("NPCCharacter", "NPCCharacters", 1);
        manager.RegisterType<MBBodyProperty>("BodyProperty", "BodyProperties", 2);
        manager.RegisterType<CultureObject>("Culture", "SPCultures", 3);
        manager.RegisterType<MBCharacterSkills>("SkillSet", "SkillSets", 4);
        manager.RegisterType<SkillObject>("Skill", "Skills", 5);
    }

    private static object TagOwner(BasicCharacterObject troop) =>
        typeof(MBBodyProperty).GetProperty("HairTags") != null ? (object)troop.BodyPropertyRange : troop;

    private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object instance, Type type, string property, object value) =>
        AccessTools.PropertySetter(type, property).Invoke(instance, new[] { value });
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void SetOption(Assembly module, List<IDisposable> scopes, bool value)
    {
        var option = module.GetType("Retinues.Settings.Configuration", true)
            .GetField("BodyModCompatibility").GetValue(null);
        scopes.Add((IDisposable)module.GetType("Retinues.Tests.TestConfig", true)
            .GetMethod("Set").MakeGenericMethod(typeof(bool)).Invoke(null, new[] { option, (object)value }));
    }
}
