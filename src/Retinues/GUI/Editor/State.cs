using System.Collections.Generic;
using System.Linq;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core;

namespace Retinues.GUI.Editor
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                         Structs                        //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    public struct EquipData
    {
        public WItem Item;
        public PendingEquipData Equip;
    }

    public struct SkillData
    {
        public int Value;
        public PendingTrainData Train;
    }

    /// <summary>
    /// Single source of truth for editor/screen state.
    /// </summary>
    [SafeClass]
    public static class State
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WFaction Faction { get; private set; }
        public static WCharacter Troop { get; private set; }
        public static WEquipment Equipment { get; private set; }
        public static EquipmentIndex Slot { get; private set; }
        public static Dictionary<EquipmentIndex, EquipData> EquipData { get; private set; }
        public static Dictionary<SkillObject, SkillData> SkillData { get; private set; }
        public static Dictionary<WCharacter, int> ConversionData { get; private set; }
        public static Dictionary<WCharacter, int> PartyData { get; private set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Reset                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ResetAll()
        {
            EventManager.FireBatch(() =>
            {
                UpdateFaction();
                UpdateTroop();
                UpdateEquipment();
                UpdateSlot();
                UpdateEquipData();
                UpdateSkillData();
                UpdateConversionData();
                UpdatePartyData();
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Updates                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void UpdateFaction(WFaction faction = null)
        {
            faction ??= Player.Clan;

            EventManager.FireBatch(() =>
            {
                Faction = faction;

                UpdateTroop();

                EventManager.Fire(UIEvent.Faction);
            });
        }

        public static void UpdateTroop(WCharacter troop = null)
        {
            troop ??= Faction.Troops.FirstOrDefault();

            EventManager.FireBatch(() =>
            {
                Troop = troop;

                UpdateEquipment();
                UpdateEquipData();
                UpdateSkillData();
                UpdateConversionData();
                UpdateSlot();

                EventManager.Fire(UIEvent.Troop);
            });
        }

        public static void UpdateEquipment(WEquipment equipment = null)
        {
            equipment ??= Troop.Loadout.Battle;

            Equipment = equipment;
            EventManager.Fire(UIEvent.Equipment);
        }

        public static void UpdateSlot(EquipmentIndex slot = EquipmentIndex.Weapon0)
        {
            Slot = slot;
            EventManager.Fire(UIEvent.Slot);
        }

        public static void UpdateEquipData(Dictionary<EquipmentIndex, EquipData> equipData = null)
        {
            equipData ??= ComputeEquipData();

            EquipData = equipData;
            EventManager.Fire(UIEvent.Equip);
        }

        public static void UpdateSkillData(Dictionary<SkillObject, SkillData> skillData = null)
        {
            skillData ??= ComputeSkillData();

            SkillData = skillData;
            EventManager.Fire(UIEvent.Train);
        }

        public static void UpdateConversionData(Dictionary<WCharacter, int> conversionData = null)
        {
            conversionData ??= ComputeConversionData();

            ConversionData = conversionData;
            EventManager.Fire(UIEvent.Conversion);
        }

        public static void UpdatePartyData(Dictionary<WCharacter, int> partyData = null)
        {
            partyData ??= ComputePartyData();

            PartyData = partyData;
            EventManager.Fire(UIEvent.Party);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Derivations                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static Dictionary<EquipmentIndex, EquipData> ComputeEquipData()
        {
            var data = new Dictionary<EquipmentIndex, EquipData>();
            foreach (EquipmentIndex slot in WEquipment.Slots)
            {
                data[slot] = new EquipData
                {
                    Item = Equipment.Get(slot),
                    Equip = TroopEquipBehavior.GetStagedChange(Troop, slot, Equipment.Index),
                };
            }
            return data;
        }

        private static Dictionary<SkillObject, SkillData> ComputeSkillData()
        {
            var data = new Dictionary<SkillObject, SkillData>();
            foreach (KeyValuePair<SkillObject, int> kvp in Troop.Skills)
            {
                data[kvp.Key] = new SkillData
                {
                    Value = kvp.Value,
                    Train = TroopTrainBehavior.GetStagedChange(Troop, kvp.Key),
                };
            }
            return data;
        }

        private static Dictionary<WCharacter, int> ComputeConversionData()
        {
            var data = new Dictionary<WCharacter, int>();

            if (Troop?.IsRetinue == false)
                return data;

            foreach (var source in TroopManager.GetRetinueSourceTroops(Troop))
                data[source] = 0;

            return data;
        }

        private static Dictionary<WCharacter, int> ComputePartyData()
        {
            var data = new Dictionary<WCharacter, int>();

            foreach (var element in Player.Party.MemberRoster.Elements)
                data[element.Troop] = element.Number + element.WoundedNumber;

            return data;
        }
    }
}
