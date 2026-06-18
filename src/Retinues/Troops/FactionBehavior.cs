using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Retinues.Troops
{
    /// <summary>
    /// Campaign behavior for saving and loading custom troop definitions.
    /// Handles serialization, event registration, and tree rebuilding for custom troops.
    /// </summary>
    [SafeClass]
    public class FactionBehavior : CampaignBehaviorBase
    {
        // Flag to reset culture troops on next save
        public static bool ResetCultureTroops = false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private FactionSaveData _clanTroops;
        private FactionSaveData _kingdomTroops;
        private List<FactionSaveData> _cultureTroops;
        private List<FactionSaveData> _minorClanTroops;

        /// <summary>
        /// Syncs custom troop data to and from the campaign save file.
        /// </summary>
        public override void SyncData(IDataStore ds)
        {
            ds.SyncData("Retinues_ClanTroops", ref _clanTroops);
            ds.SyncData("Retinues_KingdomTroops", ref _kingdomTroops);
            ds.SyncData("Retinues_CultureTroops", ref _cultureTroops);
            ds.SyncData("Retinues_MinorClanTroops", ref _minorClanTroops);

            if (ds.IsSaving)
            {
                // Backup troop data before saving
                TroopImportExport.MakeBackup();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            // Save roots before saving
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);

            // Load roots after loading
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);

            // Possible clan unlock event
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, ClanUnlock);

            // Possible kingdom unlock event
            CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, KingdomUnlock);
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
            // Collect clan troops
            if (Player.Clan is not null)
                _clanTroops = new FactionSaveData(Player.Clan);

            // Collect kingdom troops. Clear stale data when the player no longer leads a
            // kingdom (e.g. a rebel kingdom that was absorbed back into another faction);
            // otherwise the defunct faction's troops linger in the save and get re-applied
            // on every load, corrupting clan troops that reuse their stub ids.
            if (Player.Kingdom is not null)
                _kingdomTroops = new FactionSaveData(Player.Kingdom);
            else
                _kingdomTroops = null;

            if (ResetCultureTroops)
            {
                ResetCultureTroops = false;
                _cultureTroops = null;
                _minorClanTroops = null;
                return;
            }

            if (Config.EnableGlobalEditor == true)
            {
                // Collect all base cultures
                var cultures =
                    MBObjectManager.Instance.GetObjectTypeList<CultureObject>()?.ToList() ?? [];

                // Initialize culture troops
                _cultureTroops = [];

                // Save each culture's troop data
                foreach (var culture in cultures)
                    _cultureTroops.Add(new FactionSaveData(new WCulture(culture)));

                // Initialize minor clan troops
                _minorClanTroops = [];

                // Save each minor clan's troop data
                foreach (var clan in Clan.All)
                    if (clan.IsMinorFaction)
                        _minorClanTroops.Add(new FactionSaveData(new WClan(clan)));
            }
        }

        /* ━━━━━━━━ Loading ━━━━━━━ */

        /// <summary>
        /// Loads and rebuilds all custom troop trees after a save is loaded.
        /// </summary>
        private void OnGameLoaded(CampaignGameStarter _)
        {
            // Restore culture reset flag
            ResetCultureTroops = false;

            // Rebuild clan troops
            _clanTroops?.Apply(Player.Clan);

            // Rebuild kingdom troops only when the player actually leads a kingdom.
            // Applying with a null faction would fall into the faction-less culture path,
            // which writes stale troop data onto free stubs (without claiming them) and
            // corrupts clan troops that later reuse those stub ids. Drop the data so it is
            // not re-saved either.
            if (Player.Kingdom is not null)
                _kingdomTroops?.Apply(Player.Kingdom);
            else
                _kingdomTroops = null;

            if (Config.EnableGlobalEditor == true)
            {
                // Rebuild culture troops
                if (_cultureTroops is not null)
                    foreach (FactionSaveData data in _cultureTroops)
                        data.Apply();

                // Rebuild minor clan troops
                if (_minorClanTroops is not null)
                    foreach (FactionSaveData data in _minorClanTroops)
                        data.Apply();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Maintenance                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Drops saved kingdom troop data when the player no longer leads a kingdom (e.g. a
        /// rebel kingdom that was absorbed). Returns true if stale data was discarded.
        /// Used by the scrub cheat to clean a corrupted save without waiting for a reload.
        /// </summary>
        public bool ClearStaleKingdomData()
        {
            if (Player.Kingdom is null && _kingdomTroops is not null)
            {
                _kingdomTroops = null;
                Log.Info("Scrub: discarded stale kingdom troop data.");
                return true;
            }

            return false;
        }

        /* ━━━━━ Fief Acquired ━━━━ */

        void ClanUnlock(
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

            Log.Debug($"Fief acquired: {s.Name}, ensuring troops exist.");
            TroopBuilder.EnsureTroopsExist(Player.Clan);
        }

        /* ━━━━━ Kingdom Created ━━━ */

        void KingdomUnlock(Kingdom k)
        {
            if (new WFaction(k).IsPlayerKingdom == false)
                return; // Not player kingdom

            Log.Debug($"Kingdom created: {k.Name}, ensuring troops exist.");
            TroopBuilder.EnsureTroopsExist(Player.Kingdom);
        }
    }
}
