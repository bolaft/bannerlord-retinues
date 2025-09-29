using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;
using Retinues.Core.Utils;

[HarmonyPatch(typeof(TroopRoster), nameof(TroopRoster.RemoveTroop))]
static class SafeRemoveTroopPatch
{
    static bool Prefix(TroopRoster __instance, CharacterObject troop, int numberToRemove,
                       UniqueTroopDescriptor troopSeed, int xp)
    {
        int idx = __instance.FindIndexOfTroop(troop);
        if (idx < 0)
        {
            Log.Error($"[SafeRemoveTroop] Tried to remove {troop?.StringId ?? "NULL"} not in roster. ");
            // Cancel original to avoid AddToCountsAtIndex(-1, …) crash
            return false;
        }
        return true; // proceed normally
    }
}

[HarmonyPatch(typeof(TeleportHeroAction), "ApplyInternal")]
static class TeleportHero_PreparePatch
{
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
                    Log.Warn($"[TeleportPrep] Re-added {hero?.Name} to {src.Name} before teleport.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Exception(ex, "[TeleportPrep] Prefix failed");
        }
    }
}
