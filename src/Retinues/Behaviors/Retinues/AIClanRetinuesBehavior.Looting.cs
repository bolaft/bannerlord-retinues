using System;
using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Interface.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Behaviors.Retinues
{
    public sealed partial class AIClanRetinuesBehavior
    {
        /// <summary>
        /// When the player loses or retreats from a battle against AI clans that have retinues,
        /// each of those retinues may loot one item from player custom troop casualties —
        /// replacing the item in a random slot if the looted item has a higher tier.
        /// </summary>
        protected override void OnMissionEnded(MMission mission)
        {
            if (!Configuration.EnableAIClanRetinues)
            {
                Log.Debug("[AIClanRetinue.Loot] Skipped: EnableAIClanRetinues is false.");
                return;
            }

            var mapEvent = CombatBehavior.MapEvent;
            Log.Debug(
                $"[AIClanRetinue.Loot] MapEvent={mapEvent != null} IsLost={mapEvent?.IsLost} IsWon={mapEvent?.IsWon} HasWinner={mapEvent?.HasWinner} IsPlayerInvolved={mapEvent?.IsPlayerInvolved}"
            );

            // Only loot when the player did not win (includes defeats and retreats).
            if (mapEvent?.IsWon == true)
            {
                Log.Debug("[AIClanRetinue.Loot] Skipped: battle was won.");
                return;
            }

            var kills = CombatBehavior.GetKills();
            Log.Debug($"[AIClanRetinue.Loot] Total kills captured: {kills?.Count ?? 0}");

            if (kills == null || kills.Count == 0)
                return;

            // Collect AI clan retinues from enemy side parties.
            var snapshot = CombatBehavior.Snapshot;
            if (snapshot == null)
            {
                Log.Debug("[AIClanRetinue.Loot] Skipped: CombatBehavior.Snapshot is null.");
                return;
            }

            var enemySide = snapshot.EnemySide;
            if (enemySide == null)
            {
                Log.Debug("[AIClanRetinue.Loot] Skipped: enemy side snapshot is null.");
                return;
            }

            // Build a set of player-side party IDs from the start snapshot so kill resolution
            // doesn't depend on IsPlayerTroop (which can be false when PlayerSideEnum is None
            // at agent-removal time during a retreat).
            var playerPartyIds = new HashSet<string>(StringComparer.Ordinal);
            if (snapshot.PlayerSide?.PartyData != null)
            {
                foreach (var pd in snapshot.PlayerSide.PartyData)
                {
                    if (!string.IsNullOrEmpty(pd?.PartyId))
                        playerPartyIds.Add(pd.PartyId);
                }
            }

            Log.Debug(
                $"[AIClanRetinue.Loot] Player party IDs in snapshot: [{string.Join(", ", playerPartyIds)}]"
            );

            // Build loot pool: all items worn by player custom troop casualties.
            var lootPool = new List<WItem>();
            int playerTroopKills = 0;
            int customCharKills = 0;
            foreach (var kill in kills)
            {
                var victimPartyId = kill.Victim.PartyId;
                bool isPlayerSideCasualty =
                    kill.Victim.IsPlayerTroop
                    || kill.Victim.IsAllyTroop
                    || (
                        !string.IsNullOrEmpty(victimPartyId)
                        && playerPartyIds.Contains(victimPartyId)
                    );

                Log.Debug(
                    $"[AIClanRetinue.Loot] Kill victim={kill.Victim.CharacterId} party={victimPartyId} IsPlayerTroop={kill.Victim.IsPlayerTroop} IsAllyTroop={kill.Victim.IsAllyTroop} isPlayerSideCasualty={isPlayerSideCasualty}"
                );

                if (!isPlayerSideCasualty)
                    continue;
                playerTroopKills++;

                var victimChar = kill.Victim.Character;
                if (victimChar == null || !victimChar.IsCustom)
                    continue;
                customCharKills++;

                var eq = kill.VictimEquipment;
                if (eq == null)
                    continue;

                foreach (var item in eq.Items)
                {
                    if (item?.Base != null)
                        lootPool.Add(item);
                }
            }

            Log.Debug(
                $"[AIClanRetinue.Loot] Player troop kills: {playerTroopKills}, custom char kills: {customCharKills}, loot pool size: {lootPool.Count}"
            );

            if (lootPool.Count == 0)
            {
                Log.Debug("[AIClanRetinue.Loot] Skipped: loot pool is empty.");
                return;
            }

            Log.Debug(
                $"[AIClanRetinue.Loot] Enemy side parties: {enemySide.PartyData?.Count ?? 0}"
            );

            var retinuesToLoot = new List<WCharacter>();
            var seenClanIds = new HashSet<string>(StringComparer.Ordinal);
            string lootingClanName = null;

            foreach (var partyData in enemySide.PartyData)
            {
                var party = partyData?.Party;
                if (party?.Base == null)
                    continue;

                var rawClan = party.Base.ActualClan ?? party.Base.LeaderHero?.Clan;
                if (rawClan == null || rawClan == Clan.PlayerClan)
                    continue;

                var clan = WClan.Get(rawClan.StringId);
                if (clan?.Base == null || clan.IsEliminated || clan.IsBanditFaction)
                    continue;

                if (!seenClanIds.Add(clan.StringId))
                    continue;

                var rawRetinues = clan.GetRawRetinues();
                Log.Debug(
                    $"[AIClanRetinue.Loot] Clan '{clan.Name}' has {rawRetinues.Count} retinues."
                );

                if (rawRetinues.Count > 0)
                    lootingClanName ??= clan.Name;

                foreach (var retinue in rawRetinues)
                {
                    if (retinue?.Base != null)
                        retinuesToLoot.Add(retinue);
                }
            }

            Log.Debug($"[AIClanRetinue.Loot] Retinues eligible to loot: {retinuesToLoot.Count}");

            if (retinuesToLoot.Count == 0)
                return;

            // Give each retinue one loot opportunity; collect results for the popup.
            var lootResults = new List<(string RetinueName, string ItemName)>();
            foreach (var retinue in retinuesToLoot)
                TryLootItemForRetinue(retinue, lootPool, lootResults);

            Log.Debug($"[AIClanRetinue.Loot] Items looted: {lootResults.Count}");

            NotifyPlayerOfLooting(lootResults, lootingClanName);
        }

        /// <summary>
        /// Fires a post-battle popup listing every item the enemy retinues copied from player casualties.
        /// </summary>
        private static void NotifyPlayerOfLooting(
            List<(string RetinueName, string ItemName)> results,
            string clanName
        )
        {
            if (results == null || results.Count == 0)
                return;

            var title = L.T("ai_retinue_loot_title", "{CLAN} Looted Your Fallen")
                .SetTextVariable("CLAN", clanName ?? "Enemy");

            const int maxLines = 5;
            var take = Math.Min(maxLines, results.Count);
            var lines = new List<string>(take + 1);

            for (int i = 0; i < take; i++)
            {
                var (retinueName, itemName) = results[i];
                lines.Add(
                    L.T("ai_retinue_loot_line", "{RETINUE} looted and equipped {ITEM} from your casualties.")
                        .SetTextVariable("RETINUE", retinueName)
                        .SetTextVariable("ITEM", itemName)
                        .ToString()
                );
            }

            if (results.Count > take)
            {
                var more = results.Count - take;
                lines.Add(
                    L.T("ai_retinue_loot_more", "And {COUNT} more pieces of gear were taken.")
                        .SetTextVariable("COUNT", more)
                        .ToString()
                );
            }

            var desc = new TextObject(string.Join("\n\n", lines));
            Inquiries.Popup(title, desc, delayUntilOnWorldMap: true);
        }

        /// <summary>
        /// Attempts to equip one item from the loot pool onto the retinue's battle equipment,
        /// choosing a random slot where the looted item is a higher tier than what is currently worn.
        /// </summary>
        private void TryLootItemForRetinue(
            WCharacter retinue,
            List<WItem> lootPool,
            List<(string RetinueName, string ItemName)> results
        )
        {
            var battleSet = retinue.FirstBattleEquipment;
            if (battleSet == null)
                return;

            // Shuffle slots for random ordering.
            var slots = new List<EquipmentIndex>(UpgradeSlots);
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            foreach (var slot in slots)
            {
                var currentItem = battleSet.GetBase(slot);
                int currentTier = currentItem?.Tier ?? 0;

                // Find looted items that fit this slot and outrank the current item.
                List<WItem> candidates = null;
                foreach (var item in lootPool)
                {
                    if (!item.IsEquippableInSlot(slot))
                        continue;
                    if (item.Tier <= currentTier)
                        continue;

                    (candidates ??= []).Add(item);
                }

                if (candidates == null || candidates.Count == 0)
                    continue;

                var picked = candidates[_rng.Next(candidates.Count)];
                battleSet.Set(slot, picked);
                Log.Debug(
                    $"[AIClanRetinue] '{retinue.Name}' looted {slot}: {currentItem?.Name ?? "empty"} → {picked.Name} (T{picked.Tier})"
                );
                results.Add((retinue.Name, picked.Name));
                return;
            }
        }
    }
}
