using System.Collections.Generic;
using System.Linq;
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

        public WHero Leader => WHero.Get(Base.Leader);

        public override List<WCharacter> RosterHeroes =>
            [.. Base.Heroes.Select(h => WHero.Get(h).Character).Where(c => c.Age >= 18)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Custom Roots/Rosters               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static List<WCharacter> _cultureRootBasics;
        static List<WCharacter> _cultureRootElites;

        [StaticClearAction]
        public static void ClearRootsCache()
        {
            _cultureRootBasics = null;
            _cultureRootElites = null;
        }

        void EnsureCultureRoots()
        {
            if (_cultureRootBasics != null && _cultureRootElites != null)
                return;

            _cultureRootBasics = [.. WCulture.All.Select(c => c.RootBasic).Where(r => r != null)];
            _cultureRootElites = [.. WCulture.All.Select(c => c.RootElite).Where(r => r != null)];
        }

        /* ━━━━━━━━━ Stored Attributes ━━━━━━━━━ */

        protected MAttribute<WCharacter> CustomRootBasicAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<WCharacter> CustomRootEliteAttribute =>
            Attribute<WCharacter>(initialValue: null);
        protected MAttribute<List<WCharacter>> RetinueTroopsAttribute =>
            Attribute<List<WCharacter>>([]);

        /* ━━━━━━━━━ Roots ━━━━━━━━━ */

        /// <summary>
        /// Gets the custom root basic troop for this map faction, if any.
        /// Uses the stored custom root first, then falls back to the vanilla BasicTroop when available.
        /// Returns null if the resolved root is a culture root (not custom).
        /// </summary>
        public override WCharacter RootBasic
        {
            get
            {
                var root = CustomRootBasicAttribute.Get();

                // Fallback to vanilla BasicTroop (Clan has a settable BasicTroop; Kingdom is culture-based).
                root ??= WCharacter.Get(Base.BasicTroop);

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

        /* ━━━━━━━━━ Mutators ━━━━━━━━━ */

        /// <summary>
        /// Sets the custom basic root for this map faction.
        /// </summary>
        public void SetRootBasic(WCharacter root)
        {
            CustomRootBasicAttribute.Set(root);
            WCharacter.InvalidateTroopFactionsCache();
            WCharacter.InvalidateCustomTreeCache();
        }

        /// <summary>
        /// Sets the custom elite root for this map faction.
        /// </summary>
        public void SetRootElite(WCharacter root)
        {
            CustomRootEliteAttribute.Set(root);
            WCharacter.InvalidateTroopFactionsCache();
            WCharacter.InvalidateCustomTreeCache();
        }

        /// <summary>
        /// Sets the retinue roster for this map faction.
        /// </summary>
        public void SetRetinues(List<WCharacter> troops)
        {
            RetinueTroopsAttribute.Set(troops ?? []);
            WCharacter.InvalidateTroopFactionsCache();
            WCharacter.InvalidateCustomTreeCache();
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
    }
}
