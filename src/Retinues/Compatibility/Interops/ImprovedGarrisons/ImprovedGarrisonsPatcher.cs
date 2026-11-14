using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Compatibility.Interops.ImprovedGarrisons
{
    /// <summary>Optional, signature-checked integration; no reference to the IG assembly or save data.</summary>
    internal static class ImprovedGarrisonsPatcher
    {
        private const string PatchId = "Retinues.ImprovedGarrisons";
        private static bool _installed;
        private static bool _reportedFailure;
        private static PropertyInfo _logic, _behavior, _home, _owner, _settings, _followsTemplate;
        private static MethodInfo _townSettings, _needed;
        [ThreadStatic] private static Scope _scope;

        private static bool Enabled => Campaign.Current != null && !Safety.Uninstall.UninstallBehavior.IsPrepared;

        internal static void TryPatch()
        {
            if (_installed)
                return;
            // Do not load an inactive mod's DLL. Retry at OnGameStart for either load order.
            var logicType = AccessTools.TypeByName("ImprovedGarrisons.Recruitment.GarrisonRecruitmentLogic");
            if (logicType == null)
                return;
            var harmony = new Harmony(PatchId);
            var patched = new List<MethodInfo>();
            try
            {
                var recruiterType = AccessTools.TypeByName("ImprovedGarrisons.AI.AITypes.GarrisonRecruiter");
                var mainType = AccessTools.TypeByName("ImprovedGarrisons.Main");
                if (recruiterType == null || mainType == null)
                    throw new MissingMemberException("Improved Garrisons recruiter types");
                _logic = RequiredProperty(mainType, "RecruitmentLogic");
                _behavior = RequiredProperty(mainType, "GarrisonBehavior");
                _home = RequiredProperty(recruiterType, "fromSettlement");
                _owner = RequiredProperty(recruiterType, "ownerHero");
                _settings = RequiredProperty(recruiterType, "homeGarrisonSettings");
                _followsTemplate = RequiredProperty(_settings.PropertyType, "RecruitmentFollowsTemplate");
                _townSettings = RequiredMethod(_behavior.PropertyType, "GetTownSettings", _settings.PropertyType, typeof(Town));
                _needed = RequiredMethod(logicType, "GetAllNeededTemplateUnitsLeftToRecruitWithRecruiter",
                    typeof(Dictionary<CharacterObject, int>), typeof(Settlement), recruiterType);

                var targets = new[] {
                    (RequiredMethod(logicType, "GetAllRecruitableRecruitsFromSettlement", typeof(List<Tuple<Hero, CharacterObject, int>>),
                        typeof(Settlement), typeof(int), typeof(Hero), typeof(bool)), nameof(ListPrefix), nameof(ListPostfix), (string)null),
                    (RequiredMethod(logicType, "AmountOfAvailableRecruitsByList", typeof(int), typeof(Settlement), typeof(int), typeof(Hero), typeof(List<CharacterObject>)), nameof(QueryPrefix), nameof(QueryPostfix), nameof(ScopeFinalizer)),
                    (RequiredMethod(logicType, "AmountOfAvailableRecruitsThatCanBeUpgraded", typeof(int), typeof(Settlement), typeof(int), typeof(Hero), typeof(List<CharacterObject>)), nameof(QueryPrefix), nameof(QueryPostfix), nameof(ScopeFinalizer)),
                    (RequiredMethod(logicType, "OverallAmountOfAvailableRecruits", typeof(int), typeof(Settlement), typeof(int), typeof(Hero), typeof(bool)), nameof(OverallPrefix), (string)null, nameof(ScopeFinalizer)),
                    (RequiredMethod(logicType, "RecruitFromSurroundingVillages", typeof(void), typeof(Settlement)), nameof(RegionalPrefix), (string)null, nameof(ScopeFinalizer)),
                    (RequiredMethod(recruiterType, "ExecuteBasicRecruitFromSettlement", typeof(void), typeof(Settlement)), nameof(RecruiterPrefix), (string)null, nameof(ScopeFinalizer)),
                    (RequiredMethod(recruiterType, "ExecuteRecruitTemplateUnitsFromSettlement", typeof(void), typeof(Settlement)), nameof(RecruiterPrefix), (string)null, nameof(ScopeFinalizer)),
                    (RequiredMethod(logicType, "RecruitFromSettlement", typeof(bool), typeof(MobileParty), typeof(Settlement), typeof(Hero), typeof(int), typeof(Hero)), nameof(TransferPrefix), nameof(TransferPostfix), nameof(TransferFinalizer)),
                    (RequiredMethod(typeof(RecruitmentCampaignBehavior), "GetRecruitVolunteerFromIndividual", typeof(void), typeof(MobileParty), typeof(CharacterObject), typeof(Hero), typeof(int)), nameof(MobileTransferPrefix), nameof(MobileTransferPostfix), (string)null),
                };
                foreach (var (target, prefix, postfix, finalizer) in targets)
                {
                    patched.Add(target);
                    harmony.Patch(target, Hook(prefix), Hook(postfix), finalizer: Hook(finalizer));
                }
                _installed = true;
                Log.Info("[ImprovedGarrisons] Recruitment compatibility installed: player troop pools, templates, elite filters and stock-safe transfers.");
            }
            catch (Exception e)
            {
                foreach (var method in patched)
                    harmony.Unpatch(method, HarmonyPatchType.All, PatchId);
                Report(e);
            }
        }

        private static HarmonyMethod Hook(string name) => name == null ? null
            : new HarmonyMethod(AccessTools.Method(typeof(ImprovedGarrisonsPatcher), name));
        private static PropertyInfo RequiredProperty(Type type, string name) => AccessTools.Property(type, name)
            ?? throw new MissingMemberException(type.FullName, name);
        private static MethodInfo RequiredMethod(Type type, string name, Type result, params Type[] parameters)
        {
            var method = AccessTools.Method(type, name, parameters);
            if (method == null || method.ReturnType != result)
                throw new MissingMethodException(type.FullName, name);
            return method;
        }
        private static void Report(Exception e)
        {
            if (_reportedFailure)
                return;
            _reportedFailure = true;
            Log.Exception(e, "[ImprovedGarrisons] Compatibility hook unavailable or failed; please include this log in a bug report.");
        }

        internal sealed class Scope : IDisposable
        {
            private readonly Scope _previous;
            internal readonly Hero Owner;
            internal readonly Func<CharacterObject, bool> Matches;
            internal readonly bool Transfers;
            internal readonly int Limit;
            internal readonly Dictionary<(Hero, int), RecruitmentOffers.Offer> Offers = new();
            internal int Recruited;
            internal Scope(Hero owner, Func<CharacterObject, bool> matches = null, bool transfers = false, int limit = int.MaxValue)
            {
                Owner = owner; Matches = matches; Transfers = transfers; Limit = limit;
                _previous = _scope; _scope = this;
            }
            public void Dispose()
            {
                _scope = _previous;
                Offers.Clear();
                if (Recruited > 0)
                    Log.Debug($"[ImprovedGarrisons] Completed {Recruited} recruitment(s) through the compatible troop pool.");
            }
        }

        private static void QueryPrefix(object[] __args, MethodBase __originalMethod, out Scope __state)
        {
            __state = null;
            if (!Enabled || __args[2] is not Hero owner || __args[3] is not List<CharacterObject> targets)
                return;
            var set = new HashSet<CharacterObject>(targets.Where(t => t != null));
            bool exact = __originalMethod.Name == "AmountOfAvailableRecruitsByList";
            __state = new Scope(owner, t => exact ? set.Contains(t) : set.Any(target => RecruitmentOffers.Reaches(t, target)), limit: (int)__args[1]);
            // Filter against the template BEFORE limiting the count, including later slots.
            __args[1] = int.MaxValue;
        }
        private static void QueryPostfix(Scope __state, ref int __result)
        {
            if (__state != null)
                __result = Math.Min(__result, Math.Max(0, __state.Limit));
        }
        private static void OverallPrefix(Hero ownerHero, out Scope __state) =>
            __state = Enabled && ownerHero != null ? new Scope(ownerHero) : null;

        private static Func<CharacterObject, bool> Template(object settings, Settlement home, object recruiter)
        {
            if (!(bool)_followsTemplate.GetValue(settings))
                return null;
            var needed = (Dictionary<CharacterObject, int>)_needed.Invoke(_logic.GetValue(null), new[] { (object)home, recruiter });
            if (needed == null)
                throw new InvalidOperationException("Improved Garrisons did not return a training template.");
            var targets = needed.Where(p => p.Key != null && p.Value > 0).Select(p => p.Key).ToArray();
            return troop => targets.Any(target => RecruitmentOffers.Reaches(troop, target));
        }
        private static void RegionalPrefix(Settlement settlement, out Scope __state)
        {
            __state = null;
            if (!Enabled || settlement?.Town == null || settlement.Owner == null)
                return;
            try
            {
                var settings = _townSettings.Invoke(_behavior.GetValue(null), new object[] { settlement.Town });
                __state = new Scope(settlement.Owner, Template(settings, settlement, null), transfers: true);
            }
            catch (Exception e) { Report(e); }
        }
        private static void RecruiterPrefix(object __instance, MethodBase __originalMethod, out Scope __state)
        {
            __state = null;
            if (!Enabled)
                return;
            try
            {
                var owner = (Hero)_owner.GetValue(__instance);
                var home = (Settlement)_home.GetValue(__instance);
                if (owner == null || home == null)
                    return;
                var matches = __originalMethod.Name == "ExecuteRecruitTemplateUnitsFromSettlement"
                    ? Template(_settings.GetValue(__instance), home, __instance) : null;
                __state = new Scope(owner, matches, transfers: true);
            }
            catch (Exception e) { Report(e); }
        }
        private static Exception ScopeFinalizer(Scope __state, Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }

        private sealed class Query
        {
            internal int Limit;
            internal bool OnlyElite;
            internal Scope Scope;
        }
        private static void ListPrefix(ref int maxAmountToRecruit, ref bool onlyEliteUnits, Hero ownerHero, out Query __state)
        {
            __state = null;
            if (!Enabled || _scope == null || _scope.Owner != ownerHero)
                return;
            __state = new Query { Limit = maxAmountToRecruit, OnlyElite = onlyEliteUnits, Scope = _scope };
            maxAmountToRecruit = int.MaxValue;
            onlyEliteUnits = false;
        }
        private static void ListPostfix(Settlement settlement, Hero ownerHero, Query __state,
            ref List<Tuple<Hero, CharacterObject, int>> __result)
        {
            if (__state == null || __result == null)
                return;
            // A repeated scan supersedes earlier offers, even if mapping this scan fails.
            foreach (var entry in __result)
                if (entry?.Item1 != null)
                    __state.Scope.Offers.Remove((entry.Item1, entry.Item3));
            try
            {
                var output = new List<Tuple<Hero, CharacterObject, int>>();
                var offers = new List<RecruitmentOffers.Offer>();
                foreach (var entry in __result)
                {
                    if (output.Count >= __state.Limit)
                        break;
                    if (entry?.Item1 == null || entry.Item2 == null)
                        continue;
                    var troop = RecruitmentOffers.Resolve(settlement, ownerHero, entry.Item1, entry.Item3, entry.Item2, __state.Scope.Matches);
                    if (troop == null || (__state.OnlyElite && !RecruitmentOffers.IsElite(troop))
                        || (__state.Scope.Matches != null && !__state.Scope.Matches(troop)))
                        continue;
                    output.Add(Tuple.Create(entry.Item1, troop, entry.Item3));
                    offers.Add(new RecruitmentOffers.Offer(ownerHero, entry.Item1, entry.Item3, entry.Item2, troop));
                }
                if (__state.Scope.Transfers)
                    foreach (var offer in offers)
                        __state.Scope.Offers[(offer.Notable, offer.Slot)] = offer;
                __result = output;
            }
            catch (Exception e)
            {
                // Original query was widened. Restore IG's original filter/limit on failure.
                __result = __result.Where(t => t?.Item2 != null && (!__state.OnlyElite
                    || RecruitmentOffers.Reaches(t.Item2.Culture?.EliteBasicTroop, t.Item2)))
                    .Take(Math.Max(0, __state.Limit)).ToList();
                Report(e);
            }
        }

        private static bool TransferPrefix(Hero notable, int bitCode, Hero partyOwner,
            out RecruitmentOffers.SlotSwap __state, ref bool __result)
        {
            __state = null;
            if (_scope?.Transfers != true || _scope.Owner != partyOwner
                || !_scope.Offers.TryGetValue((notable, bitCode), out var offer))
                return true;
            __state = offer.Apply();
            if (__state != null)
                return true;
            __result = false; // The slot was consumed or changed since discovery.
            return false;
        }
        private static void TransferPostfix(RecruitmentOffers.SlotSwap __state, bool __result)
        {
            if (__state != null && __result && _scope != null)
                _scope.Recruited++;
        }
        private static Exception TransferFinalizer(RecruitmentOffers.SlotSwap __state, Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
        private static bool MobileTransferPrefix(CharacterObject subject, Hero individual, int bitCode,
            out RecruitmentOffers.Offer __state)
        {
            __state = null;
            if (_scope?.Transfers != true || !_scope.Offers.TryGetValue((individual, bitCode), out var offer))
                return true;
            if (offer.Troop != subject || !offer.IsCurrent)
                return false;
            __state = offer;
            return true;
        }
        private static void MobileTransferPostfix(RecruitmentOffers.Offer __state)
        {
            if (__state != null && __state.Notable.VolunteerTypes[__state.Slot] == null && _scope != null)
                _scope.Recruited++;
        }

        // The general AI guard uses the settlement's base clan tree. An IG offer
        // may instead be an eligible kingdom troop; retain that already-checked
        // offer only for its owning recruiter, original slot and current scope.
        internal static bool IsApprovedOffer(MobileParty party, Hero notable, int slot, CharacterObject troop) =>
            _scope?.Transfers == true && Enabled && party?.ActualClan != null
            && party.ActualClan == _scope.Owner?.Clan
            && _scope.Offers.TryGetValue((notable, slot), out var offer)
            && offer.Troop == troop && offer.IsCurrent;
    }
}
