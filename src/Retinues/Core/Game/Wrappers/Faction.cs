using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Game.Wrappers
{
    public class WFaction(IFaction faction) : StringIdentifier
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly IFaction _faction = faction;

        public IFaction Base => _faction;

        // =========================================================================
        // Properties
        // =========================================================================

        public string Name => _faction.Name.ToString();

        public override string StringId => _faction.StringId;

        public string BannerCodeText => _faction.Banner.Serialize();

        public uint Color => _faction.Color;

        public uint Color2 => _faction.Color2;

        public WCulture Culture => new(_faction.Culture);

        public IReadOnlyList<Settlement> Fiefs =>
            _faction.Settlements.Where(s => s.IsTown || s.IsCastle).ToList();

        // =========================================================================
        // Troops
        // =========================================================================

        public IEnumerable<WCharacter> Troops
        {
            get
            {
                yield return RetinueElite;
                yield return RetinueBasic;
                foreach (var troop in EliteTroops)
                    yield return troop;
                foreach (var troop in BasicTroops)
                    yield return troop;
            }
        }

        public WCharacter RetinueElite { get; set; }
        public WCharacter RetinueBasic { get; set; }

        public WCharacter RootElite => EliteTroops.Find(t => t?.Parent == null);
        public WCharacter RootBasic => BasicTroops.Find(t => t?.Parent == null);

        public List<WCharacter> EliteTroops { get; } = [];
        public List<WCharacter> BasicTroops { get; } = [];

        public void ClearTroops()
        {
            EliteTroops.Clear();
            BasicTroops.Clear();
            RetinueElite = null;
            RetinueBasic = null;
        }

        // =========================================================================
        // Fiefs
        // =========================================================================

        public bool HasFiefs => Base.Fiefs?.Count > 0;
    }
}
