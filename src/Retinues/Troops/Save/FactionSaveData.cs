using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Helpers.Character;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.SaveSystem;

namespace Retinues.Troops.Save
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

        [SaveableField(13)]
        public List<TroopSaveData> Civilians;

        [SaveableField(14)]
        public List<TroopSaveData> Bandits;

        public FactionSaveData()
        {
            // Default constructor for deserialization
        }

        public FactionSaveData(ITroopFaction faction)
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

            // Civilians troops
            var civilians = faction
                .CivilianTroops?.Select(CreateIfNeeded)
                .Where(d => d != null)
                .ToList();

            Civilians = (civilians != null && civilians.Count > 0) ? civilians : null;

            // Bandit troops
            var bandits = faction
                .BanditTroops?.Select(CreateIfNeeded)
                .Where(d => d != null)
                .ToList();

            Bandits = (bandits != null && bandits.Count > 0) ? bandits : null;
        }

        /// <summary>
        /// Assigns the given troop to the appropriate field based on its type.
        /// </summary>
        public void Assign(WCharacter troop)
        {
            if (troop == null)
                return;
            
            var data = new TroopSaveData(troop);

            switch (troop.Type)
            {
                case WCharacter.TroopType.RetinueElite:
                    RetinueElite = data;
                    break;
                case WCharacter.TroopType.RetinueBasic:
                    RetinueBasic = data;
                    break;
                case WCharacter.TroopType.Elite:
                    RootElite = data;
                    break;
                case WCharacter.TroopType.Basic:
                    RootBasic = data;
                    break;
                case WCharacter.TroopType.MilitiaMelee:
                    MilitiaMelee = data;
                    break;
                case WCharacter.TroopType.MilitiaMeleeElite:
                    MilitiaMeleeElite = data;
                    break;
                case WCharacter.TroopType.MilitiaRanged:
                    MilitiaRanged = data;
                    break;
                case WCharacter.TroopType.MilitiaRangedElite:
                    MilitiaRangedElite = data;
                    break;
                case WCharacter.TroopType.CaravanGuard:
                    CaravanGuard = data;
                    break;
                case WCharacter.TroopType.CaravanMaster:
                    CaravanMaster = data;
                    break;
                case WCharacter.TroopType.Villager:
                    Villager = data;
                    break;
            }
        }

        /// <summary>
        /// Applies the saved troop data to a faction.
        /// </summary>
        public void Apply(WFaction faction)
        {
            if (faction is null)
                return; // Null faction, nothing to do

            Log.Info($"Applying saved troop data to faction '{faction.Name}' (ID: {faction.StringId})");

            faction.RetinueElite = RetinueElite?.Deserialize();
            faction.RetinueBasic = RetinueBasic?.Deserialize();
            faction.RootElite = RootElite?.Deserialize();
            faction.RootBasic = RootBasic?.Deserialize();
            faction.MilitiaMelee = MilitiaMelee?.Deserialize();
            faction.MilitiaMeleeElite = MilitiaMeleeElite?.Deserialize();
            faction.MilitiaRanged = MilitiaRanged?.Deserialize();
            faction.MilitiaRangedElite = MilitiaRangedElite?.Deserialize();
            faction.CaravanGuard = CaravanGuard?.Deserialize();
            faction.CaravanMaster = CaravanMaster?.Deserialize();
            faction.Villager = Villager?.Deserialize();

            Log.Info($"Registering faction roots for faction '{faction.Name}' (ID: {faction.StringId})");

            // To be done before activating troops
            CharacterIndexer.RegisterFactionRoots(faction);
        }

        /// <summary>
        /// Deserializes all troop save data.
        /// Used to overwrite each troop's CharacterObject data.
        /// </summary>
        public void DeserializeTroops()
        {
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

            if (Civilians != null)
                foreach (var troopData in Civilians)
                    troopData.Deserialize();

            if (Bandits != null)
                foreach (var troopData in Bandits)
                    troopData.Deserialize();
        }

        /// <summary>
        /// Creates a TroopSaveData instance for the given troop if needed.
        /// Custom and edited troops are always saved; unedited vanilla troops are not.
        /// </summary>
        private static TroopSaveData CreateIfNeeded(WCharacter troop)
        {
            if (troop is null)
                return null;

            // Custom / retinue / cloned troops are always persisted
            if (troop.IsCustom)
                return new TroopSaveData(troop);

            // Vanilla troops are only persisted if they were edited
            return troop.IsVanillaEdited ? new TroopSaveData(troop) : null;
        }
    }
}
