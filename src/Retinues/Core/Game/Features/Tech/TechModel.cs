using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Features.Tech
{
    public enum DoctrineStatus
    {
        Locked,       // prerequisite doctrine and/or feat not fulfilled
        Available,    // can pay cost & start research
        Researching,  // a companion is assigned; finishes at EndTime
        Completed     // unlocked
    }

    public sealed class DoctrineDef
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconId { get; set; } // you can map to your sprite/brush
        public int Column { get; set; }     // 0..3
        public int Row { get; set; }        // 0..3
        public int GoldCost { get; set; }
        public CampaignTime Duration { get; set; }  // research time
        public string PrerequisiteId { get; set; }  // doctrine above in the column
        public string FeatId { get; set; }          // e.g., "defeat_king_party"
        public SkillObject RequiredSkill { get; set; }   // skill checked on assigned companion
        public int RequiredSkillValue { get; set; }      // minimum skill value
    }

    public sealed class DoctrineState
    {
        public string Id;               // matches DoctrineDef.Id
        public DoctrineStatus Status;   // current status
        public string AssignedHeroId;   // when Researching
        public CampaignTime StartTime;  // when Researching
        public CampaignTime EndTime;    // when Researching
    }
}
