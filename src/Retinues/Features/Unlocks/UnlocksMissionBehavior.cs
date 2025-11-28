using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;

namespace Retinues.Features.Unlocks
{
    /// <summary>
    /// Mission behavior for unlocking items from kills at mission end.
    /// Applies doctrine modifiers and counts equipped items on defeated agents.
    /// </summary>
    [SafeClass]
    public class UnlocksMissionBehavior : Combat
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// On mission end, count unlockable items from valid kills and apply doctrine effects.
        /// </summary>
        protected override void OnEndMission()
        {
            if (Config.UnlockFromKills == false)
                return; // Unlocks from kills disabled

            if (Config.AllEquipmentUnlocked)
                return; // All equipment already unlocked

            if (IsDefeat)
                return; // No unlocks on defeat

            Log.Info("OnEndMission (victory): counting items for unlocks.");

            Dictionary<ItemObject, int> counts = [];

            foreach (var kill in Kills)
            {
                if (kill.VictimIsPlayerTroop || kill.VictimIsPlayer)
                    continue; // No unlock from player troop casualty

                if (kill.VictimIsAllyTroop)
                    if (!DoctrineAPI.IsDoctrineUnlocked<PragmaticScavengers>())
                        continue; // No unlock from ally casualty unless doctrine is enabled

                if (kill.KillerIsEnemyTroop)
                    continue; // Enemies don't unlock anything

                if (kill.KillerIsAllyTroop)
                    if (!DoctrineAPI.IsDoctrineUnlocked<BattlefieldTithes>())
                        continue; // No unlock from ally killers unless doctrine is enabled

                int unlockModifier = 1;

                if (DoctrineAPI.IsDoctrineUnlocked<LionsShare>())
                    if (kill.KillerIsPlayer)
                        unlockModifier = 2; // Double count if player personally landed the killing blow

                // Enumerate equipped items on the victim and add to the unlock counts
                foreach (var item in GetLoot(kill.LootCode))
                {
                    if (!IsUnlockable(item.Base))
                        continue;
                    counts[item.Base] = counts.TryGetValue(item.Base, out var c)
                        ? c + unlockModifier
                        : unlockModifier;
                }
            }

            // Add the counts to the owner's battle counts
            UnlocksBehavior.Instance.AddUnlockCounts(counts);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the item is unlockable (not null or invalid type).
        /// </summary>
        private static bool IsUnlockable(ItemObject i)
        {
            if (i == null)
                return false;
            if (i.ItemType == ItemObject.ItemTypeEnum.Invalid)
                return false;
            return true;
        }

        /// <summary>
        /// Gets the loot items from the given equipment.
        /// </summary>
        private static IEnumerable<WItem> GetLoot(string equipmentCode)
        {
            if (string.IsNullOrEmpty(equipmentCode))
                yield break;
            var equipment = Equipment.CreateFromEquipmentCode(equipmentCode);
            foreach (var slot in WEquipment.Slots)
                yield return new WItem(equipment[slot].Item);
        }
    }
}
