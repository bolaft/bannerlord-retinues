using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Helpers;
using Retinues.Game.Helpers.Character;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for CharacterObject, providing helpers for custom troop logic, equipment, skills, upgrades, and lifecycle.
    /// Used for all custom troop operations and UI integration.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
    public class WCharacter(CharacterObject characterObject) : StringIdentifier
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Character Helper                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly ICharacterHelper _customHelper = new CustomCharacterHelper();
        private static readonly ICharacterHelper _vanillaHelper = new VanillaCharacterHelper();
        private readonly ICharacterHelper _helper = LooksCustomId(characterObject?.StringId)
            ? _customHelper
            : _vanillaHelper;

        private static bool LooksCustomId(string id) =>
            id?.StartsWith("ret_", StringComparison.Ordinal) == true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter(
            bool isKingdom,
            bool isElite,
            bool isRetinue = false,
            bool isMilitiaMelee = false,
            bool isMilitiaRanged = false,
            IReadOnlyList<int> path = null
        )
            : this(
                _customHelper.GetCharacterObject(
                    isKingdom,
                    isElite,
                    isRetinue,
                    isMilitiaMelee,
                    isMilitiaRanged,
                    path
                )
            ) { }

        public WCharacter(string stringId)
            : this(
                (LooksCustomId(stringId) ? _customHelper : _vanillaHelper).GetCharacterObject(
                    stringId
                )
            ) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Identity                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CharacterObject _co =
            characterObject ?? throw new ArgumentNullException(nameof(characterObject));

        public CharacterObject Base => _co;

        public override string StringId => _co.StringId;

        public static Dictionary<string, string> VanillaStringIdMap = [];

        public string VanillaStringId =>
            VanillaStringIdMap.TryGetValue(StringId, out var vid) ? vid : StringId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                View-Model (VM) Accessors               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public CharacterCode CharacterCode => CharacterCode.CreateFrom(Base);
        public ImageIdentifier ImageIdentifier => new CharacterImageIdentifier(CharacterCode);

#if BL13
        public CharacterImageIdentifierVM Image => new(CharacterCode);
#else
        public ImageIdentifierVM Image => new(CharacterCode);
