using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Features.Retinues.Behaviors
{
    /// <summary>
    /// Tracks per-retinue hire caps, accumulates renown toward the next hire,
    /// and spawns queued retinues into the main party on the daily tick.
    /// </summary>
    [SafeClass]
    public class RetinueHireBehavior : CampaignBehaviorBase
    {
        private static readonly Random rng = new();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static Dictionary<string, int> _caps = [];
        private static float _renownReserve;
        private static float _lastRenown;

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
                return; // Only care about main party

            var retinues = GetHireableRetinues();

            Log.Info(
                $"[RetinueHire] Hireable: {retinues.Count}, Renown: {Player.Renown}, Reserve: {_renownReserve}"
            );

            // Check if any retinues need hiring
            if (retinues.Count == 0)
            {
                _renownReserve = 0;
                return; // Nothing to do today
            }

            var renownGained = Player.Renown - _lastRenown;
            _lastRenown = Player.Renown;

            if (renownGained > 0)
                _renownReserve += renownGained;

            var wparty = new WParty(party);
            var space = Math.Max(0, wparty.PartySizeLimit - wparty.MemberRoster.Count);

            if (space <= 0)
                return; // No space today

            var hires = new Dictionary<WCharacter, int>();
            int i = rng.Next(10); // Up to 10 attempts per day to hire retinues

            while (space > 0 && i-- > 0)
            {
                int idx = rng.Next(retinues.Count);
                var retinue = retinues[idx];

                if (GetCountOf(retinue) >= GetRetinueCap(retinue))
                    continue; // Already at cap

                var cost = TroopRules.ConversionRenownCostPerUnit(retinue);

                if (cost > _renownReserve)
                    continue; // Can't afford this one yet

                _renownReserve -= cost;
                wparty.MemberRoster.AddTroop(retinue, 1);

                if (!hires.ContainsKey(retinue))
                    hires[retinue] = 0;
                hires[retinue]++;

                space--;
            }

            if (hires.Count > 0)
                ShowUnlockInquiry(hires);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int GetRetinueCap(WCharacter troop)
        {
            if (troop == null || !troop.IsRetinue)
                return 0;
            if (_caps.TryGetValue(troop.StringId, out var cap))
                return cap;
            return 0;
        }

        public static void SetRetinueCap(WCharacter troop, int cap)
        {
            if (troop == null || !troop.IsRetinue)
                return;
            if (cap <= 0)
                _caps.Remove(troop.StringId);
            else
                _caps[troop.StringId] = cap;
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
            List<WCharacter> retinues = Player.Clan.RetinueTroops;

            if (Player.Kingdom != null)
                retinues.AddRange(Player.Kingdom.RetinueTroops);

            return [.. retinues.Where(r => GetRetinueCap(r) > GetCountOf(r))];
        }

        private static void ShowUnlockInquiry(Dictionary<WCharacter, int> hires)
        {
            var joinedLines = string.Join(
                "\n",
                hires.Where(h => h.Key != null).Select(h => $"{h.Value} {h.Key.Name}")
            );

            var body = L.T(
                    "retinue_hire_inquiry_body",
                    "The following retinues have joined your party:\n\n{JOINED_LINES}"
                )
                .SetTextVariable("JOINED_LINES", joinedLines);

            Popup.Display(L.T("retinues_joined", "New Retinues Joined"), body);
        }
    }
}
