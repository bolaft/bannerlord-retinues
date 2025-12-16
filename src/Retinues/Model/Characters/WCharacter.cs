using System.Linq;
using Retinues.Model.Factions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Model.Characters
{
    public partial class WCharacter(CharacterObject @base)
        : WBase<WCharacter, CharacterObject>(@base),
            IEditableUnit
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Tier ━━━━━━━━━ */

        public int Tier => Base.Tier;

        /* ━━━━━━━━━ Level ━━━━━━━━ */

        MAttribute<int> LevelAttribute => Attribute<int>(nameof(CharacterObject.Level));

        public int Level
        {
            get => LevelAttribute.Get();
            set => LevelAttribute.Set(value);
        }

        /* ━━━━━━━━━ Name ━━━━━━━━━ */

        MAttribute<TextObject> NameAttribute => Attribute<TextObject>("_basicName");

        public string Name
        {
            get => NameAttribute.Get().ToString();
            set => NameAttribute.Set(new TextObject(value));
        }

        /* ━━━━━━━━━ Hero ━━━━━━━━━ */

        public WHero Hero => WHero.Get(Base.HeroObject);

        /* ━━━━━━━━━ Unit ━━━━━━━━━ */

        public IEditableUnit Editable => IsHero ? Hero : this;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Mutable Properties                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsCivilian { get; set; } = false;

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
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Encyclopedia Visibility                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        MAttribute<bool> HiddenInEncyclopediaAttribute =>
            Attribute<bool>(nameof(CharacterObject.HiddenInEncyclopedia));
#else
        MAttribute<bool> HiddenInEncyclopediaAttribute =>
            Attribute<bool>(nameof(CharacterObject.HiddenInEncylopedia));
#endif

        public bool HiddenInEncyclopedia
        {
            get => HiddenInEncyclopediaAttribute.Get();
            set => HiddenInEncyclopediaAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Stubs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const string CustomTroopPrefix = "retinues_custom_";

        MAttribute<bool> IsActiveStubAttribute => Attribute(false);

        /// <summary>
        /// Whether this WCharacter is currently allocated as an active stub for custom troop creation.
        /// </summary>
        public bool IsActiveStub
        {
            get => IsActiveStubAttribute.Get();
            set => IsActiveStubAttribute.Set(value);
        }

        /// <summary>
        /// Allocates a free stub WCharacter for new custom troop creation.
        /// </summary>
        public static WCharacter GetFreeStub()
        {
            foreach (var wc in All)
            {
                if (!wc.IsCustom)
                    continue;

                if (wc.IsActiveStub)
                    continue;

                // Mark as active.
                wc.IsActiveStub = true;

                // Found a free stub.
                return wc;
            }

            return null; // No free stubs.
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsHero => Base.IsHero;
        public bool IsPlayer => Base.IsPlayerCharacter;
        public bool IsCustom => StringId.StartsWith(CustomTroopPrefix);
        public bool IsVanilla => !IsCustom;
        public bool IsRoot => Root == this;
        public bool IsLeaf => UpgradeTargets.Count == 0;
        public bool IsElite => Root == Culture.RootElite;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<CultureObject> CultureAttribute => Attribute(c => c.Culture);

        public WCulture Culture
        {
            get => WCulture.Get(CultureAttribute.Get());
            set => CultureAttribute.Set(value?.Base);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Formation                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<FormationClass> FormationClassAttribute =>
            Attribute<FormationClass>(nameof(CharacterObject.DefaultFormationClass));

        public FormationClass FormationClass
        {
            get => FormationClassAttribute.Get();
            set => FormationClassAttribute.Set(value);
        }

        MAttribute<int> FormationGroupAttribute =>
            Attribute<int>(nameof(CharacterObject.DefaultFormationGroup));

        public int FormationGroup
        {
            get => FormationGroupAttribute.Get();
            set => FormationGroupAttribute.Set(value);
        }

        MAttribute<bool> IsRangedAttribute => Attribute<bool>(nameof(CharacterObject.IsRanged));

        public bool IsRanged
        {
            get => IsRangedAttribute.Get();
            set => IsRangedAttribute.Set(value);
        }

        MAttribute<bool> IsMountedAttribute => Attribute<bool>(nameof(CharacterObject.IsMounted));

        public bool IsMounted
        {
            get => IsMountedAttribute.Get();
            set => IsMountedAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Code ━━━━━━━━━ */

        public CharacterCode GetCharacterCode(bool civilian = false) =>
            CharacterCode.CreateFrom(
# if BL13
                Base,
                civilian ? Base.FirstCivilianEquipment : Base.FirstBattleEquipment
# else
                Base // No equipment type parameter in BL12
# endif
            );

        /* ━━━━━━━━━ Image ━━━━━━━━ */

# if BL13
        public CharacterImageIdentifierVM GetImage(bool civilian = false) =>
#else
        public ImageIdentifierVM GetImage(bool civilian = false) =>
# endif
            new(GetCharacterCode(civilian));

        /* ━━━ Image Identifier ━━━ */

        public ImageIdentifier GetImageIdentifier(bool civilian = false) =>
# if BL13
            new CharacterImageIdentifier(GetCharacterCode(civilian));
#else
            new(GetCharacterCode(civilian));
# endif
    }
}
