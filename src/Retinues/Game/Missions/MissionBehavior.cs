using System;
using System.Collections.Generic;
using System.Text;
using Retinues.Domain.Characters.Models;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Missions
{
    /// <summary>
    /// Mission lifecycle hook.
    /// Sets MMission.Current on mission start and clears it on mission end.
    /// Also captures lightweight kill snapshots via OnAgentRemoved.
    /// </summary>
    public sealed class MissionBehavior : BaseMissionBehavior
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Start                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _started;

        public override void AfterStart()
        {
            if (_started)
                return;

            _started = true;
            MMission.SetCurrent(Mission);

            Log.Debug($"Mission started. Scene='{Mission?.SceneName}'.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           End                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _ended;

        protected override void OnEndMission() => End();

        public override void OnRemoveBehavior() => End();

        private void End()
        {
            if (_ended)
                return;

            _ended = true;

#if DEBUG
            try
            {
                var current = MMission.Current;
                if (current != null)
                    DebugLogMissionSummary(current);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
#endif

            Log.Debug($"Mission ended. Scene='{Mission?.SceneName}'.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Kill Tracker                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void OnAgentRemoved(
            Agent victim,
            Agent killer,
            AgentState state,
            KillingBlow blow
        )
        {
            try
            {
                var mission = MMission.Current;
                if (mission == null)
                    return;

                var v = victim != null ? new MAgent(victim) : null;
                var k = killer != null ? new MAgent(killer) : null;

                if (!MMission.Kill.IsValid(v, k, state))
                    return;

                var kill = new MMission.Kill(v, k, state, blow);
                mission.AddKill(in kill);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Debug                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if DEBUG
        private static void DebugLogMissionSummary(MMission mission)
        {
            var kills = mission.Kills;

            var sb = new StringBuilder(4096);
            sb.AppendLine("=== Mission Summary ===");
            sb.Append("Scene: ").AppendLine(mission.SceneName ?? "<null>");
            sb.Append("Kills captured: ").Append(kills?.Count ?? 0).AppendLine();

            if (kills == null || kills.Count == 0)
            {
                Log.Debug(sb.ToString());
                return;
            }

            int killed = 0;
            int unconscious = 0;
            int headshots = 0;
            int missiles = 0;

            int attackerKills = 0;
            int defenderKills = 0;

            var killsByKiller = new Dictionary<string, int>(64);
            var deathsByVictim = new Dictionary<string, int>(64);
            var killsByWeaponClass = new Dictionary<int, int>(16);
            var killsByDamageType = new Dictionary<int, int>(16);

            for (int i = 0; i < kills.Count; i++)
            {
                var k = kills[i];

                if (k.State == AgentState.Killed)
                    killed++;
                else if (k.State == AgentState.Unconscious)
                    unconscious++;

                if (k.IsHeadShot)
                    headshots++;

                if (k.IsMissile)
                    missiles++;

                if (k.KillerSide == BattleSideEnum.Attacker)
                    attackerKills++;
                else if (k.KillerSide == BattleSideEnum.Defender)
                    defenderKills++;

                if (!string.IsNullOrEmpty(k.KillerCharacterId))
                {
                    killsByKiller.TryGetValue(k.KillerCharacterId, out var count);
                    killsByKiller[k.KillerCharacterId] = count + 1;
                }

                if (!string.IsNullOrEmpty(k.VictimCharacterId))
                {
                    deathsByVictim.TryGetValue(k.VictimCharacterId, out var count);
                    deathsByVictim[k.VictimCharacterId] = count + 1;
                }

                killsByWeaponClass.TryGetValue(k.WeaponClass, out var wc);
                killsByWeaponClass[k.WeaponClass] = wc + 1;

                var dt = (int)k.DamageType;
                killsByDamageType.TryGetValue(dt, out var dc);
                killsByDamageType[dt] = dc + 1;
            }

            sb.Append("Outcomes: killed=")
                .Append(killed)
                .Append(", unconscious=")
                .Append(unconscious)
                .Append(", headshots=")
                .Append(headshots)
                .Append(", missiles=")
                .Append(missiles)
                .AppendLine();

            sb.Append("Kills by side: attacker=")
                .Append(attackerKills)
                .Append(", defender=")
                .Append(defenderKills)
                .AppendLine();

            AppendTopNCharacters(sb, "Top killers", killsByKiller, 8);
            AppendTopNCharacters(sb, "Most deaths", deathsByVictim, 8);
            AppendTopN(sb, "WeaponClass distribution", killsByWeaponClass, 8);
            AppendTopN(sb, "DamageType distribution", killsByDamageType, 8);

            Log.Debug(sb.ToString());
        }

        private static void AppendTopNCharacters(
            StringBuilder sb,
            string title,
            Dictionary<string, int> counts,
            int n
        )
        {
            sb.AppendLine(title + ":");

            foreach (var kv in TakeTop(counts, n))
            {
                var name = ResolveCharacterName(kv.Key);

                sb.Append("  - ").Append(name);

                if (!string.IsNullOrEmpty(kv.Key) && name != kv.Key)
                    sb.Append(" (").Append(kv.Key).Append(")");

                sb.Append(": ").Append(kv.Value).AppendLine();
            }
        }

        private static string ResolveCharacterName(string stringId)
        {
            if (string.IsNullOrEmpty(stringId))
                return "<none>";

            try
            {
                var w = WCharacter.Get(stringId);
                var n = w?.Name?.ToString();
                if (!string.IsNullOrEmpty(n))
                    return n;
            }
            catch
            {
                // Debug summary only.
            }

            return stringId;
        }

        private static void AppendTopN(
            StringBuilder sb,
            string title,
            Dictionary<int, int> counts,
            int n
        )
        {
            sb.AppendLine(title + ":");
            foreach (var kv in TakeTop(counts, n))
                sb.Append("  - ").Append(kv.Key).Append(": ").Append(kv.Value).AppendLine();
        }

        private static IEnumerable<KeyValuePair<string, int>> TakeTop(
            Dictionary<string, int> counts,
            int n
        )
        {
            var list = new List<KeyValuePair<string, int>>(counts.Count);
            foreach (var kv in counts)
                list.Add(kv);

            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            for (int i = 0; i < list.Count && i < n; i++)
                yield return list[i];
        }

        private static IEnumerable<KeyValuePair<int, int>> TakeTop(
            Dictionary<int, int> counts,
            int n
        )
        {
            var list = new List<KeyValuePair<int, int>>(counts.Count);
            foreach (var kv in counts)
                list.Add(kv);

            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            for (int i = 0; i < list.Count && i < n; i++)
                yield return list[i];
        }
#endif
    }
}
