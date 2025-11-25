using System;
using TaleWorlds.Core;

namespace Retinues.Features.Agents
{
    /// <summary>
    /// Origin wrapper that swaps the Troop to a specific BasicCharacterObject (captain),
    /// but otherwise delegates to the inner origin.
    /// </summary>
    public sealed class CaptainAgentOrigin : IAgentOriginBase
    {
        private readonly IAgentOriginBase _inner;
        private readonly BasicCharacterObject _troopOverride;

        public CaptainAgentOrigin(IAgentOriginBase inner, BasicCharacterObject troopOverride)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _troopOverride =
                troopOverride ?? throw new ArgumentNullException(nameof(troopOverride));
        }

        public bool IsUnderPlayersCommand => _inner.IsUnderPlayersCommand;

        public uint FactionColor => _inner.FactionColor;

        public uint FactionColor2 => _inner.FactionColor2;

        public IBattleCombatant BattleCombatant => _inner.BattleCombatant;

        public int UniqueSeed => _inner.UniqueSeed;

        public int Seed => _inner.Seed;

        public Banner Banner => _inner.Banner;

        // This is the only thing we actually override.
        public BasicCharacterObject Troop => _troopOverride;

        public void SetWounded() => _inner.SetWounded();

        public void SetKilled() => _inner.SetKilled();

        public void SetRouted() => _inner.SetRouted();

        public void OnAgentRemoved(float agentHealth) => _inner.OnAgentRemoved(agentHealth);

        public void OnScoreHit(
            BasicCharacterObject victim,
            BasicCharacterObject formationCaptain,
            int damage,
            bool isFatal,
            bool isTeamKill,
            WeaponComponentData attackerWeapon
        ) =>
            _inner.OnScoreHit(
                victim,
                formationCaptain,
                damage,
                isFatal,
                isTeamKill,
                attackerWeapon
            );

        public void SetBanner(Banner banner) => _inner.SetBanner(banner);
    }
}
