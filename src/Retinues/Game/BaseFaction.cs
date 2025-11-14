using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game
{
    public enum RootCategory
    {
        RetinueBasic,
        RetinueElite,
        RootBasic,
        RootElite,
        MilitiaMelee,
        MilitiaMeleeElite,
        MilitiaRanged,
        MilitiaRangedElite,
        CaravanGuard,
        CaravanMaster,
        Villager,
        Other,
    }

    public abstract class BaseFaction : StringIdentifier
    {
        // Character -> Faction map for quick lookup
        public static Dictionary<string, BaseFaction> TroopFactionMap = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public abstract override string StringId { get; }
        public abstract string Name { get; }
        public abstract uint Color { get; }
        public abstract uint Color2 { get; }
        public abstract string BannerCodeText { get; }

        public WCulture Culture
        {
            get
            {
                if (this is WCulture c)
                    return c;

                if (this is WFaction f)
                    return new(f.Base.Culture);

                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines if the given troop is considered elite within this faction.
        /// </summary>
        public bool IsElite(WCharacter troop)
        {
            if (troop == null)
                return false;

            if (troop == RetinueElite)
                return true;

            if (troop == MilitiaMeleeElite || troop == MilitiaRangedElite)
                return true;

            if (troop == CaravanMaster)
                return true;

            if (EliteTroops.Contains(troop))
                return true;

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Roots                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter GetRoot(RootCategory category)
        {
            return category switch
            {
                RootCategory.RetinueBasic => RetinueBasic,
                RootCategory.RetinueElite => RetinueElite,
                RootCategory.RootBasic => RootBasic,
                RootCategory.RootElite => RootElite,
                RootCategory.MilitiaMelee => MilitiaMelee,
                RootCategory.MilitiaMeleeElite => MilitiaMeleeElite,
                RootCategory.MilitiaRanged => MilitiaRanged,
                RootCategory.MilitiaRangedElite => MilitiaRangedElite,
                RootCategory.CaravanGuard => CaravanGuard,
                RootCategory.CaravanMaster => CaravanMaster,
                RootCategory.Villager => Villager,
                _ => null,
            };
        }

        public void SetRoot(RootCategory category, WCharacter troop)
        {
            switch (category)
            {
                case RootCategory.RetinueBasic:
                    RetinueBasic = troop;
                    break;
                case RootCategory.RetinueElite:
                    RetinueElite = troop;
                    break;
                case RootCategory.RootBasic:
                    RootBasic = troop;
                    break;
                case RootCategory.RootElite:
                    RootElite = troop;
                    break;
                case RootCategory.MilitiaMelee:
                    MilitiaMelee = troop;
                    break;
                case RootCategory.MilitiaMeleeElite:
                    MilitiaMeleeElite = troop;
                    break;
                case RootCategory.MilitiaRanged:
                    MilitiaRanged = troop;
                    break;
                case RootCategory.MilitiaRangedElite:
                    MilitiaRangedElite = troop;
                    break;
                case RootCategory.CaravanGuard:
                    CaravanGuard = troop;
                    break;
                case RootCategory.CaravanMaster:
                    CaravanMaster = troop;
                    break;
                case RootCategory.Villager:
                    Villager = troop;
                    break;
                default:
                    break;
            }
        }

        /* ━━━━━━━ Retinues ━━━━━━━ */

        public abstract WCharacter RetinueElite { get; set; }
        public abstract WCharacter RetinueBasic { get; set; }

        /* ━━━━━━━━ Regular ━━━━━━━ */

        public abstract WCharacter RootElite { get; set; }
        public abstract WCharacter RootBasic { get; set; }

        /* ━━━━━━━━ Special ━━━━━━━ */

        public abstract WCharacter MilitiaMelee { get; set; }
        public abstract WCharacter MilitiaMeleeElite { get; set; }
        public abstract WCharacter MilitiaRanged { get; set; }
        public abstract WCharacter MilitiaRangedElite { get; set; }
        public abstract WCharacter CaravanGuard { get; set; }
        public abstract WCharacter CaravanMaster { get; set; }
        public abstract WCharacter Villager { get; set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
                foreach (var troop in CaravanTroops)
                    yield return troop;
                foreach (var troop in VillagerTroops)
                    yield return troop;
                foreach (var troop in CivilianTroops)
                    yield return troop;
                foreach (var troop in BanditTroops)
                    yield return troop;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Troop Lists                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Filters out inactive/null troops from the given list.
        protected List<WCharacter> GetActiveList(List<WCharacter> list) =>
            list == null ? [] : [.. list.Where(t => t?.IsActive == true && t?.Body.Age >= 18)];

        protected List<WCharacter> GetActiveList(List<CharacterObject> list) =>
            GetActiveList([.. list.Where(t => t != null).Select(t => new WCharacter(t))]);

        /* ━━━━━━━ Retinues ━━━━━━━ */

        public List<WCharacter> RetinueTroops => GetActiveList([RetinueElite, RetinueBasic]);

        /* ━━━━━━━━ Regular ━━━━━━━ */

        public List<WCharacter> EliteTroops =>
            RootElite != null ? GetActiveList([.. RootElite.Tree]) : [];
        public List<WCharacter> BasicTroops =>
            RootBasic != null ? GetActiveList([.. RootBasic.Tree]) : [];

        /* ━━━━━━━━ Special ━━━━━━━ */

        public List<WCharacter> MilitiaTroops =>
            GetActiveList([MilitiaMelee, MilitiaMeleeElite, MilitiaRanged, MilitiaRangedElite]);
        public List<WCharacter> CaravanTroops => GetActiveList([CaravanGuard, CaravanMaster]);
        public List<WCharacter> VillagerTroops => GetActiveList([Villager]);

        /* ━━━━━━━ Rootless ━━━━━━━ */

        public virtual List<WCharacter> CivilianTroops => [];
        public virtual List<WCharacter> BanditTroops => [];
    }
}
