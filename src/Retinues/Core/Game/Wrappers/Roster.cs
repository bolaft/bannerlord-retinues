using System.Collections.Generic;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Retinues.Core.Game.Wrappers
{
    [SafeClass(SwallowByDefault = false)]
    public class WRoster(TroopRoster roster, WParty party)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly TroopRoster _roster = roster;

        public TroopRoster Base => _roster;

        private readonly WParty _party = party;

        public WParty Party => _party;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WRoster(TroopRoster roster)
            : this(
                roster,
                new WParty(Reflector.GetPropertyValue<PartyBase>(roster, "OwnerParty").MobileParty)
            ) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Elements                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IEnumerable<WRosterElement> Elements
        {
            get
            {
                int i = 0;
                foreach (var element in _roster.GetTroopRoster())
                {
                    yield return new WRosterElement(element, this, i);
                    i++;
                }
            }
        }

        public WRosterElement PlayerElement
        {
            get
            {
                int idx = 0;
                foreach (var e in Elements)
                {
                    if (e.Troop.StringId == Player.Character.StringId)
                        return new WRosterElement(e.Base, this, idx);
                    idx++;
                }
                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int Count => _roster.TotalHealthyCount;

        public int CountOf(WCharacter troop)
        {
            if (troop.Base == null)
            {
                Log.Warn($"CountOf: troop has no base!");
                return 0;
            }
            return _roster.GetTroopCount(troop.Base);
        }

        public void AddTroop(
            WCharacter troop,
            int healthy,
            int wounded = 0,
            int xp = 0,
            int index = -1
        )
        {
            if (troop.Base == null)
                return;

            _roster.AddToCounts(
                troop.Base,
                healthy,
                woundedCount: wounded,
                xpChange: xp,
                index: index
            );
        }

        public void RemoveTroop(WCharacter troop, int healthy, int wounded = 0)
        {
            if (troop.Base == null)
                return;

            _roster.AddToCounts(troop.Base, -healthy, woundedCount: -wounded);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int HeroCount
        {
            get
            {
                int count = 0;
                foreach (var e in Elements)
                    if (e.Troop.IsHero)
                        count += e.Number;
                return count;
            }
        }

        public int EliteCount
        {
            get
            {
                int count = 0;
                foreach (var e in Elements)
                    if (e.Troop.IsElite)
                        count += e.Number;
                return count;
            }
        }

        public float EliteRatio => Count == 0 ? 0 : (float)EliteCount / (Count - HeroCount);

        public int CustomCount
        {
            get
            {
                int count = 0;
                foreach (var e in Elements)
                    if (e.Troop.IsCustom)
                        count += e.Number;
                return count;
            }
        }

        public float CustomRatio => Count == 0 ? 0 : (float)CustomCount / (Count - HeroCount);

        public int RetinueCount
        {
            get
            {
                int count = 0;
                foreach (var e in Elements)
                    if (e.Troop.IsRetinue)
                        count += e.Number;
                return count;
            }
        }

        public float RetinueRatio => Count == 0 ? 0 : (float)RetinueCount / (Count - HeroCount);

        public int InfantryCount => CountByFormation(FormationClass.Infantry);
        public int ArchersCount => CountByFormation(FormationClass.Ranged);
        public int CavalryCount => CountByFormation(FormationClass.Cavalry);

        public float InfantryRatio => Count == 0 ? 0 : (float)InfantryCount / (Count - HeroCount);
        public float ArchersRatio => Count == 0 ? 0 : (float)ArchersCount / (Count - HeroCount);
        public float CavalryRatio => Count == 0 ? 0 : (float)CavalryCount / (Count - HeroCount);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private int CountByFormation(FormationClass cls)
        {
            int count = 0;
            foreach (var e in Elements)
            {
                var co = e.Troop.Base;
                var c = co?.DefaultFormationClass;
                if (c == cls)
                    count += e.Number;
            }
            return count;
        }
    }
}
