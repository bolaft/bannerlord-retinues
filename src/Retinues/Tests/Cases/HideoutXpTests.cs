using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Helpers;
using Retinues.Behaviors.Experience;
using Retinues.Behaviors.Experience.Patches;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Tests.Cases
{
    public static class HideoutXpTests
    {
        private static int _captured;
        private static bool Capture(int gainedXp) { _captured += gainedXp; return false; }
        private static void DoubleGrant(ref int xpAmount) => xpAmount *= 2;

        [GameTest("SkillXpCountsNewGrantsWithoutRecountingStoredXp", "experience", RequiresCampaign = false)]
        public static void NewGrantsOnly()
        {
            using var f = new Fixture();
            f.SetBattleType(MapEvent.BattleTypes.FieldBattle);
            f.AddXp(200);
            Tests.AssertEqual(200, _captured);
            _captured = 0;
            f.AddXp(25);
            Tests.AssertEqual(25, _captured, "The existing 200 XP must not be earned a second time.");
            Tests.AssertEqual(225, f.Roster.GetElementXp(0));
            _captured = 0;
            f.AddXp(0);
            f.AddXp(-10);
            Tests.AssertEqual(0, _captured, "Zero awards and XP deductions do not mint skill points.");

            int cap = SkillPointExperiencePatches.ComputeMaxXp(100,
                SkillPointExperiencePatches.GetXpRequired(f.Troop, f.Party));
            f.Roster.SetElementXp(0, cap);
            f.AddXp(25);
            Tests.AssertEqual(25, _captured, "A troop at its roster XP cap still earns the newly awarded XP.");
            Tests.AssertEqual(cap, f.Roster.GetElementXp(0), "The roster cap itself is unchanged.");
            f.EnableMultiplier();
            _captured = 0;
            f.AddXp(25);
            Tests.AssertEqual(50, _captured, "An ordinary XP-multiplier prefix is included before the snapshot.");
        }

        [GameTest("HideoutHeroKillsExcludeReservesFromSkillXp", "experience", RequiresCampaign = false)]
        public static void HeroKills()
        {
            using var f = new Fixture();
            f.Kill();
            Tests.AssertEqual(500, f.Roster.GetElementXp(0), "The real perk still grants normal upgrade XP to all 100 infantry.");
            Tests.AssertEqual(0, _captured, "No retinues selected for the hideout means no skill-point XP from the hero's kill.");

            f.Allocate(3);
            f.Kill();
            Tests.AssertEqual(15, _captured, "Only the three selected retinues contribute five XP each, not the 97 reserves.");
            Tests.AssertEqual(1000, f.Roster.GetElementXp(0), "Filtering skill XP must not alter the native roster award.");

            // A regular soldier's damage/kill XP is already individual: never scale it again.
            _captured = 0;
            f.AddXp(200);
            Tests.AssertEqual(200, _captured, "Direct combat XP remains intact after the perk scope ends.");

            f.SetBattleType(MapEvent.BattleTypes.FieldBattle);
            f.Kill();
            Tests.AssertEqual(500, _captured, "Ordinary field battles preserve existing perk skill XP.");
            f.SetBattleType(MapEvent.BattleTypes.Hideout);
            f.Event.IsPlayerSimulation = true;
            f.Kill();
            Tests.AssertEqual(500, _captured, "This change does not rewrite auto-resolve rewards.");
        }

        [GameTest("HideoutSharedXpPreservesParticipantsAndNativeAwards", "experience", RequiresCampaign = false)]
        public static void SharedAwards()
        {
            using var f = new Fixture();
            Tests.AssertTrue(MobilePartyHelper.CanTroopGainXp(f.Party, f.Troop, out var headroom),
                "The fixture's terminal retinue can earn normal roster XP.");
            Tests.AssertTrue(headroom > 0, "The fixture has positive XP headroom.");
            MobilePartyHelper.PartyAddSharedXp(f.Mobile, 500);
            Tests.AssertEqual(0, _captured, "Shared battle XP cannot credit troops outside the hideout either.");
            Tests.AssertEqual(500, f.Roster.GetElementXp(0));

            f.Allocate(4);
            MobilePartyHelper.PartyAddSharedXp(f.Mobile, 500);
            Tests.AssertEqual(20, _captured);
            Tests.AssertEqual(1000, f.Roster.GetElementXp(0));

            f.KillAllocated(1);
            _captured = 0;
            f.Kill();
            Tests.AssertEqual(15, _captured, "Killed soldiers no longer inflate the participating count.");

            // A second party can field the same troop definition. Its allocation is not ours.
            f.AllocateOtherParty(2);
            f.Kill();
            Tests.AssertEqual(15, _captured, "Matching troop identity in another party is not participation in this party.");
        }

        [GameTest("HideoutXpScopeRestoresAfterNestedAwardsAndErrors", "experience", RequiresCampaign = false)]
        public static void ScopeRestoration()
        {
            using var f = new Fixture();
            var prefix = AccessTools.Method(typeof(HideoutSharedXpPatch), "Prefix");
            var finalizer = AccessTools.Method(typeof(HideoutSharedXpPatch), "Finalizer");
            object[] outer = { f.Party, null };
            prefix.Invoke(null, outer);
            try
            {
                // Real nested game callback; no participants remain ineligible afterwards.
                f.Kill();
                Tests.AssertEqual(0, HideoutSharedXpPatch.GetSkillPointXp(f.Party, f.Troop, 500, 100));
            }
            finally { finalizer.Invoke(null, new[] { outer[1] }); }

            bool threw = false;
            try { MobilePartyHelper.PartyAddSharedXp(null, 500); }
            catch (NullReferenceException) { threw = true; }
            Tests.AssertTrue(threw, "The scope must not swallow the original engine error.");
            Tests.AssertEqual(500, HideoutSharedXpPatch.GetSkillPointXp(f.Party, f.Troop, 500, 100),
                "A failing award must not leak its scope into subsequent direct combat XP.");
        }

        private sealed class Fixture : IDisposable
        {
            private readonly Europe1700RecruitmentTests.Fixture _engine = new();
            private readonly Harmony _harmony = new("retinues.tests.hideout-xp." + Guid.NewGuid().ToString("N"));
            internal readonly CharacterObject Troop;
            internal readonly PartyBase Party = Empty<PartyBase>();
            internal readonly MobileParty Mobile = Empty<MobileParty>();
            internal readonly MapEvent Event = Empty<MapEvent>();
            internal readonly TroopRoster Roster;
            private readonly MapEventParty _battleParty = Empty<MapEventParty>();
            private readonly MapEventSide _side = Empty<MapEventSide>();
            private readonly FlattenedTroopRoster _battleRoster = new();
            private readonly Dictionary<UniqueTroopDescriptor, MapEventParty> _allocated = new();
            private readonly CharacterObject _hero;
            private readonly WeaponComponentData _weapon = Empty<WeaponComponentData>();
            private readonly MethodInfo _heroHit = AccessTools.Method(typeof(BattleCampaignBehavior), "OnHeroCombatHit");
            private readonly Dictionary<string, List<IBaseFaction>> _factions =
                (Dictionary<string, List<IBaseFaction>>)AccessTools.Field(typeof(FactionCache), "ByTroopId").GetValue(null);
            private readonly object _factionsBuilt = AccessTools.Field(typeof(FactionCache), "_built").GetValue(null);
            private int _seed;

            internal Fixture()
            {
                try
                {
                    _captured = 0;
                    Troop = _engine.Troop("hideout_retinue", custom: true);
                    _engine.Wrap(Troop, TroopSourceFlags.Retinue);
                    var clan = Empty<Clan>();
                    clan.StringId = Troop.StringId + "_clan";
                    _factions.Add(Troop.StringId, new List<IBaseFaction> { new WClan(clan) });
                    AccessTools.Field(typeof(FactionCache), "_built").SetValue(null, true);
                    Set(Mobile, "Party", Party);
                    Set(Party, "MobileParty", Mobile);
                    Set(Campaign.Current, "MainParty", Mobile);
                    Roster = new TroopRoster(Party);
                    AccessTools.Field(typeof(TroopRoster), "data").SetValue(Roster,
                        new[] { new TroopRosterElement(Troop) { Number = 100 } });
                    AccessTools.Field(typeof(TroopRoster), "_count").SetValue(Roster, 1);
                    AccessTools.Method(typeof(TroopRoster), "InitializeCachedData").Invoke(Roster, null);
                    Set(Party, "MemberRoster", Roster);
                    SetBattleType(MapEvent.BattleTypes.Hideout);
                    AccessTools.Field(typeof(PartyBase), "_mapEventSide").SetValue(Party, _side);
                    AccessTools.Field(typeof(MapEventSide), "_mapEvent").SetValue(_side, Event);
                    AccessTools.Field(typeof(MapEventSide), "_battleParties").SetValue(_side, new MBList<MapEventParty> { _battleParty });
                    AccessTools.Field(typeof(MapEventSide), "_allocatedTroops").SetValue(_side, _allocated);
                    Set(_battleParty, "Party", Party);
                    AccessTools.Field(typeof(MapEventParty), "_roster").SetValue(_battleParty, _battleRoster);
                    // Include unselected soldiers, as older game versions do. Allocation is decisive.
                    for (int i = 0; i < 100; i++)
                    {
                        var descriptor = new UniqueTroopDescriptor(++_seed);
                        _battleRoster[descriptor] = new FlattenedTroopRosterElement(Troop, uniqueNo: descriptor);
                    }

                    var perk = Empty<PerkObject>();
                    Set(perk, "PrimaryBonus", 5f);
                    var perks = Empty<DefaultPerks>();
                    AccessTools.Field(typeof(DefaultPerks), "_twoHandedBaptisedInBlood").SetValue(perks, perk);
                    Set(Campaign.Current, "DefaultPerks", perks);
                    var hero = Empty<Hero>();
                    AccessTools.Field(typeof(Hero), "_clan").SetValue(hero, clan);
                    var perkField = AccessTools.Field(typeof(Hero), "_heroPerks");
                    var heroPerks = (PropertyOwner<PerkObject>)Activator.CreateInstance(perkField.FieldType);
                    heroPerks.SetPropertyValue(perk, 1);
                    perkField.SetValue(hero, heroPerks);
                    _hero = _engine.Troop("hideout_player");
                    AccessTools.Field(typeof(CharacterObject), "_heroObject").SetValue(_hero, hero);
                    Game.Current.PlayerTroop = _hero;
                    Set(_weapon, "WeaponClass", WeaponClass.TwoHandedSword);

                    _harmony.CreateClassProcessor(typeof(SkillPointExperiencePatches)).Patch();
                    _harmony.CreateClassProcessor(typeof(HideoutSharedXpPatch)).Patch();
                    // Observe the XP reaching the existing skill-point converter, without UI/native calls.
                    _harmony.Patch(AccessTools.Method(typeof(SkillPointExperiencePatches), "ApplyXpToSkillPointProgress"),
                        prefix: new HarmonyMethod(typeof(HideoutXpTests), nameof(Capture)));
                    BattleXpWindow.Open(Event);
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            internal void Kill()
            {
                _captured = 0;
                _heroHit.Invoke(_heroHit.IsStatic ? null : Empty<BattleCampaignBehavior>(),
                    new object[] { _hero, Troop, Party, _weapon, true, 10 });
            }

            internal void AddXp(int xp) => Roster.AddXpToTroopAtIndex(index: 0, xpAmount: xp);
            internal void EnableMultiplier() => _harmony.Patch(
                AccessTools.Method(typeof(TroopRoster), nameof(TroopRoster.AddXpToTroopAtIndex)),
                prefix: new HarmonyMethod(typeof(HideoutXpTests), nameof(DoubleGrant)) { priority = Priority.Normal });
            internal void SetBattleType(MapEvent.BattleTypes type)
            {
                var field = AccessTools.Field(typeof(MapEvent), "_mapEventType");
                if (field != null) field.SetValue(Event, type);
                else
                {
                    // BL 1.5.6 derives the event type from its component.
                    string name = type == MapEvent.BattleTypes.Hideout
                        ? "HideoutEventComponent" : "FieldBattleEventComponent";
                    var component = typeof(MapEvent).Assembly.GetType("TaleWorlds.CampaignSystem.MapEvents." + name, true);
                    Set(Event, "Component", FormatterServices.GetUninitializedObject(component));
                }
            }

            internal void Allocate(int count)
            {
                _allocated.Clear();
                for (int i = 1; i <= count; i++) _allocated[new UniqueTroopDescriptor(i)] = _battleParty;
            }

            internal void KillAllocated(int seed)
            {
                var descriptor = new UniqueTroopDescriptor(seed);
                var soldier = _battleRoster[descriptor];
                soldier.IsKilled = true;
                _battleRoster[descriptor] = soldier;
                var rows = (TroopRosterElement[])AccessTools.Field(typeof(TroopRoster), "data").GetValue(Roster);
                rows[0].Number--;
                AccessTools.Method(typeof(TroopRoster), "InitializeCachedData").Invoke(Roster, null);
            }

            internal void AllocateOtherParty(int count)
            {
                var other = Empty<MapEventParty>();
                Set(other, "Party", Empty<PartyBase>());
                for (int i = 0; i < count; i++)
                {
                    var descriptor = new UniqueTroopDescriptor(++_seed);
                    _battleRoster[descriptor] = new FlattenedTroopRosterElement(Troop, uniqueNo: descriptor);
                    _allocated[descriptor] = other;
                }
            }

            public void Dispose()
            {
                BattleXpWindow.Close(Event);
                _harmony.UnpatchAll(_harmony.Id);
                if (Troop != null) _factions.Remove(Troop.StringId);
                AccessTools.Field(typeof(FactionCache), "_built").SetValue(null, _factionsBuilt);
                _engine.Dispose();
            }
        }

        private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
        private static void Set(object target, string property, object value) =>
            AccessTools.PropertySetter(target.GetType(), property).Invoke(target, new[] { value });
    }
}
