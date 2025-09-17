using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Retinues.Core.Game.Wrappers
{
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
                return 0;
            return _roster.GetTroopCount(troop.Base as CharacterObject);
        }

        public void AddTroop(WCharacter troop, int healthy, int wounded = 0, int index = -1)
        {
            _roster.AddToCounts(
                troop.Base as CharacterObject,
                healthy,
                woundedCount: wounded,
                index: index
            );
        }

        public void RemoveTroop(WCharacter troop, int healthy, int wounded = 0)
        {
            if (troop.Base == null)
                return;

            _roster.AddToCounts(troop.Base as CharacterObject, -healthy, woundedCount: -wounded);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
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

        public int CountByFormation(FormationClass cls)
        {
            int count = 0;
            foreach (var e in Elements)
            {
                var co = e.Troop.Base as CharacterObject;
                var c = co?.DefaultFormationClass;
                if (c == cls)
                    count += e.Number;
            }
            return count;
        }

        public int CountByCulture(WCulture culture)
        {
            int count = 0;
            foreach (var e in Elements)
                if (e.Troop.Culture.StringId == culture.StringId)
                    count += e.Number;
            return count;
        }
    }
}
