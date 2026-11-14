using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

// Isolated host only: real campaign event registration, roster snapshots, feat rewards
// and Harmony dispatch. Stub campaign singletons and native skill/release side effects.
internal static class RescueMissionProbe
{
    private static Clan PlayerClan;
    private static CampaignEvents Events;
    private static MapEvent PlayerBattle;
    private static BattleSideEnum PlayerSide;
    private static bool UninstallPrepared;

    private static bool GetClan(ref Clan __result) { __result = PlayerClan; return false; }
    private static bool GetEvents(ref CampaignEvents __result) { __result = Events; return false; }
    private static bool IsPlayerBattle(MapEvent __instance, ref bool __result) { __result = __instance == PlayerBattle; return false; }
    private static bool GetSide(ref BattleSideEnum __result) { __result = PlayerSide; return false; }
    private static bool GetPrepared(ref bool __result) { __result = UninstallPrepared; return false; }
    private static bool SkipNative() => false;

    internal static void Run(Assembly module)
    {
        var harmony = new Harmony("retinues.headless.rescue-mission");
        var registry = module.GetType("Retinues.Behaviors.Doctrines.DoctrinesRegistry", true);
        registry.GetMethod("EnsureRegistered").Invoke(null, null);
        var feats = (IDictionary)AccessTools.Field(registry, "_feats").GetValue(null);
        const string featId = "feat_sp_rescue_mission";
        var previousFeat = feats[featId];
        var config = module.GetType("Retinues.Settings.Configuration", true);
        var requirements = (IDisposable)module.GetType("Retinues.Tests.TestConfig").GetMethod("Set")
            .MakeGenericMethod(typeof(bool)).Invoke(null, new[] { config.GetField("EnableFeatRequirements").GetValue(null), true });
        try
        {
            Prefix(harmony, AccessTools.PropertyGetter(typeof(Clan), "PlayerClan"), nameof(GetClan));
            Prefix(harmony, AccessTools.PropertyGetter(typeof(CampaignEvents), "Instance"), nameof(GetEvents));
            Prefix(harmony, AccessTools.PropertyGetter(typeof(MapEvent), "IsPlayerMapEvent"), nameof(IsPlayerBattle));
            Prefix(harmony, AccessTools.PropertyGetter(typeof(MapEvent), "PlayerSide"), nameof(GetSide));
            Prefix(harmony, AccessTools.PropertyGetter(module.GetType("Retinues.Safety.Uninstall.UninstallBehavior"), "IsPrepared"), nameof(GetPrepared));
            var prisonBreak = AccessTools.Method(typeof(SkillLevelingManager), "OnPrisonBreakEnd");
            Prefix(harmony, prisonBreak, nameof(SkipNative));
            harmony.CreateClassProcessor(module.GetType("Retinues.Behaviors.Doctrines.Patches.PrisonBreakFeatPatch", true)).Patch();
            foreach (var method in typeof(EndCaptivityAction).GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (method.Name == "ApplyByRansom" || method.Name == "ApplyByEscape")
                    Prefix(harmony, method, nameof(SkipNative));

            Events = (CampaignEvents)Activator.CreateInstance(typeof(CampaignEvents), true);
            PlayerClan = Raw<Clan>();
            var kingdom = Raw<Kingdom>();
            AccessTools.Field(typeof(Clan), "_kingdom").SetValue(PlayerClan, kingdom);
            var alliedClan = Raw<Clan>();
            AccessTools.Field(typeof(Clan), "_kingdom").SetValue(alliedClan, kingdom);
            var ally = Captive(alliedClan, Occupation.Lord);
            var ownLord = Captive(PlayerClan, Occupation.Lord);
            var enemy = Captive(Raw<Clan>(), Occupation.Lord);
            var companion = Captive(PlayerClan, Occupation.Wanderer);
            var clanless = Captive(null, Occupation.Lord);
            var name = previousFeat.GetType().GetProperty("Name").GetValue(previousFeat);
            var categoryType = module.GetType("Retinues.Behaviors.Doctrines.Definitions.Category", true);
            var category = Activator.CreateInstance(categoryType, new[] { "rescue_probe", name });
            var doctrine = categoryType.GetMethod("Add").Invoke(category,
                new object[] { "rescue_probe", name, name, "", null, null, null, false });
            var feat = doctrine.GetType().GetMethod("Add").Invoke(doctrine,
                new object[] { featId, name, name, 1, 40, false });
            feats[featId] = feat;
            var behaviorType = module.GetType("Retinues.Behaviors.Doctrines.Feats.Loot.Feat_PragmaticScavengers_RescueMission", true);
            var behavior = (CampaignBehaviorBase)Activator.CreateInstance(behaviorType);
            behavior.RegisterEvents();

            void Reset()
            {
                feat.GetType().GetMethod("ForceSet").Invoke(feat, new object[] { 0 });
                doctrine.GetType().GetMethod("ForceSet").Invoke(doctrine, new object[] { 0 });
                doctrine.GetType().GetProperty("IsAcquired").SetValue(doctrine, false);
            }
            void AssertProgress(int expected, string message)
            {
                Require((int)feat.GetType().GetProperty("Progress").GetValue(feat) == expected, message);
                Require((int)doctrine.GetType().GetProperty("Progress").GetValue(doctrine) == expected * 40,
                    "Doctrine reward must match Rescue Mission completion: " + message);
            }
            void Start(MapEvent battle) => Fire(CampaignEvents.MapEventStarted, battle, null, null);
            void End(MapEvent battle) => Fire(CampaignEvents.MapEventEnded, battle);

            // Both battle modes work without a CombatBehavior mission snapshot. Enemy
            // prisoners are already absent when the campaign sends MapEventEnded.
            foreach (bool simulate in new[] { false, true })
            foreach (var side in new[] { BattleSideEnum.Attacker, BattleSideEnum.Defender })
            {
                Reset();
                PlayerSide = side;
                PlayerBattle = Battle(ally, side, simulate, out var captor);
                Start(PlayerBattle);
                Set(captor, "PrisonRoster", TroopRoster.CreateDummyTroopRoster());
                End(PlayerBattle);
                End(PlayerBattle);
                AssertProgress(1, "Manual and auto-resolved victories must each credit once, on either side.");
            }

            foreach (var captive in new[] { enemy, companion, clanless, null })
            {
                Reset();
                PlayerSide = BattleSideEnum.Attacker;
                PlayerBattle = Battle(captive, PlayerSide, true, out _);
                Start(PlayerBattle);
                End(PlayerBattle);
                AssertProgress(0, "A battle without a captive allied lord must not count.");
                prisonBreak.Invoke(null, new object[] { captive, true });
                AssertProgress(0, "A prison break must rescue an allied lord, not a companion or enemy.");
            }

            Reset();
            PlayerBattle = Battle(ally, BattleSideEnum.Attacker, true, out _);
            Start(PlayerBattle);
            AccessTools.Field(typeof(MapEvent), "_battleState").SetValue(PlayerBattle, BattleState.DefenderVictory);
            End(PlayerBattle);
            AssertProgress(0, "A defeat cannot rescue a captive.");
            AccessTools.Field(typeof(MapEvent), "_battleState").SetValue(PlayerBattle, BattleState.AttackerVictory);
            End(PlayerBattle);
            AssertProgress(0, "A consumed battle snapshot cannot be reused.");

            Reset();
            PlayerBattle = Battle(ally, BattleSideEnum.Attacker, true, out _);
            Start(PlayerBattle);
            var unrelated = Battle(ally, BattleSideEnum.Attacker, true, out _);
            End(unrelated);
            AssertProgress(0, "An AI battle cannot consume the player's snapshot.");
            End(PlayerBattle);
            AssertProgress(1, "The correct player battle still credits afterward.");
            Reset();
            PlayerBattle = unrelated;
            End(PlayerBattle);
            AssertProgress(0, "A new battle without a start snapshot cannot reuse old rescue data.");

            // Joining an existing AI battle uses the party-added path, not MapEventStarted.
            Reset();
            PlayerBattle = Battle(ally, BattleSideEnum.Attacker, true, out var joiningParty);
            var mobile = Raw<MobileParty>();
            mobile.StringId = string.Empty; // Avoid retaining the fixture in wrapper caches.
            Set(mobile, "Party", joiningParty);
            Set(joiningParty, "MobileParty", mobile);
            object partyAdded = typeof(CampaignEvents).GetProperty("OnPartyAddedToMapEventEvent")?.GetValue(null)
                ?? AccessTools.Property(module.GetType("Retinues.Framework.Behaviors.LegacyMapEventEvents", true), "PartyAdded").GetValue(null);
            Fire(partyAdded, joiningParty);
            End(PlayerBattle);
            AssertProgress(1, "Joining and auto-resolving an ongoing battle must count.");

            Reset();
            prisonBreak.Invoke(null, new object[] { ally, false });
            AssertProgress(0, "Failed prison breaks do not count.");
            // Neither generic release path is hooked by Retinues.
            foreach (var method in typeof(EndCaptivityAction).GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (method.Name == "ApplyByRansom" || method.Name == "ApplyByEscape")
                {
                    var parameters = method.GetParameters();
                    var args = new object[parameters.Length];
                    for (int i = 0; i < args.Length; i++)
                        args[i] = i == 0 ? ally : parameters[i].ParameterType == typeof(bool) ? (object)true : null;
                    method.Invoke(null, args);
                }
            AssertProgress(0, "Ransoms and random escapes do not count.");
            prisonBreak.Invoke(null, new object[] { ally, true });
            prisonBreak.Invoke(null, new object[] { ally, true });
            AssertProgress(1, "A successful prison break credits the feat and doctrine once.");
            Reset();
            prisonBreak.Invoke(null, new object[] { ownLord, true });
            AssertProgress(1, "The player's own clan lords also qualify.");

            foreach (bool uninstall in new[] { false, true })
            {
                Reset();
                UninstallPrepared = uninstall;
                doctrine.GetType().GetProperty("IsAcquired").SetValue(doctrine, !uninstall);
                PlayerBattle = Battle(ally, BattleSideEnum.Attacker, true, out _);
                Start(PlayerBattle);
                End(PlayerBattle);
                prisonBreak.Invoke(null, new object[] { ally, true });
                AssertProgress(0, "Inactive doctrines and prepared uninstall saves cannot gain rescue progress.");
            }
        }
        finally
        {
            feats[featId] = previousFeat;
            requirements.Dispose();
            harmony.UnpatchAll(harmony.Id);
            UninstallPrepared = false;
            PlayerClan = null;
            PlayerBattle = null;
            Events = null;
        }
    }

    private static MapEvent Battle(Hero captive, BattleSideEnum side, bool simulate, out PartyBase captor)
    {
        var battle = Raw<MapEvent>();
        battle.IsPlayerSimulation = simulate;
        AccessTools.Field(typeof(MapEvent), "_battleState").SetValue(battle,
            side == BattleSideEnum.Attacker ? BattleState.AttackerVictory : BattleState.DefenderVictory);
        AccessTools.Field(typeof(MapEvent), "_state").SetValue(battle, MapEventState.Wait);
        var enemy = Raw<MapEventSide>();
        AccessTools.Field(typeof(MapEventSide), "_mapEvent").SetValue(enemy, battle);
        captor = Raw<PartyBase>();
        var roster = TroopRoster.CreateDummyTroopRoster();
        // Populate the native roster without campaign/native hero-injury callbacks.
        if (captive != null)
        {
            AccessTools.Field(typeof(TroopRoster), "data").SetValue(roster,
                new[] { new TroopRosterElement(captive.CharacterObject) { Number = 1 } });
            AccessTools.Field(typeof(TroopRoster), "_count").SetValue(roster, 1);
        }
        Set(captor, "PrisonRoster", roster);
        AccessTools.Field(typeof(PartyBase), "_mapEventSide").SetValue(captor, enemy);
        var party = Raw<MapEventParty>();
        Set(party, "Party", captor);
        AccessTools.Field(typeof(MapEventSide), "_battleParties").SetValue(enemy, new MBList<MapEventParty> { party });
        var sides = new MapEventSide[2];
        sides[side == BattleSideEnum.Attacker ? 0 : 1] = enemy;
        AccessTools.Field(typeof(MapEvent), "_sides").SetValue(battle, sides);
        return battle;
    }

    private static Hero Captive(Clan clan, Occupation occupation)
    {
        var hero = Raw<Hero>();
        AccessTools.Field(typeof(Hero), "_clan").SetValue(hero, clan);
        Set(hero, "Occupation", occupation);
        var character = Raw<CharacterObject>();
        AccessTools.Field(typeof(CharacterObject), "_heroObject").SetValue(character, hero);
        AccessTools.Field(typeof(Hero), "_characterObject").SetValue(hero, character);
        return hero;
    }

    private static T Raw<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object obj, string property, object value) =>
        AccessTools.PropertySetter(obj.GetType(), property).Invoke(obj, new[] { value });
    private static void Fire(object evt, params object[] args) => AccessTools.Method(evt.GetType(), "Invoke").Invoke(evt, args);
    private static void Prefix(Harmony harmony, MethodBase method, string hook) =>
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(RescueMissionProbe), hook));
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
