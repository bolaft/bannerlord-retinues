using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Editor
{
    /// <summary>
    /// Centralized gating for opening the Player editor from the map bar.
    /// </summary>
    public static class EditorAvailability
    {
        /// <summary>
        /// True if this map-faction has any custom-tree troops configured.
        /// Custom-tree sources are: RootBasic, RootElite, and Retinues.
        /// </summary>
        public static bool HasAnyCustomTreeTroops(IBaseFaction faction)
        {
            if (faction == null)
                return false;

            if (faction.RootBasic != null)
                return true;

            if (faction.RootElite != null)
                return true;

            var retinues = faction.RosterRetinues;
            return retinues != null && retinues.Count > 0;
        }

        /// <summary>
        /// True if the current player clan has any custom-tree troops.
        /// </summary>
        public static bool HasPlayerClanCustomTreeTroops()
        {
            try
            {
                var hero = Hero.MainHero;
                if (hero == null)
                    return false;

                var clan = WClan.Get(hero.Clan);
                return HasAnyCustomTreeTroops(clan);
            }
            catch (System.Exception e)
            {
                Log.Exception(e, "Failed to evaluate editor availability.");
                return false;
            }
        }
    }
}
