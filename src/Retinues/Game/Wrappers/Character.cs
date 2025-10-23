using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Game.Helpers;
using Retinues.Game.Helpers.Character;
using Retinues.Safety.Sanitizer;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Localization;
# if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

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

#if BL13
        public CharacterImageIdentifierVM Image => new(CharacterCode);
        public ImageIdentifier ImageIdentifier => new CharacterImageIdentifier(CharacterCode);
#else
        public ImageIdentifierVM Image => new(CharacterCode);
        public ImageIdentifier ImageIdentifier => new(CharacterCode);
#endif

        public CharacterViewModel GetModel(int index = 0)
        {
            var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
            vm.FillFrom(Base, seed: -1);

            // Apply staged equipment changes (if any)
            vm.SetEquipment(EquipmentPreview.BuildStagedEquipment(this, index));

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

        public WCulture Culture
        {
            get => new(Base.Culture);
            set
            {
                try
                {
                    if (value == null) return;
                    if (IsHero) return; // Skip heroes (their culture lives on HeroObject.Culture)

                    // CharacterObject has a 'new Culture' with a private setter that forwards to base.Culture.
                    // Explicitly target the declaring base property to avoid AmbiguousMatchException.
                    var baseType = typeof(BasicCharacterObject); 
                    var prop = baseType.GetProperty(
                        "Culture",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                    );
                    prop?.SetValue(Base, value.Base, null);

                    ApplyCultureVisualsFrom(value.Base);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

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

        public bool IsDeletable
        {
            get
            {
                if (!IsCustom)
                    return false; // Vanilla troops cannot be deleted
                if (IsRetinue)
                    return false; // Retinues cannot be deleted
                if (IsMilitia)
                    return false; // Militias cannot be deleted
                if (IsHero)
                    return false; // Heroes cannot be deleted
                if (UpgradeTargets.Any())
                    return false; // Troops with upgrades cannot be deleted
                return true;
            }
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

        public WLoadout Loadout => new(this);

        public void Equip(WItem item, EquipmentIndex slot, int index = 0)
        {
            if (item != null && !item.Slots.Contains(slot))
                return; // Invalid slot for this item

            // Get equipment in specified category/index
            var equipment = Loadout.Get(index);

            // Equip item in correct equipment's specified slot
            equipment.SetItem(slot, item);

            // Formation class is derived from main battle equipment
            if (equipment.Category == EquipmentCategory.Battle)
                FormationClass = Loadout.Battle.ComputeFormationClass();

            // Horse requirements may need an update
            if (slot == EquipmentIndex.Horse)
                UpgradeItemRequirement = Loadout.ComputeUpgradeItemRequirement();

            // Cascade to children
            foreach (var child in UpgradeTargets)
                child.UpgradeItemRequirement = child.Loadout.ComputeUpgradeItemRequirement();
        }

        public void Unequip(EquipmentIndex slot, int index = 0, bool stock = false)
        {
            if (stock)
                Loadout.Get(index).Get(slot)?.Stock();

            // Same as equip with null item
            Equip(null, slot, index);
        }

        public void UnequipAll(int index = 0, bool stock = false)
        {
            foreach (var slot in WEquipment.Slots)
                Unequip(slot, index: index, stock: stock);
        }

        public void Unstage(EquipmentIndex slot, int index = 0, bool stock = false)
        {
            if (stock)
                Loadout.Get(index).GetStaged(slot)?.Stock();

            Loadout.Get(index).UnstageItem(slot);
        }

        public void UnstageAll(int index, bool stock = false)
        {
            foreach (var slot in WEquipment.Slots)
                Unstage(slot, index: index, stock: stock);
        }

        public bool MeetsItemRequirements(WItem item)
        {
            if (item == null)
                return true;
            if (item.RelevantSkill == null)
                return true;
            return item.Difficulty <= GetSkill(item.RelevantSkill);
        }

        public bool IsRanged => Loadout.Battle.HasNonThrowableRangedWeapons;
        public bool IsMounted => Loadout.Battle.HasMount;

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

        public ItemCategory UpgradeItemRequirement
        {
            get { return Base.UpgradeRequiresItemFromCategory; }
            set
            {
                if (!IsCustom)
                    return;

                Reflector.SetPropertyValue(Base, "UpgradeRequiresItemFromCategory", value);
            }
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

        public void Remove(bool stock = false)
        {
            if (!IsCustom)
                return;

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

            // Revert existing instances in parties
            SanitizerBehavior.Sanitize();
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

            FormationClass = Loadout.Battle.ComputeFormationClass();
            UpgradeItemRequirement = Loadout.ComputeUpgradeItemRequirement();

            if (!IsActive)
                ActiveTroops.Add(StringId);
        }

        public void Deactivate()
        {
            HiddenInEncyclopedia = true;
            IsNotTransferableInPartyScreen = false;
            IsNotTransferableInHideouts = false;

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

            // Detach skills so parent/clone no longer share the same container
            var freshSkills = (MBCharacterSkills)
                Activator.CreateInstance(typeof(MBCharacterSkills), nonPublic: true);
            Reflector.SetFieldValue(_co, "DefaultCharacterSkills", freshSkills);

            // Skills
            if (keepSkills)
                Skills = CoreSkills.ToDictionary(skill => skill, src.GetSkill);
            else
                Skills = [];

            // Equipment - re-create from code to avoid shared references
            if (keepEquipment)
            {
                // Loadout copy
                Loadout.FillFrom(src.Loadout);

                // Upgrade item requirement refresh
                UpgradeItemRequirement = Loadout.ComputeUpgradeItemRequirement();

                // Formation class refresh
                FormationClass = Loadout.Battle.ComputeFormationClass();
            }
            else
                Loadout.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Visuals                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Re-derive body/face and style from a culture template.
        /// </summary>
        public void ApplyCultureVisualsFrom(CultureObject culture)
        {
            try
            {
                if (culture == null || IsHero) return; // skip heroes

                var template = culture.BasicTroop ?? culture.EliteBasicTroop;
                if (template == null) return;

                // break shared reference
                EnsureOwnBodyRange();

                var range = Reflector.GetPropertyValue<object>(Base, "BodyPropertyRange");
                var tplRange = Reflector.GetPropertyValue<object>(template, "BodyPropertyRange");

                // 1) Copy style tags & race (affects FaceGen sampling)
                Reflector.SetPropertyValue(Base, "Race", template.Race);

                // 2) Copy min/max envelope from template
                var min = template.GetBodyPropertiesMin();
                var max = template.GetBodyPropertiesMax();
                Reflector.InvokeMethod(range, "Init", [typeof(BodyProperties), typeof(BodyProperties)], min, max);

                // 3) Copy style tags
#if BL13
                if (tplRange != null && range != null)
                {
                    var hairSrc = Reflector.GetPropertyValue<IEnumerable<string>>(tplRange, "HairTags");
                    var beardSrc = Reflector.GetPropertyValue<IEnumerable<string>>(tplRange, "BeardTags");
                    var tattooSrc = Reflector.GetPropertyValue<IEnumerable<string>>(tplRange, "TattooTags");

                    ReplaceStringCollection(range, "HairTags", hairSrc);
                    ReplaceStringCollection(range, "BeardTags", beardSrc);
                    ReplaceStringCollection(range, "TattooTags", tattooSrc);
                }
#else
                Reflector.SetPropertyValue(Base, "HairTags", template.HairTags);
                Reflector.SetPropertyValue(Base, "BeardTags", template.BeardTags);
                Reflector.SetPropertyValue(Base, "TattooTags", template.TattooTags);
#endif

                // 4) Snap age to the template’s mid-age
                var midAge = (min.Age + max.Age) * 0.5f;
                Reflector.SetPropertyValue(Base, "Age", midAge);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// Ensure this CharacterObject has its own BodyPropertyRange instance (not shared with clones).
        /// </summary>
        private void EnsureOwnBodyRange()
        {
            // Get current range (may be shared across clones)
            var current = Reflector.GetPropertyValue<object>(Base, "BodyPropertyRange");
            if (current == null)
                return;

            // Create a brand-new range object of the same runtime type
            var rangeType = current.GetType(); // e.g., TaleWorlds.Core.BodyPropertyRange
            var fresh = Activator.CreateInstance(rangeType);

            // Swap it in so this troop no longer shares with anyone else
            Reflector.SetPropertyValue(Base, "BodyPropertyRange", fresh);
        }

        /// <summary>
        /// Clone/replace a string-collection property on BodyPropertyRange.
        /// </summary>
        private static void ReplaceStringCollection(object range, string propName, IEnumerable<string> source)
        {
            var target = Reflector.GetPropertyValue<object>(range, propName);
            if (target == null) return;

            // Try IList<string>: clear + add (avoids needing the concrete MBList type)
            if (target is IList<string> list)
            {
                list.Clear();
                foreach (var s in source ?? [])
                    list.Add(s);
                return;
            }

            // Fallback: build a new List<string> and set the property if it has a public/private setter
            var cloned = source != null ? [.. source] : new List<string>();
            try { Reflector.SetPropertyValue(range, propName, cloned); }
            catch { }
        }
    }
}
