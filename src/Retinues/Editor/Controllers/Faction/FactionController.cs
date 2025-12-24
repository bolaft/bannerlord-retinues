using Retinues.Model.Factions;

namespace Retinues.Editor.Controllers.Faction
{
    public class FactionController : EditorController
    {
        /// <summary>
        /// Updates the selected culture.
        /// </summary>
        public static void SelectCulture(WCulture culture)
        {
            if (culture == null)
                return;

            if (State.Faction is WCulture c && culture == c)
                return;

            State.Clan = null;

            if (State.Culture != culture)
                State.Culture = culture;

            State.Faction = culture;
        }

        /// <summary>
        /// Updates the selected clan.
        /// </summary>
        public static void SelectClan(WClan clan)
        {
            if (clan == null)
                return;

            if (State.Faction is WClan c && clan == c)
                return;

            if (State.Culture != clan.Culture)
                State.Culture = clan.Culture;

            State.Clan = clan;
            State.Faction = clan;
        }
    }
}
