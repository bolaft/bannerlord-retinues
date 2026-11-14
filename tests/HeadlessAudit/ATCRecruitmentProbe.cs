using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

// Optional test using the downloaded ATC DLL. Native campaign state and the final
// payment/roster boundary are fixtures; ATC selection and Retinues matching are real.
internal static class ATCRecruitmentProbe
{
    private static readonly BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly BindingFlags AllStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Dictionary<string, CharacterObject> Troops = new Dictionary<string, CharacterObject>();
    private static Hero Owner;
    private static Clan OwnerClan;
    private static object Settings, Faction, Basic, Elite;
    private static bool PickElite, Prepared, IsPlayer;
    private static CharacterObject Hired;

    internal static void Run(Assembly module, string atcPath, string branch)
    {
        IsPlayer = Prepared = PickElite = false;
        var fixtureType = module.GetType("Retinues.Tests.Cases.Europe1700RecruitmentTests+Fixture", true);
        var harmony = new Harmony("Retinues.Tests.ATCFixture");
        using (var fixture = (IDisposable)Activator.CreateInstance(fixtureType, true))
        try
        {
            var culture = (CultureObject)fixtureType.GetField("Culture", AllInstance).GetValue(fixture);
            var native = (CharacterObject)fixtureType.GetField("Ordinary", AllInstance).GetValue(fixture);
            CharacterObject Troop(string name, bool custom = false)
            {
                var result = (CharacterObject)fixtureType.GetMethod("Troop", AllInstance).Invoke(fixture, new object[] { name, 11, custom });
                Troops[result.StringId] = result;
                return result;
            }
            var basic = Troop("atc_basic"); var elite = Troop("atc_elite");
            var customBasic = Troop("custom_basic", true); var customElite = Troop("custom_elite", true);
            Owner = Empty<Hero>(); Owner.StringId = "atc_owner";
            AccessTools.Field(typeof(Hero), "_characterObject").SetValue(Owner, Troop("owner"));
            OwnerClan = Empty<Clan>(); OwnerClan.StringId = "atc_clan";
            AccessTools.Field(typeof(Hero), "_clan").SetValue(Owner, OwnerClan);
            Set(OwnerClan, "Culture", culture);
            Patch(harmony, AccessTools.PropertyGetter(typeof(Hero), "MainHero"), nameof(MainHero));
            Patch(harmony, AccessTools.PropertyGetter(typeof(Clan), "PlayerClan"), nameof(PlayerClan));
            Patch(harmony, AccessTools.PropertyGetter(typeof(Clan), "Leader"), nameof(MainHero));
            Patch(harmony, AccessTools.PropertyGetter(typeof(Hero), "MapFaction"), nameof(MapFaction));
            Patch(harmony, AccessTools.PropertyGetter(typeof(Settlement), "OwnerClan"), nameof(PlayerClan));
            Patch(harmony, AccessTools.PropertyGetter(typeof(Settlement), "Owner"), nameof(MainHero));
            Patch(harmony, AccessTools.PropertyGetter(typeof(Settlement), "MapFaction"), nameof(MapFaction));
            Patch(harmony, AccessTools.PropertyGetter(typeof(Settlement), "Name"), nameof(TestName));
            Patch(harmony, AccessTools.PropertyGetter(typeof(PartyBase), "Owner"), nameof(MainHero));
            Patch(harmony, AccessTools.PropertyGetter(typeof(MobileParty), "LeaderHero"), nameof(MainHero));
            Patch(harmony, AccessTools.PropertyGetter(typeof(MobileParty), "MapFaction"), nameof(MapFaction));
            Patch(harmony, AccessTools.PropertyGetter(typeof(MobileParty), "IsMainParty"), nameof(MainParty));
            Patch(harmony, AccessTools.PropertyGetter(module.GetType("Retinues.Safety.Uninstall.UninstallBehavior"), "IsPrepared"), nameof(Uninstall));
            Patch(harmony, AccessTools.Method(typeof(CharacterObject), "Find"), nameof(FindTroop));
            Patch(harmony, AccessTools.PropertyGetter(typeof(CampaignTime), "Now"), nameof(Time));

            var settlement = Empty<Settlement>(); settlement.StringId = "atc_settlement"; settlement.Culture = culture;
            AccessTools.Field(typeof(Settlement), "_name").SetValue(settlement, new TaleWorlds.Localization.TextObject("ATC test town"));
            var notable = Empty<Hero>(); notable.StringId = "atc_notable";
            AccessTools.Field(typeof(Hero), "_characterObject").SetValue(notable, Troop("notable"));
            AccessTools.Field(typeof(Hero), "_name").SetValue(notable, new TaleWorlds.Localization.TextObject("ATC notable"));
            AccessTools.Field(typeof(Hero), "_stayingInSettlement").SetValue(notable, settlement);
            AccessTools.Field(typeof(Settlement), "_notablesCache").SetValue(settlement, new MBList<Hero> { notable });
            var party = Empty<MobileParty>(); party.StringId = "atc_party";
            Set(party, "Party", Empty<PartyBase>());
            AccessTools.Field(typeof(MobileParty), "_actualClan").SetValue(party, OwnerClan);
            var adapter = module.GetType("Retinues.Compatibility.ATCRecruitment", true);
            var getRole = adapter.GetMethod("GetRole", AllStatic);
            string Role(CharacterObject t) => getRole.Invoke(null, new object[] { t, settlement, notable, null }).ToString();
            Require(Role(basic) == "None", "ATC absent retains ordinary rules");

            var atc = Assembly.LoadFrom(atcPath);
            var configType = atc.GetType("AdonnaysTroopChanger.XMLReader.ATCConfig", true);
            var atcSubmodule = atc.GetType("AdonnaysTroopChanger.SubModule", true);
            var settingsType = atc.GetType("AdonnaysTroopChanger.Settings.MCMUISettings", true);
            Settings = FormatterServices.GetUninitializedObject(settingsType);
            Set(Settings, "LevelRecruitsUpToTier", 1);
            var mcm = settingsType.BaseType.Assembly;
            var providerType = mcm.GetType("MCM.Implementation.DefaultSettingsProvider", true);
            AccessTools.PropertySetter(mcm.GetType("MCM.Abstractions.BaseSettingsProvider", true), "Instance")
                .Invoke(null, new[] { FormatterServices.GetUninitializedObject(providerType) });
            Patch(harmony, AccessTools.Method(providerType, "GetSettings"), nameof(GetSettings));
            Patch(harmony, AccessTools.Method(configType, "NotableShouldGiveEliteTroop"), nameof(ChooseElite));
            var config = Activator.CreateInstance(atc.GetType("AdonnaysTroopChanger.XMLReader.ATCCulture"));
            Set(config, "CultureID", culture.StringId);
            object Pool(CharacterObject root)
            {
                var poolType = atc.GetType("AdonnaysTroopChanger.XMLReader.ATCTroops");
                var volunteerType = atc.GetType("AdonnaysTroopChanger.XMLReader.ATCVolunteer");
                var pool = Activator.CreateInstance(poolType);
                var volunteer = Activator.CreateInstance(volunteerType);
                Set(volunteer, "VolunteerID", root.StringId); Set(volunteer, "TroopPercent", 100f);
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(volunteerType)); list.Add(volunteer);
                Set(pool, "Volunteers", list);
                return pool;
            }
            Set(config, "BasicTroops", Pool(basic)); Set(config, "EliteTroops", Pool(elite));
            var factionConfig = Activator.CreateInstance(atc.GetType("AdonnaysTroopChanger.XMLReader.ATCMapFaction"));
            Set(factionConfig, "FactionID", "default");
            var cultures = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(config.GetType())); cultures.Add(config);
            Set(factionConfig, "Cultures", cultures);
            var configs = (IList)configType.GetField("factionList").GetValue(null); configs.Clear(); configs.Add(factionConfig);
            var modulesField = (FieldInfo)fixtureType.GetField("_modules", AllInstance).GetValue(fixture);
            var modules = (IList)Activator.CreateInstance(modulesField.FieldType);
            var moduleEntry = Activator.CreateInstance(modulesField.FieldType.GetGenericArguments()[0]);
            Set(moduleEntry, "Id", "AdonnaysTroopChanger"); modules.Add(moduleEntry); modulesField.SetValue(null, modules);
            Require(Role(basic) == "Basic" && Role(elite) == "Elite", "real ATC runtime configuration exposes both recruitment roles");
            atcSubmodule.GetField("disableATC").SetValue(null, true);
            Require(Role(basic) == "None", "disabled ATC is ignored");
            atcSubmodule.GetField("disableATC").SetValue(null, false);
            Prepared = true; Require(Role(basic) == "None", "prepared uninstall disables the adapter"); Prepared = false;

