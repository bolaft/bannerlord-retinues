using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Features.Recruits
{
    public sealed class MilitiaSwapBehavior : CampaignBehaviorBase
    {
        // Cache replacement map per culture stringId to avoid recomputing every tick
        private readonly Dictionary<string, (CharacterObject melee, CharacterObject meleeElite, CharacterObject ranged, CharacterObject rangedElite)> _replCache
            = new(StringComparer.Ordinal);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, settlement => OnDailyTickSettlement(settlement));
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, (mapEvent, attackerParty, defenderParty) => OnMapEventStarted(mapEvent, attackerParty, defenderParty));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, _ => _replCache.Clear());
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, _ => _replCache.Clear());
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Daily Tick ━━━━━━ */

        private void OnDailyTickSettlement(Settlement settlement)
        {
            try
            {
                if (!ShouldCustomize(settlement)) return;

                var mp = settlement?.MilitiaPartyComponent?.MobileParty;
                if (mp == null) return;

                var repl = ResolvePlayerCultureMilitia();
                if (IsEmpty(repl)) return;

                SwapMilitiaInParty(mp, settlement.Culture, repl);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"MilitiaSwapBehavior.OnDailyTickSettlement @ {settlement?.Name}");
            }
        }

        /* ━━━━━━━ Map Event ━━━━━━ */

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                // Only care about raids/sieges/assaults where defenders can include militia
                var settlement = mapEvent?.MapEventSettlement;
                if (!ShouldCustomize(settlement)) return;

                var repl = ResolvePlayerCultureMilitia();
                if (IsEmpty(repl)) return;

                // Defenders side parties may include militia party and/or spawned defenders
                foreach (var sideParty in mapEvent?.DefenderSide?.Parties ?? Enumerable.Empty<MapEventParty>())
                {
                    var party = sideParty?.Party;
                    if (party?.MobileParty != null)
                    {
                        // Militia parties or settlement defenders (town/village)
                        SwapMilitiaInParty(party.MobileParty, settlement.Culture, repl);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "MilitiaSwapBehavior.OnMapEventStarted");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Swap                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool ShouldCustomize(Settlement settlement)
        {
            if (settlement?.OwnerClan == null) return false;
            var playerKingdom = Clan.PlayerClan?.Kingdom;
            if (playerKingdom == null) return false;
            return settlement.OwnerClan.Kingdom == playerKingdom;
        }

        // Build the replacement set from the player's culture custom militia
        private (CharacterObject melee, CharacterObject meleeElite, CharacterObject ranged, CharacterObject rangedElite) ResolvePlayerCultureMilitia()
        {
            var playerCulture = Clan.PlayerClan?.Culture;
            if (playerCulture == null) return default;

            var cid = playerCulture.StringId;
            if (_replCache.TryGetValue(cid, out var cached))
                return cached;

            var wc = new WCulture(playerCulture);

            CharacterObject melee       = TryActive(wc.MilitiaMelee);
            CharacterObject meleeElite  = TryActive(wc.MilitiaMeleeElite);
            CharacterObject ranged      = TryActive(wc.MilitiaRanged);
            CharacterObject rangedElite = TryActive(wc.MilitiaRangedElite);

            // Cache, even if some are null (so we don't recompute constantly)
            var tuple = (melee, meleeElite, ranged, rangedElite);
            _replCache[cid] = tuple;
            return tuple;

            static CharacterObject TryActive(WCharacter w)
            {
                if (w == null) return null;
                if (!w.IsCustom) return null;
                if (!w.IsActive) return null;
                return w.Base;
            }
        }

        private static bool IsEmpty((CharacterObject melee, CharacterObject meleeElite, CharacterObject ranged, CharacterObject rangedElite) set)
            => set.melee == null && set.meleeElite == null && set.ranged == null && set.rangedElite == null;

        private static (CharacterObject melee, CharacterObject meleeElite, CharacterObject ranged, CharacterObject rangedElite) VanillaMilitia(CultureObject culture)
        {
            return (
                culture?.MeleeMilitiaTroop,
                culture?.MeleeEliteMilitiaTroop,
                culture?.RangedMilitiaTroop,
                culture?.RangedEliteMilitiaTroop
            );
        }

        private static void SwapMilitiaInParty(MobileParty mp, CultureObject settlementCulture,
            (CharacterObject melee, CharacterObject meleeElite, CharacterObject ranged, CharacterObject rangedElite) repl)
        {
            if (mp?.Party == null || settlementCulture == null) return;

            var vanilla = VanillaMilitia(settlementCulture);

            // If settlement culture is same as player's culture and replacements happen to be the same object, nothing to do.
            bool anyTarget = vanilla.melee != null || vanilla.meleeElite != null || vanilla.ranged != null || vanilla.rangedElite != null;
            if (!anyTarget) return;

            // Replace in MemberRoster
            ReplaceInRoster(mp.MemberRoster, vanilla, repl);

            // Replace in PrisonRoster
            ReplaceInRoster(mp.PrisonRoster, vanilla, repl);
        }

        private static void ReplaceInRoster(TroopRoster roster,
            (CharacterObject melee, CharacterObject meleeElite, CharacterObject ranged, CharacterObject rangedElite) vanilla,
            (CharacterObject melee, CharacterObject meleeElite, CharacterObject ranged, CharacterObject rangedElite) repl)
        {
            if (roster == null || roster.Count == 0) return;

            // Collect replacements to apply (in deltas); avoid touching wounded counts.
            var toApply = new List<(CharacterObject from, CharacterObject to, int count)>();

            // Snapshot
            var list = roster.GetTroopRoster();
            for (int i = 0; i < list.Count; i++)
            {
                TroopRosterElement e;
                try { e = list[i]; }
                catch { continue; }

                if (e.Character == null || e.Number <= 0) continue;

                CharacterObject targetReplacement = null;

                if (e.Character == vanilla.melee && repl.melee != null && repl.melee.StringId != e.Character.StringId)
                    targetReplacement = repl.melee;
                else if (e.Character == vanilla.meleeElite && repl.meleeElite != null && repl.meleeElite.StringId != e.Character.StringId)
                    targetReplacement = repl.meleeElite;
                else if (e.Character == vanilla.ranged && repl.ranged != null && repl.ranged.StringId != e.Character.StringId)
                    targetReplacement = repl.ranged;
                else if (e.Character == vanilla.rangedElite && repl.rangedElite != null && repl.rangedElite.StringId != e.Character.StringId)
                    targetReplacement = repl.rangedElite;

                if (targetReplacement != null)
                    toApply.Add((e.Character, targetReplacement, e.Number));
            }

            if (toApply.Count == 0) return;

            // Apply via safe deltas
            foreach (var (from, to, count) in toApply)
            {
                try
                {
                    roster.AddToCounts(to, +count);
                    roster.AddToCounts(from, -count);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"MilitiaSwap ReplaceInRoster {from?.Name} -> {to?.Name} x{count}");
                }
            }
        }
    }
}
