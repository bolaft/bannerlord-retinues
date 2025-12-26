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

        public static int TroopFactionMapVersion { get; set; }

        internal static void TouchTroopFactionMap()
        {
            TroopFactionMapVersion++;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public abstract override string StringId { get; }
        public abstract string Name { get; }
        public abstract uint Color { get; }
        public abstract uint Color2 { get; }

        public WCulture Culture
        {
            get
            {
                if (this is WCulture cu)
                    return cu;

                if (this is WClan cl)
                    return new(cl.Base.Culture);

                if (this is WFaction f)
                    return new(f.Base.Culture);

                return null;
            }
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

            InvalidateCategoryCache();
        }

        /* ━━━━━━━ Retinues ━━━━━━━ */

        public virtual WCharacter RetinueElite { get; set; }
        public virtual WCharacter RetinueBasic { get; set; }

        /* ━━━━━━━━ Regular ━━━━━━━ */

        public virtual WCharacter RootElite { get; set; }
        public virtual WCharacter RootBasic { get; set; }

        /* ━━━━━━━━ Special ━━━━━━━ */

        public virtual WCharacter MilitiaMelee { get; set; }
        public virtual WCharacter MilitiaMeleeElite { get; set; }
        public virtual WCharacter MilitiaRanged { get; set; }
        public virtual WCharacter MilitiaRangedElite { get; set; }
        public virtual WCharacter CaravanGuard { get; set; }
        public virtual WCharacter CaravanMaster { get; set; }
        public virtual WCharacter Villager { get; set; }

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
                foreach (var troop in Heroes)
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

        public List<WCharacter> RegularTroops => [.. EliteTroops, .. BasicTroops];
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

        public virtual List<WCharacter> MercenaryTroops => [];
        public virtual List<WCharacter> BanditTroops => [];
        public virtual List<WCharacter> CivilianTroops => [];

        /* ━━━━━━━━━ NPCs ━━━━━━━━━ */

        public virtual List<WHero> Heroes => [];

        // In BaseFaction.cs

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Category cache                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _categoryCacheDirty = true;

        private readonly HashSet<string> _retinueIds = new(System.StringComparer.Ordinal);
        private readonly HashSet<string> _regularIds = new(System.StringComparer.Ordinal);
        private readonly HashSet<string> _eliteIds = new(System.StringComparer.Ordinal);

        public void InvalidateCategoryCache()
        {
            _categoryCacheDirty = true;
        }

        private void EnsureCategoryCache()
        {
            if (!_categoryCacheDirty)
                return;

            if (this is not WFaction)
                return;

            _categoryCacheDirty = false;

            _retinueIds.Clear();
            _regularIds.Clear();
            _eliteIds.Clear();

            // Retinues
            foreach (var t in GetActiveList([RetinueElite, RetinueBasic]))
                _retinueIds.Add(t.StringId);

            // Retinue elites are also elites
            if (RetinueElite != null && RetinueElite.IsActive)
                _eliteIds.Add(RetinueElite.StringId);

            // Regulars: RootElite tree (elite) + RootBasic tree (basic)
            var eliteTree = RootElite != null ? GetActiveList([.. RootElite.Tree]) : [];
            var basicTree = RootBasic != null ? GetActiveList([.. RootBasic.Tree]) : [];

            foreach (var t in eliteTree)
            {
                _eliteIds.Add(t.StringId);
                _regularIds.Add(t.StringId);
            }

            foreach (var t in basicTree)
                _regularIds.Add(t.StringId);

            // Militias
            var militiaList = GetActiveList([MilitiaRanged, MilitiaRangedElite]);
            foreach (var t in militiaList)
            {
                if (t == MilitiaMeleeElite || t == MilitiaRangedElite)
                    _eliteIds.Add(t.StringId);
            }

            // Caravan master is also elite
            if (CaravanMaster != null && CaravanMaster.IsActive)
                _eliteIds.Add(CaravanMaster.StringId);
        }

        public bool IsRetinueId(string troopId)
        {
            if (string.IsNullOrWhiteSpace(troopId))
                return false;

            // Fast path for WFaction: use cached sets
            if (this is WFaction)
            {
                EnsureCategoryCache();
                return _retinueIds.Contains(troopId);
            }

            // Fallback for non-WFaction
            return (
                    RetinueElite != null
                    && RetinueElite.IsActive
                    && RetinueElite.StringId == troopId
                )
                || (
                    RetinueBasic != null
                    && RetinueBasic.IsActive
                    && RetinueBasic.StringId == troopId
                );
        }

        // Fast lookups used by WCharacter
        public bool IsRetinue(WCharacter troop)
        {
            if (troop == null || troop.IsHero)
                return false;

            if (troop.IsVanilla)
                return false;

            return IsRetinueId(troop.StringId);
        }

        public bool IsRegular(WCharacter troop)
        {
            if (troop == null || troop.IsHero)
                return false;

            if (troop.IsVanilla)
                return true;

            EnsureCategoryCache();
            return _regularIds.Contains(troop.StringId);
        }

        public bool IsElite(WCharacter troop)
        {
            if (troop == null || troop.IsHero)
                return false;

            if (troop.IsVanilla)
                return EliteTroops.Contains(troop);

            EnsureCategoryCache();
            return _eliteIds.Contains(troop.StringId);
        }
    }
}
