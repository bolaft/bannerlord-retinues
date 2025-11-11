using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Helpers.Character;
using Retinues.Game.Wrappers;
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

        [SaveableField(14)]
        public TroopSaveData ArmedTrader;

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

        public FactionSaveData()
        {
            // Default constructor for deserialization
        }

        public FactionSaveData(ITroopFaction faction)
        {
            if (faction is null)
                return; // Null faction, nothing to do

            // Troop references
            RetinueElite = new TroopSaveData(faction.RetinueElite);
            RetinueBasic = new TroopSaveData(faction.RetinueBasic);
            RootElite = new TroopSaveData(faction.RootElite);
            RootBasic = new TroopSaveData(faction.RootBasic);
            MilitiaMelee = new TroopSaveData(faction.MilitiaMelee);
            MilitiaMeleeElite = new TroopSaveData(faction.MilitiaMeleeElite);
            MilitiaRanged = new TroopSaveData(faction.MilitiaRanged);
            MilitiaRangedElite = new TroopSaveData(faction.MilitiaRangedElite);
            ArmedTrader = new TroopSaveData(faction.ArmedTrader);
            CaravanGuard = new TroopSaveData(faction.CaravanGuard);
            CaravanMaster = new TroopSaveData(faction.CaravanMaster);
            Villager = new TroopSaveData(faction.Villager);

            // Civilians troops
            Civilians = [.. faction.CivilianTroops.Select(t => new TroopSaveData(t))];
        }

        public void Apply(WFaction faction)
        {
            if (faction is null)
                return; // Null faction, nothing to do

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

            CharacterGraphIndex.RegisterFactionRoots(faction);
        }

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
            ArmedTrader?.Deserialize();
            CaravanGuard?.Deserialize();
            CaravanMaster?.Deserialize();
            Villager?.Deserialize();

            if (Civilians != null)
                foreach (var troopData in Civilians)
                    troopData.Deserialize();
        }
    }
}
