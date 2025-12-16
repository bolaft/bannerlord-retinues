using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Equipments;
using Retinues.Model.Factions;
using Retinues.Utilities;
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

            // Snapshot upgrade sources to avoid modification during iteration.
            var sources = UpgradeSources.ToList();

            // Detach from sources so nobody upgrades into this anymore.
            foreach (var source in sources)
                source.RemoveUpgradeTarget(this);

            // Custom troop: "remove" means return to unassigned stub pool.
            if (IsCustom)
            {
                if (IsRoot)
                    return;

                IsActiveStub = false; // also clamps encyclopedia visibility via stub rule
                return;
            }

            // Vanilla troop: "remove" means hide (persisted).
            HiddenInEncyclopedia = true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Encyclopedia Visibility                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool HiddenInEncyclopedia
        {
            get => HiddenInEncyclopediaAttribute.Get();
            set => HiddenInEncyclopediaAttribute.Set(value);
        }

        bool _hiddenInEncyclopedia;

        MAttribute<bool> _hiddenInEncyclopediaAttribute;
        MAttribute<bool> HiddenInEncyclopediaAttribute
        {
            get
            {
                if (_hiddenInEncyclopediaAttribute != null)
                    return _hiddenInEncyclopediaAttribute;

                _hiddenInEncyclopedia = ReadHiddenInEncyclopediaFromBase();

                _hiddenInEncyclopediaAttribute = new MAttribute<bool>(
                    baseInstance: Base,
                    getter: _ => _hiddenInEncyclopedia,
                    setter: (_, value) =>
                    {
                        _hiddenInEncyclopedia = value;
                        ApplyHiddenInEncyclopediaToBase(value);

                        // Clamp for custom stubs (inactive must be hidden, active must be visible).
                        EnforceStubEncyclopediaRule();
                    },
                    targetName: "hidden_in_encyclopedia"
                );

                // Clamp on first init too (covers "unassigned stub should always be hidden").
                EnforceStubEncyclopediaRule();

                return _hiddenInEncyclopediaAttribute;
            }
        }

        bool ReadHiddenInEncyclopediaFromBase()
        {
            try
            {
#if BL13
                return Reflection.GetPropertyValue<bool>(Base, "HiddenInEncyclopedia");
#else
                return Reflection.GetPropertyValue<bool>(Base, "HiddenInEncylopedia");
#endif
            }
            catch
            {
                return false;
            }
        }

        void ApplyHiddenInEncyclopediaToBase(bool hidden)
        {
            try
            {
#if BL13
                Reflection.SetPropertyValue(Base, "HiddenInEncyclopedia", hidden);
#else
                Reflection.SetPropertyValue(Base, "HiddenInEncylopedia", hidden);
#endif
            }
            catch { }
        }

        /// <summary>
        /// Invariant for custom troop stubs:
        /// - Unassigned stub (inactive) => always hidden in encyclopedia
        /// - Assigned stub (active) => always visible in encyclopedia
        /// This does not affect vanilla troops.
        /// </summary>
        void EnforceStubEncyclopediaRule()
        {
            if (!IsCustom)
                return;

            if (IsHero)
                return;

            var forcedHidden = !IsActiveStub;

            if (_hiddenInEncyclopedia == forcedHidden)
                return;

            _hiddenInEncyclopedia = forcedHidden;
            ApplyHiddenInEncyclopediaToBase(forcedHidden);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Stubs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const string CustomTroopPrefix = "retinues_custom_";

        /// <summary>
        /// Whether this WCharacter is currently allocated as an active stub for custom troop creation.
        /// </summary>
        public bool IsActiveStub
        {
            get => IsActiveStubAttribute.Get();
            set => IsActiveStubAttribute.Set(value);
        }

        bool _isActiveStub = false; // Default to inactive.

        MAttribute<bool> _isActiveStubAttribute;
        MAttribute<bool> IsActiveStubAttribute =>
            _isActiveStubAttribute ??= new MAttribute<bool>(
                baseInstance: Base,
                getter: _ => _isActiveStub,
                setter: (_, value) =>
                {
                    _isActiveStub = value;

                    // Keep custom stubs consistent with encyclopedia visibility.
                    EnforceStubEncyclopediaRule();
                },
                targetName: "is_active_stub"
            );

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

                // Mark as active (also forces encyclopedia visibility via setter).
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
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public FormationClass FormationClass => Base.GetFormationClass();

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
