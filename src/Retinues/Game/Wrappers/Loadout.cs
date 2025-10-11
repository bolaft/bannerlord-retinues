using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Game.Wrappers
{
    [SafeClass(SwallowByDefault = false)]
    public class WLoadout(WCharacter wc)
    {
        // Owner character
        private readonly WCharacter _owner = wc;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Enum                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Equipment categories
        public enum Category
        {
            Battle = 0,
            Civilian = 1,
            Alternate = 2,
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Roster                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Battle equipment
        public WEquipment Battle
        {
            get => Equipments[(int)Category.Battle];
            set
            {
                var list = Equipments;
                list[(int)Category.Battle] = value;
                Equipments = list;
            }
        }

        // Civilian equipment
        public WEquipment Civilian
        {
            get => Equipments[(int)Category.Civilian];
            set
            {
                var list = Equipments;
                list[(int)Category.Civilian] = value;
                Equipments = list;
            }
        }

        // Alternate equipments (if any)
        public List<WEquipment> Alternates
        {
            get => Equipments.Skip((int)Category.Alternate).ToList();
            set => Equipments = [.. Equipments.Take((int)Category.Alternate), .. value];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Equipment Lists                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets or sets the list of base Equipment objects in this loadout.
        /// </summary>
        public List<Equipment> BaseEquipments
        {
            get =>
                Reflector
                    .GetFieldValue<MBEquipmentRoster>(_owner.Base, "_equipmentRoster")
                    ?.AllEquipments ?? [];
            set
            {
                // ensure index 1 is civilian
                for (int i = 0; i < value.Count; i++)
                {
#if BL13
                    var want =
                        (i == 1)
                            ? Equipment.EquipmentType.Civilian
                            : Equipment.EquipmentType.Battle;
                    try
                    {
                        Reflector.SetFieldValue(value[i], "_equipmentType", want);
                    }
                    catch { }
#else
                    // BL 1.2 doesn't expose type publicly; reconstruct when needed
                    if (i == 1 && !value[i].IsCivilian)
                    {
                        var code = value[i].CalculateEquipmentCode();
                        value[i] = string.IsNullOrEmpty(code)
                            ? new Equipment(true)
                            : Equipment.CreateFromEquipmentCode(code); // this usually carries proper type too
                    }
#endif
                }

                // Create a new MBEquipmentRoster
                var roster = new MBEquipmentRoster();
                // Set the internal equipment list via reflection
                Reflector.SetFieldValue(roster, "_equipments", new MBList<Equipment>(value));
                Reflector.SetFieldValue(_owner.Base, "_equipmentRoster", roster);
            }
        }

        /// <summary>
        /// Gets or sets the list of equipments in this loadout.
        /// </summary>
        public List<WEquipment> Equipments
        {
            get => [.. BaseEquipments.Select(e => new WEquipment(e))];
            set => BaseEquipments = [.. value.Select(we => we.Base)];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Items                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<WItem> Items =>
            [.. Equipments.SelectMany(eq => eq.Items).Where(i => i != null)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the equipment for the specified category and index.
        /// </summary>
        public WEquipment Get(Category category, int index = 0)
        {
            return category switch
            {
                Category.Battle => Battle,
                Category.Civilian => Civilian,
                Category.Alternate => (index >= 0 && index < Alternates.Count)
                    ? Alternates[index]
                    : null,
                _ => null,
            };
        }

        /// <summary>
        /// Clears all equipments, setting them to default empty equipments.
        /// </summary>
        public void Clear()
        {
            Equipments =
            [
                WEquipment.FromCode(null), // Battle
                WEquipment.FromCode(null, civilian: true), // Civilian
            ];
        }

        /// <summary>
        /// Fills this loadout from another loadout.
        /// </summary>
        public void FillFrom(WLoadout loadout, bool ordered = false)
        {
            // Origin loadout is already in order
            if (ordered)
            {
                Battle = WEquipment.FromCode(loadout.Battle.Code);
                Civilian = WEquipment.FromCode(loadout.Civilian.Code, civilian: true);
                Alternates = [.. loadout.Alternates.Select(eq => WEquipment.FromCode(eq.Code))];
            }
            // Need to detect categories
            else
            {
                WEquipment battle = null;
                WEquipment civilian = null;

                foreach (var eq in loadout.Equipments)
                {
                    if (eq.IsCivilian && civilian == null)
                        civilian = WEquipment.FromCode(eq.Code, civilian: true);
                    else if (!eq.IsCivilian && battle == null)
                        battle = WEquipment.FromCode(eq.Code);

                    if (battle != null && civilian != null)
                        break;
                }

                // Replace the entire list so no alternates leak in from source
                Equipments =
                [
                    battle ?? WEquipment.FromCode(null),
                    civilian ?? WEquipment.FromCode(null, civilian: true),
                ];
            }
        }

        /// <summary>
        /// Gets the skill requirement for a specific skill.
        /// </summary>
        public int ComputeSkillRequirement(SkillObject skill)
        {
            int requirement = 0;

            foreach (var eq in Equipments)
            {
                var req = eq.ComputeSkillRequirement(skill);
                if (req > requirement)
                    requirement = req;
            }

            return requirement;
        }

        /// <summary>
        /// Computes the item category requirement for upgrading to the owner troop.
        /// Checks all equipments for horse items, and returns the best one found.
        /// </summary>
        public ItemCategory ComputeUpgradeItemRequirement()
        {
            ItemCategory requirement = null;

            foreach (var eq in Equipments)
            {
                var item = eq.GetItem(EquipmentIndex.Horse);
                if (item == null)
                    continue; // no horse in this equipment

                requirement = item.Category; // at least basic horse

                if (item.Category == DefaultItemCategories.WarHorse)
                {
                    // war horse overrides basic horse
                    requirement = item.Category;
                }
                else if (item.Category == DefaultItemCategories.NobleHorse)
                {
                    requirement = item.Category;
                    break; // no need to check further, noble horse is the best
                }
            }

            return requirement;
        }
    }
}
