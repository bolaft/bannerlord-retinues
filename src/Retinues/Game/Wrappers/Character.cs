using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Configuration;
using Retinues.Game.Helpers;
using Retinues.Mods;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
# if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for CharacterObject, exposing custom troop logic and properties.
    /// </summary>
    [SafeClass]
    public class WCharacter(CharacterObject characterObject) : BaseFactionMember
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WCharacter FromStringId(string stringId) =>
            new(MBObjectManager.Instance.GetObject<CharacterObject>(stringId));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Constants                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const string CustomIdPrefix = "retinues_custom_";
        public const string LegacyCustomIdPrefix = "ret_";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Stubs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// List of active custom troop ids.
        /// </summary>
        public static List<string> ActiveStubIds { get; } = [];

        /// <summary>
        /// Allocates a free stub CharacterObject for new custom troop creation.
        /// </summary>
        public static CharacterObject AllocateStub()
        {
            foreach (var co in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
                if (co.StringId.StartsWith(CustomIdPrefix))
                    if (!ActiveStubIds.Contains(co.StringId)) // not allocated yet
                        return co;

            throw new InvalidOperationException("No free stub CharacterObject available.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensures the given string id is valid, or returns null.
        /// Used to force old legacy ids to be reallocated.
        /// </summary>
        private static string NullifyLegacyIds(string stringId) =>
            stringId?.StartsWith(LegacyCustomIdPrefix) == true ? null : stringId;

        // Constructor for root troops
        public WCharacter(WFaction faction, RootCategory category, string stringId = null)
            : this(NullifyLegacyIds(stringId) ?? AllocateStub().StringId)
        {
            // Enforce binding on construction
            faction.SetRoot(category, this);

            Initialize(faction);
        }

        // Constructor for upgrade troops
        public WCharacter(WCharacter parent, string stringId = null)
            : this(NullifyLegacyIds(stringId) ?? AllocateStub().StringId)
        {
            Initialize(parent.Faction);

            // Set parent AFTER initialization
            Parent = parent;
        }

        /// <summary>
        /// Common initialization logic for custom troops.
        /// </summary>
        private void Initialize(BaseFaction faction)
        {
            // Register as active stub
            ActiveStubIds.Add(StringId);

            // Assign to faction
            Faction = faction;

            // Toggle flags
            HiddenInEncyclopedia = false;
            IsNotTransferableInHideouts = false;
            IsNotTransferableInPartyScreen = IsRetinue;
        }

        /// <summary>
        /// Computes derived properties such as FormationClass and UpgradeItemRequirement.
        /// </summary>
        public void ComputeDerivedProperties()
        {
            FormationClass = ComputeFormationClass();
            UpgradeItemRequirement = Loadout.ComputeUpgradeItemRequirement();
        }

        /// <summary>
        /// Creates a new WCharacter wrapper around the specified CharacterObject id.
        /// </summary>
        public WCharacter(string stringId)
            : this(MBObjectManager.Instance.GetObject<CharacterObject>(stringId)) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Vanilla                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Vanilla troops that were modified through the editor and must be persisted.
        /// </summary>
        public static HashSet<string> EditedVanillaRootIds { get; } =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// True if this is a vanilla troop that has been edited in the studio/global editor.
        /// Custom troops always return true here.
        /// </summary>
        public bool NeedsPersistence
        {
            get => IsCustom || (IsVanilla && EditedVanillaRootIds.Contains(Root.StringId));
            set
            {
                if (IsCustom)
                    return; // Custom troops are always persisted

                if (value)
                    EditedVanillaRootIds.Add(Root.StringId);
                else
                    EditedVanillaRootIds.Remove(Root.StringId);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Identity                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━ CharacterObject ━━━ */

        private readonly CharacterObject _co =
            characterObject ?? throw new ArgumentNullException(nameof(characterObject));

        public CharacterObject Base => _co;

        public override string StringId => Base.StringId;

        /* ━━━━━━━ Template ━━━━━━━ */

        public static Dictionary<string, string> VanillaStringIdMap = [];

        public string VanillaStringId =>
            VanillaStringIdMap.TryGetValue(StringId, out var vid) ? vid : StringId;

        /* ━━━━━━━━━ Type ━━━━━━━━━ */

        public bool IsCustom =>
            StringId.StartsWith(CustomIdPrefix) == true
            || StringId.StartsWith(LegacyCustomIdPrefix) == true;
        public bool IsLegacyCustom => StringId.StartsWith(LegacyCustomIdPrefix) == true;
        public bool IsVanilla => !IsCustom;

        /* ━━━━━━━ Category ━━━━━━━ */

        public bool IsRetinue => Faction != null && Faction.IsRetinue(this);
        public bool IsRegular => Faction != null && Faction.IsRegular(this);
        public bool IsElite => Faction != null && Faction.IsElite(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Tree, Relations & Faction               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Mapping of upgrade troop -> parent troop
        public static readonly Dictionary<string, WCharacter> UpgradeMap = [];

        public WCharacter Root
        {
            get
            {
                WCharacter current = this;
                while (current.Parent != null)
                    current = current.Parent;
                return current;
            }
        }

        public WCharacter Parent
        {
            get =>
                IsCustom
                    ? UpgradeMap.TryGetValue(StringId, out var parent)
                        ? parent
                        : null
                    : VanillaHelper.GetParent(this);
            set
            {
                if (IsVanilla)
                    return; // Cannot set parent on vanilla troops

                var oldParent = Parent;
                var oldFaction = Faction;

                // 1) Remove from old parent's upgrade list
                if (Parent != null)
                {
                    var oldList = Parent.UpgradeTargets.ToList();
                    if (oldList.Remove(this))
                        Parent.UpgradeTargets = [.. oldList];
                }

                // 2) Add to new parent's upgrade list
                if (value != null)
                {
                    var list = value.UpgradeTargets.ToList();
                    if (!list.Any(wc => wc.StringId == StringId))
                    {
                        list.Add(this);
                        value.UpgradeTargets = [.. list];
                    }

                    // Keep faction inherited
                    Faction = value.Faction;
                }

                // Update map
                if (value == null)
                    UpgradeMap.Remove(StringId);
                else
                    UpgradeMap[StringId] = value;

                // Invalidate category caches
                oldFaction?.InvalidateCategoryCache();
                if (Faction != null && Faction != oldFaction)
                    Faction.InvalidateCategoryCache();
            }
        }

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

        public BaseFaction Faction
        {
            get =>
                IsVanilla ? Culture
                : BaseFaction.TroopFactionMap.TryGetValue(StringId, out var faction) ? faction
                : null;
            set
            {
                if (value == null)
                    BaseFaction.TroopFactionMap.Remove(StringId);
                else
                    BaseFaction.TroopFactionMap[StringId] = value;
            }
        }

        public override WFaction Clan => IsCustom ? Player.Clan : null;
        public override WFaction Kingdom => IsCustom ? Player.Kingdom : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Basic Attributes                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public virtual string Name
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
                NeedsPersistence = true;
            }
        }

        public int Tier => Base.Tier;

        public virtual int Level
        {
            get => Base.Level;
            set
            {
                Base.Level = value;
                NeedsPersistence = true;
            }
        }

        public virtual WCulture Culture
        {
            get => new(Base.Culture);
            set
            {
                try
                {
                    if (value == null)
                        return;
                    if (IsHero)
                        return; // Skip heroes (their culture lives on HeroObject.Culture)

                    // CharacterObject has a 'new Culture' with a private setter that forwards to base.Culture.
                    // Explicitly target the declaring base property to avoid AmbiguousMatchException.
                    var baseType = typeof(BasicCharacterObject);
                    var prop = baseType.GetProperty(
                        "Culture",
                        BindingFlags.Instance
                            | BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.DeclaredOnly
                    );
                    prop?.SetValue(Base, value.Base, null);
                    NeedsPersistence = true;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private FormationClass _formationClassOverride = FormationClass.Unset;
        public FormationClass FormationClassOverride
        {
            get => _formationClassOverride;
            set
            {
                _formationClassOverride = value;
                NeedsPersistence = true;
            }
        }

        public FormationClass FormationClass
        {
            get => Base.GetFormationClass();
            set
            {
                try
                {
                    // protected setter -> set via reflection
                    Reflector.SetPropertyValue(Base, "DefaultFormationClass", value);
                    Reflector.SetPropertyValue(Base, "DefaultFormationGroup", (int)value);
                    var isRanged =
                        value == FormationClass.Ranged || value == FormationClass.HorseArcher;
                    var isMounted =
                        value == FormationClass.Cavalry || value == FormationClass.HorseArcher;
                    Reflector.SetFieldValue(Base, "_isRanged", isRanged);
                    Reflector.SetFieldValue(Base, "_isMounted", isMounted);

                    NeedsPersistence = true;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        /// <summary>
        /// Computes the formation class for this troop based on its equipment and override.
        /// </summary>
        public FormationClass ComputeFormationClass()
        {
            // If no override, derive from battle equipment
            if (FormationClassOverride == FormationClass.Unset)
                return Loadout.Battle.ComputeFormationClass();

            // Else return override
            return FormationClassOverride;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsCivilian { get; set; } = false;

        public bool IsMercenary { get; set; } = false;

        public bool IsHero => Base.IsHero;

        public bool IsRuler => Base.HeroObject?.IsFactionLeader ?? false;

        public bool IsClanLeader => Base.HeroObject?.IsClanLeader ?? false;

        public int MaxTier => (IsElite ? 6 : 5) + (ModCompatibility.Tier7Unlocked ? 1 : 0);

        public bool IsMaxTier => Tier >= MaxTier;

        public bool IsDeletable
        {
            get
            {
                if (!IsCustom)
                    return false; // Vanilla troops cannot be deleted
                if (Parent == null)
                    return false; // Root troops cannot be deleted
                if (!IsRegular)
                    return false; // Only regular troops can be deleted
                if (IsHero)
                    return false; // Heroes cannot be deleted
                if (UpgradeTargets.Any())
                    return false; // Troops with upgrades cannot be deleted
                return true;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Toggles                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // List of troop-relevant vanilla skills
        // NOTE: cannot be static because SkillObject is not initialized yet at that time.
        public readonly SkillObject[] BaseSkills =
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

        // Skills added by mods (non-vanilla ids)
        // This is static, but lazily initialized after MBObjectManager has skills.
        private static List<SkillObject> _moddedSkills;
        public static List<SkillObject> ModdedSkills
        {
            get
            {
                if (_moddedSkills != null)
                    return _moddedSkills;

                var all = MBObjectManager.Instance.GetObjectTypeList<SkillObject>();
                _moddedSkills =
                [
                    .. all.Where(s => !SkillsHelper.VanillaSkillIds.Contains(s.StringId)),
                ];
                return _moddedSkills;
            }
        }

        public List<SkillObject> TroopSkills
        {
            get { return [.. BaseSkills, .. ModdedSkills]; }
        }

        // Skill dictionary for easy get/set (vanilla troop skills only)
        public virtual Dictionary<SkillObject, int> Skills
        {
            get { return TroopSkills.ToDictionary(skill => skill, GetSkill); }
            set
            {
                foreach (var skill in TroopSkills)
                {
                    var v = (value != null && value.TryGetValue(skill, out var val)) ? val : 0;
                    SetSkill(skill, v);
                }
            }
        }

        /// <summary>
        /// Returns the value of the specified skill.
        /// </summary>
        public virtual int GetSkill(SkillObject skill)
        {
            var value = Base.GetSkillValue(skill);
            return value;
        }

        /// <summary>
        /// Sets the specified skill to the given value.
        /// </summary>
        public virtual void SetSkill(SkillObject skill, int value)
        {
            var skills = Reflector.GetFieldValue<MBCharacterSkills>(Base, "DefaultCharacterSkills");
            ((PropertyOwner<SkillObject>)(object)skills.Skills).SetPropertyValue(skill, value);
            NeedsPersistence = true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Visuals                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public virtual bool IsFemale
        {
            get => Reflector.GetPropertyValue<bool>(Base, "IsFemale");
            set
            {
                Reflector.SetPropertyValue(Base, "IsFemale", value);
                NeedsPersistence = true;
            }
        }

        public int Race
        {
            get => Base.Race;
            set
            {
                Reflector.SetPropertyValue(Base, "Race", value);
                NeedsPersistence = true;
            }
        }

        public WBody Body => new(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Loadout ━━━━━━━ */

        public WLoadout Loadout => new(this);

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        public bool IsRanged => Loadout.Battle.HasNonThrowableRangedWeapons;
        public bool IsMounted => Loadout.Battle.HasMount;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Upgrades                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━ Upgrade Targets ━━━ */

        public WCharacter[] UpgradeTargets
        {
            get
            {
                var raw =
                    Reflector.GetPropertyValue<CharacterObject[]>(Base, "UpgradeTargets") ?? [];
                return [.. raw.Select(obj => new WCharacter(obj))];
            }
            set =>
                Reflector.SetPropertyValue(
                    Base,
                    "UpgradeTargets",
                    value?.Select(wc => wc.Base).ToArray() ?? []
                );
        }

        /* ━ Upgrade Requirements ━ */

        public ItemCategory UpgradeItemRequirement
        {
            get { return Base.UpgradeRequiresItemFromCategory; }
            set { Reflector.SetPropertyValue(Base, "UpgradeRequiresItemFromCategory", value); }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Lifecycle                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Removes this custom troop from the game, including all its upgrades and existing instances.
        /// </summary>
        public void Remove(WCharacter replacement = null)
        {
            if (!IsCustom)
                return; // Only custom troops can be removed

            Log.Debug(
                $"Removing troop {Name} from parent {Parent?.Name ?? "null"} and faction {Faction?.Name ?? "null"}"
            );

            // Detach from parent
            Parent = null;

            // Detach from faction
            Faction = null;

            // Unregister from the game systems
            HiddenInEncyclopedia = true;
            IsNotTransferableInPartyScreen = false;
            IsNotTransferableInHideouts = false;

            if (IsActive)
                ActiveStubIds.Remove(StringId);

            // Remove all children
            foreach (var target in UpgradeTargets)
                target.Remove();

            // Revert existing instances in parties
            if (replacement != null)
                Log.Info($"Replacing existing instances of {Name} with {replacement.Name}.");
            else
                Log.Info($"Replacing existing instances of {Name} with best match from culture.");

            Replace(
                replacement
                    ?? TroopMatcher.PickBestFromFaction(
                        Culture,
                        this,
                        sameTierOnly: false,
                        sameCategoryOnly: false
                    )
            );
        }

        /// <summary>
        /// Replaces all instances of this troop with another troop in parties and settlements.
        /// </summary>
        public void Replace(WCharacter other)
        {
            if (other == null || other == this)
                return;

            // Replace in all parties
            foreach (var party in WParty.All)
            {
                party?.MemberRoster.SwapTroop(this, other);
                party?.PrisonRoster.SwapTroop(this, other);
            }

            // Replace in all settlements
            foreach (var settlement in WSettlement.All)
            foreach (var notable in settlement.Notables)
                notable.SwapVolunteer(this, other);

            // Recurse into upgrades
            for (int i = 0; i < UpgradeTargets.Length; i++)
            {
                if (other.UpgradeTargets.Length <= i)
                    break;
                UpgradeTargets[i].Replace(other.UpgradeTargets[i]);
            }
        }

        public bool IsActive => !IsCustom || ActiveStubIds.Contains(StringId);

        public bool IsValid =>
            IsActive
            && Base != null
            && !string.IsNullOrWhiteSpace(StringId)
            && !string.IsNullOrWhiteSpace(Name)
            && !LooksLikeEmptyStub();

        /// <summary>
        /// Checks if this troop looks like an unedited empty stub.
        /// </summary>
        private bool LooksLikeEmptyStub()
        {
            if (IsVanilla)
                return false; // Only applies to custom troops
            if (Name != StringId)
                return false; // Not default name
            if (Level != 1)
                return false; // Has level

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       View Model                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public CharacterCode CharacterCode => CharacterCode.CreateFrom(Base);

#if BL13
        public CharacterImageIdentifierVM Image => new(CharacterCode);
        public ImageIdentifier ImageIdentifier => new CharacterImageIdentifier(CharacterCode);
#else
        public ImageIdentifierVM Image => new(CharacterCode);
        public ImageIdentifier ImageIdentifier => new(CharacterCode);
#endif

        /// <summary>
        /// Tries to generate a CharacterViewModel for this troop with the specified equipment set index.
        /// Returns false and captures the exception on failure.
        /// </summary>
        public bool TryGetModel(int index, out CharacterViewModel model, out Exception error)
        {
            model = null;
            error = null;

            try
            {
                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                vm.FillFrom(Base, seed: -1);

                // Apply staged equipment changes (if any)
                vm.SetEquipment(Loadout.Get(index).StagingPreview());

                if (Faction != null)
                {
                    // Armor colors
                    vm.ArmorColor1 = Faction.Color;
                    vm.ArmorColor2 = Faction.Color2;

                    // Heraldic items
                    var bbf = Faction as BaseBannerFaction;
                    vm.BannerCodeText = bbf?.Banner.Serialize();
                }

                model = vm;
                return true;
            }
            catch (AccessViolationException ex)
            {
                // FaceGen "bad combo"
                error = ex;
                model = null;
                return false;
            }
            catch (Exception ex)
            {
                // Any other error: still capture for the caller
                Log.Exception(ex);
                error = ex;
                model = null;
                return false;
            }
        }

        /// <summary>
        /// Generates a CharacterViewModel for this troop with the specified equipment set index.
        /// Returns null on failure.
        /// </summary>
        public CharacterViewModel GetModel(int index = 0)
        {
            TryGetModel(index, out var vm, out _);
            return vm;
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
            if (IsVanilla && !NeedsPersistence)
            {
                Log.Error("Cannot FillFrom on an unedited vanilla troop.");
                return;
            }

            // Character object copy
            CharacterHelper.CopyInto(src.Base, _co);

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
                Skills = TroopSkills.ToDictionary(skill => skill, src.GetSkill);
            else
                Skills = [];

            // Equipment - re-create from code to avoid shared references
            if (keepEquipment)
            {
                if (Config.CopyAllSetsWhenCloning && !ModCompatibility.NoAlternateEquipmentSets)
                {
                    // Copy every set verbatim
                    Loadout.FillFrom(src.Loadout, copyAll: true);
                }
                else
                {
                    // Default: one battle + one civilian
                    Loadout.FillFrom(src.Loadout, copyAll: false);
                }

                // Upgrade item requirement refresh
                UpgradeItemRequirement = Loadout.ComputeUpgradeItemRequirement();

                // Formation class from first battle set (index 0 normalized to battle)
                FormationClass = ComputeFormationClass();
            }
            else
            {
                Loadout.Clear();
            }
        }
    }
}