            object Wrap(CharacterObject troop, string flags)
            {
                if (branch == "stable")
                {
                    var type = module.GetType("Retinues.Game.Wrappers.WCharacter");
                    ((IList)type.GetProperty("ActiveStubIds").GetValue(null)).Add(troop.StringId);
                    return Activator.CreateInstance(type, troop);
                }
                var wrap = fixtureType.GetMethod("Wrap", AllInstance);
                var value = Enum.Parse(wrap.GetParameters()[1].ParameterType, flags);
                return wrap.Invoke(fixture, new object[] { troop, value });
            }
            Basic = Wrap(customBasic, "Basic"); Elite = Wrap(customElite, "Elite");
            Set(culture, "MeleeMilitiaTroop", basic);
            if (branch == "v2")
            {
                Wrap(basic, "Basic, Militia");
                Faction = Activator.CreateInstance(module.GetType("Retinues.Domain.Factions.Wrappers.WClan"), OwnerClan);
                foreach (string name in new[] { "_cultureRootBasics", "_cultureRootElites", "_cultureMilitiaIds", "_cultureCaravanIds", "_cultureVillagerIds" })
                {
                    var field = AccessTools.Field(Faction.GetType(), name);
                    field.SetValue(null, Activator.CreateInstance(field.FieldType));
                }
                foreach (var root in new[] { Tuple.Create("CustomRootBasicAttribute", Basic), Tuple.Create("CustomRootEliteAttribute", Elite) })
                {
                    var attribute = AccessTools.PropertyGetter(Faction.GetType(), root.Item1).Invoke(Faction, null);
                    attribute.GetType().GetMethod("Set").Invoke(attribute, new[] { root.Item2 });
                }
                var cache = module.GetType("Retinues.Domain.Characters.Services.Caches.FactionCache");
                cache.GetField("_built", AllStatic).SetValue(null, true);
                var factions = (IDictionary)cache.GetField("ByTroopId", AllStatic).GetValue(null);
                foreach (var troop in new[] { customBasic, customElite })
                {
                    var list = (IList)Activator.CreateInstance(factions.GetType().GetGenericArguments()[1]); list.Add(Faction);
                    factions[troop.StringId] = list;
                }
                var wsType = module.GetType("Retinues.Domain.Settlements.Wrappers.WSettlement");
                Patch(harmony, AccessTools.Method(wsType, "GetBaseTroopsFaction"), nameof(BaseFaction));
                module.GetType("Retinues.Settings.ConfigurationManager").GetField("_loadingConfig", AllStatic).SetValue(null, true);
                Option(module, "AIClanTroopsAvailability", "TheirFiefs"); Option(module, "AllowedRecruiters", "Everyone");
                Option(module, "SameCultureOnly", false); Option(module, "ClanTroopsAvailability", "Everywhere");
                Option(module, "KingdomTroopsAvailability", "Nowhere"); Option(module, "MixWithVanillaTroops", false);

                var atcModel = Activator.CreateInstance(atc.GetType("AdonnaysTroopChanger.Models.ATCVolunteerProductionModel"));
                var wrapper = Activator.CreateInstance(module.GetType("Retinues.Behaviors.Volunteers.Models.CustomVolunteerModel"), atcModel);
                var volunteer = wrapper.GetType().GetMethod("GetBasicVolunteer");
                // Avoid Hero.IsAlive's unrelated character fixture internals.
                Patch(harmony, AccessTools.PropertyGetter(typeof(Hero), "IsAlive"), nameof(Alive));
                foreach (var eliteChoice in new[] { false, true })
                {
                    PickElite = eliteChoice;
                    Require((CharacterObject)volunteer.Invoke(wrapper, new object[] { notable }) == (PickElite ? customElite : customBasic), "V2 wraps real ATC generation with the correct custom role");
                }
                Option(module, "AIClanTroopsAvailability", "Off"); PickElite = false;
                Require((CharacterObject)volunteer.Invoke(wrapper, new object[] { notable }) == basic, "V2 AI generation off retains ATC recruits");
                Option(module, "AIClanTroopsAvailability", "TheirFiefs");
                var playerSwap = module.GetType("Retinues.Behaviors.Volunteers.PlayerVolunteerSwapState");
                var ws = Activator.CreateInstance(wsType, settlement);
                notable.VolunteerTypes = new[] { basic, elite, null };
                playerSwap.GetMethod("SnapshotVolunteers", AllStatic).Invoke(null, new[] { ws });
                playerSwap.GetMethod("ApplySwapForPlayer", AllStatic).Invoke(null, new[] { ws });
                Require(notable.VolunteerTypes[0] == customBasic && notable.VolunteerTypes[1] == customElite && notable.VolunteerTypes[2] == null,
                    "V2 player menu maps both ATC roles and preserves empty slots");
                notable.VolunteerTypes[0] = null;
                playerSwap.GetMethod("RestoreIfActive", AllStatic).Invoke(null, null);
                Require(notable.VolunteerTypes[0] == null && notable.VolunteerTypes[1] == elite,
                    "V2 restores the ATC offers without restoring a purchased slot");
                Option(module, "ClanTroopsAvailability", "Nowhere");
                notable.VolunteerTypes = new[] { basic, elite };
                playerSwap.GetMethod("ApplySwapForPlayer", AllStatic).Invoke(null, new[] { ws });
                Require(notable.VolunteerTypes[0] == basic && notable.VolunteerTypes[1] == elite,
                    "V2 disabled player trees retain the ATC offers");
                Option(module, "ClanTroopsAvailability", "Everywhere");
                Option(module, "SameCultureOnly", true);
                var foreignCulture = Empty<CultureObject>(); foreignCulture.StringId = "foreign_culture";
                AccessTools.PropertySetter(typeof(BasicCharacterObject), "Culture").Invoke(customBasic, new object[] { foreignCulture });
                playerSwap.GetMethod("ApplySwapForPlayer", AllStatic).Invoke(null, new[] { ws });
                Require(notable.VolunteerTypes[0] == basic, "V2 player culture restriction retains ATC recruit");
                AccessTools.PropertySetter(typeof(BasicCharacterObject), "Culture").Invoke(customBasic, new object[] { culture });
                Option(module, "SameCultureOnly", false);
            }
            else
            {
                Faction = Activator.CreateInstance(module.GetType("Retinues.Tests.Cases.Europe1700RecruitmentTests+TargetFaction", true), true);
                Set(Faction, "RootBasic", Basic); Set(Faction, "RootElite", Elite);
                Set(Basic, "Faction", Faction); Set(Elite, "Faction", Faction);
                var wsType = module.GetType("Retinues.Game.Wrappers.WSettlement");
                Patch(harmony, AccessTools.PropertyGetter(wsType, "PlayerFaction"), nameof(BaseFaction));
                var ws = Activator.CreateInstance(wsType, settlement);
                var wn = Activator.CreateInstance(module.GetType("Retinues.Game.Wrappers.WNotable"), notable, ws);
                notable.VolunteerTypes = new[] { basic, elite, null };
                wn.GetType().GetMethods().Single(m => m.Name == "SwapVolunteers" && m.GetParameters().Length == 1).Invoke(wn, new[] { Faction });
                Require(notable.VolunteerTypes[0] == customBasic && notable.VolunteerTypes[1] == customElite && notable.VolunteerTypes[2] == null,
                    "stable notable pool maps both ATC roles and preserves empty slots");
                var leftover = Troop("leftover_militia", true);
                var militia = Wrap(leftover, "Militia");
                Set(militia, "Faction", Faction); Set(Faction, "MilitiaMelee", militia);
                var originals = (IDictionary)Basic.GetType().GetField("VanillaStringIdMap").GetValue(null);
                originals[leftover.StringId] = basic.StringId;
                var swap = wn.GetType().GetMethods().Single(m => m.Name == "SwapVolunteers" && m.GetParameters().Length == 1);
                notable.VolunteerTypes = new[] { leftover, customBasic, null };
                swap.Invoke(wn, new[] { Faction });
                Require(notable.VolunteerTypes[0] == customBasic && notable.VolunteerTypes[1] == customBasic
                    && notable.VolunteerTypes[2] == null && (CharacterObject)militia.GetType().GetProperty("Base").GetValue(militia) == leftover,
                    "stable repairs proven leftover militia offers without editing the militia troop");
                Set(Faction, "RootBasic", null); notable.VolunteerTypes[0] = leftover;
                swap.Invoke(wn, new[] { Faction });
                Require(notable.VolunteerTypes[0] == basic, "stable unavailable tree restores the recorded ATC recruit");
                Set(Faction, "RootBasic", Basic);
                originals.Remove(leftover.StringId); notable.VolunteerTypes[0] = leftover;
                swap.Invoke(wn, new[] { Faction });
                Require(notable.VolunteerTypes[0] == leftover, "stable does not guess a missing militia origin");
                originals[leftover.StringId] = basic.StringId;
                atcSubmodule.GetField("disableATC").SetValue(null, true);
                swap.Invoke(wn, new[] { Faction });
                Require(notable.VolunteerTypes[0] == leftover, "stable leaves other militia offers alone when ATC is disabled");
                atcSubmodule.GetField("disableATC").SetValue(null, false);
                Option(module, "AllLordsCanRecruitCustomTroops", true);
                Option(module, "RestrictToSameCultureSettlements", false);
            }

