using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Features.Loadouts.Behaviors
{
    public enum BattleType
    {
        FieldBattle,
        SiegeDefense,
        SiegeAssault,
    }

    /// <summary>
    /// Per (troop, altIndex) toggle for where an alternate set may be used.
    /// </summary>
    [Serializable]
    public class EquipmentUsePolicy
    {
        [SaveableField(1)]
        public bool FieldBattle = false;

        [SaveableField(2)]
        public bool SiegeDefense = false;

        [SaveableField(3)]
        public bool SiegeAssault = false;

        public static readonly EquipmentUsePolicy None = new()
        {
            FieldBattle = false,
            SiegeDefense = false,
            SiegeAssault = false,
        };

        public static readonly EquipmentUsePolicy All = new()
        {
            FieldBattle = true,
            SiegeDefense = true,
            SiegeAssault = true,
        };
    }

    /// <summary>
    /// Stores which alternates are enabled for each battle context.
    /// </summary>
    public sealed class CombatLoadoutBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, Dictionary<int, EquipmentUsePolicy>> _byTroop = [];

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

        public EquipmentUsePolicy GetPolicy(WCharacter troop, int index)
        {
            if (troop == null)
                return EquipmentUsePolicy.None;
            if (
                _byTroop.TryGetValue(troop.StringId, out var per)
                && per.TryGetValue(index, out var p)
                && p != null
            )
                return p;
            return EquipmentUsePolicy.All;
        }

        public bool IsEnabled_Field(WCharacter troop, int index) =>
            GetPolicy(troop, index).FieldBattle;

        public bool IsEnabled_SiegeDefense(WCharacter troop, int index) =>
            GetPolicy(troop, index).SiegeDefense;

        public bool IsEnabled_SiegeAssault(WCharacter troop, int index) =>
            GetPolicy(troop, index).SiegeAssault;

        /* ━━━━━━━ Commands ━━━━━━━ */

        public void Toggle_Field(WCharacter troop, int index)
        {
            if (troop == null)
                return;
            if (!CanDisable(troop, index, BattleType.FieldBattle))
                return;
            Set(troop, index, p => p.FieldBattle = !p.FieldBattle);
        }

        public void Toggle_SiegeDefense(WCharacter troop, int index)
        {
            if (troop == null)
                return;
            if (!CanDisable(troop, index, BattleType.SiegeDefense))
                return;
            Set(troop, index, p => p.SiegeDefense = !p.SiegeDefense);
        }

        public void Toggle_SiegeAssault(WCharacter troop, int index)
        {
            if (troop == null)
                return;
            if (!CanDisable(troop, index, BattleType.SiegeAssault))
                return;
            Set(troop, index, p => p.SiegeAssault = !p.SiegeAssault);
        }

        public void OnRemoveAlt(WCharacter troop, int removedIndex)
        {
            if (troop == null || removedIndex < 2)
                return;
            if (!_byTroop.TryGetValue(troop.StringId, out var perAlt))
                return;

            var next = new Dictionary<int, EquipmentUsePolicy>();
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

        private void Set(WCharacter troop, int index, Action<EquipmentUsePolicy> mut)
        {
            if (troop == null)
                return;

            if (!_byTroop.TryGetValue(troop.StringId, out var per))
                _byTroop[troop.StringId] = per = [];

            if (!per.TryGetValue(index, out var p) || p == null)
                per[index] = p = new EquipmentUsePolicy();

            mut(p);
        }

        /* ━━━━━━━ Accessors ━━━━━━ */

        private static CombatLoadoutBehavior Inst =>
            Campaign.Current?.GetCampaignBehavior<CombatLoadoutBehavior>();

        public static bool IsEnabled(WCharacter troop, int altIndex, BattleType t) =>
            t switch
            {
                BattleType.FieldBattle => Inst?.IsEnabled_Field(troop, altIndex) ?? true,
                BattleType.SiegeDefense => Inst?.IsEnabled_SiegeDefense(troop, altIndex) ?? true,
                BattleType.SiegeAssault => Inst?.IsEnabled_SiegeAssault(troop, altIndex) ?? true,
                _ => true,
            };

        public static void Toggle(WCharacter troop, int altIndex, BattleType t)
        {
            switch (t)
            {
                case BattleType.FieldBattle:
                    Inst?.Toggle_Field(troop, altIndex);
                    break;
                case BattleType.SiegeDefense:
                    Inst?.Toggle_SiegeDefense(troop, altIndex);
                    break;
                case BattleType.SiegeAssault:
                    Inst?.Toggle_SiegeAssault(troop, altIndex);
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
                }
            );
        }

        public static void OnRemoved(WCharacter troop, int removedIndex) =>
            Inst?.OnRemoveAlt(troop, removedIndex);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private int CountEnabled(WCharacter troop, BattleType t)
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

        private bool CanDisable(WCharacter troop, int index, BattleType t)
        {
            // If it's already disabled, ok
            if (!IsEnabled(troop, index, t))
                return true;

            // Otherwise ensure at least one other enabled
            int enabled = CountEnabled(troop, t);
            return enabled > 1; // after disabling there will be >=1 left
        }
    }
}
