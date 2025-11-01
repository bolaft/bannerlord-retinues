using System.Collections.Generic;
using System.Linq;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

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

        public List<WSettlement> Settlements =>
            [.. _faction?.Settlements.Select(s => s == null ? null : new WSettlement(s))];

        public List<WParty> Parties =>
            [.. MobileParty.All.Select(mp => new WParty(mp)).Where(p => p.PlayerFaction == this)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter RetinueElite { get; set; }
        public WCharacter RetinueBasic { get; set; }

        public WCharacter RootElite { get; set; }
        public WCharacter RootBasic { get; set; }

        public WCharacter MilitiaMelee { get; set; }
        public WCharacter MilitiaMeleeElite { get; set; }
        public WCharacter MilitiaRanged { get; set; }
        public WCharacter MilitiaRangedElite { get; set; }

        /// <summary>
        /// Gets all custom troops for this faction.
        /// </summary>
        public IEnumerable<WCharacter> Troops
        {
            get
            {
                foreach (var troop in RetinueTroops)
                    yield return troop;
                foreach (var troop in EliteTroops)
                    yield return troop;
                foreach (var troop in BasicTroops)
                    yield return troop;
                foreach (var troop in MilitiaTroops)
                    yield return troop;
            }
        }

        /// <summary>
        /// Gets all retinue troops for this faction that are active.
        /// </summary>
        public List<WCharacter> RetinueTroops =>
            [
                .. new List<WCharacter> { RetinueElite, RetinueBasic }.Where(t =>
                    t?.IsActive == true
                ),
            ];

        /// <summary>
        /// Gets all elite troops in the upgrade tree that are active.
        /// </summary>
        public List<WCharacter> EliteTroops => [.. RootElite.Tree.Where(t => t?.IsActive == true)];

        /// <summary>
        /// Gets all basic troops in the upgrade tree that are active.
        /// </summary>
        public List<WCharacter> BasicTroops => [.. RootBasic.Tree.Where(t => t?.IsActive == true)];

        /// <summary>
        /// Gets all militia troops for this faction that are active.
        /// </summary>
        public List<WCharacter> MilitiaTroops =>
            DoctrineAPI.IsDoctrineUnlocked<CulturalPride>()
                ?
                [
                    .. new List<WCharacter>
                    {
                        MilitiaMeleeElite,
                        MilitiaMelee,
                        MilitiaRangedElite,
                        MilitiaRanged,
                    }.Where(t => t?.IsActive == true),
                ]
                : [];

        public bool HasFiefs => Base.Fiefs?.Count > 0;
        public bool IsClan => Base is Clan;
        public bool IsKingdom => Base is Kingdom;

        public bool IsPlayerFaction => this == Player.Clan || this == Player.Kingdom;
        public bool IsPlayerClan => this == Player.Clan;
        public bool IsPlayerKingdom => this == Player.Kingdom;
    }
}
