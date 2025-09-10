using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Persistence.Troop
{
    public class TroopSaveBehavior : CampaignBehaviorBase
    {
        private List<TroopSaveData> _troopData = [];
        private List<RosterSaveData> _rosterStacks = [];

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist the troops inside the native save.
            dataStore.SyncData("Retinues_Troops", ref _troopData);
            dataStore.SyncData("Retinues_Rosters", ref _rosterStacks);
        }

        private void OnBeforeSave()
        {
            try
            {
                Log.Debug("Collecting root troops.");
                _troopData = CollectAllCurrentTroops();
                Log.Debug($"{_troopData.Count} root troops serialized.");

                Log.Debug("Snapshotting and stripping custom stacks from rosters.");
                _rosterStacks = SnapshotAndStripAllCustomStacks();
                Log.Debug($"{_rosterStacks.Count} custom stacks snapshotted & stripped.");

                // Then restore rosters (using vanillaId → current clone maps)
                if (_rosterStacks != null && _rosterStacks.Count > 0)
                {
                    RestoreRosterStacks(_rosterStacks);
                    Log.Debug($"Restored {_rosterStacks.Count} custom stacks to rosters.");
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private void OnGameLoaded(CampaignGameStarter _)
        {
            try
            {
                // Rebuild all custom trees first
                if (_troopData != null && _troopData.Count > 0)
                {
                    Player.Clan?.ClearTroops();
                    Player.Kingdom?.ClearTroops();
                    foreach (var root in _troopData)
                        TroopSave.Load(root);
                    Log.Debug($"Rebuilt {_troopData.Count} root troops.");
                }
                else
                {
                    Log.Debug("No root troops in save.");
                }

                // Then restore rosters (using vanillaId → current clone maps)
                if (_rosterStacks != null && _rosterStacks.Count > 0)
                {
                    RestoreRosterStacks(_rosterStacks);
                    Log.Debug($"Restored {_rosterStacks.Count} custom stacks to rosters.");
                }
                else
                {
                    Log.Debug("No roster stacks in save.");
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // -------------------------
        // Troop Collection
        // -------------------------

        private static List<TroopSaveData> CollectAllCurrentTroops()
        {
            var list = new List<TroopSaveData>();

            Log.Debug("Collecting from clan.");
            CollectFromFaction(Player.Clan, list);

            Log.Debug("Collecting from kingdom.");
            CollectFromFaction(Player.Kingdom, list);

            return list;
        }

        private static void CollectFromFaction(WFaction faction, List<TroopSaveData> list)
        {
            if (faction is null)
            {
                Log.Debug("No faction, skipping.");
                return;
            }

            if (faction.RetinueElite != null && faction.RetinueBasic != null)
            {
                Log.Debug("Collecting retinue troops.");
                list.Add(TroopSave.Save(faction.RetinueElite));
                list.Add(TroopSave.Save(faction.RetinueBasic));
            }
            else
            {
                Log.Debug("No retinue troops found.");
            }

            if (faction.RootElite != null && faction.RootBasic != null)
            {
                Log.Debug("Collecting root troops.");
                list.Add(TroopSave.Save(faction.RootElite));
                list.Add(TroopSave.Save(faction.RootBasic));
            }
            else
            {
                Log.Debug("No root troops found.");
            }
        }

        // -------------------------
        // Snapshot & Strip
        // -------------------------

        private static bool IsCustom(CharacterObject c)
            // Keep this in sync with WCharacter.IsVanilla
            => c != null && c.StringId != null && c.StringId.StartsWith("CharacterObject_", StringComparison.Ordinal);

        private static List<RosterSaveData> SnapshotAndStripAllCustomStacks()
        {
            var result = new List<RosterSaveData>();

            // Build quick id sets to know if a custom id belongs to Clan or Kingdom
            var clanIds = AllCustomIdsForFaction(Player.Clan);
            var kingdomIds = AllCustomIdsForFaction(Player.Kingdom);

            foreach (var party in MobileParty.All)
            {
                SnapshotRoster(party, party.MemberRoster, "Member", clanIds, kingdomIds, result);
                SnapshotRoster(party, party.PrisonRoster, "Prison", clanIds, kingdomIds, result);
            }

            return result;
        }

        private static HashSet<string> AllCustomIdsForFaction(WFaction f)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (f == null) return set;

            void addTree(WCharacter root)
            {
                if (root == null) return;
                foreach (var w in root.Tree)
                    set.Add(w.StringId);
            }

            addTree(f.RetinueElite);
            addTree(f.RetinueBasic);
            addTree(f.RootElite);
            addTree(f.RootBasic);

            return set;
        }

        private static void SnapshotRoster(
            MobileParty owner,
            TroopRoster roster,
            string kind,
            HashSet<string> clanIds,
            HashSet<string> kingdomIds,
            List<RosterSaveData> sink)
        {
            if (roster == null || roster.Count == 0) return;

            for (int i = roster.Count - 1; i >= 0; i--)
            {
                var elem = roster.GetElementCopyAtIndex(i);
                var c = elem.Character;
                if (c == null || !IsCustom(c)) continue;

                bool isKingdom = kingdomIds.Contains(c.StringId);
                bool isClan    = clanIds.Contains(c.StringId);
                if (!isKingdom && !isClan) continue;

                var w = new WCharacter(c, faction: isKingdom ? Player.Kingdom : Player.Clan);
                var vanillaId = w.VanillaStringId;
                if (string.IsNullOrEmpty(vanillaId)) continue;

                sink.Add(new RosterSaveData
                {
                    PartyId = owner.StringId,
                    RosterKind = kind,
                    VanillaStringId = vanillaId,
                    Healthy = elem.Number - elem.WoundedNumber,
                    Wounded = elem.WoundedNumber,
                    Xp = elem.Xp,
                    IsKingdom = isKingdom,
                    IsRetinue = w.IsRetinue
                });

                if (elem.Number > 0)
                    roster.AddToCounts(c, -elem.Number, removeDepleted: true, insertAtFront: false);
            }
        }

        // -------------------------
        // Restore
        // -------------------------

        private static void RestoreRosterStacks(List<RosterSaveData> stacks)
        {
            if (stacks == null || stacks.Count == 0) return;

            var clanRegMap     = BuildVanillaToCloneMap(Player.Clan,    retinue: false);
            var clanRetMap     = BuildVanillaToCloneMap(Player.Clan,    retinue: true);
            var kingdomRegMap  = BuildVanillaToCloneMap(Player.Kingdom, retinue: false);
            var kingdomRetMap  = BuildVanillaToCloneMap(Player.Kingdom, retinue: true);

            foreach (var ss in stacks)
            {
                try
                {
                    var party = MobileParty.All.FirstOrDefault(p => p.StringId == ss.PartyId);
                    if (party == null) continue;

                    var roster = ss.RosterKind == "Prison" ? party.PrisonRoster : party.MemberRoster;
                    if (roster == null) continue;

                    // Pick map by kingdom/retinue flags
                    Dictionary<string, CharacterObject> map =
                        ss.IsKingdom
                            ? (ss.IsRetinue ? kingdomRetMap : kingdomRegMap)
                            : (ss.IsRetinue ? clanRetMap    : clanRegMap);

                    // Backwards-compat: old saves had IsRetinue=false by default → try other map if not found
                    if (!map.TryGetValue(ss.VanillaStringId, out var current) || current == null)
                    {
                        var alt =
                            ss.IsKingdom
                                ? (ss.IsRetinue ? kingdomRegMap : kingdomRetMap)
                                : (ss.IsRetinue ? clanRegMap    : clanRetMap);

                        if (!alt.TryGetValue(ss.VanillaStringId, out current) || current == null)
                            continue; // couldn't resolve
                    }

                    int healthy = ss.Healthy, wounded = ss.Wounded, xp = ss.Xp;

                    if (healthy > 0)
                        roster.AddToCounts(current, healthy, insertAtFront: false, woundedCount: 0, xpChange: xp);
                    if (wounded > 0)
                        roster.AddToCounts(current, wounded, insertAtFront: false, woundedCount: wounded, xpChange: 0);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }

        private static Dictionary<string, CharacterObject> BuildVanillaToCloneMap(WFaction f, bool retinue)
        {
            var map = new Dictionary<string, CharacterObject>(StringComparer.OrdinalIgnoreCase);
            if (f == null) return map;

            void addTree(WCharacter root)
            {
                if (root == null) return;
                foreach (var w in root.Tree)
                {
                    var v = w.VanillaStringId;
                    if (!string.IsNullOrEmpty(v) && !map.ContainsKey(v))
                        map.Add(v, (CharacterObject) w.Base);
                }
            }

            if (retinue)
            {
                addTree(f.RetinueElite);
                addTree(f.RetinueBasic);
            }
            else
            {
                addTree(f.RootElite);
                addTree(f.RootBasic);
            }

            return map;
        }
    }
}