            var apply = AccessTools.Method(typeof(RecruitmentCampaignBehavior), "ApplyInternal");
            harmony.CreateClassProcessor(atc.GetType("AdonnaysTroopChanger.PatchRecruitActionFromIndividual", true)).Patch();
            var patchName = branch == "v2" ? "Retinues.Behaviors.Volunteers.Patches.Recruitement_ApplyInternal_Patch"
                : "Retinues.Features.Volunteers.Patches.ATCVolunteerRecruitmentPatch";
            harmony.CreateClassProcessor(module.GetType(patchName, true)).Patch();
            harmony.Patch(apply, prefix: new HarmonyMethod(AccessTools.Method(typeof(ATCRecruitmentProbe), nameof(CaptureTransfer))) { priority = Priority.Last });
            var recruit = AccessTools.Method(typeof(RecruitmentCampaignBehavior), "GetRecruitVolunteerFromIndividual");
            var behavior = Empty<RecruitmentCampaignBehavior>();
            foreach (var eliteChoice in new[] { false, true })
            {
                PickElite = eliteChoice; notable.VolunteerTypes = new[] { native }; Hired = null;
                recruit.Invoke(behavior, new object[] { party, native, notable, 0 });
                if (Hired != (PickElite ? customElite : customBasic))
                    Console.WriteLine("Unexpected transfer: " + Hired?.StringId + "; role="
                        + getRole.Invoke(null, new object[] { Hired, settlement, notable, party }));
                Require(Hired == (PickElite ? customElite : customBasic) && notable.VolunteerTypes[0] == null,
                    "ATC AI prefix followed by Retinues remaps the final transfer and consumes one slot");
            }
            if (branch == "v2") Option(module, "AllowedRecruiters", "PlayerOnly");
            else
            {
                Option(module, "AllLordsCanRecruitCustomTroops", false);
                Option(module, "VassalLordsCanRecruitCustomTroops", false);
                Option(module, "VassalLordsRecruitCustomTroopsAnywhere", false);
            }
            PickElite = false; notable.VolunteerTypes = new[] { native };
            recruit.Invoke(behavior, new object[] { party, native, notable, 0 });
            Require(Hired == basic, "restricted AI retains the ATC offer");
            Console.WriteLine("PASS actual ATC configuration, generation, player pools, AI ordering and restrictions (" + branch + ")");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static void Option(Assembly module, string name, object value)
    {
        var type = module.GetType("Retinues.Settings.Configuration") ?? module.GetType("Retinues.Configuration.Config");
        var option = type.GetField(name).GetValue(null);
        if (type.FullName == "Retinues.Configuration.Config")
        {
            AccessTools.PropertySetter(option.GetType(), "Getter").Invoke(option, new object[] { new Func<bool>(() => (bool)value) });
            return;
        }
        var prop = option.GetType().GetProperty("Value");
        prop.SetValue(option, value is string ? Enum.Parse(prop.PropertyType, (string)value) : value);
    }
    private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object obj, string name, object value) => AccessTools.PropertySetter(obj.GetType(), name).Invoke(obj, new[] { value });
    private static void Patch(Harmony harmony, MethodBase method, string prefix)
    {
        if (method == null) throw new MissingMethodException(prefix);
        method = method.DeclaringType.GetMethod(method.Name, AllInstance | AllStatic,
            null, method.GetParameters().Select(p => p.ParameterType).ToArray(), null);
        harmony.Patch(method, prefix: new HarmonyMethod(AccessTools.Method(typeof(ATCRecruitmentProbe), prefix)));
    }
    private static void Require(bool value, string text) { if (!value) throw new InvalidOperationException(text); Console.WriteLine("PASS " + text); }
    private static bool MainHero(ref Hero __result) { __result = Owner; return false; }
    private static bool PlayerClan(ref Clan __result) { __result = OwnerClan; return false; }
    private static bool MapFaction(ref IFaction __result) { __result = OwnerClan; return false; }
    private static bool MainParty(ref bool __result) { __result = IsPlayer; return false; }
    private static bool Uninstall(ref bool __result) { __result = Prepared; return false; }
    private static bool Alive(ref bool __result) { __result = true; return false; }
    private static bool GetSettings(ref object __result) { __result = Settings; return false; }
    private static bool BaseFaction(ref object __result) { __result = Faction; return false; }
    private static bool ChooseElite(ref bool __result) { __result = PickElite; return false; }
    private static bool FindTroop(string __0, ref CharacterObject __result) { Troops.TryGetValue(__0, out __result); return false; }
    private static bool Time(ref CampaignTime __result) { __result = CampaignTime.Zero; return false; }
    private static bool TestName(ref TaleWorlds.Localization.TextObject __result)
    { __result = new TaleWorlds.Localization.TextObject("ATC test town"); return false; }
    private static bool CaptureTransfer(Hero individual, CharacterObject troop, int bitCode)
    { Hired = troop; individual.VolunteerTypes[bitCode] = null; return false; }
}
