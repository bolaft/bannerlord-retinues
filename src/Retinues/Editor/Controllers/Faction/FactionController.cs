using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Model.Exports;
using Retinues.UI.Services;

namespace Retinues.Editor.Controllers.Faction
{
    public class FactionController : BaseController
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

        public static void ExportFaction()
        {
            var f = State.Faction;
            if (f == null)
            {
                Notifications.Message("No faction selected.");
                return;
            }

            if (string.IsNullOrWhiteSpace(f.StringId))
            {
                Notifications.Message("Selected faction has no StringId.");
                return;
            }

            MImportExport.ExportFaction(f.StringId);
        }
    }
}
