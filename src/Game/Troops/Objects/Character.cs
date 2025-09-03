using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Game.Troops.Campaign;

namespace CustomClanTroops.Game.Troops.Objects
{
    public class TroopCharacter(CharacterObject character, TroopClan clan = null, TroopCharacter parent = null) : CharacterWrapper(character)
    {
        // =========================================================================
        // Relationships
        // =========================================================================

        private readonly TroopCharacter _parent = parent;

        public TroopCharacter Parent => _parent;

        private readonly TroopClan _clan = clan;

        public TroopClan Clan => _clan;

        // =========================================================================
        // Flags
        // =========================================================================

        public bool IsElite => Clan.EliteTroops.Contains(this);

        // =========================================================================
        // Skillsœ
        // =========================================================================

        public int SkillCap
        {
            get
            {
                return Tier switch
                {
                    1 => 20, 2 => 50, 3 => 80, 4 => 120, 5 => 160, 6 => 260,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }

        public int SkillPoints
        {
            get
            {
                return Tier switch
                {
                    1 => 90, 2 => 210, 3 => 360, 4 => 535, 5 => 710, 6 => 915,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }

        public int SkillPointsUsed => Skills.Sum(skill => GetSkill(skill.Key));

        public int SkillPointsLeft => SkillPoints - SkillPointsUsed;

        public bool CanIncrementSkill(SkillObject skill)
        {
            // Skills can't go above the tier skill cap
            if (GetSkill(skill) >= SkillCap)
                return false;

            // Check if we have enough skill points left
            if (SkillPointsLeft <= 0)
                return false;
            return true;
        }

        public bool CanDecrementSkill(SkillObject skill)
        {
            // Skills can't go below zero
            if (GetSkill(skill) <= 0)
                return false;

            // Check for equipment skill requirements
            if (GetSkill(skill) <= Equipment.GetSkillRequirement(skill))
                return false;

            return true;
        }

        // =========================================================================
        // Equipment
        // =========================================================================

        public new List<TroopEquipment> Equipments
        {
            get => [.. base.Equipments.Select(ew => new TroopEquipment(ew.Base))];
            set => base.Equipments = [.. value.Select(e => new EquipmentWrapper(e.Base))];
        }

        public TroopEquipment Equipment => new(Equipments.FirstOrDefault().Base);

        public void Equip(TroopItem item, EquipmentIndex slot)
        {
            Equipment.SetItem(slot, item);
        }

        public void Unequip(EquipmentIndex slot)
        {
            Equipment.SetItem(slot, null);
        }

        public void UnequipAll()
        {
            foreach (var slot in EquipmentWrapper.Slots)
                Unequip(slot);
        }

        // =========================================================================
        // Management methods
        // =========================================================================

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

        public TroopCharacter Clone(bool keepUpgrades = true, bool keepEquipment = true, bool keepSkills = true)
        {
            // Clone from the source troop
            var cloneObject = CharacterObject.CreateFrom(_characterObject);

            // Wrap it
            TroopCharacter clone = new(cloneObject, Clan);

            if (keepUpgrades)
                clone.UpgradeTargets = [.. UpgradeTargets];  // Unlink
            else
                clone.UpgradeTargets = [];

            if (keepEquipment)
                clone.Equipments = [.. Equipments];  // Unlink
            else
                clone.Equipments = [];

            if (keepSkills)
                clone.Skills = new Dictionary<SkillObject, int>(Skills);  // Unlink
            else
                clone.Skills = [];

            return clone;
        }
    }
}