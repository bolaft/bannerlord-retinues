using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for IFaction (Clan or Kingdom), exposes troop roots, retinues, and helpers for custom logic.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
    public class WFaction(IFaction faction) : StringIdentifier
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly IFaction _faction = faction;

        public IFaction Base => _faction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => _faction?.Name.ToString();

        public override string StringId => _faction?.StringId;

        public string BannerCodeText => _faction?.Banner.Serialize();

        public uint Color => _faction?.Color ?? 0;

        public uint Color2 => _faction?.Color2 ?? 0;

        public WCulture Culture => new(_faction?.Culture);

        public IReadOnlyList<Settlement> Fiefs =>
            [.. _faction?.Settlements.Where(s => s.IsTown || s.IsCastle)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter RetinueElite => new(this == Player.Kingdom, true, true);
        public WCharacter RetinueBasic => new(this == Player.Kingdom, false, true);

        public WCharacter RootElite => new(this == Player.Kingdom, true, false);
        public WCharacter RootBasic => new(this == Player.Kingdom, false, false);

        public WCharacter MilitiaMelee => new(this == Player.Kingdom, false, false, true, false);
        public WCharacter MilitiaMeleeElite =>
            new(this == Player.Kingdom, true, false, true, false);
        public WCharacter MilitiaRanged => new(this == Player.Kingdom, false, false, false, true);
        public WCharacter MilitiaRangedElite =>
            new(this == Player.Kingdom, true, false, false, true);

        /// <summary>
        /// Gets all custom troops for this faction, including retinues and active tree members.
        /// </summary>
        public IEnumerable<WCharacter> Troops
        {
            get
            {
                yield return RetinueElite;
                yield return RetinueBasic;
                foreach (var troop in RootElite.Tree)
                    if (troop.IsActive)
                        yield return troop;
                foreach (var troop in RootBasic.Tree)
                    if (troop.IsActive)
                        yield return troop;
            }
        }

        /// <summary>
        /// Gets all elite troops in the upgrade tree that are active.
        /// </summary>
        public List<WCharacter> EliteTroops =>
            [.. RootElite.Tree.Where(t => t.IsActive && t.IsElite)];

        /// <summary>
        /// Gets all basic troops in the upgrade tree that are active.
        /// </summary>
        public List<WCharacter> BasicTroops =>
            [.. RootBasic.Tree.Where(t => t.IsActive && !t.IsElite)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool HasFiefs => Base.Fiefs?.Count > 0;
        public bool IsClan => Base is Clan;
        public bool IsKingdom => Base is Kingdom;

        public bool IsPlayerFaction => this == Player.Clan || this == Player.Kingdom;
        public bool IsPlayerClan => this == Player.Clan;
        public bool IsPlayerKingdom => this == Player.Kingdom;
    }
}
