using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using ActiveModules = Retinues.Utils.ModuleChecker;
using Log = Retinues.Utils.Log;

namespace Retinues.Compatibility
{
    internal enum ATCRecruitRole { None, Basic, Elite, Ambiguous }

    /// <summary>
    /// Reads ATC's loaded configuration without taking a dependency on its assembly.
    /// Roles apply only to recruitment; culture trees and saved troops are never modified.
    /// </summary>
    internal static partial class ATCRecruitment
    {
        private static Type _configType, _subModuleType;
        private static bool _reported, _reportedFailure;

        internal static ATCRecruitRole GetRole(CharacterObject troop, Settlement settlement,
            Hero notable = null, MobileParty recruiter = null)
        {
            if (troop == null || troop.IsHero || settlement?.Culture == null
                || Safety.Uninstall.UninstallBehavior.IsPrepared
                || !ActiveModules.GetActiveModules().Any(m =>
                    string.Equals(m.Id, "AdonnaysTroopChanger", StringComparison.OrdinalIgnoreCase)))
                return ATCRecruitRole.None;
            try
            {
                if (!TryGetConfiguration(out var clans, out var factions))
                    return ATCRecruitRole.None;

                var ownerClan = recruiter != null ? recruiter.Party?.Owner?.Clan : settlement.OwnerClan;
                string clanId = ownerClan != null && ownerClan == Clan.PlayerClan
                    ? "player_clan" : ownerClan?.StringId;
                var faction = recruiter != null ? recruiter.MapFaction : notable?.MapFaction ?? settlement.MapFaction;
                var player = Hero.MainHero;
                // ATC uses the settlement owner's faction for the player-faction special case.
                var playerFaction = recruiter != null ? recruiter.MapFaction : settlement.Owner?.MapFaction;
                string factionId = player != null && playerFaction != null
                    && playerFaction == player.MapFaction && playerFaction.Leader == player
                        ? "player_faction" : faction?.StringId;
                string recruitType = recruiter == null ? null
                    : ownerClan != null && ownerClan == Clan.PlayerClan ? "player_clan" : "default";
                var config = SelectCulture(clans, factions, clanId, factionId, settlement.Culture.StringId);
                return Classify(config, troop, CharacterObject.Find, recruitType,
                    settlement.Culture.BasicTroop, settlement.Culture.EliteBasicTroop);
            }
            catch (Exception e)
            {
                if (!_reportedFailure)
                {
                    _reportedFailure = true;
                    Log.Exception(e, "[ATC] Recruitment configuration could not be read; retaining normal recruitment rules.");
                }
                return ATCRecruitRole.None;
            }
        }

        private static bool TryGetConfiguration(out IEnumerable clans, out IEnumerable factions)
        {
            clans = factions = null;
            if (_configType == null)
            {
                // Never load an inactive mod from disk. Retry if ATC loads after Retinues.
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "AdonnaysTroopChanger");
                if (assembly == null)
                    return false;
                _configType = assembly.GetType("AdonnaysTroopChanger.XMLReader.ATCConfig", true);
                _subModuleType = assembly.GetType("AdonnaysTroopChanger.SubModule", true);
            }
            if ((bool)RequiredField(_subModuleType, "disableATC").GetValue(null))
                return false;
            clans = RequiredField(_configType, "clanList").GetValue(null) as IEnumerable;
            factions = RequiredField(_configType, "factionList").GetValue(null) as IEnumerable;
            if (clans == null || factions == null)
                return false;
            if (!_reported)
            {
                _reported = true;
                Log.Info("[ATC] Recruitment compatibility active: reading configured basic and elite volunteer trees.");
            }
            return true;
        }

        private static FieldInfo RequiredField(Type type, string name) =>
            type.GetField(name, BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingFieldException(type.FullName, name);

        private static object Read(object value, string name)
        {
            if (value == null)
                return null;
            var property = value.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMemberException(value.GetType().FullName, name);
            return property.GetValue(value);
        }

        /// <summary>Same clan/faction/culture precedence as ATC, without drawing another random recruit.</summary>
        internal static object SelectCulture(IEnumerable clans, IEnumerable factions,
            string clanId, string factionId, string cultureId)
        {
            object Find(IEnumerable entries, string key, string id, bool allowDefaultCulture)
            {
                if (entries == null || string.IsNullOrEmpty(id))
                    return null;
                foreach (var entry in entries)
                {
                    if (!string.Equals(Read(entry, key) as string, id, StringComparison.Ordinal))
                        continue;
                    var cultures = Read(entry, "Cultures") as IEnumerable;
                    if (cultures == null)
                        continue;
                    object fallback = null;
                    foreach (var culture in cultures)
                    {
                        var candidate = Read(culture, "CultureID") as string;
                        if (candidate == cultureId)
                            return culture;
                        if (allowDefaultCulture && candidate == "default")
                            fallback = culture;
                    }
                    if (fallback != null)
                        return fallback;
                }
                return null;
            }
            return Find(clans, "ClanID", clanId, true)
                ?? Find(factions, "FactionID", factionId, true)
                ?? Find(factions, "FactionID", "default", false);
        }

        internal static ATCRecruitRole Classify(object config, CharacterObject troop,
            Func<string, CharacterObject> resolve, string recruitType,
            CharacterObject basicFallback, CharacterObject eliteFallback)
        {
            if (config == null || troop == null || troop.IsHero)
                return ATCRecruitRole.None;
            bool basic = InPool(Read(config, "BasicTroops"), troop, resolve, recruitType, basicFallback);
            bool elite = InPool(Read(config, "EliteTroops"), troop, resolve, recruitType, eliteFallback);
            // One ID can be configured in both pools. Its tier does not tell us which
            // ATC selected, so leave it alone instead of silently changing troop class.
            return basic && elite ? ATCRecruitRole.Ambiguous
                : elite ? ATCRecruitRole.Elite : basic ? ATCRecruitRole.Basic : ATCRecruitRole.None;
        }

        private static bool InPool(object pool, CharacterObject troop,
            Func<string, CharacterObject> resolve, string recruitType, CharacterObject fallback)
        {
            if (Read(pool, "Volunteers") is not IEnumerable volunteers)
                return false;
            foreach (var entry in volunteers)
            {
                if (entry == null)
                    continue;
                bool substitute = recruitType == "default"
                    ? (bool)Read(entry, "PlayerOnly") || (bool)Read(entry, "ClanOnly")
                    : recruitType == "player_clan" && (bool)Read(entry, "PlayerOnly");
                string id = Read(entry, substitute ? "ReplaceWith" : "VolunteerID") as string;
                var root = string.IsNullOrEmpty(id) ? (substitute ? fallback : null) : resolve(id);
                if (Contains(root, troop))
                    return true;
            }
            return false;
        }

        internal static bool Contains(CharacterObject root, CharacterObject troop)
        {
            if (root == null || troop == null)
                return false;
            var seen = new HashSet<CharacterObject>();
            var pending = new Stack<CharacterObject>();
            pending.Push(root);
            while (pending.Count != 0)
            {
                var next = pending.Pop();
                if (next == null || !seen.Add(next))
                    continue;
                if (next == troop)
                    return true;
                foreach (var child in next.UpgradeTargets ?? Array.Empty<CharacterObject>())
                    pending.Push(child);
            }
            return false;
        }
    }
}
