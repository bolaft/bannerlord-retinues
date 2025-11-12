using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Helpers
{
    public static class AgentHelper
    {
        /// <summary>
        /// Extract a WCharacter from the given AgentBuildData.
        /// </summary>
        public static WCharacter TroopFromAgentBuildData(AgentBuildData data, bool origin = false)
        {
            if (data == null)
                return null;

            var character = origin ? data.AgentOrigin?.Troop : data.AgentCharacter;

            if (character is not CharacterObject co)
                return null;

            return new WCharacter(co);
        }

        /// <summary>
        /// Create a new AgentBuildData using and copy all relevant fields from the source,
        /// but replacing the CharacterObject with the provided replacement.
        /// </summary>
        public static AgentBuildData ReplaceCharacterInBuildData(
            AgentBuildData src,
            CharacterObject replacement
        )
        {
            var dst = new AgentBuildData(replacement)
                // controller, team, formation basics
                .Controller(src.AgentController)
                .Team(src.AgentTeam)
                .Formation(src.AgentFormation)
                // visuals / monster
                .Monster(src.AgentMonster)
                .VisualsIndex(src.AgentVisualsIndex)
                // equipment flags & seed (copied below with conditionals)
                .EquipmentSeed(src.AgentEquipmentSeed)
                .NoHorses(src.AgentNoHorses)
                .NoWeapons(src.AgentNoWeapons)
                .NoArmor(src.AgentNoArmor)
                // colors & body
                .ClothingColor1(src.AgentClothingColor1)
                .ClothingColor2(src.AgentClothingColor2)
                .IsFemale(src.AgentIsFemale)
                .Race(src.AgentRace)
                // origin
                .TroopOrigin(src.AgentOrigin);

            if (src.AgentInitialDirection != null)
                dst = dst.InitialDirection((Vec2)src.AgentInitialDirection);

            if (src.AgentInitialPosition != null)
                dst = dst.InitialPosition((Vec3)src.AgentInitialPosition);

            // Body properties & age, if they were overridden on the source
            if (src.BodyPropertiesOverriden)
                dst.BodyProperties(src.AgentBodyProperties);
            if (src.AgeOverriden)
                dst.Age(src.AgentAge);

            // Equipment handling:
            // - If the source had fixed overridden equipment, copy it exactly.
            // - Else if it used civilian equipment, keep that flag.
            // - Otherwise let the engine pick from the replacement character's sets.
            if (src.AgentFixedEquipment && src.AgentOverridenSpawnEquipment != null)
            {
                dst.Equipment(src.AgentOverridenSpawnEquipment).FixedEquipment(true);
            }
            else if (src.AgentCivilianEquipment)
            {
                dst.CivilianEquipment(true);
            }

            // Banner & formation spawn indexing (copy if present)
            if (src.AgentBanner != null)
                dst.Banner(src.AgentBanner);
            if (src.AgentBannerItem != null)
                dst.BannerItem(src.AgentBannerItem);
            if (src.AgentBannerReplacementWeaponItem != null)
                dst.BannerReplacementWeaponItem(src.AgentBannerReplacementWeaponItem);

            if (src.AgentIndexOverriden)
                dst.Index(src.AgentIndex);
            if (src.AgentMountIndexOverriden)
                dst.MountIndex(src.AgentMountIndex);

            dst.FormationTroopSpawnCount(src.AgentFormationTroopSpawnCount)
                .FormationTroopSpawnIndex(src.AgentFormationTroopSpawnIndex)
                .CanSpawnOutsideOfMissionBoundary(src.AgentCanSpawnOutsideOfMissionBoundary);

            // Optional: keep spawn/own-formation flags if you rely on them in battles
            if (src.AgentSpawnsIntoOwnFormation)
                dst.SpawnsIntoOwnFormation(true);
            if (src.AgentSpawnsUsingOwnTroopClass)
                dst.SpawnsUsingOwnTroopClass(true);

            return dst;
        }
    }
}
