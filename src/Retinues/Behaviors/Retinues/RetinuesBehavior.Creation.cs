using System.Linq;
using Retinues.Behaviors.Troops;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Interface.Services;
using Retinues.Settings;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Retinues
{
    public partial class RetinuesBehavior
    {
        /// <summary>
        /// Ensures the given clan has a default retinue; creates one if none exist.
        /// </summary>
        public WCharacter EnsureDefaultRetinue(WClan clan, string name, bool notifyUnlocks = true)
        {
            if (!Configuration.EnableRetinues)
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
        /// Ensures the given kingdom has a default retinue; creates one if none exist.
        /// </summary>
        public WCharacter EnsureDefaultRetinue(
            WKingdom kingdom,
            string name,
            bool notifyUnlocks = true
        )
        {
            if (!Configuration.EnableRetinues)
                return null;

            if (kingdom?.Base == null)
                return null;

            if (!kingdom.RosterRetinues.IsEmpty())
                return null;

            var retinue = CreateRetinue(kingdom.Culture, name, notifyUnlocks);
            if (retinue?.Base == null)
                return null;

            kingdom.AddRetinue(retinue);
            return retinue;
        }

        /// <summary>
        /// Ensures the player's kingdom has a default retinue when the player is a ruler.
        /// </summary>
        private void EnsureDefaultRetinueForPlayerKingdom()
        {
            if (!Configuration.EnableRetinues)
                return;

            if (!Player.IsRuler)
                return;

            var kingdom = Player.Kingdom;
            if (kingdom?.Base == null)
                return;

            if (!kingdom.RosterRetinues.IsEmpty())
                return;

            var name = Player.IsFemale
                ? L.T("retinue_kingdom_default_name_female", "{KINGDOM} Queen's Guard")
                    .SetTextVariable("KINGDOM", kingdom.Name)
                    .ToString()
                : L.T("retinue_kingdom_default_name_male", "{KINGDOM} King's Guard")
                    .SetTextVariable("KINGDOM", kingdom.Name)
                    .ToString();

            EnsureDefaultRetinue(kingdom, name, notifyUnlocks: false);
        }

        /// <summary>
        /// Creates a new retinue character based on the given culture.
        /// </summary>
        public WCharacter CreateRetinue(WCulture culture, string name, bool notifyUnlocks = true)
        {
            if (!Configuration.EnableRetinues)
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
                    TargetLevel = GetRetinueLevelOverride(template),
                }
            );
        }

        // A retinue must be at least this tier. Conversion matches a source exactly one tier below,
        // so a tier 0 retinue looks for tier -1 and can never be recruited.
        private const int MinRetinueTier = 1;

        /// <summary>
        /// Returns a level override that keeps a new retinue at or above <see cref="MinRetinueTier"/>,
        /// or 0 to keep the template's own level.
        ///
        /// Retinues inherit the level of the culture's elite root, which troop overhauls are free to
        /// redefine. Some use a very low-level unit there (Warlords Battlefield's Vlandian noble is
        /// level 3), which produced a tier 0 retinue that no troop could ever convert into. Only the
        /// degenerate case is corrected; any template at tier 1 or above is still inherited as-is.
        /// </summary>
        private static int GetRetinueLevelOverride(WCharacter template)
        {
            if (template?.Base == null)
                return 0;

            if (template.Tier >= MinRetinueTier)
                return 0; // Inherit the template level.

            // Mid-range level of the target tier, matching the editor's "tier * 5 + 8" convention.
            return (MinRetinueTier - 1) * 5 + 8;
        }
    }
}
