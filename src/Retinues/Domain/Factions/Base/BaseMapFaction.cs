using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Models;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Model.Attributes;
using Retinues.Framework.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Factions.Base
{
    public abstract class BaseMapFaction<TWrapper, TFaction>(TFaction @base)
        : BaseFaction<TWrapper, TFaction>(@base)
        where TWrapper : BaseMapFaction<TWrapper, TFaction>
        where TFaction : MBObjectBase, IFaction
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string Name => Base.Name?.ToString();
        public override uint Color => Base.Color;
        public override uint Color2 => Base.Color2;
        public override Banner Banner => Base.Banner;

#if BL12
        public override BannerCode BannerCode => BannerCode.CreateFrom(Banner);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCulture Culture => WCulture.Get(Base.Culture);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Faction                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IFaction MapFaction => Base.MapFaction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Heroes                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override WHero Leader => WHero.Get(Base.Leader);

        public override List<WCharacter> RosterHeroes =>
            [.. Base.Heroes.Select(h => WHero.Get(h).Character).Where(c => c.Age >= 18)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Custom Roots/Rosters               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static List<WCharacter> _cultureRootBasics;
        static List<WCharacter> _cultureRootElites;

        static HashSet<string> _cultureMilitiaIds;
        static HashSet<string> _cultureCaravanIds;
        static HashSet<string> _cultureVillagerIds;

        [StaticClearAction]
        public static void ClearRootsCache()
        {
            _cultureRootBasics = null;
            _cultureRootElites = null;

            _cultureMilitiaIds = null;
            _cultureCaravanIds = null;
            _cultureVillagerIds = null;
        }

        /// <summary>
        /// Ensures that the culture root caches are populated.
        /// </summary>
        void EnsureCultureRoots()
        {
            if (
                _cultureRootBasics != null
                && _cultureRootElites != null
                && _cultureMilitiaIds != null
                && _cultureCaravanIds != null
                && _cultureVillagerIds != null
            )
                return;

            _cultureRootBasics = [.. WCulture.All.Select(c => c.RootBasic).Where(r => r != null)];
            _cultureRootElites = [.. WCulture.All.Select(c => c.RootElite).Where(r => r != null)];

            _cultureMilitiaIds = new HashSet<string>();
            _cultureCaravanIds = new HashSet<string>();
            _cultureVillagerIds = new HashSet<string>();

            foreach (var culture in WCulture.All)
            {
                if (culture == null)
                    continue;

                foreach (var t in culture.RosterMilitia)
                    if (!string.IsNullOrEmpty(t?.StringId))
                        _cultureMilitiaIds.Add(t.StringId);

                foreach (var t in culture.RosterCaravan)
                    if (!string.IsNullOrEmpty(t?.StringId))
                        _cultureCaravanIds.Add(t.StringId);

                foreach (var t in culture.RosterVillager)
                    if (!string.IsNullOrEmpty(t?.StringId))
                        _cultureVillagerIds.Add(t.StringId);
            }
        }

        /* ━━━━━━━━━ Stored Attributes ━━━━━━━━━ */

        protected MAttribute<WCharacter> CustomRootBasicAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomRootEliteAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<List<WCharacter>> RetinueTroopsAttribute =>
            Attribute<List<WCharacter>>([]);

        protected MAttribute<WCharacter> CustomVillagerAttribute =>
            Attribute<WCharacter>(initialValue: null);

        protected MAttribute<WCharacter> CustomCaravanMasterAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomCaravanGuardAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomArmedTraderAttribute =>
            Attribute<WCharacter>(initialValue: null);

        protected MAttribute<WCharacter> CustomMeleeMilitiaTroopAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomMeleeEliteMilitiaTroopAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomRangedMilitiaTroopAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomRangedEliteMilitiaTroopAttribute =>
            Attribute<WCharacter>(initialValue: null);

        protected MAttribute<WCharacter> CustomMilitiaArcherAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomMilitiaSpearmanAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomMilitiaVeteranSpearmanAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomMilitiaVeteranArcherAttribute =>
            Attribute<WCharacter>(initialValue: null);

        /* ━━━━━━━━━ Roots ━━━━━━━━━ */

        /// <summary>
        /// Gets the custom root basic troop for this map faction, if any.
        /// Returns null if unset or if the stored root is a culture root (not custom).
        /// </summary>
        public override WCharacter RootBasic
        {
            get
            {
                var root = CustomRootBasicAttribute.Get();
                if (root == null)
                    return null;

                EnsureCultureRoots();

                if (_cultureRootBasics.Contains(root))
                    return null;

                return root;
            }
        }

        /// <summary>
        /// Gets the custom root elite troop for this map faction, if any.
        /// Returns null if the stored root is a culture root (not custom).
        /// </summary>
        public override WCharacter RootElite
        {
            get
            {
                var root = CustomRootEliteAttribute.Get();
                if (root == null)
                    return null;

                EnsureCultureRoots();

                if (_cultureRootElites.Contains(root))
                    return null;

                return root;
            }
        }

        /* ━━━━━━━━━ Retinues ━━━━━━━━━ */

        /// <summary>
        /// Retinue troops owned by this map faction.
        /// Stored as a List<WCharacter> (serialized as StringIds).
        /// </summary>
        public override List<WCharacter> RosterRetinues
        {
            get
            {
                if (Settings.EnableRetinues == false)
                    return [];

                var src = RetinueTroopsAttribute.Get();
                if (src == null || src.Count == 0)
                    return [];

                var list = new List<WCharacter>(src.Count);
                var seen = new HashSet<string>();

                for (int i = 0; i < src.Count; i++)
                {
                    var c = src[i];
                    if (!IsValid(c))
                        continue;

                    var id = c.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (seen.Add(id))
                        list.Add(c);
                }

                return list;
            }
        }

        /* ━━━━━━━ Villager ━━━━━━━ */

        public override WCharacter Villager
        {
            get
            {
                var troop = CustomVillagerAttribute.Get();
                if (troop == null)
                    return null;

                EnsureCultureRoots();
                if (
                    !string.IsNullOrEmpty(troop.StringId)
                    && _cultureVillagerIds.Contains(troop.StringId)
                )
                    return null;

                return troop;
            }
        }

        /* ━━━━━━━ Caravans ━━━━━━━ */

        public override WCharacter CaravanMaster =>
            GetCustomOrNull(CustomCaravanMasterAttribute, _cultureCaravanIds);
        public override WCharacter CaravanGuard =>
            GetCustomOrNull(CustomCaravanGuardAttribute, _cultureCaravanIds);
        public override WCharacter ArmedTrader =>
            GetCustomOrNull(CustomArmedTraderAttribute, _cultureCaravanIds);

        /* ━━━━━━━ Militias ━━━━━━━ */

        public override WCharacter MeleeMilitiaTroop =>
            GetCustomOrNull(CustomMeleeMilitiaTroopAttribute, _cultureMilitiaIds);
        public override WCharacter MeleeEliteMilitiaTroop =>
            GetCustomOrNull(CustomMeleeEliteMilitiaTroopAttribute, _cultureMilitiaIds);
        public override WCharacter RangedMilitiaTroop =>
            GetCustomOrNull(CustomRangedMilitiaTroopAttribute, _cultureMilitiaIds);
        public override WCharacter RangedEliteMilitiaTroop =>
            GetCustomOrNull(CustomRangedEliteMilitiaTroopAttribute, _cultureMilitiaIds);

        WCharacter GetCustomOrNull(MAttribute<WCharacter> attr, HashSet<string> cultureSet)
        {
            var troop = attr.Get();
            if (troop == null)
                return null;

            EnsureCultureRoots();

            var id = troop.StringId;
            if (!string.IsNullOrEmpty(id) && cultureSet != null && cultureSet.Contains(id))
                return null;

            return troop;
        }

        /* ━━━━━━━━━ Mutators ━━━━━━━━━ */

        /// <summary>
        /// Sets the custom basic root for this map faction.
        /// </summary>
        public void SetRootBasic(WCharacter root)
        {
            CustomRootBasicAttribute.Set(root);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the custom elite root for this map faction.
        /// </summary>
        public void SetRootElite(WCharacter root)
        {
            CustomRootEliteAttribute.Set(root);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the retinue roster for this map faction.
        /// </summary>
        public void SetRetinues(List<WCharacter> troops)
        {
            RetinueTroopsAttribute.Set(troops ?? []);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Adds a retinue troop to this map faction.
        /// </summary>
        public void AddRetinue(WCharacter troop)
        {
            var troops = RosterRetinues;
            if (troops.Contains(troop))
                return;

            troops.Add(troop);
            SetRetinues(troops);
        }

        /// <summary>
        /// Sets the villager troop for this map faction.
        /// </summary>
        public void SetVillager(WCharacter troop)
        {
            CustomVillagerAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the caravan master troop for this map faction.
        /// </summary>
        public void SetCaravanMaster(WCharacter troop)
        {
            CustomCaravanMasterAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the caravan guard troop for this map faction.
        /// </summary>
        public void SetCaravanGuard(WCharacter troop)
        {
            CustomCaravanGuardAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the armed trader troop for this map faction.
        /// </summary>
        public void SetArmedTrader(WCharacter troop)
        {
            CustomArmedTraderAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the melee militia troop for this map faction.
        /// </summary>
        public void SetMeleeMilitiaTroop(WCharacter troop)
        {
            CustomMeleeMilitiaTroopAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the melee elite militia troop for this map faction.
        /// </summary>
        public void SetMeleeEliteMilitiaTroop(WCharacter troop)
        {
            CustomMeleeEliteMilitiaTroopAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the ranged militia troop for this map faction.
        /// </summary>
        public void SetRangedMilitiaTroop(WCharacter troop)
        {
            CustomRangedMilitiaTroopAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the ranged elite militia troop for this map faction.
        /// </summary>
        public void SetRangedEliteMilitiaTroop(WCharacter troop)
        {
            CustomRangedEliteMilitiaTroopAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the militia archer troop for this map faction.
        /// </summary>
        public void SetMilitiaArcher(WCharacter troop)
        {
            CustomMilitiaArcherAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the militia veteran archer troop for this map faction.
        /// </summary>
        public void SetMilitiaVeteranArcher(WCharacter troop)
        {
            CustomMilitiaVeteranArcherAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the militia spearman troop for this map faction.
        /// </summary>
        public void SetMilitiaSpearman(WCharacter troop)
        {
            CustomMilitiaSpearmanAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        /// <summary>
        /// Sets the militia veteran spearman troop for this map faction.
        /// </summary>
        public void SetMilitiaVeteranSpearman(WCharacter troop)
        {
            CustomMilitiaVeteranSpearmanAttribute.Set(troop);
            WCharacter.InvalidateTroopSourceCaches();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Territory                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IReadOnlyList<WSettlement> Settlements =>
            [.. Base.Settlements.Select(WSettlement.Get)];
        public IReadOnlyList<MTown> Fiefs => [.. Base.Fiefs.Select(f => new MTown(f))];

        public bool HasFiefs => Base.Fiefs != null && Base.Fiefs.Count > 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Parties                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IReadOnlyList<WParty> Parties =>
            [.. Base.WarPartyComponents.Select(c => WParty.Get(c.MobileParty))];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Flags & Stats                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsBanditFaction => Base.IsBanditFaction;
        public bool IsMinorFaction => Base.IsMinorFaction;
        public bool IsKingdomFaction => Base.IsKingdomFaction;
        public float TotalStrength =>
#if BL13
            Base.CurrentTotalStrength;
#else
            Base.TotalStrength;
#endif
        public bool IsEliminated => Base.IsEliminated;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Access                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets a map faction wrapper from the campaign by string ID.
        /// </summary>
        protected static TWrapper GetFromCampaign(
            string stringId,
            Func<IEnumerable<TFaction>> allGetter
        )
        {
            if (string.IsNullOrEmpty(stringId))
                return null;

            // Prefer MBObjectManager (does not require Campaign.Current / Clan.All to be ready)
            try
            {
                var mgr = MBObjectManager.Instance;
                if (mgr != null)
                {
                    var mbo = mgr.GetObject<TFaction>(stringId);
                    if (mbo != null)
                        return Get(mbo);
                }
            }
            catch
            {
                // ignore; fallback to campaign list
            }

            var all = allGetter?.Invoke();
            if (all == null)
                return null;

            foreach (var f in all)
            {
                if (f == null)
                    continue;

                if (f.StringId == stringId)
                    return Get(f);
            }

            return null;
        }

        /// <summary>
        /// Gets all map faction wrappers from the campaign.
        /// </summary>
        protected static IEnumerable<TWrapper> AllFromCampaign(
            Func<IEnumerable<TFaction>> allGetter
        )
        {
            var all = allGetter?.Invoke();
            if (all == null)
                yield break;

            foreach (var f in all)
            {
                if (f == null)
                    continue;

                var w = Get(f);
                if (w != null)
                    yield return w;
            }
        }
    }
}
