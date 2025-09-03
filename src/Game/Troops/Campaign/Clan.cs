
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Game.Troops.Objects;

namespace CustomClanTroops.Game.Troops.Campaign
{
    public class TroopClan(Clan clan) : ClanWrapper(clan)
    {

        // =========================================================================
        // Troops
        // =========================================================================

        public List<TroopCharacter> EliteTroops { get; } = [];
        public List<TroopCharacter> BasicTroops { get; } = [];
    }
}
