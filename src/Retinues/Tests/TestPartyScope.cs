using System;
using System.Collections.Generic;
using Retinues.Game;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests
{
    /// <summary>
    /// Disposable scope that snapshots and restores the player's main party member roster, gold,
    /// and influence. For tests that exercise conversion/economy flows which must mutate the live
    /// party (Convert/GetMaxConvertible hardcode the main party). Use only on a disposable save.
    /// </summary>
    public sealed class TestPartyScope : IDisposable
    {
        private readonly List<(string id, int healthy, int wounded, int xp)> _roster = new();
        private readonly int _gold;
        private readonly int _influence;

        public TestPartyScope()
        {
            _gold = Player.Gold;
            _influence = Player.Influence;

            foreach (var e in Player.Party.MemberRoster.Elements)
                _roster.Add((e.Troop.StringId, e.Number, e.WoundedNumber, e.Xp));
        }

        public void Dispose()
        {
            try
            {
                var roster = Player.Party.MemberRoster.Base;
                roster.Clear();
                foreach (var (id, healthy, wounded, xp) in _roster)
                {
                    var co = MBObjectManager.Instance.GetObject<CharacterObject>(id);
                    if (co != null)
                        roster.AddToCounts(co, healthy, woundedCount: wounded, xpChange: xp);
                }

                Player.ChangeGold(_gold - Player.Gold);
                Player.ChangeInfluence(_influence - Player.Influence);
            }
            catch (Exception e)
            {
                Log.Exception(e, "TestPartyScope: failed to restore party state.");
            }
        }
    }
}
