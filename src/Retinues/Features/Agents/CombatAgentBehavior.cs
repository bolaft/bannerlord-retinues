using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Features.Agents
{
    public enum PolicyToggleType
    {
        FieldBattle,
        SiegeDefense,
        SiegeAssault,
        GenderOverride,
    }

    /// <summary>
    /// Per (troop, altIndex) toggle for where an alternate set may be used.
    /// </summary>
    [Serializable]
    public class EquipmentPolicy
    {
        [SaveableField(1)]
        public bool FieldBattle = false;

        [SaveableField(2)]
        public bool SiegeDefense = false;

        [SaveableField(3)]
        public bool SiegeAssault = false;

        [SaveableField(4)]
        public bool GenderOverride = false;

        public static readonly EquipmentPolicy None = new()
        {
            FieldBattle = false,
            SiegeDefense = false,
            SiegeAssault = false,
            GenderOverride = false,
        };

        public static readonly EquipmentPolicy All = new()
        {
            FieldBattle = true,
            SiegeDefense = true,
            SiegeAssault = true,
            GenderOverride = false, // default to no override
        };
    }

    /// <summary>
    /// Stores which alternates are enabled for each battle context.
    /// </summary>
    public sealed class CombatAgentBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, Dictionary<int, EquipmentPolicy>> _byTroop = [];

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_EquipmentUsePolicy", ref _byTroop);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Queries ━━━━━━━ */

        public EquipmentPolicy GetPolicy(WCharacter troop, int index)
        {
            if (troop == null)
                return EquipmentPolicy.None;
            if (
                _byTroop.TryGetValue(troop.StringId, out var per)
                && per.TryGetValue(index, out var p)
                && p != null
            )
                return p;
            return EquipmentPolicy.All;
        }

        public bool IsEnabled_Field(WCharacter troop, int index) =>
            GetPolicy(troop, index).FieldBattle;

        public bool IsEnabled_SiegeDefense(WCharacter troop, int index) =>
            GetPolicy(troop, index).SiegeDefense;

        public bool IsEnabled_SiegeAssault(WCharacter troop, int index) =>
            GetPolicy(troop, index).SiegeAssault;

        public bool IsEnabled_GenderOverride(WCharacter troop, int index) =>
            GetPolicy(troop, index).GenderOverride;

        /* ━━━━━━━ Commands ━━━━━━━ */

        public void Toggle_Field(WCharacter troop, int index)
        {
            if (troop == null)
                return;
            if (!CanDisable(troop, index, PolicyToggleType.FieldBattle))
                return;
            Set(troop, index, p => p.FieldBattle = !p.FieldBattle);
        }

        public void Toggle_SiegeDefense(WCharacter troop, int index)
        {
            if (troop == null)
                return;
            if (!CanDisable(troop, index, PolicyToggleType.SiegeDefense))
                return;
            Set(troop, index, p => p.SiegeDefense = !p.SiegeDefense);
        }

        public void Toggle_SiegeAssault(WCharacter troop, int index)
        {
            if (troop == null)
                return;
            if (!CanDisable(troop, index, PolicyToggleType.SiegeAssault))
                return;
            Set(troop, index, p => p.SiegeAssault = !p.SiegeAssault);
        }

        public void Toggle_GenderOverride(WCharacter troop, int index)
        {
            if (troop == null)
                return;
            Set(troop, index, p => p.GenderOverride = !p.GenderOverride);
        }

        public void OnRemoveAlt(WCharacter troop, int removedIndex)
        {
            if (troop == null || removedIndex < 2)
                return;
            if (!_byTroop.TryGetValue(troop.StringId, out var perAlt))
                return;

            var next = new Dictionary<int, EquipmentPolicy>();
            foreach (var kv in perAlt)
            {
                if (kv.Key < removedIndex)
                    next[kv.Key] = kv.Value;
                else if (kv.Key > removedIndex)
                    next[kv.Key - 1] = kv.Value; // shift down
            }
            if (next.Count == 0)
                _byTroop.Remove(troop.StringId);
            else
                _byTroop[troop.StringId] = next;
        }

        private void Set(WCharacter troop, int index, Action<EquipmentPolicy> mut)
        {
            if (troop == null)
                return;

            if (!_byTroop.TryGetValue(troop.StringId, out var per))
                _byTroop[troop.StringId] = per = [];

            if (!per.TryGetValue(index, out var p) || p == null)
            {
                // Initialize as "All" so the first toggle actually flips from true -> false
                per[index] = p = new EquipmentPolicy
                {
                    FieldBattle = true,
                    SiegeDefense = true,
                    SiegeAssault = true,
                    GenderOverride = false,
                };
            }

            mut(p);
        }

        /* ━━━━━━━ Accessors ━━━━━━ */

        private static CombatAgentBehavior Inst =>
            Campaign.Current?.GetCampaignBehavior<CombatAgentBehavior>();

        public static bool IsEnabled(WCharacter troop, int altIndex, PolicyToggleType t) =>
            t switch
            {
                PolicyToggleType.FieldBattle => Inst?.IsEnabled_Field(troop, altIndex) ?? true,
                PolicyToggleType.SiegeDefense => Inst?.IsEnabled_SiegeDefense(troop, altIndex)
                    ?? true,
                PolicyToggleType.SiegeAssault => Inst?.IsEnabled_SiegeAssault(troop, altIndex)
                    ?? true,
                PolicyToggleType.GenderOverride => Inst?.IsEnabled_GenderOverride(troop, altIndex)
                    ?? true,
                _ => true,
            };

        public static void Toggle(WCharacter troop, int altIndex, PolicyToggleType t)
        {
            switch (t)
            {
                case PolicyToggleType.FieldBattle:
                    Inst?.Toggle_Field(troop, altIndex);
                    break;
                case PolicyToggleType.SiegeDefense:
                    Inst?.Toggle_SiegeDefense(troop, altIndex);
                    break;
                case PolicyToggleType.SiegeAssault:
                    Inst?.Toggle_SiegeAssault(troop, altIndex);
                    break;
                case PolicyToggleType.GenderOverride:
                    Inst?.Toggle_GenderOverride(troop, altIndex);
                    break;
            }
        }

        public static void DisableAll(WCharacter troop, int altIndex)
        {
            if (troop == null)
                return;
            Inst?.Set(
                troop,
                altIndex,
                p =>
                {
                    p.FieldBattle = false;
                    p.SiegeDefense = false;
                    p.SiegeAssault = false;
                    p.GenderOverride = false;
                }
            );
        }

        public static void OnRemoved(WCharacter troop, int removedIndex) =>
            Inst?.OnRemoveAlt(troop, removedIndex);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private int CountEnabled(WCharacter troop, PolicyToggleType t)
        {
            if (troop == null)
                return 0;
            int c = 0;
            var eqs = troop.Loadout.Equipments;
            for (int i = 0; i < eqs.Count; i++)
            {
                if (eqs[i].IsCivilian)
                    continue;
                if (IsEnabled(troop, i, t))
                    c++;
            }
            return c;
        }

        private bool CanDisable(WCharacter troop, int index, PolicyToggleType t)
        {
            // If it's already disabled, ok
            if (!IsEnabled(troop, index, t))
                return true;

            // If gender override, always ok
            if (t == PolicyToggleType.GenderOverride)
                return true;

            // Otherwise ensure at least one other enabled
            int enabled = CountEnabled(troop, t);
            return enabled > 1; // after disabling there will be >=1 left
        }
    }
}
