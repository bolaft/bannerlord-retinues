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
                if (Parent == null)
                    return false; // Root troops cannot be deleted
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

        public int Race
        {
            get => Base.Race;
            set => Reflector.SetPropertyValue(Base, "Race", value);
        }

        public float Age
        {
            get => Base.Age;
            set => Reflector.SetPropertyValue(Base, "Age", value);
        }

        /* ━━━━━━━ Min Side ━━━━━━━ */

        public float AgeMin
        {
            get => Base.GetBodyPropertiesMin().Age;
            set => SetDynamicEnd(minEnd: true, age: value, weight: null, build: null);
        }
        public float WeightMin
        {
            get => Base.GetBodyPropertiesMin().Weight;
            set => SetDynamicEnd(minEnd: true, age: null, weight: value, build: null);
        }
        public float BuildMin
        {
            get => Base.GetBodyPropertiesMin().Build;
            set => SetDynamicEnd(minEnd: true, age: null, weight: null, build: value);
        }

        /* ━━━━━━━ Max Side ━━━━━━━ */

        public float AgeMax
        {
            get => Base.GetBodyPropertiesMax().Age;
            set => SetDynamicEnd(minEnd: false, age: value, weight: null, build: null);
        }
        public float WeightMax
        {
            get => Base.GetBodyPropertiesMax().Weight;
            set => SetDynamicEnd(minEnd: false, age: null, weight: value, build: null);
        }
        public float BuildMax
        {
            get => Base.GetBodyPropertiesMax().Build;
            set => SetDynamicEnd(minEnd: false, age: null, weight: null, build: value);
        }

        /* ━━━━ Min/Max Helper ━━━━ */
        public void SetDynamicEnd(bool minEnd, float? age, float? weight, float? build)
        {
            try
            {
                EnsureOwnBodyRange();

                var curMin = Base.GetBodyPropertiesMin();
                var curMax = Base.GetBodyPropertiesMax();

                var src = minEnd ? curMin : curMax;
                var oth = minEnd ? curMax : curMin;

                var dyn = src.DynamicProperties;
                var newDyn = new DynamicBodyProperties(
                    age    ?? dyn.Age,
                    weight ?? dyn.Weight,
                    build  ?? dyn.Build
                );

                var newSrc = new BodyProperties(newDyn, src.StaticProperties);
                var newMin = minEnd ? newSrc : oth;
                var newMax = minEnd ? oth    : newSrc;

                var range = Reflector.GetPropertyValue<object>(Base, "BodyPropertyRange");
                Reflector.InvokeMethod(
                    range, "Init",
                    [typeof(BodyProperties), typeof(BodyProperties)],
                    newMin, newMax
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /* ━━━━━━━━ Helper ━━━━━━━━ */

        /// <summary>
        /// Ensure this CharacterObject has its *own* BodyPropertyRange instance
        /// </summary>
        public void EnsureOwnBodyRange()
        {
            try
            {
                var current = Reflector.GetPropertyValue<object>(Base, "BodyPropertyRange");
                if (current == null)
                {
                    // Create a fresh one using current min/max
                    var min = Base.GetBodyPropertiesMin();
                    var max = Base.GetBodyPropertiesMax();
                    var type = typeof(BodyProperties); // just to get assembly; we'll reflect range type from current if needed

                    // Try to find the runtime range type via current; if null, create by name fallback
                    var rangeType = current?.GetType();
                    if (rangeType == null)
                    {
                        // Fallback: try to resolve MBBodyProperty type by name
                        rangeType = Type.GetType("TaleWorlds.Core.MBBodyProperty, TaleWorlds.Core");
                        if (rangeType == null)
                        {
                            // As a last resort, reuse whatever is already on Base (if any)
                            rangeType = current?.GetType();
                        }
                    }

                    var fresh = (rangeType != null) ? Activator.CreateInstance(rangeType) : null;
                    if (fresh != null)
                    {
                        Reflector.InvokeMethod(fresh, "Init", [typeof(BodyProperties), typeof(BodyProperties)], min, max);
                        Reflector.SetPropertyValue(Base, "BodyPropertyRange", fresh);
                    }
                    return;
                }

                // Try to clone; if clone not available, re-create with same min/max
                try
                {
                    var clone = Reflector.InvokeMethod(current, "Clone", Type.EmptyTypes);
                    if (clone != null)
                    {
                        Reflector.SetPropertyValue(Base, "BodyPropertyRange", clone);
                        return;
                    }
                }
                catch { /* ignore */ }

                var curMin = Base.GetBodyPropertiesMin();
                var curMax = Base.GetBodyPropertiesMax();
                var type2 = current.GetType();
                var fresh2 = Activator.CreateInstance(type2);
                Reflector.InvokeMethod(fresh2, "Init", [typeof(BodyProperties), typeof(BodyProperties)], curMin, curMax);
                Reflector.SetPropertyValue(Base, "BodyPropertyRange", fresh2);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /* ━━━━━━━ Height (Min/Max) ━━━━━━━ */

        // Height multiplier: KeyPart7 bits [19..24] (6 bits) → 0..1
        private const int HEIGHT_PART = 8;
        private const int HEIGHT_START = 19;
        private const int HEIGHT_BITS = 6;

        public float HeightMin
        {
            get => ReadStaticChannel(minEnd: true, HEIGHT_PART, HEIGHT_START, HEIGHT_BITS);
            set => SetStaticChannelEnd(minEnd: true, HEIGHT_PART, HEIGHT_START, HEIGHT_BITS, value);
        }
        public float HeightMax
        {
            get => ReadStaticChannel(minEnd: false, HEIGHT_PART, HEIGHT_START, HEIGHT_BITS);
            set => SetStaticChannelEnd(minEnd: false, HEIGHT_PART, HEIGHT_START, HEIGHT_BITS, value);
        }

        private float ReadStaticChannel(bool minEnd, int partIdx, int startBit, int numBits)
        {
            var bp = minEnd ? Base.GetBodyPropertiesMin() : Base.GetBodyPropertiesMax();
            var sp = bp.StaticProperties;
            ulong part = GetKeyPart(sp, partIdx);
            int raw = GetBitsValueFromKey(part, startBit, numBits);
            int max = (1 << numBits) - 1;
            return max > 0 ? raw / (float)max : 0f;
        }

        private void SetStaticChannelEnd(bool minEnd, int partIdx, int startBit, int numBits, float value01)
        {
            try
            {
                EnsureOwnBodyRange();

                float v = Math.Max(0f, Math.Min(1f, value01));
                int raw = (int)Math.Round(v * ((1 << numBits) - 1));

                var curMin = Base.GetBodyPropertiesMin();
                var curMax = Base.GetBodyPropertiesMax();

                var src = minEnd ? curMin : curMax;
                var oth = minEnd ? curMax : curMin;

                var sp = src.StaticProperties;
                ulong part = GetKeyPart(sp, partIdx);
                part = SetBits(part, startBit, numBits, raw);
                var newSp = SetKeyPart(sp, partIdx, part);

                var newSrc = new BodyProperties(src.DynamicProperties, newSp);
                var newMin = minEnd ? newSrc : oth;
                var newMax = minEnd ? oth    : newSrc;

                var range = Reflector.GetPropertyValue<object>(Base, "BodyPropertyRange");
                Reflector.InvokeMethod(
                    range, "Init",
                    [typeof(BodyProperties), typeof(BodyProperties)],
                    newMin, newMax
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /* ━━━ StaticBodyProperties bit and part helpers ━━━ */

        private static int GetBitsValueFromKey(ulong part, int startBit, int numBits)
        {
            ulong shifted = part >> startBit;
            ulong mask = ((1UL << numBits) - 1);
            return (int)(shifted & mask);
        }

        private static ulong SetBits(ulong part, int startBit, int numBits, int newValue)
        {
            ulong mask = (((1UL << numBits) - 1) << startBit);
            return (part & ~mask) | ((ulong)newValue << startBit);
        }

        private static ulong GetKeyPart(in StaticBodyProperties sp, int idx) => idx switch
        {
            1 => sp.KeyPart1, 2 => sp.KeyPart2, 3 => sp.KeyPart3, 4 => sp.KeyPart4,
            5 => sp.KeyPart5, 6 => sp.KeyPart6, 7 => sp.KeyPart7, _ => sp.KeyPart8
        };

        private static StaticBodyProperties SetKeyPart(in StaticBodyProperties sp, int idx, ulong val) => idx switch
        {
            1 => new(val, sp.KeyPart2, sp.KeyPart3, sp.KeyPart4, sp.KeyPart5, sp.KeyPart6, sp.KeyPart7, sp.KeyPart8),
            2 => new(sp.KeyPart1, val, sp.KeyPart3, sp.KeyPart4, sp.KeyPart5, sp.KeyPart6, sp.KeyPart7, sp.KeyPart8),
            3 => new(sp.KeyPart1, sp.KeyPart2, val, sp.KeyPart4, sp.KeyPart5, sp.KeyPart6, sp.KeyPart7, sp.KeyPart8),
            4 => new(sp.KeyPart1, sp.KeyPart2, sp.KeyPart3, val, sp.KeyPart5, sp.KeyPart6, sp.KeyPart7, sp.KeyPart8),
            5 => new(sp.KeyPart1, sp.KeyPart2, sp.KeyPart3, sp.KeyPart4, val, sp.KeyPart6, sp.KeyPart7, sp.KeyPart8),
            6 => new(sp.KeyPart1, sp.KeyPart2, sp.KeyPart3, sp.KeyPart4, sp.KeyPart5, val, sp.KeyPart7, sp.KeyPart8),
            7 => new(sp.KeyPart1, sp.KeyPart2, sp.KeyPart3, sp.KeyPart4, sp.KeyPart5, sp.KeyPart6, val, sp.KeyPart8),
            _ => new(sp.KeyPart1, sp.KeyPart2, sp.KeyPart3, sp.KeyPart4, sp.KeyPart5, sp.KeyPart6, sp.KeyPart7, val),
        };
    }
}
