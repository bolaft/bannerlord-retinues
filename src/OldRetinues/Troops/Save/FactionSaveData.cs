using System.Collections.Generic;
using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.SaveSystem;

namespace OldRetinues.Troops.Save
{
    /// <summary>
    /// Serializable save data for a faction's troops.
    /// Used for saving and loading custom faction state.
    /// </summary>
    public class FactionSaveData
    {
        [SaveableField(1)]
        public TroopSaveData RetinueElite;

        [SaveableField(2)]
        public TroopSaveData RetinueBasic;

        [SaveableField(3)]
        public TroopSaveData RootElite;

        [SaveableField(4)]
        public TroopSaveData RootBasic;

        [SaveableField(5)]
        public TroopSaveData MilitiaMelee;

        [SaveableField(6)]
        public TroopSaveData MilitiaMeleeElite;

        [SaveableField(7)]
        public TroopSaveData MilitiaRanged;

        [SaveableField(8)]
        public TroopSaveData MilitiaRangedElite;

        [SaveableField(9)]
        public TroopSaveData CaravanGuard;

        [SaveableField(10)]
        public TroopSaveData CaravanMaster;

        [SaveableField(11)]
        public TroopSaveData Villager;

        [SaveableField(12)]
        public TroopSaveData PrisonGuard; // Legacy, unused

        [SaveableField(16)]
        public List<TroopSaveData> Mercenaries;

        [SaveableField(14)]
        public List<TroopSaveData> Bandits;

        [SaveableField(13)]
        public List<TroopSaveData> Civilians;

        [SaveableField(15)]
        public List<TroopSaveData> Heroes;

        public FactionSaveData()
        {
            // Default constructor for deserialization
        }

        public FactionSaveData(BaseFaction faction)
        {
            if (faction is null)
                return; // Null faction, nothing to do

            // Troop references
            RetinueElite = CreateIfNeeded(faction.RetinueElite);
            RetinueBasic = CreateIfNeeded(faction.RetinueBasic);
            RootElite = CreateIfNeeded(faction.RootElite);
            RootBasic = CreateIfNeeded(faction.RootBasic);
            MilitiaMelee = CreateIfNeeded(faction.MilitiaMelee);
            MilitiaMeleeElite = CreateIfNeeded(faction.MilitiaMeleeElite);
            MilitiaRanged = CreateIfNeeded(faction.MilitiaRanged);
            MilitiaRangedElite = CreateIfNeeded(faction.MilitiaRangedElite);
            CaravanGuard = CreateIfNeeded(faction.CaravanGuard);
            CaravanMaster = CreateIfNeeded(faction.CaravanMaster);
            Villager = CreateIfNeeded(faction.Villager);

            // Mercenary troops
            var mercenaries = faction
                .MercenaryTroops?.Select(CreateIfNeeded)
                .Where(d => d != null)
                .ToList();

            Mercenaries = (mercenaries != null && mercenaries.Count > 0) ? mercenaries : null;

            // Bandit troops
            var bandits = faction
                .BanditTroops?.Select(CreateIfNeeded)
                .Where(d => d != null)
                .ToList();

            Bandits = (bandits != null && bandits.Count > 0) ? bandits : null;

            // Civilians troops
            var civilians = faction
                .CivilianTroops?.Select(CreateIfNeeded)
                .Where(d => d != null)
                .ToList();

            Civilians = (civilians != null && civilians.Count > 0) ? civilians : null;
        }

        /// <summary>
        /// Deserializes all troop save data.
        /// Used to overwrite each troop's CharacterObject data.
        /// </summary>
        public void Apply(WFaction faction = null)
        {
            if (faction == null)
            {
                // Culture troops
                RetinueElite?.Deserialize();
                RetinueBasic?.Deserialize();
                RootElite?.Deserialize();
                RootBasic?.Deserialize();
                MilitiaMelee?.Deserialize();
                MilitiaMeleeElite?.Deserialize();
                MilitiaRanged?.Deserialize();
                MilitiaRangedElite?.Deserialize();
                CaravanGuard?.Deserialize();
                CaravanMaster?.Deserialize();
                Villager?.Deserialize();

                if (Mercenaries != null)
                    foreach (var troopData in Mercenaries)
                        troopData.Deserialize(); // No assignments

                if (Bandits != null)
                    foreach (var troopData in Bandits)
                        troopData.Deserialize(); // No assignments

                if (Civilians != null)
                    foreach (var troopData in Civilians)
                        troopData.Deserialize(); // No assignments
            }
            else
            {
                Log.Info($"Deserializing troop data for faction: {faction.Name}");

                // Faction troops
                RetinueElite?.Deserialize(faction, RootCategory.RetinueElite);
                RetinueBasic?.Deserialize(faction, RootCategory.RetinueBasic);
                RootElite?.Deserialize(faction, RootCategory.RootElite);
                RootBasic?.Deserialize(faction, RootCategory.RootBasic);
                MilitiaMelee?.Deserialize(faction, RootCategory.MilitiaMelee);
                MilitiaMeleeElite?.Deserialize(faction, RootCategory.MilitiaMeleeElite);
                MilitiaRanged?.Deserialize(faction, RootCategory.MilitiaRanged);
                MilitiaRangedElite?.Deserialize(faction, RootCategory.MilitiaRangedElite);
                CaravanGuard?.Deserialize(faction, RootCategory.CaravanGuard);
                CaravanMaster?.Deserialize(faction, RootCategory.CaravanMaster);
                Villager?.Deserialize(faction, RootCategory.Villager);
            }
        }

        /// <summary>
        /// Creates a TroopSaveData instance for the given troop if needed.
        /// Custom and edited troops are always saved; unedited vanilla troops are not.
        /// </summary>
        private static TroopSaveData CreateIfNeeded(WCharacter troop)
        {
            return troop?.NeedsPersistence == true ? new TroopSaveData(troop) : null;
        }
    }
}
