using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Statistics;
using Retinues.Game.Helpers;
using Retinues.Mods;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
# if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace OldRetinues.Game.Wrappers
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
        public static CharacterObject AllocateStub(string stringId = null)
        {
            // Try to find by id first
            if (!string.IsNullOrWhiteSpace(stringId))
            {
                var co = MBObjectManager.Instance.GetObject<CharacterObject>(stringId);
                if (co != null && co.StringId.StartsWith(CustomIdPrefix))
                {
                    if (!ActiveStubIds.Contains(co.StringId)) // not allocated yet
                        return co;
                }
            }

            // Else find any free stub
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
            : this(NullifyLegacyIds(stringId) ?? AllocateStub(stringId).StringId)
        {
            // Enforce binding on construction
            faction.SetRoot(category, this);

            Initialize(faction);
        }

        // Constructor for upgrade troops
        public WCharacter(WCharacter parent, string stringId = null)
            : this(NullifyLegacyIds(stringId) ?? AllocateStub(stringId).StringId)
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
            IsNotTransferableInPartyScreen = false;
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

        private readonly string _stringId = characterObject.StringId;

        public CharacterObject Base => _co;
        public override string StringId => _stringId;

        /* ━━━━━━━ Template ━━━━━━━ */

        public static Dictionary<string, string> VanillaStringIdMap = [];

        public string VanillaStringId =>
            VanillaStringIdMap.TryGetValue(StringId, out var vid) ? vid : StringId;

        /* ━━━━━━━━━ Type ━━━━━━━━━ */

        private readonly bool _isLegacyCustom = characterObject.StringId.StartsWith(
            LegacyCustomIdPrefix
        );

        private readonly bool _isCustom =
            characterObject.StringId.StartsWith(CustomIdPrefix)
            || characterObject.StringId.StartsWith(LegacyCustomIdPrefix);

        public bool IsCustom => _isCustom;
        public bool IsLegacyCustom => _isLegacyCustom;
        public bool IsVanilla => !_isCustom;

        /* ━━━━━━━ Category ━━━━━━━ */

        public bool IsRetinue => Faction != null && Faction.IsRetinue(this);
        public bool IsRegular => Faction != null && Faction.IsRegular(this);
        public bool IsElite => Faction != null && Faction.IsElite(this);
        public bool IsCaptain = false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Captains                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Dictionary<string, CharacterObject> CaptainObjectByBaseId = new(
            StringComparer.Ordinal
        );
        private static readonly Dictionary<string, string> BaseIdByCaptainId = new(
            StringComparer.Ordinal
        );

        private static readonly Dictionary<string, WCharacter> CaptainCache = new(
            StringComparer.Ordinal
        );

        private static readonly Dictionary<string, bool> CaptainEnabledCache = new(
            StringComparer.Ordinal
        );

        public bool CanHaveCaptain
        {
            get
            {
                if (!_isCustom)
                    return false;

                // If this id is known to be a captain, do not treat it as a base troop.
                if (IsCaptain || IsCaptainId(_stringId))
                    return false;

                if (Base.IsHero)
                    return false;

                // Cheap retinue check without calling WCharacter.Faction / WCharacter.IsRetinue
                if (BaseFaction.TroopFactionMap.TryGetValue(_stringId, out var f) && f != null)
                {
                    if (f.IsRetinueId(_stringId))
                        return false;
                }

                return true;
            }
        }

        internal static bool IsCustomId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            return id.StartsWith(CustomIdPrefix);
        }

        internal static bool TryGetBaseIdFromCaptainId(string id, out string baseId) =>
            BaseIdByCaptainId.TryGetValue(id, out baseId);

        internal static bool IsCaptainId(string id) =>
            !string.IsNullOrEmpty(id) && BaseIdByCaptainId.ContainsKey(id);

        internal static bool IsCaptainEnabledId(string baseId) =>
            !string.IsNullOrEmpty(baseId)
            && CaptainEnabledCache.TryGetValue(baseId, out var enabled)
            && enabled;

        internal static bool TryGetCaptainObject(string baseId, out CharacterObject captain) =>
            CaptainObjectByBaseId.TryGetValue(baseId, out captain) && captain != null;

        private static void RegisterCaptainPair(string baseId, WCharacter captain)
        {
            if (string.IsNullOrEmpty(baseId) || captain?.Base == null)
                return;

            CaptainObjectByBaseId[baseId] = captain.Base;
            BaseIdByCaptainId[captain.StringId] = baseId;
        }

        public static void ClearCaptainCaches()
        {
            CaptainCache.Clear();
            CaptainEnabledCache.Clear();
            CaptainObjectByBaseId.Clear();
            BaseIdByCaptainId.Clear();
        }

        public WCharacter BaseTroop;

        internal bool HasCaptainInstance =>
            _captain != null
            || (CaptainCache.TryGetValue(StringId, out var existing) && existing != null);

        internal WCharacter GetExistingCaptain()
        {
            if (_captain != null)
                return _captain;

            if (CaptainCache.TryGetValue(StringId, out var existing) && existing != null)
                return existing;

            return null;
        }

        private WCharacter _captain;
        public WCharacter Captain
        {
            get
            {
                if (!CanHaveCaptain)
                    return null; // fallback for captains and non-custom troops

                // Do not create or return captains if the doctrine is locked
                if (!DoctrineAPI.IsDoctrineUnlocked<Captains>() && !Config.NoDoctrineRequirements)
                    return null;

                // Try global cache first (base troop stringId -> captain instance).
                if (CaptainCache.TryGetValue(StringId, out var cached) && cached != null)
                {
                    // Make sure links are wired correctly.
                    cached.IsCaptain = true;
                    if (cached.BaseTroop == null)
                        cached.BaseTroop = this;

                    _captain = cached;
                    return cached;
                }

                // No cached captain yet: create and register one.
                _captain ??= CreateCaptain();
                if (_captain != null)
                {
                    CaptainCache[StringId] = _captain;
                    _captain.IsCaptain = true;
                    _captain.BaseTroop = this;
                }

                return _captain;
            }
        }

        /// <summary>
        /// If false, this troop's captain will not spawn in battles.
        /// Only meaningful for regular custom troops (base side of the captain pair).
        /// </summary>
        public bool CaptainEnabled
        {
            get
            {
                if (IsCaptain)
                    return BaseTroop?.CaptainEnabled ?? false;

                // Prefer global cache
                if (CaptainEnabledCache.TryGetValue(StringId, out var flag))
                    return flag;

                // Not recorded yet: default false
                return false;
            }
            set
            {
                if (IsCaptain)
                {
                    if (BaseTroop != null)
                        BaseTroop.CaptainEnabled = value;
                    return;
                }

                CaptainEnabledCache[StringId] = value;
                NeedsPersistence = true;
            }
        }

        public void BindCaptain(WCharacter captain)
        {
            if (captain == null)
                return;

            _captain = captain;
            captain.IsCaptain = true;
            captain.BaseTroop = this;

            CaptainCache[StringId] = captain;
            RegisterCaptainPair(StringId, captain);

            // Ensure faction / flags match the base troop
            if (Faction != null)
                captain.Faction = Faction;

            captain.HiddenInEncyclopedia = HiddenInEncyclopedia;
            captain.IsNotTransferableInPartyScreen = false;
            captain.IsNotTransferableInHideouts = false;

            if (captain.IsCustom && !ActiveStubIds.Contains(captain.StringId))
                ActiveStubIds.Add(captain.StringId);
        }

        private WCharacter CreateCaptain()
        {
            if (!CanHaveCaptain)
                return null; // Only for custom regular troops

            // If already in cache for this base, reuse it
            if (CaptainCache.TryGetValue(StringId, out var existing) && existing != null)
                return existing;

            // Create new captain troop from a free stub
            var stub = AllocateStub();
            var captain = new WCharacter(stub);

            // Register as active stub + share faction
            ActiveStubIds.Add(captain.StringId);
            if (Faction != null)
                captain.Faction = Faction;

            // Copy data from base troop
            captain.FillFrom(this, keepUpgrades: false, keepEquipment: true, keepSkills: true);
            captain.IsCaptain = true;
            captain.BaseTroop = this;

            RegisterCaptainPair(StringId, captain);

            // Rank up if possible
            if (!IsMaxTier)
                captain.Level += 5;

            // Rename
            captain.Name = L.T("captain_name", "{NAME} Captain")
                .SetTextVariable("NAME", Name)
                .ToString();

            // Flags
            captain.HiddenInEncyclopedia = true;
            captain.IsNotTransferableInPartyScreen = false;
            captain.IsNotTransferableInHideouts = false;

            return captain;
        }

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

        private BaseFaction _factionCached;
        private int _factionCachedVersion = -1;

        private WCulture _cultureCached;

        public BaseFaction Faction
        {
            get
            {
                if (IsVanilla)
                    return _cultureCached ??= new(Base.Culture);

                var v = BaseFaction.TroopFactionMapVersion;
                if (_factionCachedVersion == v)
                    return _factionCached;

                _factionCached = BaseFaction.TroopFactionMap.TryGetValue(StringId, out var f)
                    ? f
                    : null;
                _factionCachedVersion = v;
                return _factionCached;
            }
            set
            {
                if (value == null)
                    BaseFaction.TroopFactionMap.Remove(StringId);
                else
                    BaseFaction.TroopFactionMap[StringId] = value;

                BaseFaction.TouchTroopFactionMap();

                _factionCached = value;
                _factionCachedVersion = BaseFaction.TroopFactionMapVersion;
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
            get => Config.AllowFormationOverrides ? _formationClassOverride : FormationClass.Unset;
            set
            {
                if (Config.AllowFormationOverrides == false)
                    return;

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

        public int MaxTier => (IsElite ? 6 : 5) + (ModCompatibility.HasTier7Unlocker ? 1 : 0);

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
            set => Reflector.SetPropertyValue(Base, "HiddenInEncyclopedia", value);
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

        public readonly List<SkillObject> CombatSkills =
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

        public readonly List<SkillObject> HeroSkills =
        [
            DefaultSkills.Crafting,
            DefaultSkills.Scouting,
            DefaultSkills.Tactics,
            DefaultSkills.Roguery,
            DefaultSkills.Charm,
            DefaultSkills.Leadership,
            DefaultSkills.Trade,
            DefaultSkills.Steward,
            DefaultSkills.Medicine,
            DefaultSkills.Engineering,
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
                    .. all.Where(s =>
                        !SkillsHelper.VanillaSkillIds.Contains(s.StringId)
                        && !SkillsHelper.NavalDLCSkillIds.Contains(s.StringId)
                    ),
                ];
                return _moddedSkills;
            }
        }

        private static List<SkillObject> _navalDlcSkills;
        public static List<SkillObject> NavalDLCSkills
        {
            get
            {
                if (ModCompatibility.HasNavalDLC == false)
                    return []; // No naval DLC, no skills

                if (_navalDlcSkills != null)
                    return _navalDlcSkills;

                _navalDlcSkills = [];

                foreach (var skillId in SkillsHelper.NavalDLCSkillIds)
                {
                    var skill = MBObjectManager.Instance.GetObject<SkillObject>(skillId);
                    if (skill == null)
                        continue;
                    _navalDlcSkills.Add(skill);
                }

                return _navalDlcSkills;
            }
        }

        public List<SkillObject> ExtraSkills =>
            IsHero ? [.. NavalDLCSkills, .. ModdedSkills] : ModdedSkills;

        public List<SkillObject> TroopSkills =>
            IsHero ? [.. CombatSkills, .. HeroSkills] : CombatSkills;

        public List<SkillObject> AllSkills => [.. TroopSkills, .. ExtraSkills];

        // Skill dictionary for easy get/set (vanilla troop skills only)
        public virtual Dictionary<SkillObject, int> Skills
        {
            get { return AllSkills.ToDictionary(skill => skill, GetSkill); }
            set
            {
                foreach (var skill in AllSkills)
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

            // Clear battle record
            TroopStatisticsBehavior.Clear(this);

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

        /// <summary>
        /// Generates a CharacterViewModel for this troop with the specified equipment set index,
        /// optionally applying a visual gender override.
        /// </summary>
        public CharacterViewModel GetModel(int index, bool applyGenderOverride)
        {
            if (!applyGenderOverride)
                return GetModel(index);

            if (!TryGetModel(index, out var vm, out _))
                return null;

            // Flip the visual gender relative to the troop definition.
            vm.IsFemale = !IsFemale;
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
            CharacterObjectHelper.CopyInto(src.Base, _co);

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
                if (Config.CopyAllSetsOnUnlock)
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

            // Mariner trait
            if (ModCompatibility.HasNavalDLC)
                IsMariner = src.IsMariner;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Naval / Mariner flag                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsMariner
        {
            get
            {
                var level = NavalTraitHelper.GetMarinerLevel(Base);
                return level > 0;
            }
            set
            {
                var level = value ? 1 : 0;
                NavalTraitHelper.SetMarinerLevel(Base, level);
                NeedsPersistence = true;
            }
        }

        public static class NavalTraitHelper
        {
            private static TraitObject _navalSoldierTrait;
            private static bool _navalTraitMissing;

            // ── Trait lookup ──────────────────────────────────────

            private static TraitObject TryGetNavalSoldierTrait()
            {
                if (_navalTraitMissing)
                    return null;

                if (_navalSoldierTrait != null)
                    return _navalSoldierTrait;

                try
                {
                    var defaultTraitsType = Type.GetType(
                        "TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits, TaleWorlds.CampaignSystem",
                        throwOnError: false
                    );

                    if (defaultTraitsType == null)
                    {
                        // If the type really doesn't exist (no Naval DLC), mark as missing.
                        _navalTraitMissing = true;
                        return null;
                    }

                    var prop = defaultTraitsType.GetProperty(
                        "NavalSoldier",
                        BindingFlags.Public | BindingFlags.Static
                    );

                    if (prop == null)
                    {
                        _navalTraitMissing = true;
                        return null;
                    }

                    if (prop.GetValue(null) is not TraitObject value)
                    {
                        // Traits not registered yet; don't permanently mark as missing.
                        return null;
                    }

                    _navalSoldierTrait = value;
                    return _navalSoldierTrait;
                }
                catch
                {
                    _navalTraitMissing = true;
                    return null;
                }
            }

            // Allows us to clear everything on a game reset/load.
            public static void Reset()
            {
                _navalSoldierTrait = null;
                _navalTraitMissing = false;
            }

            // ── Public API used by WCharacter ─────────────────────

            public static int GetMarinerLevel(CharacterObject co)
            {
                if (co == null || !ModCompatibility.HasNavalDLC)
                    return 0;

                var navalTrait = TryGetNavalSoldierTrait();
                if (navalTrait == null)
                    return 0;

                try
                {
                    // This is exactly what NavalDLC uses internally.
                    return co.GetTraitLevel(navalTrait);
                }
                catch
                {
                    return 0;
                }
            }

            public static void SetMarinerLevel(CharacterObject co, int level)
            {
                if (co == null || !ModCompatibility.HasNavalDLC)
                    return;

                var navalTrait = TryGetNavalSoldierTrait();
                if (navalTrait == null)
                    return;

                level = level > 0 ? 1 : 0;

                try
                {
                    if (co.IsHero)
                    {
                        co.HeroObject?.SetTraitLevel(navalTrait, level);
                        return;
                    }

                    // No static FieldInfo/MethodInfo; resolve fresh each time.
                    var coType = typeof(CharacterObject);
                    var traitsField = coType.GetField(
                        "_characterTraits",
                        BindingFlags.Instance | BindingFlags.NonPublic
                    );

                    if (traitsField == null)
                        return;

                    var owner = traitsField.GetValue(co);
                    if (owner == null)
                    {
                        // Ensure a PropertyOwner<TraitObject> exists.
                        var ownerType = traitsField.FieldType;
                        owner = Activator.CreateInstance(ownerType);
                        traitsField.SetValue(co, owner);
                    }

                    var setMethod = owner
                        .GetType()
                        .GetMethod(
                            "SetPropertyValue",
                            BindingFlags.Instance | BindingFlags.Public,
                            binder: null,
                            types: [typeof(TraitObject), typeof(int)],
                            modifiers: null
                        );

                    setMethod?.Invoke(owner, [navalTrait, level]);
                }
                catch
                {
                    // best-effort; Naval DLC is optional
                }
            }
        }
    }
}
