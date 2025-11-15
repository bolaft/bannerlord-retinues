using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for IFaction (Clan or Kingdom).
    /// </summary>
    [SafeClass]
    public class WFaction(IFaction faction) : BaseFaction
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly IFaction _faction = faction;
        public IFaction Base => _faction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string Name => _faction?.Name.ToString();
        public override string StringId => _faction?.StringId;
        public override string BannerCodeText => _faction?.Banner.Serialize();
        public override uint Color => _faction?.Color ?? 0;
        public override uint Color2 => _faction?.Color2 ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ General ━━━━━━━ */

        public bool HasFiefs => Base.Fiefs?.Count > 0;
        public bool IsClan => Base is Clan;
        public bool IsKingdom => Base is Kingdom;

        /* ━━━━━━━━ Player ━━━━━━━━ */

        public bool IsPlayerFaction => this == Player.Clan || this == Player.Kingdom;
        public bool IsPlayerClan => this == Player.Clan;
        public bool IsPlayerKingdom => this == Player.Kingdom;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Lists                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<WSettlement> Settlements =>
            [.. _faction?.Settlements.Select(s => s == null ? null : new WSettlement(s))];

        public List<WParty> Parties =>
            [.. MobileParty.All.Select(mp => new WParty(mp)).Where(p => p.PlayerFaction == this)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Replaces an old troop with a new troop in all relevant references.
        /// </summary>
        private void Replace(WCharacter oldTroop, WCharacter newTroop)
        {
            if (oldTroop == null || newTroop == null)
                return;

            oldTroop.Remove(replacement: newTroop);
        }

        /* ━━━━━━━ Retinues ━━━━━━━ */

        private WCharacter _retinueElite;
        public override WCharacter RetinueElite
        {
            get => _retinueElite?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_retinueElite, value);
                _retinueElite = value;
            }
        }

        private WCharacter _retinueBasic;
        public override WCharacter RetinueBasic
        {
            get => _retinueBasic?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_retinueBasic, value);
                _retinueBasic = value;
            }
        }

        /* ━━━━━━━━ Regular ━━━━━━━ */

        private WCharacter _rootElite;
        public override WCharacter RootElite
        {
            get => _rootElite?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_rootElite, value);
                _rootElite = value;
            }
        }

        private WCharacter _rootBasic;
        public override WCharacter RootBasic
        {
            get => _rootBasic?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_rootBasic, value);
                _rootBasic = value;
            }
        }

        /* ━━━━━━━━ Special ━━━━━━━ */

        private WCharacter _militiaMelee;
        public override WCharacter MilitiaMelee
        {
            get => _militiaMelee?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_militiaMelee, value);
                _militiaMelee = value;
            }
        }

        private WCharacter _militiaMeleeElite;
        public override WCharacter MilitiaMeleeElite
        {
            get => _militiaMeleeElite?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_militiaMeleeElite, value);
                _militiaMeleeElite = value;
            }
        }

        private WCharacter _militiaRanged;
        public override WCharacter MilitiaRanged
        {
            get => _militiaRanged?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_militiaRanged, value);
                _militiaRanged = value;
            }
        }

        private WCharacter _militiaRangedElite;
        public override WCharacter MilitiaRangedElite
        {
            get => _militiaRangedElite?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_militiaRangedElite, value);
                _militiaRangedElite = value;
            }
        }

        private WCharacter _caravanGuard;
        public override WCharacter CaravanGuard
        {
            get => _caravanGuard?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_caravanGuard, value);
                _caravanGuard = value;
            }
        }

        private WCharacter _caravanMaster;
        public override WCharacter CaravanMaster
        {
            get => _caravanMaster?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_caravanMaster, value);
                _caravanMaster = value;
            }
        }

        private WCharacter _villager;
        public override WCharacter Villager
        {
            get => _villager?.IsValid == true ? _retinueElite : null;
            set
            {
                Replace(_villager, value);
                _villager = value;
            }
        }
    }
}
