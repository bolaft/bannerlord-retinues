using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Helpers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Core.Game.Wrappers
{
    public class WCharacter(CharacterObject characterObject) : StringIdentifier
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter(
            bool isKingdom,
            bool isElite,
            bool isRetinue = false,
            IReadOnlyList<int> path = null
        )
            : this(CharacterHelper.GetCharacterObject(isKingdom, isElite, isRetinue, path)) { }

        public WCharacter(string stringId)
            : this(CharacterHelper.GetCharacterObject(stringId)) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Identity                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CharacterObject _co =
            characterObject ?? throw new ArgumentNullException(nameof(characterObject));

        public CharacterObject Base => _co;

        public override string StringId => _co.StringId;

        public static Dictionary<string, string> VanillaStringIdMap = [];

        public string VanillaStringId => VanillaStringIdMap.TryGetValue(StringId, out var vid)
            ? vid
            : StringId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                View-Model (VM) Accessors               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ImageIdentifierVM Image => new(CharacterCode.CreateFrom(Base));

        public CharacterViewModel Model
        {
            get
            {
                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                vm.FillFrom(Base, seed: -1);

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

        public bool IsCustom => CharacterHelper.IsCustom(StringId);
        public bool IsElite => CharacterHelper.IsElite(StringId);
        public bool IsRetinue => CharacterHelper.IsRetinue(StringId);

        public WCharacter Parent => CharacterHelper.GetParent(this);
        public WFaction Faction => CharacterHelper.ResolveFaction(StringId);

        public IReadOnlyList<int> PositionInTree => CharacterHelper.GetPath(StringId);
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

        public FormationClass FormationClass => Base.GetFormationClass();

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
            // NOTE: game-side property is misspelled "HiddenInEncylopedia"
            get => Reflector.GetPropertyValue<bool>(Base, "HiddenInEncylopedia");
            set => Reflector.SetPropertyValue(Base, "HiddenInEncylopedia", value);
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

        public bool IsRanged => Equipment.HasRangedWeapons;

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
            get
            {
                try
                {
                    return CoreSkills.ToDictionary(skill => skill, GetSkill);
                }
                catch (Exception ex)
                {
                    // Handle or log the exception as needed
                    Log.Exception(ex);
                    return [];
                }
            }
            set
            {
                try
                {
                    foreach (var skill in CoreSkills)
                    {
                        var v = (value != null && value.TryGetValue(skill, out var val)) ? val : 0;
                        SetSkill(skill, v);
                    }
                }
                catch (Exception ex)
                {
                    // Handle or log the exception as needed
                    Log.Exception(ex);
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
                var equipments = Base.AllEquipments.ToList();
                return [.. equipments.Select(e => new WEquipment(e))];
            }
            set
            {
                var equipments = value.Select(e => e.Base).ToList();
                var roster = new MBEquipmentRoster();
                Reflector.SetFieldValue(roster, "_equipments", new MBList<Equipment>(equipments));
                Reflector.SetFieldValue(Base, "_equipmentRoster", roster);
            }
        }

        // Convenience for accessing the first equipment set.
        public WEquipment Equipment => new(Equipments.FirstOrDefault().Base);

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

        public void AddUpgradeTarget(WCharacter target)
        {
            if (UpgradeTargets.Any(wc => wc.StringId == target.StringId))
                return;

            var list = UpgradeTargets?.ToList() ?? [];
            list.Add(target);
            Reflector.SetPropertyValue(Base, "UpgradeTargets", ToCharacterArray(list));
        }

        public void RemoveUpgradeTarget(WCharacter target)
        {
            var list = UpgradeTargets?.ToList() ?? [];
            list.RemoveAll(wc => wc.StringId == target.StringId);
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
        public bool IsActive => ActiveTroops.Contains(StringId);

        public void Activate()
        {
            Log.Info($"Activating troop {StringId}.");
            HiddenInEncyclopedia = false;
            IsNotTransferableInPartyScreen = false;
            IsNotTransferableInHideouts = false;

            if (!IsActive)
                ActiveTroops.Add(StringId);
        }

        public void Deactivate()
        {
            Log.Info($"Deactivating troop {StringId}.");
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
            CharacterHelper.CopyInto(src.Base, _co);

            // Vanilla id
            VanillaStringIdMap[StringId] = src.VanillaStringId;

            // Upgrades
            UpgradeTargets = keepUpgrades ? [.. UpgradeTargets] : [];

            // Equipment — re-create from code to avoid shared references
            if (keepEquipment)
                Equipments = [WEquipment.FromCode(Equipment.Code)];
            else
                Equipments = [];

            // Detach skills so parent/clone no longer share the same container
            var freshSkills = (MBCharacterSkills)
                Activator.CreateInstance(typeof(MBCharacterSkills), nonPublic: true);
            Reflector.SetFieldValue(_co, "DefaultCharacterSkills", freshSkills);

            // Skills
            if (keepSkills)
                Skills = CoreSkills.ToDictionary(skill => skill, src.GetSkill);
            else
                Skills = [];
        }
    }
}
