using System.Collections.Generic;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Features.Unlocks.Behaviors
{
    [SafeClass]
    public sealed class UnlocksMissionBehavior(UnlocksBehavior owner) : Combat
    {
        private readonly UnlocksBehavior _owner = owner;

        protected override void OnEndMission()
        {
            if (IsDefeat)
                return; // No unlocks on defeat

            Log.Info("OnEndMission (victory): counting items for unlocks.");

            Dictionary<ItemObject, int> counts = [];

            foreach (var kill in Kills)
            {
                if (kill.Victim.IsPlayerTroop || kill.Victim.IsPlayer)
                    continue; // No unlock from player troop casualty

                if (kill.Victim.IsAllyTroop)
                    if (!DoctrineAPI.IsDoctrineUnlocked<PragmaticScavengers>())
                        continue; // No unlock from ally casualty unless doctrine is enabled

                if (kill.Killer.IsEnemyTroop)
                    continue; // Enemies don't unlock anything

                if (kill.Killer.IsAllyTroop)
                    if (!DoctrineAPI.IsDoctrineUnlocked<BattlefieldTithes>())
                        continue; // No unlock from ally killers unless doctrine is enabled

                int unlockModifier = 1;

                if (DoctrineAPI.IsDoctrineUnlocked<LionsShare>())
                    if (kill.Killer.IsPlayer)
                        unlockModifier = 2; // Double count if player personally landed the killing blow

                // Enumerate equipped items on the victim and add to the unlock counts
                foreach (var item in EnumerateEquippedItems(kill.Victim.Agent))
                {
                    if (!IsUnlockable(item.Base))
                        continue;
                    counts[item.Base] = counts.TryGetValue(item.Base, out var c)
                        ? c + unlockModifier
                        : unlockModifier;
                }
            }
            // Add the counts to the owner's battle counts
            _owner.AddBattleCounts(counts);
        }

        private static bool IsUnlockable(ItemObject i)
        {
            if (i == null)
                return false;
            if (i.ItemType == ItemObject.ItemTypeEnum.Invalid)
                return false;
            return true;
        }

        private static IEnumerable<WItem> EnumerateEquippedItems(Agent agent)
        {
            var eq = new WEquipment(agent?.SpawnEquipment);

            if (eq.Base == null)
                yield break;

            foreach (var item in eq.Items)
                if (item != null)
                    yield return item;
        }
    }
}
