using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;

namespace Retinues.Compatibility
{
    internal static partial class ATCRecruitment
    {
        internal static WCharacter GetSource(WCharacter troop, Settlement settlement, Hero notable, out ATCRecruitRole role)
        {
            role = GetRole(troop?.Base, settlement, notable);
            if (role != ATCRecruitRole.None || troop == null || !troop.IsCustom || troop.IsHero)
                return troop;
            // A previous version may already have replaced an ATC offer with custom
            // militia. Repair only a known militia slot with a recorded original;
            // ordinary custom troops and actual militia rosters are left alone.
            var faction = troop.Faction;
            if (faction == null || (troop != faction.MilitiaMelee && troop != faction.MilitiaMeleeElite
                && troop != faction.MilitiaRanged && troop != faction.MilitiaRangedElite))
                return troop;
            var culture = troop.Base.Culture;
            if (culture == null || troop.VanillaStringId == troop.StringId)
                return troop;
            var original = new[] { culture.MeleeMilitiaTroop, culture.RangedMilitiaTroop,
                culture.MeleeEliteMilitiaTroop, culture.RangedEliteMilitiaTroop }
                .FirstOrDefault(t => t != null && t.StringId == troop.VanillaStringId);
            role = GetRole(original, settlement, notable);
            return role == ATCRecruitRole.None ? troop : new WCharacter(original);
        }
        internal static WCharacter MatchVolunteer(BaseFaction faction, WCharacter source, ATCRecruitRole role)
        {
            if (role != ATCRecruitRole.Basic && role != ATCRecruitRole.Elite)
                return source;
            var root = role == ATCRecruitRole.Elite ? faction?.RootElite : faction?.RootBasic;
            var result = TroopMatcher.PickBestFromTree(root, source, sameTierOnly: false);
            // Preserve the existing stable tier-matching policy and locked-tree behavior.
            return result?.IsCustom == true ? result : source;
        }
    }
}
