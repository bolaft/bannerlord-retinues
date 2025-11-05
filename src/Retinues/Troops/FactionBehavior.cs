using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Safety.Legacy;
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
    public class FactionBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private FactionSaveData _clanTroops;
        private FactionSaveData _kingdomTroops;

        /// <summary>
        /// Syncs custom troop data to and from the campaign save file.
        /// </summary>
        public override void SyncData(IDataStore ds)
        {
            ds.SyncData("Retinues_ClanTroops", ref _clanTroops);
            ds.SyncData("Retinues_KingdomTroops", ref _kingdomTroops);
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

            // Collect kingdom troops
            if (Player.Kingdom is not null)
                _kingdomTroops = new FactionSaveData(Player.Kingdom);
        }

        /* ━━━━━━━━ Loading ━━━━━━━ */

        /// <summary>
        /// Loads and rebuilds all custom troop trees after a save is loaded.
        /// </summary>
        private void OnGameLoaded(CampaignGameStarter _)
        {
            // Rebuild clan troops
            _clanTroops?.Apply(Player.Clan);

            // Rebuild kingdom troops
            _kingdomTroops?.Apply(Player.Kingdom);
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
