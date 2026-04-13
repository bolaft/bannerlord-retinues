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
                return;

            // Only loot on player loss/retreat.
            if (CombatBehavior.MapEvent?.IsLost != true)
                return;

            var kills = CombatBehavior.GetKills();
            if (kills == null || kills.Count == 0)
                return;

            // Build loot pool: all items worn by player custom troop casualties.
            var lootPool = new List<WItem>();
            foreach (var kill in kills)
            {
                if (!kill.Victim.IsPlayerTroop)
                    continue;

                var victimChar = kill.Victim.Character;
                if (victimChar == null || !victimChar.IsCustom)
                    continue;

                var eq = kill.VictimEquipment;
                if (eq == null)
                    continue;

                foreach (var item in eq.Items)
                {
                    if (item?.Base != null)
                        lootPool.Add(item);
                }
            }

            if (lootPool.Count == 0)
                return;

            // Collect AI clan retinues from enemy side parties.
            var snapshot = CombatBehavior.Snapshot;
            if (snapshot == null)
                return;

            var enemySide = snapshot.EnemySide;
            if (enemySide == null)
                return;

            var retinuesToLoot = new List<WCharacter>();
            var seenClanIds = new HashSet<string>(StringComparer.Ordinal);

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

                foreach (var retinue in clan.GetRawRetinues())
                {
                    if (retinue?.Base != null)
                        retinuesToLoot.Add(retinue);
                }
            }

            if (retinuesToLoot.Count == 0)
                return;

            // Give each retinue one loot opportunity; collect results for the popup.
            var lootResults = new List<(string RetinueName, string ItemName)>();
            foreach (var retinue in retinuesToLoot)
                TryLootItemForRetinue(retinue, lootPool, lootResults);

            NotifyPlayerOfLooting(lootResults);
        }

        /// <summary>
        /// Fires a post-battle popup listing every item the enemy retinues copied from player casualties.
        /// </summary>
        private static void NotifyPlayerOfLooting(
            List<(string RetinueName, string ItemName)> results
        )
        {
            if (results == null || results.Count == 0)
                return;

            var title = L.T("ai_retinue_loot_title", "Enemy Retinues Looted Your Fallen");

            const int maxLines = 5;
            var take = Math.Min(maxLines, results.Count);
            var lines = new List<string>(take + 1);

            for (int i = 0; i < take; i++)
            {
                var (retinueName, itemName) = results[i];
                lines.Add(
                    L.T("ai_retinue_loot_line", "{RETINUE} equipped {ITEM} from your casualties.")
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
