using HarmonyLib;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Retinues.Core.Safety.Patches
{
    /// <summary>
    /// Patch for TroopRoster.RemoveTroop to prevent errors when removing a troop not present in the roster.
    /// Logs an error and skips removal if the troop is missing.
    /// </summary>
    [HarmonyPatch(typeof(TroopRoster), nameof(TroopRoster.RemoveTroop))]
    static class SafeRemoveTroopPatch
    {
        [SafeMethod]
        static bool Prefix(
            TroopRoster __instance,
            CharacterObject troop,
            int numberToRemove,
            UniqueTroopDescriptor troopSeed,
            int xp
        )
        {
            int idx = __instance.FindIndexOfTroop(troop);
            if (idx < 0)
            {
                Log.Error($"Tried to remove {troop?.StringId ?? "NULL"} not in roster. ");
                return false;
            }
            return true; // proceed normally
        }
    }

    /// <summary>
    /// Patch for TeleportHeroAction.ApplyInternal to ensure the hero is present in the source party before teleporting.
    /// Re-adds the hero if missing and logs a warning.
    /// </summary>
    [HarmonyPatch(typeof(TeleportHeroAction), "ApplyInternal")]
    static class TeleportHero_PreparePatch
    {
        [SafeMethod]
        static void Prefix(Hero hero, Settlement targetSettlement, MobileParty targetParty)
        {
            try
            {
                var src = hero?.PartyBelongedTo;
                if (src?.MemberRoster != null)
                {
                    int idx = src.MemberRoster.FindIndexOfTroop(hero.CharacterObject);
                    if (idx < 0)
                    {
                        src.MemberRoster.AddToCounts(hero.CharacterObject, +1, insertAtFront: true);
                        Log.Warn($"Re-added {hero?.Name} to {src?.Name} before teleport.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex, "Prefix failed");
            }
        }
    }
}
