using TaleWorlds.SaveSystem;

namespace Retinues.Core.Persistence.Troop
{
    public class RosterSaveData
    {
        // Party that owns the roster (MobileParty.StringId)
        [SaveableField(1)] public string PartyId;

        // "Member" or "Prison"
        [SaveableField(2)] public string RosterKind;

        // Vanilla source id of the custom clone at this tier
        [SaveableField(3)] public string VanillaStringId;

        // Counts
        [SaveableField(4)] public int Healthy;
        [SaveableField(5)] public int Wounded;

        // Best-effort XP preservation for the stack
        [SaveableField(6)] public int Xp;

        // Distinguish player clan vs player kingdom clones if both exist for same vanilla id
        [SaveableField(7)] public bool IsKingdom;

        // Distinguish retinue troops from non-retinue troops
        [SaveableField(8)] public bool IsRetinue;
    }
}
