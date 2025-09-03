
using System.Linq;
using System.Collections.Generic;
using CustomClanTroops.Game.Troops.Objects;

namespace CustomClanTroops.Game
{
    public static class Setup
    {
        public static void Initialize()
        {
            var culture = Player.Culture;
            var clan = Player.Clan;

            // Clone culture's elite troops
            foreach (var troop in CopyTroopTree($"{clan.Name} ", culture.EliteTroops))
                clan.EliteTroops.Add(troop);

            // Clone culture's basic troops
            foreach (var troop in CopyTroopTree($"{clan.Name} ", culture.BasicTroops))
                clan.BasicTroops.Add(troop);

            // Unlock all items of all equipments of all troops
            foreach (var troop in Enumerable.Concat(clan.EliteTroops, clan.BasicTroops))
                foreach (var equipment in troop.Equipments)
                    foreach (var item in equipment.Items)
                        item.Unlock();
        }

        public static IEnumerable<TroopCharacter> CopyTroopTree(string prefix, List<TroopCharacter> troops)
        {
            foreach (var troop in troops)
            {
                // Clone the troop
                var clone = troop.Clone();
                // Prefix its name
                clone.Name = $"{prefix} {troop.Name}";
                // Return it
                yield return clone;
            }
        }
    }
}
