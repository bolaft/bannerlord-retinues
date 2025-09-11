using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Game.Wrappers
{
    public class WCharacter(
        CharacterObject characterObject,
        WFaction _faction = null,
        WCharacter parent = null
    ) : StringIdentifier
    {
        // =========================================================================
        // Constants & Private Helpers
        // =========================================================================

        private static CharacterObject[] ToCharacterArray(IEnumerable<WCharacter> items) =>
            items?.Select(wc => (CharacterObject)wc.Base).ToArray() ?? [];

        // Backing fields (initialized from primary-ctor parameters)
        private readonly CharacterObject _characterObject = characterObject;
        private WCharacter _parent = parent;
        private WFaction _faction = _faction;

        // Fast access to the engine object
        public object Base => _characterObject;

        // =========================================================================
        // Identity & Vanilla Mapping
        // =========================================================================

        public override string StringId => _characterObject.StringId;

        public bool IsVanilla => Faction is null;
        public bool IsCustom => !IsVanilla;

        public static Dictionary<string, string> VanillaStringIdMap = new();

        // Maps a custom troop's StringId back to its vanilla origin.
        public string VanillaStringId
        {
            get
            {
                if (IsVanilla)
                    return StringId;

                return VanillaStringIdMap.TryGetValue(StringId, out var vanillaId)
                    ? vanillaId
                    : null;
            }
            set
            {
                if (IsVanilla)
                    return; // Cannot set vanilla id for vanilla troops

                VanillaStringIdMap[StringId] = value;
            }
        }

        // =========================================================================
        // View-Model (VM) Accessors
        // =========================================================================

        public ImageIdentifierVM Image => new(CharacterCode.CreateFrom(_characterObject));

        public CharacterViewModel Model
        {
            get
            {
                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                vm.FillFrom(_characterObject, seed: -1);

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

        // =========================================================================
        // Tree, Relations & Faction
        // =========================================================================

        public WCulture Culture => new(_characterObject.Culture);

        // Lazy-resolves the associated faction by probing player's clan/kingdom lists.
        public WFaction Faction
        {
            get
            {
                if (_faction != null)
                    return _faction;

                var candidates = new List<WFaction> { Player.Clan };
                if (Player.Kingdom != null)
                    candidates.Add(Player.Kingdom);

                foreach (var fac in candidates)
                {
                    // Listed as a troop of the faction?
                    if (fac.EliteTroops.Any(t => t.StringId == StringId) ||
                        fac.BasicTroops.Any(t => t.StringId == StringId))
                    {
                        _faction = fac;
                        break;
                    }

                    // Or used as the faction's retinue?
                    if (fac.RetinueElite?.StringId == StringId ||
                        fac.RetinueBasic?.StringId == StringId)
                    {
                        _faction = fac;
                        break;
                    }
                }

                return _faction;
            }
        }

        // Lazy parent discovery by walking faction troop trees.
        public WCharacter Parent
        {
            get
            {
                if (_parent != null)
                    return _parent;

                if (Faction == null)
                    return null;

                var allTroops = Faction.EliteTroops.Concat(Faction.BasicTroops);
                foreach (var troop in allTroops)
                {
                    if (troop.UpgradeTargets.Any(t => t.StringId == StringId))
                    {
                        _parent = troop;
                        break;
                    }
                }

                return _parent;
            }
        }

        // Depth-first enumeration of this node and all descendants.
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

        // Returns the culture root matching elite/basic branch within this faction.
        public CharacterObject Root
        {
            get
            {
                var rootId = IsElite ? Faction?.RootElite?.StringId : Faction?.RootBasic?.StringId;
                return MBObjectManager.Instance.GetObject<CharacterObject>(rootId);
            }
        }

        public static WCharacter GetFromPositionInTree(WCharacter root, List<int> pos)
        {
            var node = root;
            for (int i = 0; i < pos.Count; i++)
            {
                int idx = pos[i];
                node = node.UpgradeTargets[idx];
            }
            return node;
        }

        public List<int> PositionInTree
        {
            get
            {
                if (Parent == null)
                    return []; // root => empty path

                var path = Parent.PositionInTree;
                path.Add(Parent.UpgradeTargets.ToList().FindIndex(t => t.StringId == StringId));
                return path;
            }
        }

        // =========================================================================
        // Basic Attributes
        // =========================================================================

        public string Name
        {
            get => _characterObject.Name.ToString();
            set
            {
                Reflector.InvokeMethod(
                    _characterObject,
                    "SetName",
                    [typeof(TextObject)],
                    new TextObject(value, (Dictionary<string, object>)null)
                );
            }
        }

        public int Tier => _characterObject.Tier;

        public int Level
        {
            get => _characterObject.Level;
            set => _characterObject.Level = value;
        }

        public FormationClass FormationClass => _characterObject.GetFormationClass();

        // =========================================================================
        // Flags & Toggles
        // =========================================================================

        public bool IsElite =>
            Faction?.EliteTroops.Contains(this) == true ||
            StringId == Faction?.RetinueElite?.StringId;

        public bool IsRetinue =>
            StringId == Faction?.RetinueElite?.StringId ||
            StringId == Faction?.RetinueBasic?.StringId;

        public bool IsMaxTier => Tier >= (IsElite ? 6 : 5);

        public bool IsFemale
        {
            get => Reflector.GetPropertyValue<bool>(_characterObject, "IsFemale");
            set => Reflector.SetPropertyValue(_characterObject, "IsFemale", value);
        }

        // NOTE: game-side property is misspelled "HiddenInEncylopedia"
        public bool HiddenInEncyclopedia
        {
            get => Reflector.GetPropertyValue<bool>(_characterObject, "HiddenInEncylopedia");
            set => Reflector.SetPropertyValue(_characterObject, "HiddenInEncylopedia", value);
        }

        public bool IsNotTransferableInHideouts
        {
            get => _characterObject.IsNotTransferableInHideouts;
            set => _characterObject.SetTransferableInHideouts(!value);
        }

        public bool IsNotTransferableInPartyScreen
        {
            get => _characterObject.IsNotTransferableInPartyScreen;
            set => _characterObject.SetTransferableInPartyScreen(!value);
        }

        // =========================================================================
        // Skills
        // =========================================================================

        private static readonly SkillObject[] CoreSkills =
        [
            DefaultSkills.Athletics,
            DefaultSkills.Riding,
            DefaultSkills.OneHanded,
            DefaultSkills.TwoHanded,
            DefaultSkills.Polearm,
            DefaultSkills.Bow,
            DefaultSkills.Crossbow,
            DefaultSkills.Throwing
        ];

        public Dictionary<SkillObject, int> Skills
        {
            get => CoreSkills.ToDictionary(skill => skill, GetSkill);
            set
            {
                foreach (var skill in CoreSkills)
                {
                    var v = (value != null && value.TryGetValue(skill, out var val)) ? val : 0;
                    SetSkill(skill, v);
                }
            }
        }

        public int GetSkill(SkillObject skill) => _characterObject.GetSkillValue(skill);

        public void SetSkill(SkillObject skill, int value)
        {
            var skills = Reflector.GetFieldValue<MBCharacterSkills>(_characterObject, "DefaultCharacterSkills");
            ((PropertyOwner<SkillObject>)(object)skills.Skills).SetPropertyValue(skill, value);
        }

        // =========================================================================
        // Equipment
        // =========================================================================

        public List<WEquipment> Equipments
        {
            get
            {
                var equipments = _characterObject.AllEquipments.ToList();
                return equipments.Select(e => new WEquipment(e)).ToList();
            }
            set
            {
                var equipments = value.Select(e => (Equipment)e.Base).ToList();
                var roster = new MBEquipmentRoster();
                Reflector.SetFieldValue(roster, "_equipments", new MBList<Equipment>(equipments));
                Reflector.SetFieldValue(_characterObject, "_equipmentRoster", roster);
            }
        }

        // Convenience for accessing the first equipment set.
        public WEquipment Equipment => new((Equipment)Equipments.FirstOrDefault().Base);

        public bool CanEquip(WItem item)
        {
            if (item.RelevantSkill == null)
                return true;
            return item.Difficulty <= GetSkill(item.RelevantSkill);
        }

        public void Equip(WItem item, EquipmentIndex slot) => Equipment.SetItem(slot, item);

        public WItem Unequip(EquipmentIndex slot)
        {
            var item = Equipment.GetItem(slot);
            Equipment.SetItem(slot, null);
            return item;
        }

        public IEnumerable<WItem> UnequipAll()
        {
            foreach (var slot in WEquipment.Slots)
                yield return Unequip(slot);
        }

        // =========================================================================
        // Upgrades
        // =========================================================================

        public WCharacter[] UpgradeTargets
        {
            get
            {
                var raw = Reflector.GetPropertyValue<CharacterObject[]>(_characterObject, "UpgradeTargets") ?? [];
                return raw.Select(obj => new WCharacter(obj, Faction, this)).ToArray();
            }
            set => Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", ToCharacterArray(value));
        }

        public void AddUpgradeTarget(WCharacter target)
        {
            if (UpgradeTargets.Any(wc => wc.StringId == target.StringId))
                return;

            var list = UpgradeTargets?.ToList() ?? new List<WCharacter>();
            list.Add(target);
            Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", ToCharacterArray(list));
        }

        public void RemoveUpgradeTarget(WCharacter target)
        {
            var list = UpgradeTargets?.ToList() ?? new List<WCharacter>();
            list.RemoveAll(wc => wc.StringId == target.StringId);
            Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", ToCharacterArray(list));
        }

        // =========================================================================
        // Registration & Lifecycle
        // =========================================================================

        // Remove from faction, detach from parent, unregister, then recursively remove children.
        public void Remove()
        {
            // Remove from faction lists
            if (Faction != null)
            {
                if (IsElite)
                    Faction.EliteTroops.Remove(this);
                else
                    Faction.BasicTroops.Remove(this);
            }

            // Remove from parent's upgrade targets
            Parent?.RemoveUpgradeTarget(this);

            Log.Debug($"Removed troop {Name} from parent {Parent?.Name ?? "null"} and faction {Faction?.Name ?? "null"}");

            // Unregister from the game systems
            Unregister();

            // Remove all children
            foreach (var target in UpgradeTargets)
                target.Remove();
        }

        public void Register()
        {
            HiddenInEncyclopedia = false;
            IsNotTransferableInPartyScreen = false;
            IsNotTransferableInHideouts = false;
        }

        public void Unregister()
        {
            HiddenInEncyclopedia = true;
            IsNotTransferableInPartyScreen = true;
            IsNotTransferableInHideouts = true;
        }

        // =========================================================================
        // Cloning
        // =========================================================================

        // Create a deep gameplay-safe clone of this troop, optionally keeping upgrades/equipment/skills.
        public WCharacter Clone(
            WFaction faction = null,
            WCharacter parent = null,
            bool keepUpgrades = true,
            bool keepEquipment = true,
            bool keepSkills = true
        )
        {
            // Clone from the source troop
            var cloneObject = CharacterObject.CreateFrom(_characterObject);

            // Detach skills so parent/clone no longer share the same container
            var freshSkills = (MBCharacterSkills)Activator.CreateInstance(typeof(MBCharacterSkills), nonPublic: true);
            Reflector.SetFieldValue(cloneObject, "DefaultCharacterSkills", freshSkills);

            // Default faction is the same as the original troop
            faction ??= Faction;

            // Wrap it
            var clone = new WCharacter(cloneObject, faction, parent);

            // Add to upgrade targets of the parent, if any
            parent?.AddUpgradeTarget(clone);

            // Upgrades
            clone.UpgradeTargets = keepUpgrades ? UpgradeTargets.ToArray() : [];

            // Equipment — re-create from code to avoid shared references
            clone.Equipments = [];
            if (keepEquipment)
                clone.Equipments = [WEquipment.FromCode(Equipment.Code)];
            else
                clone.Equipments = [];

            // Skills
            if (keepSkills)
                clone.Skills = CoreSkills.ToDictionary(skill => skill, GetSkill);
            else
                clone.Skills = [];

            // Register it
            clone.Register();

            // Id of the basic vanilla troop
            var vanillaId = IsVanilla ? StringId : VanillaStringId;
            if (!string.IsNullOrEmpty(vanillaId))
                clone.VanillaStringId = vanillaId;

            return clone;
        }
    }
}
