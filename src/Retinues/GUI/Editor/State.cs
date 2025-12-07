using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Features.Agents;
using Retinues.Features.Staging;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using Retinues.Mods;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
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
        // Items that are "available from another set" for the current troop + set.
        // Keyed by ItemObject.StringId. Rebuilt whenever equip data changes.
        public static HashSet<string> AvailableFromAnotherSetCache { get; private set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Launch Options                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static BaseFaction PendingFaction { get; set; }
        public static WCharacter PendingTroop { get; set; }

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

        /* ━━━━━━ Convenience ━━━━━ */

        public static WCulture Culture => Faction?.Culture;
        public static WClan Clan
        {
            get
            {
                if (Faction is WClan clan)
                    return clan;
                if (Faction is WFaction faction)
                    return new(Hero.MainHero.Clan);
                if (Faction is WCulture culture)
                    return WClan.All.FirstOrDefault(c => c?.Culture?.StringId == culture.StringId);
                return null;
            }
        }

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
                if (PendingFaction != null)
                {
                    UpdateFaction(PendingFaction);
                    PendingFaction = null;
                }
                else
                {
                    UpdateFaction();
                }

                if (PendingTroop != null)
                {
                    UpdateTroop(PendingTroop);
                    PendingTroop = null;
                }

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
            faction?.InvalidateCategoryCache();

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
        public static void UpdateTroop(WCharacter troop = null)
        {
            troop ??= Faction.Troops.FirstOrDefault();

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
            EventManager.FireBatch(() =>
            {
                // Default to battle loadout unless civilian troop
                equipment ??= Troop.IsCivilian ? Troop.Loadout.Civilian : Troop.Loadout.Battle;

                Equipment = equipment;

                // Rebuild equip data (multi-slot change)
                UpdateEquipData(singleUpdate: false);

                // Notify equipment-related bindings
                EventManager.Fire(UIEvent.Equipment);
            });
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

            RebuildAvailableFromAnotherSetCache();

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
                data[element.Troop] = element.Number;

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
            void EnsureOne(PolicyToggleType t)
            {
                // Count enabled among battle sets
                int enabled = 0;
                var eqs = troop.Loadout.Equipments;
                for (int i = 0; i < eqs.Count; i++)
                {
                    if (eqs[i].IsCivilian)
                        continue;
                    if (CombatAgentBehavior.IsEnabled(troop, i, t))
                        enabled++;
                }
                if (enabled == 0)
                {
                    // Enable index 0 (Normalize guarantees index 0 is a battle set)
                    if (!CombatAgentBehavior.IsEnabled(troop, 0, t))
                        CombatAgentBehavior.Toggle(troop, 0, t); // Toggle will enable
                }
            }

            EnsureOne(PolicyToggleType.FieldBattle);
            EnsureOne(PolicyToggleType.SiegeDefense);
            EnsureOne(PolicyToggleType.SiegeAssault);
            if (ModCompatibility.HasNavalDLC)
                EnsureOne(PolicyToggleType.NavalBattle);
        }

        private static void RebuildAvailableFromAnotherSetCache()
        {
            Utils.Timer.Begin("BuildAvailableFromAnotherSetCache");

            var result = new HashSet<string>(StringComparer.Ordinal);

            try
            {
                var troop = Troop;
                var equipment = Equipment;

                if (troop == null || equipment == null)
                {
                    AvailableFromAnotherSetCache = result;
                    return;
                }

                var loadout = troop.Loadout;
                var eqs = loadout.Equipments;
                int setIndex = equipment.Index;

                if (setIndex < 0 || setIndex >= eqs.Count)
                {
                    AvailableFromAnotherSetCache = result;
                    return;
                }

                // ─────────────────────────────────────────────────────────
                // 1) Per-set item sets for the current troop (by StringId)
                // ─────────────────────────────────────────────────────────
                var perSetItems = new List<HashSet<string>>(eqs.Count);
                for (int i = 0; i < eqs.Count; i++)
                {
                    var setItems = new HashSet<string>(StringComparer.Ordinal);
                    var eqBase = eqs[i].Base;

                    foreach (var slot in WEquipment.Slots)
                    {
                        var item = eqBase[slot].Item;
                        if (item != null)
                            setItems.Add(item.StringId);
                    }

                    perSetItems.Add(setItems);
                }

                var currentItems = perSetItems[setIndex];

                // ─────────────────────────────────────────────────────────
                // 2) Counterpart (captain/base) union, if any
                // ─────────────────────────────────────────────────────────
                HashSet<string> counterpartItems = null;
                WCharacter counterpart = null;

                if (troop.IsCaptain && troop.BaseTroop != null)
                    counterpart = troop.BaseTroop;
                else if (!troop.IsCaptain && troop.Captain != null)
                    counterpart = troop.Captain;

                if (counterpart?.IsValid == true)
                {
                    counterpartItems = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var eqWrap in counterpart.Loadout.Equipments)
                    {
                        var eqBase = eqWrap.Base;
                        foreach (var slot in WEquipment.Slots)
                        {
                            var item = eqBase[slot].Item;
                            if (item != null)
                                counterpartItems.Add(item.StringId);
                        }
                    }
                }

                // ─────────────────────────────────────────────────────────
                // 3) Global union of all items from troop + counterpart
                // ─────────────────────────────────────────────────────────
                var union = new HashSet<string>(StringComparer.Ordinal);
                foreach (var set in perSetItems)
                    union.UnionWith(set);

                if (counterpartItems != null)
                    union.UnionWith(counterpartItems);

                // ─────────────────────────────────────────────────────────
                // 4) Decide which items are "available from another set"
                // ─────────────────────────────────────────────────────────
                foreach (var id in union)
                {
                    // if this set already has the item, it's not "from another set"
                    if (currentItems.Contains(id))
                        continue;

                    bool inOthers = false;
                    for (int i = 0; i < perSetItems.Count; i++)
                    {
                        if (i == setIndex)
                            continue;

                        if (perSetItems[i].Contains(id))
                        {
                            inOthers = true;
                            break;
                        }
                    }

                    bool inCounterpart = counterpartItems != null && counterpartItems.Contains(id);

                    if (inOthers || inCounterpart)
                        result.Add(id);
                }

                AvailableFromAnotherSetCache = result;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                AvailableFromAnotherSetCache = result;
            }
            finally
            {
                Utils.Timer.End("BuildAvailableFromAnotherSetCache");
            }
        }
    }
}
