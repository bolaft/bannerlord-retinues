using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Features.Xp.Behaviors
{
    /// <summary>Hooks into CampaignEvents to track troop XP deltas and persist the bank.</summary>
    public sealed class TroopXpBehavior : CampaignBehaviorBase
    {
        private bool _postBattleAccumulatePending;
        private CampaignTime _notBefore;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ =>
            {
                TroopXpService.InitializeSnapshotFromRoster();
            });

            // Mark as pending when a player battle ends (XP not applied yet here)
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, mapEvent =>
            {
                _postBattleAccumulatePending = true;
                _notBefore = CampaignTime.Now; // allow as soon as time advances
            });

            // Passive/fallback accumulation & first-battle fix: do it when time actually moves
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, party =>
            {
                if (party != Player.Party.Base) return;

                // Always sweep hourly to catch passive XP
                TroopXpService.AccumulateFromPlayerParty();

                // If a battle just ended, also force a sweep on the first hour after it
                if (_postBattleAccumulatePending && CampaignTime.Now >= _notBefore)
                {
                    TroopXpService.AccumulateFromPlayerParty();
                    _postBattleAccumulatePending = false;
                }
            });

            // Keep your daily sweep if you want (optional)
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, party =>
            {
                if (party == Player.Party.Base)
                    TroopXpService.AccumulateFromPlayerParty();
            });
        }

        public override void SyncData(IDataStore dataStore)
        {
            // unchanged
            var dict = TroopXpService._pool;
            dataStore.SyncData("CCT_TroopXpPool", ref dict);
            TroopXpService._pool = dict ?? new System.Collections.Generic.Dictionary<string, int>();
        }
    }
}
