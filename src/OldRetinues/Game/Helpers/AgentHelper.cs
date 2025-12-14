using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace OldRetinues.Game.Helpers
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

            // Second attempt: try the other one
            character ??= origin ? data.AgentCharacter : data.AgentOrigin?.Troop;

            if (character is not CharacterObject co)
                return null;

            return new WCharacter(co);
        }
    }
}
