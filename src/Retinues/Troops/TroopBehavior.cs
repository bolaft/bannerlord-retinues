using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Troops
{
    /// <summary>
    /// Campaign behavior for saving and loading custom troop definitions.
    /// Handles serialization, event registration, and tree rebuilding for custom troops.
    /// </summary>
    [SafeClass]
    public class TroopBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private List<TroopSaveData> _troopData;
        public List<TroopSaveData> TroopData => _troopData ??= [];

        private Dictionary<string, TroopIndexEntry> _index;
        public static Dictionary<string, TroopIndexEntry> Index { get; set; }

        /// <summary>
        /// Returns true if there is any troop sync data present.
        /// </summary>
        public bool HasSyncData => TroopData.Count > 0;

        /// <summary>
        /// Syncs custom troop data to and from the campaign save file.
        /// </summary>
        public override void SyncData(IDataStore ds)
        {
            // Persist custom troop roots in the save file.
            ds.SyncData("Retinues_Troops_Data", ref _troopData);
            ds.SyncData("Retinues_TroopIndex", ref _index);

            Index ??= _index ?? new Dictionary<string, TroopIndexEntry>(StringComparer.Ordinal);
            _index = Index;

            Log.Info($"{TroopData.Count} root troops.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            // Save roots before saving
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);

            // Rebuild all trees from serialized roots after a save is loaded
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);

            // Fief acquired
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(
                this,
                OnSettlementOwnerChanged
            );

            // Kingdom created
            CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, OnKingdomCreated);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Saving ━━━━━━━━ */

        /// <summary>
        /// Collects all defined custom troops before saving.
        /// </summary>
        private void OnBeforeSave()
        {
            try
            {
                _troopData = CollectAllDefinedCustomTroops();
                // Ensure _index is not null
                _index ??= Index ?? new Dictionary<string, TroopIndexEntry>(StringComparer.Ordinal);
                Log.Debug($"Serialized {TroopData.Count} root troops.");
            }
            catch (Exception e)
            {
                Log.Exception(e, "OnBeforeSave failed");
            }
        }

        /* ━━━━━━━━ Loading ━━━━━━━ */

        /// <summary>
        /// Loads and rebuilds all custom troop trees after a save is loaded.
        /// </summary>
        private void OnGameLoaded(CampaignGameStarter _)
        {
            try
            {
                Index ??= _index ?? new Dictionary<string, TroopIndexEntry>(StringComparer.Ordinal);
                _index = Index;

                // One-time migration from old "smart IDs"
                if (Index.Count == 0)
                    TroopIndex.MigrateFromLegacyIds();

                if (TroopData.Count > 0)
                {
                    foreach (var root in TroopData)
                    {
                        var troop = TroopLoader.Load(root);
                        // sanitize old saves: enforce [0]=battle, [1]=civilian, [2+]=battle
                        var eqs = troop.Loadout.Equipments; // WEquipment list
                        troop.Loadout.Equipments =
                        [
                            .. eqs.Select(
                                (we, i) => WEquipment.FromCode(we.Code, troop.Loadout, i)
                            ),
                        ];
                    }
                    Log.Debug($"Rebuilt {TroopData.Count} root troops from save.");
                }
                else
                {
                    Log.Debug("No custom roots found in save.");
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "OnGameLoaded failed");
            }
        }

        /* ━━━━━ Fief Acquired ━━━━ */

        void OnSettlementOwnerChanged(
            Settlement s,
            bool _,
            Hero n,
            Hero o,
            Hero __,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail d
        )
        {
            if (new WFaction(n?.Clan).IsPlayerClan == false)
                return; // Not player clan gaining a fief

            Log.Debug($"Fief acquired: {s.Name}, triggering troop ensure.");
            TroopBuilder.EnsureTroopsExist(Player.Clan);
        }

        /* ━━━━━ Kingdom Created ━━━ */

        void OnKingdomCreated(Kingdom k)
        {
            if (new WFaction(k).IsPlayerKingdom == false)
                return; // Not player kingdom

            Log.Debug($"Kingdom created: {k.Name}, triggering troop ensure.");
            TroopBuilder.EnsureTroopsExist(Player.Kingdom);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Collects all custom troop roots from player clan and kingdom.
        /// </summary>
        public static List<TroopSaveData> CollectAllDefinedCustomTroops()
        {
            var list = new List<TroopSaveData>();

            CollectFromFaction(Player.Clan, list);
            CollectFromFaction(Player.Kingdom, list);

            return list;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds all active custom troop types from a faction to the list.
        /// </summary>
        private static void CollectFromFaction(WFaction faction, List<TroopSaveData> list)
        {
            if (faction is null)
            {
                Log.Debug("Collect: no faction, skipping.");
                return;
            }

            // Retinues
            if (faction.RetinueElite.IsActive)
                list.Add(TroopLoader.Save(faction.RetinueElite));
            if (faction.RetinueBasic.IsActive)
                list.Add(TroopLoader.Save(faction.RetinueBasic));

            // Regular troops
            if (faction.RootElite.IsActive)
                list.Add(TroopLoader.Save(faction.RootElite));
            if (faction.RootBasic.IsActive)
                list.Add(TroopLoader.Save(faction.RootBasic));

            // Militias
            if (faction.MilitiaMelee.IsActive)
                list.Add(TroopLoader.Save(faction.MilitiaMelee));
            if (faction.MilitiaMeleeElite.IsActive)
                list.Add(TroopLoader.Save(faction.MilitiaMeleeElite));
            if (faction.MilitiaRanged.IsActive)
                list.Add(TroopLoader.Save(faction.MilitiaRanged));
            if (faction.MilitiaRangedElite.IsActive)
                list.Add(TroopLoader.Save(faction.MilitiaRangedElite));
        }
    }
}
