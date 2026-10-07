#if BL12
using HarmonyLib;
using Retinues.Framework.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Framework.Behaviors
{
    /// <summary>
    /// Bannerlord 1.2 has no party-added campaign event. Emit the equivalent notification
    /// after a party joins a battle side, including when the player joins an existing battle.
    /// </summary>
    [HarmonyPatch(typeof(MapEventSide), "AddPartyInternal")]
    internal static class LegacyMapEventEvents
    {
        internal static MbEvent<PartyBase> PartyAdded { get; private set; } = new();

        [StaticClearAction]
        internal static void Reset() => PartyAdded = new();

        [HarmonyPostfix]
        internal static void OnPartyAdded(PartyBase party)
        {
            if (party != null)
                PartyAdded.Invoke(party);
        }
    }
}
#endif
