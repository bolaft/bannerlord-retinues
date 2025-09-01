using System;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.Core;
using Bannerlord.UIExtenderEx.Attributes;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentRowVM : ViewModel
    {
        private readonly Action<EquipmentRowVM> _onSelect;

        public ItemObject Equipment { get; }

        private bool _isSelected;

        public EquipmentRowVM(ItemObject equipment, Action<EquipmentRowVM> onSelect)
        {
            Equipment = equipment;
            _onSelect = onSelect;
        }

        // ---- Existing ----
        [DataSourceProperty] public string Name => Equipment?.Name?.ToString() ?? string.Empty;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        [DataSourceMethod] public void ExecuteSelect() => _onSelect?.Invoke(this);

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(StatsText));
        }

        private ImageIdentifierVM Image => Equipment != null ? new ImageIdentifierVM(Equipment, "") : null;

        [DataSourceProperty] public int ImageTypeCode => Image?.ImageTypeCode ?? 0;

        [DataSourceProperty] public string ImageId => Image?.Id ?? "";

        [DataSourceProperty] public string ImageAdditionalArgs => Image?.AdditionalArgs ?? "";

        [DataSourceProperty] public int Price => Equipment?.Value ?? 0;

        [DataSourceProperty] public string StatsText
        {
            get
            {
                if (Equipment == null) return string.Empty;

                // Armor
                var ac = Equipment.ArmorComponent;
                if (ac != null)
                {
                    return $"Armor H{ac.HeadArmor} B{ac.BodyArmor} A{ac.ArmArmor} L{ac.LegArmor}";
                }

                // Weapon
                var wc = Equipment.WeaponComponent;
                if (wc != null && wc.PrimaryWeapon != null)
                {
                    var w = wc.PrimaryWeapon;
                    if (w.MissileDamage > 0)
                        return $"Ranged Dmg{w.MissileDamage} Spd{w.MissileSpeed} Acc{w.Accuracy}";
                    return $"Melee SD{w.SwingDamage}@{w.SwingSpeed} TD{w.ThrustDamage}@{w.ThrustSpeed}";
                }

                // Mount
                var hc = Equipment.HorseComponent;
                if (hc != null)
                    return $"Mount Spd{hc.Speed} Mvr{hc.Maneuver} Chg{hc.ChargeDamage}";

                // Harness / Banner
                if (Equipment.ItemType == ItemObject.ItemTypeEnum.HorseHarness)
                    return "Harness";
                if (Equipment.ItemType == ItemObject.ItemTypeEnum.Banner)
                    return "Banner";

                return string.Empty;
            }
        }
    }
}
