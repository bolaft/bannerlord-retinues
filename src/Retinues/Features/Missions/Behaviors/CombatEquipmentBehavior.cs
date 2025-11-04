using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Features.Missions.Behaviors
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
    public sealed class CombatEquipmentBehavior : CampaignBehaviorBase
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

        public EquipmentUsePolicy GetPolicy(WCharacter troop, int altIndex)
        {
            if (troop == null || altIndex < 2)
                return EquipmentUsePolicy.All; // 0 battle, 1 civilian => always allowed
            if (
                _byTroop.TryGetValue(troop.StringId, out var perAlt)
                && perAlt.TryGetValue(altIndex, out var p)
                && p != null
            )
                return p;
            return EquipmentUsePolicy.None;
        }

        public bool IsEnabled_Field(WCharacter troop, int altIndex) =>
            GetPolicy(troop, altIndex).FieldBattle;

        public bool IsEnabled_SiegeDefense(WCharacter troop, int altIndex) =>
            GetPolicy(troop, altIndex).SiegeDefense;

        public bool IsEnabled_SiegeAssault(WCharacter troop, int altIndex) =>
            GetPolicy(troop, altIndex).SiegeAssault;

        /* ━━━━━━━ Commands ━━━━━━━ */

        public void Toggle_Field(WCharacter troop, int altIndex) =>
            Set(troop, altIndex, p => p.FieldBattle = !p.FieldBattle);

        public void Toggle_SiegeDefense(WCharacter troop, int altIndex) =>
            Set(troop, altIndex, p => p.SiegeDefense = !p.SiegeDefense);

        public void Toggle_SiegeAssault(WCharacter troop, int altIndex) =>
            Set(troop, altIndex, p => p.SiegeAssault = !p.SiegeAssault);

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

        private void Set(WCharacter troop, int altIndex, System.Action<EquipmentUsePolicy> mut)
        {
            if (troop == null || altIndex < 2)
                return;

            if (!_byTroop.TryGetValue(troop.StringId, out var perAlt))
                _byTroop[troop.StringId] = perAlt = [];

            if (!perAlt.TryGetValue(altIndex, out var p) || p == null)
                perAlt[altIndex] = p = new EquipmentUsePolicy();

            mut(p);
        }

        /* ━━━━━━━ Accessors ━━━━━━ */

        private static CombatEquipmentBehavior Inst =>
            Campaign.Current?.GetCampaignBehavior<CombatEquipmentBehavior>();

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

        public static void OnRemoved(WCharacter troop, int removedIndex) =>
            Inst?.OnRemoveAlt(troop, removedIndex);
    }
}
