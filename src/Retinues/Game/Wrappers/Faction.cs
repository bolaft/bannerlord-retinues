using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
#endif

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for IFaction (Clan or Kingdom).
    /// </summary>
    [SafeClass]
    public class WFaction : BaseBannerFaction
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        
        /// <summary>
        /// If true, allows multiple cultures per faction.
        /// If false, only one clan and one kingdom.
        /// </summary>
        public static bool MultiCultureMode = false;

        /// <summary>
        /// Lists all existing WFaction wrappers.
        /// </summary>
        public static IEnumerable<WFaction> Factions
        {
            get
            {
                foreach (var faction in FactionCultureMap.Keys)
                {
                    foreach (var culture in FactionCultureMap[faction].Keys)
                    {
                        yield return FactionCultureMap[faction][culture];
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Dictionary<IFaction, Dictionary<WCulture, WFaction>> FactionCultureMap = [];

        // Private constructor to enforce use of factory methods
        private WFaction(IFaction faction, WCulture culture)
        { 
            _culture = culture;
            _faction = faction;
        }

        public static WFaction GetClan(WCulture culture)
        {
            var faction = Player.Clan;

            return GetFactionWrapper(faction, culture);
        }

        public static WFaction GetKingdom(WCulture culture)
        {
            if (!Player.IsKingdomLeader)
                return null;

            var faction = Player.Kingdom;

            return GetFactionWrapper(faction, culture);
        }

        private static WFaction GetFactionWrapper(IFaction faction, WCulture culture)
        {
            if (faction == null)
                return null;

            if (MultiCultureMode == false)
                culture = Player.Culture; // Force single culture mode

            if (!FactionCultureMap.ContainsKey(faction))
                FactionCultureMap[faction] = [];

            if (!FactionCultureMap[faction].ContainsKey(culture))
                FactionCultureMap[faction][culture] = new WFaction(faction, culture);

            return FactionCultureMap[faction][culture];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly IFaction _faction;
        public IFaction Base => _faction;

        private WCulture _culture;
        public override WCulture Culture => _culture;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string Name => _faction?.Name.ToString();
        public override string StringId => _faction?.StringId;
        public override uint Color => _faction?.Color ?? 0;
        public override uint Color2 => _faction?.Color2 ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Banner                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override Banner BaseBanner => Base?.Banner;

#if BL13
        public BannerImageIdentifier Image =>
            Base.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
        public ImageIdentifier ImageIdentifier =>
            Base.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
#else
        public BannerCode BannerCode => BannerCode.CreateFrom(Base.Banner);
        public ImageIdentifierVM Image => new(BannerCode);
        public ImageIdentifier ImageIdentifier => new(BannerCode);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool HasFiefs => Base.Fiefs?.Count > 0;

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
            get => _retinueBasic?.IsValid == true ? _retinueBasic : null;
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
            get => _rootElite?.IsValid == true ? _rootElite : null;
            set
            {
                Replace(_rootElite, value);
                _rootElite = value;
            }
        }

        private WCharacter _rootBasic;
        public override WCharacter RootBasic
        {
            get => _rootBasic?.IsValid == true ? _rootBasic : null;
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
            get => _militiaMelee?.IsValid == true ? _militiaMelee : null;
            set
            {
                Replace(_militiaMelee, value);
                _militiaMelee = value;
            }
        }

        private WCharacter _militiaMeleeElite;
        public override WCharacter MilitiaMeleeElite
        {
            get => _militiaMeleeElite?.IsValid == true ? _militiaMeleeElite : null;
            set
            {
                Replace(_militiaMeleeElite, value);
                _militiaMeleeElite = value;
            }
        }

        private WCharacter _militiaRanged;
        public override WCharacter MilitiaRanged
        {
            get => _militiaRanged?.IsValid == true ? _militiaRanged : null;
            set
            {
                Replace(_militiaRanged, value);
                _militiaRanged = value;
            }
        }

        private WCharacter _militiaRangedElite;
        public override WCharacter MilitiaRangedElite
        {
            get => _militiaRangedElite?.IsValid == true ? _militiaRangedElite : null;
            set
            {
                Replace(_militiaRangedElite, value);
                _militiaRangedElite = value;
            }
        }

        private WCharacter _caravanGuard;
        public override WCharacter CaravanGuard
        {
            get => _caravanGuard?.IsValid == true ? _caravanGuard : null;
            set
            {
                Replace(_caravanGuard, value);
                _caravanGuard = value;
            }
        }

        private WCharacter _caravanMaster;
        public override WCharacter CaravanMaster
        {
            get => _caravanMaster?.IsValid == true ? _caravanMaster : null;
            set
            {
                Replace(_caravanMaster, value);
                _caravanMaster = value;
            }
        }

        private WCharacter _villager;
        public override WCharacter Villager
        {
            get => _villager?.IsValid == true ? _villager : null;
            set
            {
                Replace(_villager, value);
                _villager = value;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Lists the IDs of all player troops. Usage: retinues.list_custom_troops
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("list_custom_troops", "retinues")]
        public static string ListCustomTroops(List<string> args)
        {
            List<string> list = [];

            foreach (var faction in Factions)
                list.AddRange([.. faction.Troops.Select(t => $"{t.StringId}: {t.Name}")]);
            
            return string.Join("\n", list);
        }
    }
}
