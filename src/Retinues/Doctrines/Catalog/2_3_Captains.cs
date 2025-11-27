using Retinues.Doctrines.Model;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class Captains : Doctrine
    {
        public override TextObject Name => L.T("captains", "Captains");
        public override TextObject Description => L.T("captains_description", "Unlocks Captains.");
        public override int Column => 2;
        public override int Row => 3;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    }
}
