using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
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

        // Helper to determine whether to use kingdom troops for retinues and roots.
        private bool UseKingdomTroops => this == Player.Kingdom && !Config.NoKingdomTroops;

        public WCharacter RetinueElite => new(UseKingdomTroops, true, true);
        public WCharacter RetinueBasic => new(UseKingdomTroops, false, true);

        public WCharacter RootElite => new(UseKingdomTroops, true, false);
        public WCharacter RootBasic => new(UseKingdomTroops, false, false);

        public WCharacter MilitiaMelee => new(UseKingdomTroops, false, false, true, false);
        public WCharacter MilitiaMeleeElite => new(UseKingdomTroops, true, false, true, false);
        public WCharacter MilitiaRanged => new(UseKingdomTroops, false, false, false, true);
        public WCharacter MilitiaRangedElite => new(UseKingdomTroops, true, false, false, true);

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

        public List<WCharacter> RetinueTroops =>
            [.. new List<WCharacter> { RetinueElite, RetinueBasic }.Where(t => t.IsActive)];

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
                    }.Where(t => t.IsActive),
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
