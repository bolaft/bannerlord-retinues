using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Tests
{
    // Read-only leak detection, not rollback. A mismatch stops the suite before another test runs.
    internal static class CampaignSafetySnapshot
    {
        internal static string Capture()
        {
            var state = new StringBuilder();
            state.Append("gold=").Append(Hero.MainHero?.Gold).Append(';');
            state.Append("renown=").Append(Clan.PlayerClan?.Renown.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            state.Append("influence=").Append(Clan.PlayerClan?.Influence.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            AppendRoster(state, PartyBase.MainParty?.MemberRoster);
            AppendRoster(state, PartyBase.MainParty?.PrisonRoster);
            state.Append(string.Join(",", ActiveIds().OrderBy(id => id, StringComparer.Ordinal)));
            return state.ToString();
        }

        private static System.Collections.Generic.IEnumerable<string> ActiveIds() =>
            WCharacter.All.Where(c => c != null && c.IsCustom && c.IsActiveStub).Select(c => c.StringId);

        private static void AppendRoster(StringBuilder state, TroopRoster roster)
        {
            if (roster == null)
            {
                state.Append("<null>;");
                return;
            }
            foreach (var row in roster.GetTroopRoster().OrderBy(r => r.Character?.StringId, StringComparer.Ordinal))
                state.Append(row.Character?.StringId).Append(':')
                    .Append(row.Character == null ? 0 : RuntimeHelpers.GetHashCode(row.Character)).Append(':')
                    .Append(row.Number).Append(':').Append(row.WoundedNumber).Append(':').Append(row.Xp).Append(';');
            state.Append('|');
        }
    }
}
