using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Features.AutoJoin
{
    /// <summary>
    /// Tracks per-retinue hire caps, accumulates renown toward the next hire,
    /// and spawns queued retinues into the main party on the daily tick.
    /// </summary>
    [SafeClass]
    public class AutoJoinBehavior : CampaignBehaviorBase
    {
        public static AutoJoinBehavior Instance { get; private set; }

        public AutoJoinBehavior() => Instance = this;

        private static readonly Random rng = new();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, int> _caps = [];
        private float _renownReserve;
        private float _lastRenown;

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_RetinueHire_Caps", ref _caps);
            dataStore.SyncData("Retinues_RetinueHire_RenownReserve", ref _renownReserve);
            dataStore.SyncData("Retinues_RetinueHire_LastRenown", ref _lastRenown);

            if (dataStore.IsLoading)
                if (_lastRenown == 0)
                    _lastRenown = Player.Renown;

            _caps ??= [];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnDailyTickParty(MobileParty party)
        {
            if (party == null || !party.IsMainParty)
                return;

            var retinues = GetHireableRetinues();
            Log.Info(
                $"[RetinueHire] Hireable: {retinues.Count}, Renown: {Player.Renown}, Reserve: {_renownReserve}"
            );

            if (retinues.Count == 0)
            {
                _renownReserve = 0;
                return;
            }

            var renownGained = Player.Renown - _lastRenown;
            _lastRenown = Player.Renown;
            if (renownGained > 0)
                _renownReserve += renownGained;

            var wparty = new WParty(party);
            var space = Math.Max(0, wparty.PartySizeLimit - wparty.MemberRoster.Count);
            if (space <= 0)
                return;

            // Snapshot counts before adding, so the message reflects what actually stuck
            var before = new Dictionary<string, int>();
            foreach (var r in retinues)
                before[r.StringId] = wparty.MemberRoster.CountOf(r);

            // Attempt hires
            int attempts = rng.Next(10); // 0..9 attempts
            while (space > 0 && attempts-- > 0)
            {
                var retinue = retinues[rng.Next(retinues.Count)];
                if (GetCountOf(retinue) >= GetJoinCap(retinue))
                    continue;

                var cost = RetinueManager.ConversionRenownCostPerUnit(retinue);
                if (cost > _renownReserve)
                    continue;

                _renownReserve -= cost;
                wparty.MemberRoster.AddTroop(retinue, 1);
                space--;
            }

            // Compute effective deltas
            var deltas = new Dictionary<WCharacter, int>();
            foreach (var r in retinues)
            {
                var after = wparty.MemberRoster.CountOf(r);
                var prev = before.TryGetValue(r.StringId, out var b) ? b : 0;
                var delta = after - prev;
                if (delta > 0)
                    deltas[r] = delta;
            }

            if (deltas.Count > 0)
                ShowUnlockMessage(deltas);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int GetJoinCap(WCharacter troop)
        {
            if (troop == null || !troop.IsRetinue)
                return 0;
            if (Instance._caps.TryGetValue(troop.StringId, out var cap))
                return cap;
            return 0;
        }

        public static void SetJoinCap(WCharacter troop, int cap)
        {
            if (troop == null || !troop.IsRetinue)
                return;
            if (cap <= 0)
                Instance._caps.Remove(troop.StringId);
            else
                Instance._caps[troop.StringId] = cap;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static int GetCountOf(WCharacter troop)
        {
            if (troop == null || !troop.IsRetinue)
                return 0;
            var wparty = new WParty(MobileParty.MainParty);
            return wparty.MemberRoster.CountOf(troop);
        }

        private static List<WCharacter> GetHireableRetinues()
        {
            // Copy first to avoid mutating the live list
            var retinues = new List<WCharacter>(Player.Clan.RetinueTroops);

            if (Player.Kingdom != null)
                retinues.AddRange(Player.Kingdom.RetinueTroops);

            // De-dup on StringId
            var unique = retinues
                .Where(r => r != null)
                .GroupBy(r => r.StringId)
                .Select(g => g.First())
                .ToList();

            return [.. unique.Where(r => GetJoinCap(r) > GetCountOf(r))];
        }

        private static void ShowUnlockMessage(Dictionary<WCharacter, int> hires)
        {
            var joined = string.Join(
                ", ",
                hires.Where(h => h.Key != null).Select(h => $"{h.Value} {h.Key.Name}")
            );

            var description = L.T(
                    "retinue_hire_inquiry_body",
                    "The following retinues have joined your party: {JOINED}."
                )
                .SetTextVariable("JOINED", joined);

            Notifications.Log(description, "#c7f5caff");
        }
    }
}
