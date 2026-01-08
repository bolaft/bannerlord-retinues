using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Agents
{
    /// <summary>
    /// Applies spawn-time overrides (equipment selection and mixed gender).
    /// Battle context is resolved from MMapEvent/MMission to avoid duplicating engine heuristics.
    /// </summary>
    public sealed class AgentSpawnResolver
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Context                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public enum BattleContext
        {
            Field = 0,
            Siege = 1,
            Naval = 2,
        }

        public static BattleContext CurrentContext => ResolveContext(Mission.Current);

        public static bool IsCombatMission => ResolveIsCombat(Mission.Current);

        private static bool ResolveIsCombat(Mission mission)
        {
            if (mission == null)
                return false;

            switch (mission.Mode)
            {
                case MissionMode.Battle:
                case MissionMode.Deployment:
                case MissionMode.Stealth:
                case MissionMode.Duel:
                case MissionMode.Tournament:
                    return true;

                default:
                    return false;
            }
        }

        private static BattleContext ResolveContext(Mission mission)
        {
            var mapEvent = MMapEvent.Current;
            if (mapEvent != null)
            {
                if (mapEvent.IsNavalBattle)
                    return BattleContext.Naval;

                if (mapEvent.IsSiege)
                    return BattleContext.Siege;

                return BattleContext.Field;
            }

            // Fallback for missions that do not have (or do not set) a current map event.
            // Keep it minimal to avoid duplicating logic already centralized in MMapEvent/NavalBattleHelper.
            if (mission == null)
                return BattleContext.Field;

            if (mission.IsSiegeBattle || mission.IsSallyOutBattle)
                return BattleContext.Siege;

            // If the engine/DLC exposes a Mission.IsNavalBattle property, honor it.
            try
            {
                if (Reflection.HasProperty(mission, "IsNavalBattle"))
                {
                    var flag = Reflection.GetPropertyValue<bool>(mission, "IsNavalBattle");
                    if (flag)
                        return BattleContext.Naval;
                }
            }
            catch
            {
                // Best effort only.
            }

            return BattleContext.Field;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Spawn Overrides                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Applies equipment and mixed gender rules to an AgentBuildData before spawning.
        /// Safe to call from Harmony patches.
        /// </summary>
        public static void ApplyTo(Mission mission, AgentBuildData data)
        {
            if (mission == null || data == null)
                return;

            if (!ResolveIsCombat(mission))
                return;

            var troop = ResolveCharacter(data);
            if (troop == null || troop.IsHero)
                return;

            var wc = WCharacter.Get(troop);
            if (wc == null)
                return;

            ApplyMixedGender(wc, data);
            ApplyEquipmentRules(wc, mission, data);
        }

        private static CharacterObject ResolveCharacter(AgentBuildData data)
        {
            if (data.AgentCharacter is CharacterObject a)
                return a;

            return data.AgentOrigin?.Troop as CharacterObject;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Mixed Gender                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ApplyMixedGender(WCharacter wc, AgentBuildData data)
        {
            if (wc == null || data == null)
                return;

            if (!wc.IsMixedGender)
                return;

            if (data.GenderOverriden)
                return;

            float ratio = Settings.MixedGenderRatio;
            if (ratio <= 0f)
                return;

            bool spawnAsOpposite = MBRandom.RandomFloat < ratio;
            bool isFemale = spawnAsOpposite ? !wc.IsFemale : wc.IsFemale;

            data.IsFemale(isFemale);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Equipments                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ApplyEquipmentRules(WCharacter wc, Mission mission, AgentBuildData data)
        {
            if (wc == null || mission == null || data == null)
                return;

            // Respect other systems that already forced equipment.
            if (data.AgentFixedEquipment)
                return;

            if (data.AgentOverridenSpawnEquipment != null)
                return;

            if (data.AgentOverridenSpawnMissionEquipment != null)
                return;

            var eqs = wc.Equipments;
            if (eqs == null || eqs.Count == 0)
                return;

            // Civilian spawns: optionally randomize among civilian sets if multiple exist.
            if (data.AgentCivilianEquipment)
            {
                var civilian = Collect(eqs, e => e != null && e.IsCivilian);
                if (civilian.Count <= 1)
                    return;

                var chosen = civilian[MBRandom.RandomInt(civilian.Count)];
                if (chosen?.Base == null)
                    return;

                data.Equipment(chosen.Base)
                    .MissionEquipment(null)
                    .FixedEquipment(true)
                    .CivilianEquipment(true);

                return;
            }

            // Battle spawns: enforce context flags (using MMapEvent when available).
            var allBattle = Collect(eqs, e => e != null && !e.IsCivilian);
            if (allBattle.Count == 0)
                return;

            var ctx = ResolveContext(mission);

            var eligible = Collect(
                allBattle,
                e =>
                    ctx switch
                    {
                        BattleContext.Siege => e.SiegeBattleSet,
                        BattleContext.Naval => e.NavalBattleSet,
                        _ => e.FieldBattleSet,
                    }
            );

            // Safety fallback if player disabled everything for this context.
            if (eligible.Count == 0)
            {
                var fallback = wc.FirstBattleEquipment;
                if (fallback?.Base == null)
                    return;

                data.Equipment(fallback.Base)
                    .MissionEquipment(null)
                    .FixedEquipment(true)
                    .CivilianEquipment(false);

                return;
            }

            // Always force a single full equipment set when multiple are eligible.
            // This prevents per-slot mixing when more than one set is enabled for the context.
            var chosenEq =
                eligible.Count == 1 ? eligible[0] : eligible[MBRandom.RandomInt(eligible.Count)];

            if (chosenEq?.Base == null)
                return;

            data.Equipment(chosenEq.Base)
                .MissionEquipment(null)
                .FixedEquipment(true)
                .CivilianEquipment(false);
        }

        private static List<MEquipment> Collect(List<MEquipment> src, Func<MEquipment, bool> pred)
        {
            var list = new List<MEquipment>(src.Count);
            for (int i = 0; i < src.Count; i++)
            {
                var e = src[i];
                if (pred(e))
                    list.Add(e);
            }
            return list;
        }
    }
}
