using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Features.Xp.Behaviors
{
    /// <summary>Hooks into CampaignEvents to track troop XP deltas and persist the bank.</summary>
    public sealed class TroopXpBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => TroopXpService.InitializeSnapshotFromRoster());

            // DailyTickParty catches passive XP trickles (Leadership/Steward perks, etc.)
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, party =>
            {
                if (party == Player.Party.Base)
                    TroopXpService.AccumulateFromPlayerParty();
            });

            // After each player battle, consolidate the battle XP
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, _ =>
            {
                TroopXpService.AccumulateFromPlayerParty();
            });
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist only the pool; snapshot is recomputed on load
            if (dataStore.IsSaving)
            {
                var dict = TroopXpService._pool;
                dataStore.SyncData("Retinues_TroopXpPool", ref dict);
            }
            else
            {
                var dict = TroopXpService._pool;
                dataStore.SyncData("Retinues_TroopXpPool", ref dict);
                TroopXpService._pool = dict ?? new System.Collections.Generic.Dictionary<string, int>();
            }
        }
    }
}
