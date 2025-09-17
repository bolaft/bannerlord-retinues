using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Persistence.Troop
{
    public class TroopSaveBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private List<TroopSaveData> _troopData = [];
        private List<RosterSaveData> _rosterData = [];

        public override void SyncData(IDataStore dataStore)
        {
            // Persist the troops inside the native save.
            dataStore.SyncData("Retinues_Troops", ref _troopData);
            dataStore.SyncData("Retinues_Rosters", ref _rosterData);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
            CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, OnSaveOver);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnBeforeSave()
        {
            try
            {
                Log.Debug("Collecting root troops.");
                _troopData = CollectAllDefinedCustomTroops();
                Log.Debug($"{_troopData.Count} root troops serialized.");

                Log.Debug("Recording and removing existing custom troops.");
                _rosterData = SaveAndStripAllRosters();
                Log.Debug($"{_rosterData.Count} roster stacks recorded.");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private void OnSaveOver(bool t1, string t2)
        {
            if (_rosterData.Count > 0)
            {
                Log.Debug("Restoring custom troops to rosters.");
                RestoreCustomTroopsToRosters(_rosterData);
                Log.Debug("Restored custom troops to rosters.");
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

                if (_rosterData.Count > 0)
                {
                    RestoreCustomTroopsToRosters(_rosterData);
                    Log.Debug($"Restored custom troops to {_rosterData.Count} rosters.");
                }
                else
                {
                    Log.Debug("No rosters in save.");
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Troop Collection                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static List<TroopSaveData> CollectAllDefinedCustomTroops()
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Roster Management                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static List<RosterSaveData> SaveAndStripAllRosters()
        {
            var rosters = new List<RosterSaveData>();

            foreach (var mobileParty in MobileParty.All)
            {
                var wParty = new WParty(mobileParty);

                var memberData = SaveAndStripRoster(wParty.MemberRoster, false);
                if (memberData != null)
                    rosters.Add(memberData);

                var prisonerData = SaveAndStripRoster(wParty.PrisonRoster, true);
                if (prisonerData != null)
                    rosters.Add(prisonerData);
            }

            return rosters;
        }

        private static RosterSaveData SaveAndStripRoster(WRoster roster, bool IsPrisonRoster)
        {
            var rosterData = new RosterSaveData
            {
                PartyId = roster.Party.StringId,
                IsPrisonRoster = IsPrisonRoster,
            };

            foreach (var element in roster.Elements)
            {
                if (element.Troop.IsVanilla)
                    continue;

                // Record the custom troop
                rosterData.Elements.Add(
                    new RosterElementSaveData
                    {
                        Healthy = element.Number,
                        Wounded = element.WoundedNumber,
                        Xp = element.Xp,
                        IsKingdom = element.Troop.Faction == Player.Kingdom,
                        IsRetinue = element.Troop.IsRetinue,
                        IsElite = element.Troop.IsElite,
                        Index = element.Index,
                    }
                );

                foreach (var pos in element.Troop.PositionInTree)
                    rosterData.Elements.Last().PositionInTree.Add(pos);

                // Remove it from the roster
                roster.RemoveTroop(element.Troop, element.Number, element.WoundedNumber);
            }

            // Return null if no custom troops were found
            return rosterData.Elements.Count > 0 ? rosterData : null;
        }

        private static void RestoreCustomTroopsToRosters(List<RosterSaveData> rosterData)
        {
            foreach (var data in rosterData)
            {
                MobileParty party;

                Log.Debug(
                    $"Restoring roster for party {data?.PartyId}, prison={data?.IsPrisonRoster}"
                );
                try
                {
                    party = MobileParty.All?.FirstOrDefault(p => p?.StringId == data?.PartyId);
                    if (party == null)
                        continue;
                }
                catch (Exception)
                {
                    continue;
                }

                var wParty = new WParty(party);
                foreach (var element in data.Elements)
                {
                    var wTroop = GetTroopFromElementData(element);

                    wParty.MemberRoster.AddTroop(
                        wTroop,
                        element.Healthy,
                        wounded: element.Wounded,
                        index: element.Index
                    );
                }
            }
        }

        private static WCharacter GetTroopFromElementData(RosterElementSaveData data)
        {
            var faction = data.IsKingdom ? Player.Kingdom : Player.Clan;

            if (faction is null)
            {
                Log.Warn("No faction found for roster element, skipping.");
                return null;
            }

            if (data.IsRetinue)
                if (data.IsElite)
                    return data.IsElite ? faction.RetinueElite : null;
                else
                    return faction.RetinueBasic;

            var root = data.IsElite ? faction.RootElite : faction.RootBasic;

            return WCharacter.GetFromPositionInTree(root, data.PositionInTree);
        }
    }
}
