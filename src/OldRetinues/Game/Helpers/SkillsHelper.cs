using System.Collections.Generic;
using Retinues.Utils;

namespace Retinues.Game.Helpers
{
    [SafeClass]
    public static class SkillsHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly HashSet<string> VanillaSkillIds =
        [
            "OneHanded",
            "TwoHanded",
            "Polearm",
            "Bow",
            "Crossbow",
            "Throwing",
            "Riding",
            "Athletics",
            "Crafting",
            "Tactics",
            "Scouting",
            "Roguery",
            "Charm",
            "Leadership",
            "Trade",
            "Steward",
            "Medicine",
            "Engineering",
        ];

        public static readonly HashSet<string> NavalDLCSkillIds =
        [
            "Mariner",
            "Boatswain",
            "Shipmaster",
        ];
    }
}
