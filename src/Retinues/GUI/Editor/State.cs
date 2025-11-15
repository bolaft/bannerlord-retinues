using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Features.Equipments;
using Retinues.Features.Staging;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.Core;

namespace Retinues.GUI.Editor
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                         Structs                        //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Equipment slot data: current item and staged equip change.
    /// </summary>
    public struct EquipData
    {
        public WItem Item;
        public PendingEquipData Equip;
    }

    /// <summary>
    /// Snapshot of an equip delta for a single slot.
    /// </summary>
    public readonly struct EquipChangeDelta(
        string oldEquippedId,
        string newEquippedId,
        string oldStagedId,
        string newStagedId
    )
    {
        public readonly string OldEquippedId = oldEquippedId;
        public readonly string NewEquippedId = newEquippedId;
        public readonly string OldStagedId = oldStagedId;
        public readonly string NewStagedId = newStagedId;
    }

    /// <summary>
    /// Skill data: current value and staged training change.
    /// </summary>
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

        public static BaseFaction Faction { get; private set; }
        public static WCharacter Troop { get; private set; }
        public static WEquipment Equipment { get; private set; }
        public static EquipmentIndex Slot { get; private set; }
        public static Dictionary<EquipmentIndex, EquipData> EquipData { get; private set; }
        public static Dictionary<SkillObject, SkillData> SkillData { get; private set; }
        public static Dictionary<WCharacter, int> ConversionData { get; private set; }
        public static Dictionary<WCharacter, int> PartyData { get; private set; }
        public static EquipChangeDelta? LastEquipChange;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Reset                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Reset all editor state.
        /// </summary>
        public static void ResetAll()
        {
            if (!ClanScreen.IsStudioMode)
                // Ensure troops exist for player factions
                foreach (var f in new[] { Player.Clan, Player.Kingdom })
                    if (f != null)
                        TroopBuilder.EnsureTroopsExist(f);

            EventManager.FireBatch(() =>
            {
                UpdateFaction();
                UpdatePartyData();
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Updates                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Set current faction (defaults to player clan).
        /// </summary>
        public static void UpdateFaction(BaseFaction faction = null)
        {
            faction ??= !ClanScreen.IsStudioMode ? Player.Clan : Player.Culture;

            EventManager.FireBatch(() =>
            {
                Faction = faction;

                UpdateTroop();

                EventManager.Fire(UIEvent.Faction);
            });
        }

        /// <summary>
        /// Set current troop (defaults to first faction troop).
        /// </summary>
        public static void UpdateTroop(WCharacter troop = null, bool checkCurrentTroop = false)
        {
            troop ??= Faction.Troops.FirstOrDefault();
            if (checkCurrentTroop && Troop != null && Troop.StringId == troop?.StringId)
                return;

            EventManager.FireBatch(() =>
            {
                Troop = troop;

                if (Troop != null)
                    FixIntegrity(Troop);

                UpdateEquipment();
                UpdateSkillData();
                UpdateSlot();

                EventManager.Fire(UIEvent.Troop);
            });
        }

        /// <summary>
        /// Set current equipment (defaults to troop battle loadout).
        /// </summary>
        public static void UpdateEquipment(WEquipment equipment = null)
        {
            equipment ??= Troop.Loadout.Battle;

            Equipment = equipment;

            UpdateEquipData(singleUpdate: false);

            EventManager.Fire(UIEvent.Equipment);
        }

        /// <summary>
        /// Set currently selected equipment slot.
        /// </summary>
        public static void UpdateSlot(EquipmentIndex slot = EquipmentIndex.Weapon0)
        {
            if (Slot == slot)
                return;

            Slot = slot;
            EventManager.Fire(UIEvent.Slot);
        }

        /// <summary>
        /// Recompute or set equip data cache.
        /// </summary>
        public static void UpdateEquipData(
            Dictionary<EquipmentIndex, EquipData> equipData = null,
            bool singleUpdate = true
        )
        {
            equipData ??= ComputeEquipData();

            if (Troop?.IsRetinue == true)
                UpdateConversionData();

            EquipChangeDelta? delta = singleUpdate ? CaptureEquipChange(equipData, Slot) : null;

            EquipData = equipData;

            if (singleUpdate)
            {
                LastEquipChange = delta;
                EventManager.Fire(UIEvent.Equip);
            }

            UpdateAppearance();
        }

        /// <summary>
        /// Recompute or set skill data cache.
        /// </summary>
        public static void UpdateSkillData(Dictionary<SkillObject, SkillData> skillData = null)
        {
            skillData ??= ComputeSkillData();

            SkillData = skillData;
            EventManager.Fire(UIEvent.Train);
        }

        /// <summary>
        /// Recompute or set conversion data.
        /// </summary>
        public static void UpdateConversionData(Dictionary<WCharacter, int> conversionData = null)
        {
            conversionData ??= ComputeConversionData();

            ConversionData = conversionData;
            EventManager.Fire(UIEvent.Conversion);
        }

        /// <summary>
        /// Clear staged conversion selections without recomputing data.
        /// </summary>
        public static void ClearPendingConversions()
        {
            if (ConversionData == null)
            {
                UpdateConversionData();
                return;
            }

            bool changed = false;
            foreach (var key in ConversionData.Keys.ToList())
            {
                if (ConversionData[key] == 0)
                    continue;

                ConversionData[key] = 0;
                changed = true;
            }

            if (changed)
                EventManager.Fire(UIEvent.Conversion);
        }

        /// <summary>
        /// Recompute or set party data.
        /// </summary>
        public static void UpdatePartyData(Dictionary<WCharacter, int> partyData = null)
        {
            partyData ??= ComputePartyData();

            PartyData = partyData;
            EventManager.Fire(UIEvent.Party);
        }

        /// <summary>
        /// Notify appearance change.
        /// </summary>
        public static void UpdateAppearance()
        {
            EventManager.Fire(UIEvent.Appearance);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Derivations                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Build equip data dictionary from current equipment and staged changes.
        /// </summary>
        private static Dictionary<EquipmentIndex, EquipData> ComputeEquipData()
        {
            var data = new Dictionary<EquipmentIndex, EquipData>();
            foreach (EquipmentIndex slot in WEquipment.Slots)
            {
                data[slot] = new EquipData
                {
                    Item = Equipment.Get(slot),
                    Equip = EquipStagingBehavior.Get(Troop, slot, Equipment.Index),
                };
            }
            return data;
        }

        /// <summary>
        /// Build skill data dictionary from troop skills and staged training changes.
        /// </summary>
        private static Dictionary<SkillObject, SkillData> ComputeSkillData()
        {
            var data = new Dictionary<SkillObject, SkillData>();
            foreach (KeyValuePair<SkillObject, int> kvp in Troop.Skills)
            {
                data[kvp.Key] = new SkillData
                {
                    Value = kvp.Value,
                    Train = TrainStagingBehavior.Get(Troop, kvp.Key),
                };
            }
            return data;
        }

        /// <summary>
        /// Build conversion data for retinue source troops if troop is a retinue.
        /// </summary>
        private static Dictionary<WCharacter, int> ComputeConversionData()
        {
            var data = new Dictionary<WCharacter, int>();

            if (Troop?.IsRetinue != true)
                return data;

            foreach (var source in RetinueManager.GetRetinueSourceTroops(Troop))
                if (source?.IsValid == true)
                    data[source] = 0;

            return data;
        }

        /// <summary>
        /// Build party counts dictionary from player's party roster.
        /// </summary>
        private static Dictionary<WCharacter, int> ComputePartyData()
        {
            var data = new Dictionary<WCharacter, int>();

            foreach (var element in Player.Party.MemberRoster.Elements)
                data[element.Troop] = element.Number + element.WoundedNumber;

            return data;
        }

        /// <summary>
        /// Compute a (old/new) delta for the given slot using the provided data snapshot.
        /// </summary>
        private static EquipChangeDelta? CaptureEquipChange(
            Dictionary<EquipmentIndex, EquipData> next,
            EquipmentIndex slot
        )
        {
            if (!next.TryGetValue(slot, out var newEntry))
                return null;

            string oldEquippedId = null;
            string oldStagedId = null;

            if (EquipData != null && EquipData.TryGetValue(slot, out var oldEntry))
            {
                oldEquippedId = oldEntry.Item?.StringId;
                oldStagedId = oldEntry.Equip?.ItemId;
            }

            var newEquippedId = newEntry.Item?.StringId;
            var newStagedId = newEntry.Equip?.ItemId;

            if (oldEquippedId == newEquippedId && oldStagedId == newStagedId)
                return null;

            return new EquipChangeDelta(oldEquippedId, newEquippedId, oldStagedId, newStagedId);
        }

        public static void FixIntegrity(WCharacter troop)
        {
            if (troop == null)
                return;

            try
            {
                troop.Loadout.EnsureMinimumSets();
                troop.Loadout.Normalize();
                FixCombatPolicies(troop);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// Ensure at least one battle set is enabled for each situation.
        /// </summary>
        public static void FixCombatPolicies(WCharacter troop)
        {
            if (troop == null)
                return;

            // Helper that checks a situation and enables index 0 if none enabled
            void EnsureOne(BattleType t)
            {
                // Count enabled among battle sets
                int enabled = 0;
                var eqs = troop.Loadout.Equipments;
                for (int i = 0; i < eqs.Count; i++)
                {
                    if (eqs[i].IsCivilian)
                        continue;
                    if (EquipmentPolicyBehavior.IsEnabled(troop, i, t))
                        enabled++;
                }
                if (enabled == 0)
                {
                    // Enable index 0 (Normalize guarantees index 0 is a battle set)
                    if (!EquipmentPolicyBehavior.IsEnabled(troop, 0, t))
                        EquipmentPolicyBehavior.Toggle(troop, 0, t); // Toggle will enable
                }
            }

            EnsureOne(BattleType.FieldBattle);
            EnsureOne(BattleType.SiegeDefense);
            EnsureOne(BattleType.SiegeAssault);
        }
    }
}
