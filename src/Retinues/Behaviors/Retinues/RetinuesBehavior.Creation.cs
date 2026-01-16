using System.Linq;
using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Game.Troops;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Game.Retinues
{
    public partial class RetinuesBehavior
    {
        /// <summary>
        /// Ensures the given clan has a default retinue; creates one if none exist.
        /// </summary>
        public WCharacter EnsureDefaultRetinue(WClan clan, string name, bool notifyUnlocks = true)
        {
            if (!Settings.EnableRetinues)
                return null;

            if (clan?.Base == null)
                return null;

            if (!clan.RosterRetinues.IsEmpty())
                return null;

            var retinue = CreateRetinue(clan.Culture, name, notifyUnlocks);
            if (retinue?.Base == null)
                return null;

            clan.AddRetinue(retinue);
            return retinue;
        }

        /// <summary>
        /// Ensures the given culture has a retinue for the player clan; creates one if none exist.
        /// </summary>
        private WCharacter EnsureRetinueExistsForCulture(WCulture culture)
        {
            if (culture?.Base == null)
                return null;

            var clan = Player.Clan;
            if (clan?.Base == null)
                return null;

            var existing = clan.RosterRetinues.FirstOrDefault(r => r.Culture == culture);
            if (existing?.Base != null)
                return existing;

            var name = L.T("retinue_culture_default_name", "{CLAN} {CULTURE} Retinue")
                .SetTextVariable("CLAN", clan.Name)
                .SetTextVariable("CULTURE", culture.Name)
                .ToString();

            var created = CreateRetinue(culture, name, notifyUnlocks: false);
            if (created?.Base == null)
                return null;

            clan.AddRetinue(created);
            return created;
        }

        /// <summary>
        /// Creates a new retinue character based on the given culture.
        /// </summary>
        public WCharacter CreateRetinue(WCulture culture, string name, bool notifyUnlocks = true)
        {
            if (!Settings.EnableRetinues)
                return null;

            var template = culture?.RootElite ?? culture?.RootBasic;
            if (template?.Base == null)
                return null;

            return Cloner.BuildFromTemplate(
                template,
                new Cloner.TroopBuildRequest
                {
                    Name = name,
                    CultureContext = culture,
                    CopySkills = true,
                    CreateCivilianSet = true,
                    NotifyUnlocks = notifyUnlocks,
                }
            );
        }
    }
}
