using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;

internal static class Program
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    // Each game version runs in a separate process, against its real assemblies and mod binary.
    private static int Main(string[] args)
    {
        if (args.Length < 3 || (args[2] != "stable" && args[2] != "v2"))
        {
            Console.Error.WriteLine("Usage: HeadlessAudit.exe <Retinues.dll> <game-dll-directory> <stable|v2> [Harmony-directory] [--seed=N] [--repeat=N] [--junit=path] [--inventory=path] [--release-check] [--patch-check] [--save-contract=path --baseline=path]");
            return 2;
        }

        string modulePath = Path.GetFullPath(args[0]);
        string references = Path.GetFullPath(args[1]);
        bool hasHarmonyArgument = args.Length > 3 && !args[3].StartsWith("--");
        string harmonyRoot = hasHarmonyArgument ? args[3] : references;
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            string name = new AssemblyName(e.Name).Name + ".dll";
            foreach (string root in new[] { harmonyRoot, references, Path.GetDirectoryName(modulePath) })
            {
                string path = Path.Combine(root, name);
                if (File.Exists(path))
                    return Assembly.LoadFrom(path);
            }
            return null;
        };

        string report = null;
        try
        {
            int seed = 12345, repeat = 1;
            string inventory = null, contract = null, baseline = null;
            bool release = false, patchCheck = false;
            foreach (string option in args.Skip(hasHarmonyArgument ? 4 : 3))
            {
                if (option.StartsWith("--seed=")) seed = int.Parse(option.Substring(7));
                else if (option.StartsWith("--repeat=")) repeat = int.Parse(option.Substring(9));
                else if (option.StartsWith("--junit=")) report = option.Substring(8);
                else if (option.StartsWith("--inventory=")) inventory = option.Substring(12);
                else if (option == "--release-check") release = true;
                else if (option == "--patch-check") patchCheck = true;
                else if (option.StartsWith("--save-contract=")) contract = option.Substring(16);
                else if (option.StartsWith("--baseline=")) baseline = option.Substring(11);
                else throw new ArgumentException("Unknown option: " + option);
            }
            var module = Assembly.LoadFrom(modulePath);
            if (patchCheck)
                return CheckAllHarmonyBindings(module, report) ? 0 : 1;
            if (contract != null)
            {
                SaveCompatibilityProbe.Run(module, args[2], contract, baseline);
                return 0;
            }
            Require(baseline == null, "--baseline requires --save-contract.");
            var framework = module.GetType("Retinues.Tests.Tests");
            if (release)
            {
                Require(framework == null && !module.GetManifestResourceNames().Any(n => n.Contains("Tests.Fixtures")),
                    "Release builds must exclude the test runner and fixtures.");
                Console.WriteLine("PASS ReleaseExcludesTestsAndFixtures");
                return 0;
            }
            Require(framework != null, "Headless suites require a Debug build.");
            if (inventory != null)
            {
                var entries = ((IEnumerable)framework.GetMethod("Discover").Invoke(null, null)).Cast<object>();
                WriteXml(inventory, new XDocument(new XElement("tests", entries.Select(t => new XElement("test",
                    new XAttribute("group", Read(t, "Group")), new XAttribute("name", Read(t, "Name")),
                    new XAttribute("requiresCampaign", Read(t, "RequiresCampaign")),
                    new XAttribute("description", Read(t, "Description") ?? ""))))));
            }
            var run = framework.GetMethod("RunSuite").Invoke(null, new object[] { null, null, true, seed, repeat, false });
            Console.WriteLine(framework.GetMethod("FormatSummary").Invoke(null, new[] { run }));
            var document = XDocument.Parse((string)run.GetType().GetMethod("ToJUnit").Invoke(run, null));
            document.Root.Element("testsuite").Element("properties").Add(
                new XElement("property", new XAttribute("name", "branch"), new XAttribute("value", args[2])),
                new XElement("property", new XAttribute("name", "modSha256"), new XAttribute("value", Hash(modulePath))),
                new XElement("property", new XAttribute("name", "campaignAssemblySha256"), new XAttribute("value", Hash(Path.Combine(references, "TaleWorlds.CampaignSystem.dll")))));
            bool success = (bool)Read(run, "Success");
            var supplemental = new XElement("testsuite", new XAttribute("name", "Engine integration"));
            if (args[2] == "v2")
            {
                success &= Supplement(supplemental, "SharedSaveRoundTripAndContinuation", () => CheckSharedSave(module));
                success &= Supplement(supplemental, "PartyBindingsAndHarmonyInstallation", () => CheckPartyPatchBindings(module));
                success &= Supplement(supplemental, "XpDiagnosticsPreserveEngineFailure", () => CheckXpDiagnostics(module));
            }
            else
                success &= Supplement(supplemental, "MilitiaHarmonyPatchInstalls", () =>
                    InstallPatch(module.GetType("Retinues.Features.Swaps.Patches.PlayerMilitiaSpawnPatch", true)));
            supplemental.SetAttributeValue("tests", supplemental.Elements("testcase").Count());
            supplemental.SetAttributeValue("failures", supplemental.Descendants("failure").Count());
            supplemental.SetAttributeValue("errors", 0);
            supplemental.SetAttributeValue("skipped", 0);
            document.Root.Add(supplemental);
            if (report != null) WriteXml(report, document);
            return success ? 0 : 1;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            for (var cause = e; cause != null; cause = cause.InnerException)
                if (cause is ReflectionTypeLoadException loader)
                    foreach (var error in loader.LoaderExceptions.Select(x => x.ToString()).Distinct())
                        Console.Error.WriteLine("Loader: " + error);
            if (report != null)
                WriteXml(report, new XDocument(new XElement("testsuites",
                    new XElement("testsuite", new XAttribute("name", "Runner"), new XAttribute("tests", 1), new XAttribute("failures", 1),
                        new XElement("testcase", new XAttribute("name", "Initialization"),
                            new XElement("failure", new XAttribute("message", e.Message), e.ToString()))))));
            return 1;
        }
    }

    private static object Read(object instance, string name) => instance.GetType().GetProperty(name).GetValue(instance);

    private static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var hash = System.Security.Cryptography.SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    private static void WriteXml(string path, XDocument document)
    {
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        document.Save(path);
    }

    private static bool Supplement(XElement suite, string name, Action action)
    {
        var entry = new XElement("testcase", new XAttribute("classname", "engine"), new XAttribute("name", name));
        suite.Add(entry);
        var watch = Stopwatch.StartNew();
        try { action(); Console.WriteLine("PASS " + name); return true; }
        catch (Exception e)
        {
            Console.Error.WriteLine("FAIL " + name + ": " + e);
            entry.Add(new XElement("failure", new XAttribute("message", e.Message), e.ToString()));
            return false;
        }
        finally { entry.SetAttributeValue("time", watch.Elapsed.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
    }

    private static void CheckSharedSave(Assembly module)
    {
        var type = module.GetType("Retinues.Behaviors.Experience.SharedSkillPoolBehavior", true);
        var sync = type.GetMethod("SyncData");
        var legacy = new Dictionary<string, object> {
            ["Retinues_SharedSkillPoints"] = 7,
            ["Retinues_SharedSkillPointsExperience"] = 33000
        };
        var oldSave = Activator.CreateInstance(type);
        sync.Invoke(oldSave, new object[] { new MemoryStore(legacy, false) });
        Require((int)type.GetField("_sharedSkillPointsExperience", PrivateInstance).GetValue(oldSave) == 33000,
            "Missing normalized-progress key must preserve legacy XP for migration.");

        var data = new Dictionary<string, object>();
        type.GetField("_sharedSkillPointsExperience", PrivateInstance).SetValue(oldSave, 0);
        type.GetField("_sharedSkillPointProgress", PrivateInstance).SetValue(oldSave, 0.875d);
        sync.Invoke(oldSave, new object[] { new MemoryStore(data, true) });
        var loaded = Activator.CreateInstance(type);
        sync.Invoke(loaded, new object[] { new MemoryStore(data, false) });
        Require((double)type.GetField("_sharedSkillPointProgress", PrivateInstance).GetValue(loaded) == 0.875d,
            "Fractional skill-point progress must survive save/load.");
        int earned = (int)type.GetMethod("AddExperience", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { 3000, 6000 });
        Require(earned == 1 && (int)type.GetProperty("SharedSkillPoints").GetValue(null) == 8,
            "Loaded progress must continue earning without losing existing points.");
        Require((double)type.GetField("_sharedSkillPointProgress", PrivateInstance).GetValue(loaded) == 0.375d,
            "Only the completed point is deducted.");
    }

    private static void CheckPartyPatchBindings(Assembly module)
    {
        var campaign = typeof(IDataStore).Assembly;
        var logic = campaign.GetType("TaleWorlds.CampaignSystem.Party.PartyScreenLogic", true);
        var command = logic.GetNestedType("PartyCommand");
        Require(logic.GetMethod("UpgradeTroop", PrivateInstance, null, new[] { command }, null) != null,
            "The command prefix must resolve to the real execution method.");
        var viewModels = Assembly.Load("TaleWorlds.CampaignSystem.ViewModelCollection");
        var vm = viewModels.GetType("TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyCharacterVM", true);
        Require(vm.GetField("_partyScreenLogic", PrivateInstance)?.FieldType == logic,
            "The UI patches must receive the current party-screen logic.");

        foreach (var suffix in new[] {
            "PartyScreenLogic_UpgradeTroop_RetinueCap_Patch",
            "PartyCharacterVM_InitializeUpgrades_RetinueCap_Patch",
            "PartyCharacterVM_RefreshValues_RetinueCap_Patch"
        })
        {
            var patch = module.GetType("Retinues.Behaviors.Retinues.Patches.RetinueDynamicUpgradePatch+" + suffix, true);
            InstallPatch(patch);
        }
    }

    private static void InstallPatch(Type patch)
    {
        var harmonyType = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony", true);
        var harmony = Activator.CreateInstance(harmonyType, "retinues.headless.audit");
        var processor = harmonyType.GetMethod("CreateClassProcessor").Invoke(harmony, new object[] { patch });
        var patched = ((IEnumerable)processor.GetType().GetMethod("Patch").Invoke(processor, null)).Cast<object>().ToList();
        Require(patched.Count > 0, "Harmony must actually install " + patch.Name + ", not silently skip it.");
    }

    private static bool CheckAllHarmonyBindings(Assembly module, string report)
    {
        // TypeByName patches rely on module assemblies that the game's loader preloads.
        // A headless process otherwise reports a false missing target for the party screen.
        Assembly.Load("SandBox.GauntletUI");
        var harmonyType = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony", true);
        bool Annotated(MemberInfo member) => member.GetCustomAttributesData().Any(a =>
            a.AttributeType.FullName == "HarmonyLib.HarmonyPatch");
        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var suite = new XElement("testsuite", new XAttribute("name", "Harmony installation against selected game assemblies"));
        foreach (var type in module.GetTypes().Where(t => Annotated(t) || t.GetMethods(flags).Any(Annotated)).OrderBy(t => t.FullName))
        {
            Supplement(suite, type.FullName, () =>
            {
                string owner = "retinues.binding.audit." + type.FullName;
                var harmony = Activator.CreateInstance(harmonyType, owner);
                try
                {
                    var processor = harmonyType.GetMethod("CreateClassProcessor").Invoke(harmony, new object[] { type });
                    var patched = ((IEnumerable)processor.GetType().GetMethod("Patch").Invoke(processor, null)).Cast<object>().ToList();
                    var entry = suite.Elements("testcase").Last();
                    entry.SetAttributeValue("installed", patched.Count);
                    // Record actual originals as well as installing patches: private field and
                    // parameter injections/transpilers are validated by Harmony, not a name scan.
                    var originals = ((IEnumerable)harmonyType.GetMethod("GetAllPatchedMethods").Invoke(null, null)).Cast<MethodBase>();
                    foreach (var original in originals)
                    {
                        var info = harmonyType.GetMethod("GetPatchInfo").Invoke(null, new object[] { original });
                        var owners = ((IEnumerable)info.GetType().GetProperty("Owners").GetValue(info)).Cast<string>();
                        if (owners.Contains(owner)) entry.Add(new XElement("target", original.DeclaringType.FullName + "::" + original));
                    }
                }
                finally
                {
                    // Keep each class isolated so one broken patch cannot poison later probes.
                    harmonyType.GetMethod("UnpatchAll", new[] { typeof(string) }).Invoke(harmony, new object[] { owner });
                }
            });
        }
        int count = suite.Elements("testcase").Count(), failures = suite.Descendants("failure").Count();
        Require(count > 0, "No annotated Harmony patch containers were found.");
        suite.SetAttributeValue("tests", count);
        suite.SetAttributeValue("failures", failures);
        suite.SetAttributeValue("skipped", 0);
        if (report != null) WriteXml(report, new XDocument(new XElement("testsuites", suite)));
        Console.WriteLine($"Harmony containers={count}; failures={failures}; installed targets={suite.Descendants("target").Count()}");
        return failures == 0;
    }

    private static void CheckXpDiagnostics(Assembly module)
    {
        var patch = module.GetType("Retinues.Behaviors.Experience.Patches.XpFaultDiagnosticsPatch", true);
        InstallPatch(patch);
        patch.GetMethod("Reset").Invoke(null, null);
        var rosterType = typeof(IDataStore).Assembly.GetType("TaleWorlds.CampaignSystem.Roster.TroopRoster", true);
        var roster = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(rosterType);
        object owner;
        MethodInfo target;
        object[] arguments;
        if ((target = rosterType.GetMethod("ClampXp", PrivateInstance)) != null)
        {
            owner = roster;
            arguments = new object[] { 0 };
        }
        else
        {
            var partyType = typeof(IDataStore).Assembly.GetType("TaleWorlds.CampaignSystem.Party.PartyBase", true);
            owner = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(partyType);
            target = partyType.GetMethod("OnXpChanged", PrivateInstance);
            arguments = new object[] { roster, Activator.CreateInstance(target.GetParameters()[1].ParameterType.GetElementType()) };
        }
        bool failed = false;
        try { target.Invoke(owner, arguments); }
        catch (TargetInvocationException e) when (e.InnerException is NullReferenceException) { failed = true; }
        Require(failed, "Diagnostics must preserve the real engine's exception for an invalid roster element.");
        var reported = patch.GetField("Reported", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        Require((int)reported.GetType().GetProperty("Count").GetValue(reported) == 1,
            "The installed finalizer must capture the failed element, not silently skip its diagnostics.");
        patch.GetMethod("Reset").Invoke(null, null);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class MemoryStore : IDataStore
    {
        private readonly Dictionary<string, object> _data;
        public bool IsSaving { get; }
        public bool IsLoading => !IsSaving;
        public MemoryStore(Dictionary<string, object> data, bool saving)
        {
            _data = data;
            IsSaving = saving;
        }
        public bool SyncData<T>(string key, ref T data)
        {
            if (IsSaving)
                _data[key] = data;
            else if (_data.TryGetValue(key, out var saved))
                data = (T)saved;
            else
                return false;
            return true;
        }
    }
}
