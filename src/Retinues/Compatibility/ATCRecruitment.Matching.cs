using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Services.Matching;
using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Compatibility
{
    internal static partial class ATCRecruitment
    {
        internal static WCharacter MatchVolunteer(WCharacter source, IEnumerable<WCharacter> candidates,
            ATCRecruitRole role)
        {
            if (role != ATCRecruitRole.Basic && role != ATCRecruitRole.Elite)
                return null;
            // A proven ATC volunteer may also be a militia or have no canonical
            // culture-tree role. Only its recruitment role overrides that classification.
            var eligible = candidates?.Where(t => t != null && !t.IsHero && !t.IsRetinue
                && t.IsRegular && t.IsElite == (role == ATCRecruitRole.Elite));
            return CharacterMatcher.FindBest(source, eligible, strictCategoryMatch: false);
        }
    }
}