#endif

        public CharacterViewModel Model
        {
            get
            {
                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                vm.FillFrom(Base, seed: -1);

                // Apply staged equipment changes (if any)
                vm.SetEquipment(EquipmentPreview.BuildStagedEquipment(this));

                if (Faction != null)
                {
                    // Armor colors
                    vm.ArmorColor1 = Faction.Color;
                    vm.ArmorColor2 = Faction.Color2;

                    // Heraldic items
                    vm.BannerCodeText = Faction.BannerCodeText;
                }

                return vm;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Tree, Relations & Faction               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsCustom => _helper.IsCustom(StringId);
        public bool IsElite => _helper.IsElite(StringId);
        public bool IsRetinue => _helper.IsRetinue(StringId);
        public bool IsMilitia => IsMilitiaMelee || IsMilitiaRanged;
        public bool IsMilitiaMelee => _helper.IsMilitiaMelee(StringId);
        public bool IsMilitiaRanged => _helper.IsMilitiaRanged(StringId);

        public WCharacter Parent => _helper.GetParent(this);
        public WFaction Faction => _helper.ResolveFaction(StringId);

        public IReadOnlyList<int> PositionInTree => _helper.GetPath(StringId);
        public IEnumerable<WCharacter> Tree
        {
            get
            {
                yield return this;
                foreach (var child in UpgradeTargets)
                foreach (var descendant in child.Tree)
                    yield return descendant;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Basic Attributes                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name
        {
            get => Base.Name.ToString();
            set
            {
                Reflector.InvokeMethod(
                    Base,
                    "SetName",
                    [typeof(TextObject)],
                    new TextObject(value, null)
                );
            }
        }

        public int Tier => Base.Tier;

        public int Level
        {
            get => Base.Level;
            set => Base.Level = value;
        }

        public WCulture Culture => new(Base.Culture);

        public FormationClass FormationClass
        {
            get => Base.GetFormationClass();
            set
            {
                try
                {
                    if (!IsCustom)
                        return;
                    // protected setter -> set via reflection
                    Reflector.SetPropertyValue(Base, "DefaultFormationClass", value);
                    Reflector.SetPropertyValue(Base, "DefaultFormationGroup", (int)value);
                    var isRanged =
                        value == FormationClass.Ranged || value == FormationClass.HorseArcher;
                    var isMounted =
                        value == FormationClass.Cavalry || value == FormationClass.HorseArcher;
                    Reflector.SetFieldValue(Base, "_isRanged", isRanged);
                    Reflector.SetFieldValue(Base, "_isMounted", isMounted);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        public void ResetFormationClass()
        {
            // Reset to default formation class based on equipment
            FormationClass = GetFormationClass();
        }

        public FormationClass GetFormationClass()
        {
            if (!IsCustom)
                return Base.GetFormationClass();

            return (IsRanged, IsMounted) switch
            {
                (true, true) => FormationClass.HorseArcher,
                (true, false) => FormationClass.Ranged,
                (false, true) => FormationClass.Cavalry,
                (false, false) => FormationClass.Infantry,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Flags & Toggles                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsMaxTier => Tier >= (IsElite ? 6 : 5);

        public bool IsHero => Base.IsHero;

        public bool IsFemale
        {
            get => Reflector.GetPropertyValue<bool>(Base, "IsFemale");
            set => Reflector.SetPropertyValue(Base, "IsFemale", value);
        }

        public bool HiddenInEncyclopedia
        {
#if BL13
            // NOTE: fixed typo in 1.3.0
            get => Reflector.GetPropertyValue<bool>(Base, "HiddenInEncyclopedia");
            set => Reflector.SetPropertyValue(Base, "HiddenInEncyclopedia", value);
#else
            // NOTE: game-side property is misspelled "HiddenInEncylopedia"
            get => Reflector.GetPropertyValue<bool>(Base, "HiddenInEncylopedia");
            set => Reflector.SetPropertyValue(Base, "HiddenInEncylopedia", value);
#endif
        }

        public bool IsNotTransferableInHideouts
        {
            get => Base.IsNotTransferableInHideouts;
            set => Base.SetTransferableInHideouts(!value);
        }

        public bool IsNotTransferableInPartyScreen
        {
            get => Base.IsNotTransferableInPartyScreen;
            set => Base.SetTransferableInPartyScreen(!value);
        }

        public bool IsRanged => Equipment.HasNonThrowableRangedWeapons;

        public bool IsMounted => Equipment.HasMount;

        public bool IsRuler => Base.HeroObject?.IsFactionLeader ?? false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly SkillObject[] CoreSkills =
        [
            DefaultSkills.Athletics,
            DefaultSkills.Riding,
            DefaultSkills.OneHanded,
            DefaultSkills.TwoHanded,
            DefaultSkills.Polearm,
            DefaultSkills.Bow,
            DefaultSkills.Crossbow,
            DefaultSkills.Throwing,
        ];

        public Dictionary<SkillObject, int> Skills
        {
            get { return CoreSkills.ToDictionary(skill => skill, GetSkill); }
            set
            {
                foreach (var skill in CoreSkills)
                {
                    var v = (value != null && value.TryGetValue(skill, out var val)) ? val : 0;
                    SetSkill(skill, v);
                }
            }
        }

        public int GetSkill(SkillObject skill) => Base.GetSkillValue(skill);

        public void SetSkill(SkillObject skill, int value)
        {
            var skills = Reflector.GetFieldValue<MBCharacterSkills>(Base, "DefaultCharacterSkills");
            ((PropertyOwner<SkillObject>)(object)skills.Skills).SetPropertyValue(skill, value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<WEquipment> Equipments
        {
            get
            {
#if BL13
                MBEquipmentRoster roster = Reflector.GetFieldValue<MBEquipmentRoster>(
                    Base,
                    "_equipmentRoster"
                );
                return [.. roster.AllEquipments.Select(e => new WEquipment(e))];
#else
                var equipments = Base.AllEquipments.ToList();
                return [.. equipments.Select(e => new WEquipment(e))];
#endif
            }
            set
            {
#if BL13
                var equipments = value.Select(e => e.Base).ToList();
                foreach (var eq in equipments)
                {
                    // Ensure IsBattle
                    Reflector.SetFieldValue(
                        eq,
                        "_equipmentType",
                        TaleWorlds.Core.Equipment.EquipmentType.Battle
                    );
                }

                // Add a civilian equipment identical to the first battle one
                var civilian = new Equipment(equipments[0]);
                Reflector.SetFieldValue(
                    civilian,
                    "_equipmentType",
                    TaleWorlds.Core.Equipment.EquipmentType.Civilian
                );
                equipments.Add(civilian);

                MBEquipmentRoster roster = Reflector.GetFieldValue<MBEquipmentRoster>(
                    Base,
                    "_equipmentRoster"
                );
                Reflector.SetFieldValue(roster, "_equipments", new MBList<Equipment>(equipments));
                Reflector.SetFieldValue(Base, "_equipmentRoster", roster);
#else
                var equipments = value.Select(e => e.Base).ToList();

                // Add a civilian equipment identical to the first battle one
                var civilian = new Equipment(equipments[0]);
                Reflector.SetFieldValue(
                    civilian,
                    "_equipmentType",
                    TaleWorlds.Core.Equipment.EquipmentType.Civilian
                );
                equipments.Add(civilian);
                var roster = new MBEquipmentRoster();
                // Set the internal equipment list via reflection.
                Reflector.SetFieldValue(roster, "_equipments", new MBList<Equipment>(equipments));
                Reflector.SetFieldValue(Base, "_equipmentRoster", roster);
#endif
            }
        }

        public WEquipment Equipment
        {
            get
            {
#if BL13
                var first = Equipments.FirstOrDefault();
                return first is null
                    ? new WEquipment(new Equipment(TaleWorlds.Core.Equipment.EquipmentType.Battle))
                    : Equipments[0];
#else
                var first = Equipments.FirstOrDefault();
                return first is null
                    ? new WEquipment(MBEquipmentRoster.EmptyEquipment)
                    : new WEquipment(first.Base);
#endif
            }
        }

        public bool CanEquip(WItem item)
        {
            if (item == null)
                return true;
            if (item.RelevantSkill == null)
                return true;
            return item.Difficulty <= GetSkill(item.RelevantSkill);
        }

        public void Equip(WItem item, EquipmentIndex slot)
        {
            Equipment.SetItem(slot, item);

            // Force recalculation of formation class based on equipment
            ResetFormationClass();

            if (slot == EquipmentIndex.Horse)
            {
                // Horse change may affect upgrade requirements
                ResetUpgradeRequiresItemFromCategory();

                // Same for children, if any
                foreach (var child in UpgradeTargets)
                    child.ResetUpgradeRequiresItemFromCategory();
            }

            // Update civilian equipment to match the new battle equipment
            // (the setter of Equipments ensures a civilian copy is always present)
            Equipments = [.. Equipments.Take(Equipments.Count - 1)];
        }

        public WItem Unequip(EquipmentIndex slot, bool resetFormation = true)
        {
            var item = Equipment.GetItem(slot);
            Equipment.SetItem(slot, null);

            // Force recalculation of formation class based on equipment
            if (resetFormation)
                ResetFormationClass();

            return item;
        }

        public IEnumerable<WItem> UnequipAll()
        {
            foreach (var slot in WEquipment.Slots)
                yield return Unequip(slot, resetFormation: false);

            // After all items are unequipped, reset formation class once
            ResetFormationClass();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Upgrades Targets                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Helper to convert a list of WCharacter to CharacterObject array
        private static CharacterObject[] ToCharacterArray(IEnumerable<WCharacter> items) =>
            items?.Select(wc => wc.Base).ToArray() ?? [];

        public WCharacter[] UpgradeTargets
        {
            get
            {
                var raw =
                    Reflector.GetPropertyValue<CharacterObject[]>(Base, "UpgradeTargets") ?? [];
                return [.. raw.Select(obj => new WCharacter(obj))];
            }
            set => Reflector.SetPropertyValue(Base, "UpgradeTargets", ToCharacterArray(value));
        }

        public ItemCategory UpgradeRequiresItemFromCategory
        {
            get { return Base.UpgradeRequiresItemFromCategory; }
            set
            {
                if (!IsCustom)
                    return;

                Reflector.SetPropertyValue(Base, "UpgradeRequiresItemFromCategory", value);
            }
        }

        public void ResetUpgradeRequiresItemFromCategory()
        {
            if (!IsCustom)
                return;

            var horse = Equipment?.GetItem(EquipmentIndex.Horse);
            if (horse == null)
            {
                // Target isn't mounted, no upgrade requirement.
                UpgradeRequiresItemFromCategory = null;
                return;
            }

            var parentHorse = Parent?.Equipment?.GetItem(EquipmentIndex.Horse);

            // Parent mounted with the same category, no upgrade requirement.
            if (parentHorse != null && parentHorse.Category == horse.Category)
            {
                UpgradeRequiresItemFromCategory = null;
                return;
            }

            // Otherwise, pay for this step (first time getting a mount or switching to war horse).
            UpgradeRequiresItemFromCategory = horse.Category;
        }

        public void AddUpgradeTarget(WCharacter target)
        {
            if (UpgradeTargets.Any(wc => wc == target))
                return;

            var list = UpgradeTargets?.ToList() ?? [];
            list.Add(target);
            Reflector.SetPropertyValue(Base, "UpgradeTargets", ToCharacterArray(list));
        }

        public void RemoveUpgradeTarget(WCharacter target)
        {
            var list = UpgradeTargets?.ToList() ?? [];
            list.RemoveAll(wc => wc == target);
            Reflector.SetPropertyValue(Base, "UpgradeTargets", ToCharacterArray(list));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Registration & Lifecycle                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void Remove()
        {
            // Remove from parent's upgrade targets
            Parent?.RemoveUpgradeTarget(this);

            Log.Debug(
                $"Removed troop {Name} from parent {Parent?.Name ?? "null"} and faction {Faction?.Name ?? "null"}"
            );

            // Unregister from the game systems
            Deactivate();

            // Remove all children
            foreach (var target in UpgradeTargets)
                target.Remove();
        }

        public static List<string> ActiveTroops { get; } = [];
        public bool IsActive => !IsCustom || ActiveTroops.Contains(StringId);
        public bool IsValid =>
            IsActive
            && Base != null
            && !string.IsNullOrWhiteSpace(StringId)
            && !string.IsNullOrWhiteSpace(Name);

        public void Activate()
        {
            HiddenInEncyclopedia = false;
            IsNotTransferableInHideouts = false;

            if (IsRetinue)
                IsNotTransferableInPartyScreen = true;
            else
                IsNotTransferableInPartyScreen = false;

            if (!IsActive)
                ActiveTroops.Add(StringId);
        }

        public void Deactivate()
        {
            HiddenInEncyclopedia = true;
            IsNotTransferableInPartyScreen = true;
            IsNotTransferableInHideouts = true;

            if (IsActive)
                ActiveTroops.Remove(StringId);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cloning                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void FillFrom(
            WCharacter src,
            bool keepUpgrades = true,
            bool keepEquipment = true,
            bool keepSkills = true
        )
        {
            // Character object copy
            _helper.CopyInto(src.Base, _co);

            // Vanilla id
            VanillaStringIdMap[StringId] = src.VanillaStringId;

            // Upgrades
            UpgradeTargets = keepUpgrades ? [.. src.UpgradeTargets] : [];

            // Equipment - re-create from code to avoid shared references
            if (keepEquipment)
                Equipments = [WEquipment.FromCode(src.Equipment.Code)];
            else
                Equipments = [WEquipment.FromCode(null)];

            // Detach skills so parent/clone no longer share the same container
            var freshSkills = (MBCharacterSkills)
                Activator.CreateInstance(typeof(MBCharacterSkills), nonPublic: true);
            Reflector.SetFieldValue(_co, "DefaultCharacterSkills", freshSkills);

            // Upgrade item requirement
            ResetUpgradeRequiresItemFromCategory();

            // Skills
            if (keepSkills)
                Skills = CoreSkills.ToDictionary(skill => skill, src.GetSkill);
            else
                Skills = [];
        }
    }
}
