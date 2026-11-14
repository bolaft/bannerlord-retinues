using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

// Optional integration test: supply the user's IG DLL, never ship it or launch the game.
internal static class ImprovedGarrisonsProbe
{
    private static Hero Owner;
    private static Clan PlayerClan;
    private static IList BasicRoots, EliteRoots;
    private static Type Wrapper;
    private static IDictionary Factions;
    private static IDictionary Flags;
    private static readonly Dictionary<CharacterObject, int> Hired = new Dictionary<CharacterObject, int>();
    private static CharacterObject Charged;
    private static bool FailTransfer, Prepared;
    private static bool FailQuery;
    private static object BaseFaction;
    private static int RelationLimit = 5;
    private static readonly BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

    internal static void Run(Assembly module, string igPath)
    {
        var patcher = module.GetType("Retinues.Compatibility.Interops.ImprovedGarrisons.ImprovedGarrisonsPatcher", true);
        var install = patcher.GetMethod("TryPatch", StaticPrivate);
        install.Invoke(null, null);
        Require(!(bool)patcher.GetField("_installed", StaticPrivate).GetValue(null), "IG absent must be harmless.");
        var ig = Assembly.LoadFrom(igPath);
        var harmony = new Harmony("Retinues.Tests.ImprovedGarrisonsFixture");
        try
        {
            // Patch native boundaries before IG methods are JIT-compiled, so small
            // engine methods cannot be inlined into the fixture's real IG callers.
            Patch(harmony, AccessTools.Method(typeof(MobileParty), "AddElementToMemberRoster", new[] { typeof(CharacterObject), typeof(int), typeof(bool) }), nameof(AddTroop));
            var activityType = ig.GetType("ImprovedGarrisons.ActivityLogging.ActivityLogManager", true);
            Patch(harmony, AccessTools.Method(activityType, "AddUnitRecruitmentCost", new[] { typeof(Town), typeof(CharacterObject), typeof(int), typeof(Hero) }), nameof(Charge));
            ig.GetType("ImprovedGarrisons.Main").GetProperty("ActivityLogManager").SetValue(null, FormatterServices.GetUninitializedObject(activityType));
            var apply = AccessTools.Method(typeof(RecruitmentCampaignBehavior), "ApplyInternal");
            harmony.CreateClassProcessor(module.GetType("Retinues.Behaviors.Volunteers.Patches.Recruitement_ApplyInternal_Patch", true)).Patch();
            harmony.Patch(apply, prefix: new HarmonyMethod(AccessTools.Method(typeof(ImprovedGarrisonsProbe), nameof(ApplyRecruit))) { priority = Priority.Last });
            install.Invoke(null, null);
            Require((bool)patcher.GetField("_installed", StaticPrivate).GetValue(null), "All downloaded IG signatures must patch successfully.");
            install.Invoke(null, null); // Repeated OnGameStart installation is idempotent.
            Owner = Empty<Hero>(); Owner.StringId = "ig_test_owner";
            PlayerClan = Empty<Clan>(); PlayerClan.StringId = "ig_test_clan";
            AccessTools.Field(typeof(Hero), "_clan").SetValue(Owner, PlayerClan);
            Patch(harmony, AccessTools.PropertyGetter(typeof(Clan), "PlayerClan"), nameof(GetPlayerClan));
            Patch(harmony, AccessTools.PropertyGetter(typeof(Hero), "MainHero"), nameof(GetMainHero));
            Patch(harmony, AccessTools.PropertyGetter(module.GetType("Retinues.Safety.Uninstall.UninstallBehavior"), "IsPrepared"), nameof(IsPrepared));
            var campaign = Empty<Campaign>(); Set(null, typeof(Campaign), "Current", campaign);
            Patch(harmony, AccessTools.PropertyGetter(typeof(CampaignTime), "Now"), nameof(GetTime));
            var models = Empty<GameModels>();
            Set(models, typeof(GameModels), "VolunteerModel", new Volunteers());
            Set(models, typeof(GameModels), "CharacterStatsModel", new DefaultCharacterStatsModel());
            AccessTools.Field(typeof(Campaign), "_gameModels").SetValue(campaign, models);
            var culture = Empty<CultureObject>(); culture.StringId = "ig_test_culture";
            var settlement = Empty<Settlement>(); settlement.StringId = "ig_test_settlement"; settlement.Culture = culture;
            var notable = Empty<Hero>(); notable.StringId = "ig_test_notable";
            AccessTools.Field(typeof(Hero), "_stayingInSettlement").SetValue(notable, settlement);
            AccessTools.Field(typeof(Settlement), "_notablesCache").SetValue(settlement, new MBList<Hero> { notable });
            var town = Empty<Town>();
            settlement.Town = town;

            Wrapper = module.GetType("Retinues.Domain.Characters.Wrappers.WCharacter", true);
            var factionInterface = module.GetType("Retinues.Domain.Factions.IBaseFaction", true);
            var factionCache = module.GetType("Retinues.Domain.Characters.Services.Caches.FactionCache", true);
            factionCache.GetField("_built", StaticPrivate).SetValue(null, true);
            Factions = (IDictionary)factionCache.GetField("ByTroopId", StaticPrivate).GetValue(null);
            var flagCache = module.GetType("Retinues.Domain.Characters.Services.Caches.SourceFlagCache", true);
            flagCache.GetField("_built", StaticPrivate).SetValue(null, true);
            Flags = (IDictionary)flagCache.GetField("ById", StaticPrivate).GetValue(null);
            var flagType = Flags.GetType().GetGenericArguments()[1];
            var clanWrapper = Activator.CreateInstance(module.GetType("Retinues.Domain.Factions.Wrappers.WClan"), PlayerClan);
            var kingdom = Empty<Kingdom>(); kingdom.StringId = "ig_test_kingdom";
            AccessTools.Field(typeof(Clan), "_kingdom").SetValue(PlayerClan, kingdom);
            var kingdomWrapper = Activator.CreateInstance(module.GetType("Retinues.Domain.Factions.Wrappers.WKingdom"), kingdom);
            var foreignClan = Empty<Clan>(); foreignClan.StringId = "ig_test_foreign_clan";
            BaseFaction = Activator.CreateInstance(module.GetType("Retinues.Domain.Factions.Wrappers.WClan"), foreignClan);
            Patch(harmony, AccessTools.Method(module.GetType("Retinues.Domain.Settlements.Wrappers.WSettlement"), "GetBaseTroopsFaction"), nameof(GetBaseFaction));
            object Wrap(CharacterObject c, bool elite, object faction = null)
            {
                Flags[c.StringId] = Enum.Parse(flagType, elite ? "Elite" : "Basic");
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(factionInterface));
                if (faction != null) list.Add(faction);
                Factions[c.StringId] = list;
                return Activator.CreateInstance(Wrapper, c);
            }
            var native = Troop("ig_test_native", culture, 6);
            var unmatched = Troop("ig_test_unmatched_tier", culture, 21);
            var nativeElite = Troop("ig_test_native_elite", culture, 11);
            var clan = Troop("retinues_custom_ig_clan", culture, 6);
            var king = Troop("retinues_custom_ig_kingdom", culture, 6);
            var elite = Troop("retinues_custom_ig_elite", culture, 11);
            Set(culture, typeof(CultureObject), "BasicTroop", native);
            Set(culture, typeof(CultureObject), "EliteBasicTroop", nativeElite);
            Wrap(native, false); Wrap(unmatched, false); Wrap(nativeElite, true);
            var clanW = Wrap(clan, false, clanWrapper);
            var kingW = Wrap(king, false, kingdomWrapper);
            var eliteW = Wrap(elite, true, kingdomWrapper);
            BasicRoots = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Wrapper));
            BasicRoots.Add(clanW); BasicRoots.Add(kingW);
            EliteRoots = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Wrapper)); EliteRoots.Add(eliteW);
            Patch(harmony, AccessTools.Method(module.GetType("Retinues.Domain.Settlements.Wrappers.WSettlement"), "GetPlayerRoots"), nameof(GetRoots));

            var config = module.GetType("Retinues.Settings.Configuration", true);
            module.GetType("Retinues.Settings.ConfigurationManager").GetField("_loadingConfig", StaticPrivate).SetValue(null, true);
            void Option(string name, object value)
            {
                var option = config.GetField(name).GetValue(null);
                var property = option.GetType().GetProperty("Value");
                property.SetValue(option, value is string ? Enum.Parse(property.PropertyType, (string)value) : value);
            }
            Option("AllowedRecruiters", "Everyone"); Option("SameCultureOnly", false);
            var logicType = ig.GetType("ImprovedGarrisons.Recruitment.GarrisonRecruitmentLogic", true);
            var logic = Activator.CreateInstance(logicType);
            var upgrade = ig.GetType("ImprovedGarrisons.Main").GetProperty("UpgradeLogic");
            upgrade.SetValue(null, Activator.CreateInstance(upgrade.PropertyType));
            var byList = logicType.GetMethod("AmountOfAvailableRecruitsByList");
            var byPath = logicType.GetMethod("AmountOfAvailableRecruitsThatCanBeUpgraded");
            var overall = logicType.GetMethod("OverallAmountOfAvailableRecruits");
            var listMethod = logicType.GetMethod("GetAllRecruitableRecruitsFromSettlement");
            var scopeType = patcher.GetNestedType("Scope", BindingFlags.NonPublic);
            IDisposable Scope(Func<CharacterObject, bool> match = null) => (IDisposable)Activator.CreateInstance(scopeType,
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { Owner, match, true, int.MaxValue }, null);
            List<Tuple<Hero, CharacterObject, int>> List(bool onlyElite = false) => (List<Tuple<Hero, CharacterObject, int>>)listMethod.Invoke(logic, new object[] { settlement, 99, Owner, onlyElite });

            notable.VolunteerTypes = new[] { unmatched, native, nativeElite };
            Require((int)byList.Invoke(logic, new object[] { settlement, 1, Owner, new List<CharacterObject> { king } }) == 1,
                "Template matching must find the later kingdom slot instead of choosing the clan tree.");
            Require((int)byPath.Invoke(logic, new object[] { settlement, 1, Owner, new List<CharacterObject> { king } }) == 1, "Upgrade-path scans must also filter before limiting.");
            Require(notable.VolunteerTypes[1] == native, "Search must not mutate world volunteers.");
            RelationLimit = 0;
            Require((int)byList.Invoke(logic, new object[] { settlement, 1, Owner, new List<CharacterObject> { king } }) == 0, "Relationship-locked slots must remain locked.");
            RelationLimit = 5;
            using (Scope()) Require(List(true).Single().Item2 == elite, "Only-elite filtering must recognize custom elite troops.");
            Option("AllowedRecruiters", "PlayerOnly");
            Require((int)byList.Invoke(logic, new object[] { settlement, 1, Owner, new List<CharacterObject> { king } }) == 0, "PlayerOnly must not permit automated custom recruitment.");
            Option("AllowedRecruiters", "FactionOnly");
            Require((int)byList.Invoke(logic, new object[] { settlement, 1, Owner, new List<CharacterObject> { king } }) == 1, "Own kingdom troops must be allowed in FactionOnly mode.");
            Option("SameCultureOnly", true);
            var otherCulture = Empty<CultureObject>(); otherCulture.StringId = "ig_other_culture";
            Set(king, typeof(BasicCharacterObject), "Culture", otherCulture);
            Require((int)byList.Invoke(logic, new object[] { settlement, 1, Owner, new List<CharacterObject> { king } }) == 0, "Culture restrictions must be honored.");
            Set(king, typeof(BasicCharacterObject), "Culture", culture); Option("SameCultureOnly", false);
            Prepared = true;
            Require((int)byList.Invoke(logic, new object[] { settlement, 1, Owner, new List<CharacterObject> { king } }) == 0, "Prepared uninstall saves must use unmodified IG behavior.");
            Prepared = false;
            BasicRoots.Clear();
            Require((int)byList.Invoke(logic, new object[] { settlement, 1, Owner, new List<CharacterObject> { king } }) == 0, "Unavailable roots must not be created by a template.");
            BasicRoots.Add(clanW); BasicRoots.Add(kingW);
            Require(List()[1].Item2 == native, "Unscoped IG calls must retain their original volunteer pool.");
            using (var outer = Scope(t => t == king))
            {
                Require((int)overall.Invoke(logic, new object[] { settlement, 2, Owner, false }) == 2, "Nested query must honor its own limit.");
                Require(ReferenceEquals(patcher.GetField("_scope", StaticPrivate).GetValue(null), outer), "Nested scope must restore the outer context.");
                FailQuery = true;
                try { byList.Invoke(logic, new object[] { settlement, 1, Owner, new List<CharacterObject> { king } }); throw new Exception("Query failure swallowed."); }
                catch (TargetInvocationException e) { Require(e.InnerException is InvalidOperationException, "Query exception must survive."); }
                finally { FailQuery = false; }
                Require(ReferenceEquals(patcher.GetField("_scope", StaticPrivate).GetValue(null), outer), "Exception must restore the outer context.");
            }
            Console.WriteLine("PASS IG template scans, eligible kingdom tree, relation locks, elite filter, recruiter/culture restrictions and uninstall gating");

            var transfer = logicType.GetMethod("RecruitFromSettlement");
            var party = Empty<MobileParty>();
            party.StringId = "ig_test_party";
            AccessTools.Field(typeof(MobileParty), "_actualClan").SetValue(party, PlayerClan);
            using (Scope(t => t == king))
            {
                var chosen = List().Single();
                Require(chosen.Item2 == king && chosen.Item3 == 1, "Template must offer the kingdom troop in its original slot.");
                Require((bool)transfer.Invoke(logic, new object[] { party, settlement, notable, 1, Owner }), "Actual IG regional transfer failed.");
                Require(Hired[king] == 1 && Charged == king && notable.VolunteerTypes[1] == null, "Transfer must hire and charge for the offered troop and consume stock.");
                Require(!(bool)transfer.Invoke(logic, new object[] { party, settlement, notable, 1, Owner }), "Repeated transfer must not duplicate recruits.");
                notable.VolunteerTypes[1] = native;
                List(); FailTransfer = true;
                try { transfer.Invoke(logic, new object[] { party, settlement, notable, 1, Owner }); throw new Exception("Fixture failure was swallowed."); }
                catch (TargetInvocationException e) { Require(e.InnerException is InvalidOperationException, "Original transfer exception must survive."); }
                finally { FailTransfer = false; }
                Require(notable.VolunteerTypes[1] == native, "Exception finalizer must restore the unconsumed original slot.");
            }
            Require(patcher.GetField("_scope", StaticPrivate).GetValue(null) == null, "Recruitment context leaked.");
            Console.WriteLine("PASS actual IG regional transfer, selected troop/cost, one-slot stock consumption, duplicate prevention and exception rollback");

            var behavior = Empty<RecruitmentCampaignBehavior>();
            var mobile = AccessTools.Method(typeof(RecruitmentCampaignBehavior), "GetRecruitVolunteerFromIndividual");
            using (Scope(t => t == king))
            {
                var chosen = List().Single();
                Hired.Clear();
                mobile.Invoke(behavior, new object[] { party, chosen.Item2, notable, chosen.Item3 });
                Require(Hired.ContainsKey(king) && !Hired.ContainsKey(native) && notable.VolunteerTypes[1] == null,
                    "Native recruitment must retain the approved kingdom troop despite a different settlement base clan.");
                mobile.Invoke(behavior, new object[] { party, chosen.Item2, notable, chosen.Item3 });
                Require(Hired[king] == 1, "Mobile recruiter must not consume the same offer twice.");
                notable.VolunteerTypes[1] = native; List(); FailTransfer = true;
                try { mobile.Invoke(behavior, new object[] { party, king, notable, 1 }); throw new Exception("Mobile failure swallowed."); }
                catch (TargetInvocationException e) { Require(e.InnerException is InvalidOperationException, "Mobile exception must survive."); }
                finally { FailTransfer = false; }
                Require(notable.VolunteerTypes[1] == native, "Failed native recruitment must leave the original stock intact.");
            }
            Hired.Clear();
            apply.Invoke(behavior, new object[] { party, settlement, notable, king, 1, 1,
                Enum.ToObject(apply.GetParameters()[6].ParameterType, 0) });
            Require(Hired.ContainsKey(native) && !Hired.ContainsKey(king), "Ordinary AI must still enforce the existing base-faction rule outside IG.");
            Require(patcher.GetField("_scope", StaticPrivate).GetValue(null) == null, "Mobile recruitment context leaked.");
            Console.WriteLine("PASS native mobile-transfer integration, existing AI guard, duplicate prevention, ordinary AI isolation and nested/exception context cleanup");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static bool GetPlayerClan(ref Clan __result) { __result = PlayerClan; return false; }
    private static bool GetTime(ref CampaignTime __result) { __result = CampaignTime.Zero; return false; }
    private static bool GetBaseFaction(ref object __result) { __result = BaseFaction; return false; }
    private static bool GetMainHero(ref Hero __result) { __result = Owner; return false; }
    private static bool GetRoots(bool elite, ref object __result) { __result = elite ? EliteRoots : BasicRoots; return false; }
    private static bool IsPrepared(ref bool __result) { __result = Prepared; return false; }
    private static bool AddTroop(CharacterObject __0, int __1)
    {
        if (FailTransfer) throw new InvalidOperationException("Fixture transfer failure");
        Hired.TryGetValue(__0, out int count); Hired[__0] = count + __1; return false;
    }
    private static bool Charge(CharacterObject __1) { Charged = __1; return false; }
    private static bool ApplyRecruit(Hero individual, CharacterObject troop, int number, int bitCode)
    {
        AddTroop(troop, number);
        individual.VolunteerTypes[bitCode] = null;
        return false;
    }
    private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object instance, Type type, string name, object value) => AccessTools.PropertySetter(type, name).Invoke(instance, new[] { value });
    private static void Patch(Harmony h, MethodInfo method, string name) => h.Patch(method, prefix: new HarmonyMethod(AccessTools.Method(typeof(ImprovedGarrisonsProbe), name)));
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static CharacterObject Troop(string id, CultureObject culture, int level)
    {
        var troop = Empty<CharacterObject>(); troop.StringId = id;
        Set(troop, typeof(BasicCharacterObject), "Culture", culture);
        Set(troop, typeof(BasicCharacterObject), "Level", level);
        Set(troop, typeof(CharacterObject), "UpgradeTargets", Array.Empty<CharacterObject>());
        return troop;
    }
    private sealed class Volunteers : VolunteerModel
    {
        public override int MaxVolunteerTier => 6;
        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyer, Hero seller, int value = -101) =>
            FailQuery ? throw new InvalidOperationException("Fixture query failure") : RelationLimit;
        public override int MaximumIndexGarrisonCanRecruitFromHero(Settlement settlement, Hero seller) => RelationLimit;
        public override bool CanHaveRecruits(Hero hero) => true;
        public override CharacterObject GetBasicVolunteer(Hero hero) => null;
        public override float GetDailyVolunteerProductionProbability(Hero hero, int index, Settlement settlement) => 0;
    }
}
