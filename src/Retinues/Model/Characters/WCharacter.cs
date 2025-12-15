using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Equipments;
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
                setter: (_, value) => _isActiveStub = value,
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
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public FormationClass FormationClass => Base.GetFormationClass();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<MBEquipmentRoster> EquipmentRosterAttribute =>
            Attribute<MBEquipmentRoster>("_equipmentRoster");

        public WEquipmentRoster EquipmentRoster
        {
            get => WEquipmentRoster.Get(EquipmentRosterAttribute.Get());
            set => EquipmentRosterAttribute.Set(value?.Base);
        }

        public List<MEquipment> Equipments
        {
            get
            {
                var list = EquipmentRoster?.Equipments.ToList();

                // Set owner references for persistence.
                foreach (var equipment in list)
                    equipment.Owner = this;

                return list;
            }
        }

        MAttribute<string> _equipmentCodesAttribute;

        public MAttribute<string> EquipmentCodesAttribute =>
            _equipmentCodesAttribute ??= new MAttribute<string>(
                baseInstance: Base, // CharacterObject => MBObjectBase => stable key
                getter: _ => EquipmentRoster?.GetEquipmentCodes() ?? string.Empty,
                setter: (_, value) => EquipmentRoster?.SetEquipmentCodes(value),
                targetName: "equipment_codes",
                persistent: true
            );

        public string EquipmentCodes
        {
            get => EquipmentCodesAttribute.Get();
            set => EquipmentCodesAttribute.Set(value);
        }

        /// <summary>
        /// Inserts the given set (by code) at the given index (0 by default).
        /// Mutates via the persistent EquipmentCodes attribute.
        /// </summary>
        public void AddEquipment(MEquipment equipment, int index = 0)
        {
            if (equipment == null)
                return;

            var codesString = EquipmentCodesAttribute.Get();
            var codes = string.IsNullOrEmpty(codesString)
                ? []
                : codesString.Split([';'], StringSplitOptions.None).ToList();

            var code = equipment.Code;
            if (string.IsNullOrEmpty(code))
                return;

            if (index < 0 || index > codes.Count)
                index = codes.Count;

            codes.Insert(index, code);

            EquipmentCodesAttribute.Set(string.Join(";", codes));
        }

        /// <summary>
        /// Removes the set at the given index (if valid), via the codes string.
        /// </summary>
        public void RemoveEquipment(int index)
        {
            var codesString = EquipmentCodesAttribute.Get();
            if (string.IsNullOrEmpty(codesString))
                return;

            var codes = codesString.Split([';'], StringSplitOptions.None).ToList();
            if (index < 0 || index >= codes.Count)
                return;

            codes.RemoveAt(index);

            var newString = codes.Count == 0 ? string.Empty : string.Join(";", codes);
            EquipmentCodesAttribute.Set(newString);
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
