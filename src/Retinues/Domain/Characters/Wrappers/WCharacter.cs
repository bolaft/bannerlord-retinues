using System.Collections.Generic;
using System.Linq;
using Retinues.Compatibility;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Characters.Services.Trees;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Model;
using Retinues.Framework.Model.Attributes;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
#if BL13 || BL14
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter(CharacterObject @base)
        : WBase<WCharacter, CharacterObject>(@base)
    {
        static WCharacter()
        {
            RegisterFactory(co => new WCharacter(co));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<TextObject> NameAttribute => Attribute<TextObject>("_basicName");

        public virtual string Name
        {
            get
            {
                if (IsHero)
                    return Hero.Name;

                return NameAttribute.Get().ToString();
            }
            set
            {
                if (IsHero)
                {
                    Hero.Name = value;
                    return;
                }

                NameAttribute.Set(new TextObject(value));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Level & Tier                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Level ━━━━━━━━ */

        MAttribute<int> LevelAttribute => Attribute<int>(nameof(CharacterObject.Level));

        public virtual int Level
        {
            get
            {
                if (IsHero)
                    return Hero.Level;

                return LevelAttribute.Get();
            }
            set
            {
                if (IsHero)
                {
                    Hero.Level = value;
                }
                else
                {
                    LevelAttribute.Set(value);
                }

                // Invalidate conversion sources cache for retinues.
                ConversionCache.Clear();
            }
        }

        /* ━━━━━━━━━ Tier ━━━━━━━━━ */

        public int Tier => Base.Tier;

        public int MaxTier
        {
            get
            {
                int maxTier = IsElite ? 6 : 5;

                if (Mods.T7TroopUnlocker.IsLoaded)
                    maxTier += 1;

                return maxTier;
            }
        }

        public bool IsMaxTier => Tier >= MaxTier;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Hero                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Hero ━━━━━━━━━ */

        public WHero Hero => WHero.Get(Base.HeroObject);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ General ━━━━━━━ */

        public bool IsPlayer => Base.IsPlayerCharacter;
        public bool IsHero => Base.IsHero && Hero != null;
        public bool IsCustom => StringId.StartsWith(CustomTroopPrefix);
        public bool IsVanilla => !IsCustom;
        public bool IsEdited => IsDirty;
        public bool IsUpgradable => IsRegular || IsMercenary || IsBandit;
        public bool IsRoot => Root == this;

        /* ━━━━━━━━━ Tree ━━━━━━━━━ */

        public WCharacter Root => CharacterTreeCache.GetRoot(this) ?? this;
        public List<WCharacter> Tree => CharacterTreeCache.GetSubtree(this);
        public List<WCharacter> RootTree => CharacterTreeCache.GetTree(this);
        public int Depth => CharacterTreeCache.GetDepth(this);
        public bool IsLeaf => UpgradeTargets.Count == 0;

        /* ━━━━━━━━ Faction ━━━━━━━ */

        /// <summary>
        /// True if this troop belongs to a custom map-faction.
        /// This is independent from IsCustom.
        /// </summary>
        public bool IsFactionTroop
        {
            get
            {
                var list = Factions;
                if (list == null || list.Count == 0)
                    return false;

                for (int i = 0; i < list.Count; i++)
                    if (list[i] is WKingdom || list[i] is WClan)
                        return true;

                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Troop Type                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public TroopSourceFlags SourceFlags => SourceFlagCache.Get(this);

        [StaticClearAction(Refresh = true)]
        public static void InvalidateTroopSourceCaches()
        {
            FactionCache.Invalidate();
            SourceFlagCache.Invalidate();
            TreeFlagCache.Invalidate();

            // Also invalidate retinue conversion sources/targets caches.
            CacheRegistry.ClearGroup(ConversionCacheGroupKey);
        }

        public bool IsRetinue => (SourceFlags & TroopSourceFlags.Retinue) != 0;
        public bool IsRegular =>
            (SourceFlags & TroopSourceFlags.Elite) != 0
            || (SourceFlags & TroopSourceFlags.Basic) != 0; // Regular troops are from basic or elite sources
        public bool IsElite => (SourceFlags & TroopSourceFlags.Elite) != 0 || IsRetinue; // Retinues are always elite
        public bool IsBasic => (SourceFlags & TroopSourceFlags.Basic) != 0 || !IsElite; // Non-elite are always basic
        public bool IsMercenary => (SourceFlags & TroopSourceFlags.Mercenary) != 0;
        public bool IsBandit => (SourceFlags & TroopSourceFlags.Bandit) != 0;
        public bool IsMilitia => (SourceFlags & TroopSourceFlags.Militia) != 0;
        public bool IsCaravan => (SourceFlags & TroopSourceFlags.Caravan) != 0;
        public bool IsVillager => (SourceFlags & TroopSourceFlags.Villager) != 0;
        public bool IsCivilian => (SourceFlags & TroopSourceFlags.Civilian) != 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Variants                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// True if this character is a variant of another character (e.g., a captain variant).
        /// </summary>
        public bool IsVariant => NonVariantBase() != this;

        /// <summary>
        /// For captain variants, returns the base unit they were cloned from; otherwise, returns self
        /// </summary>
        public WCharacter NonVariantBase() => IsCaptain ? (CaptainBase ?? this) : this;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<CultureObject> CultureAttribute =>
            Attribute(c => c.Culture, priority: AttributePriority.High);

        public virtual WCulture Culture
        {
            get => WCulture.Get(CultureAttribute.Get());
            set
            {
                CultureAttribute.Set(value?.Base);

                FactionCache.Invalidate();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Code ━━━━━━━━━ */

        public CharacterCode GetCharacterCode(bool civilian = false) =>
            CharacterCode.CreateFrom(
# if BL13 || BL14
                Base,
                civilian ? Base.FirstCivilianEquipment : Base.FirstBattleEquipment
# else
                Base // No equipment type parameter in BL12
# endif
            );

        /* ━━━━━━━━━ Image ━━━━━━━━ */

# if BL13 || BL14
        public CharacterImageIdentifierVM GetImage(bool civilian = false) =>
#else
        public ImageIdentifierVM GetImage(bool civilian = false) =>
# endif
            new(GetCharacterCode(civilian));

        /* ━━━ Image Identifier ━━━ */

        public ImageIdentifier GetImageIdentifier(bool civilian = false) =>
# if BL13 || BL14
            new CharacterImageIdentifier(GetCharacterCode(civilian));
#else
            new(GetCharacterCode(civilian));
# endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Encyclopedia Visibility                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13 || BL14
        MAttribute<bool> HiddenInEncyclopediaAttribute =>
            Attribute<bool>(nameof(CharacterObject.HiddenInEncyclopedia));
#else
        MAttribute<bool> HiddenInEncyclopediaAttribute =>
            Attribute<bool>(nameof(CharacterObject.HiddenInEncylopedia)); // Typo in BL12
#endif

        public bool HiddenInEncyclopedia
        {
            get => HiddenInEncyclopediaAttribute.Get();
            set => HiddenInEncyclopediaAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Mariner Flag                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<bool> IsMarinerAttribute =>
            Attribute(
                getter: _ => NavalTraitHelper.GetMarinerLevel(Base) > 0,
                setter: (_, value) => NavalTraitHelper.SetMarinerLevel(Base, value ? 1 : 0)
            );

        public bool IsMariner
        {
            get => NonVariantBase().IsMarinerAttribute.Get();
            set => NonVariantBase().IsMarinerAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Mixed Gender Flag                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<bool> IsMixedGenderAttribute => Attribute(false);

        public bool IsMixedGender
        {
            get => IsMixedGenderAttribute.Get();
            set => IsMixedGenderAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Removal                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Removes this character.
        /// </summary>
        public void Remove()
        {
            // Never "remove" heroes here.
            if (IsHero)
                return;

            // Never "remove" root troops.
            if (IsRoot)
                return;

            // Snapshot upgrade sources to avoid modification during iteration.
            var sources = UpgradeSources.ToList();

            // Detach from sources so nobody upgrades into this anymore.
            foreach (var source in sources)
                source.RemoveUpgradeTarget(this);

            // Custom troop: "remove" means return to unassigned stub pool.
            if (IsCustom)
                IsActiveStub = false;

            // Hide from encyclopedia.
            HiddenInEncyclopedia = true;

            // If custom, clear dirty flags so the stub properties stop persisting.
            if (IsCustom)
                MarkAllAttributesClean();
        }
    }
}
