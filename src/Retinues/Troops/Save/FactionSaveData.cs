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

        public FactionSaveData()
        {
            // Default constructor for deserialization
        }

        public FactionSaveData(ITroopFaction faction)
        {
            if (faction is null)
                return; // Null faction, nothing to do

            RetinueElite = new TroopSaveData(faction.RetinueElite);
            RetinueBasic = new TroopSaveData(faction.RetinueBasic);
            RootElite = new TroopSaveData(faction.RootElite);
            RootBasic = new TroopSaveData(faction.RootBasic);
            MilitiaMelee = new TroopSaveData(faction.MilitiaMelee);
            MilitiaMeleeElite = new TroopSaveData(faction.MilitiaMeleeElite);
            MilitiaRanged = new TroopSaveData(faction.MilitiaRanged);
            MilitiaRangedElite = new TroopSaveData(faction.MilitiaRangedElite);
        }

        public void Apply(WFaction faction)
        {
            if (faction is null)
                return; // Null faction, nothing to do

            faction.RetinueElite = RetinueElite.Deserialize();
            faction.RetinueBasic = RetinueBasic.Deserialize();
            faction.RootElite = RootElite.Deserialize();
            faction.RootBasic = RootBasic.Deserialize();
            faction.MilitiaMelee = MilitiaMelee.Deserialize();
            faction.MilitiaMeleeElite = MilitiaMeleeElite.Deserialize();
            faction.MilitiaRanged = MilitiaRanged.Deserialize();
            faction.MilitiaRangedElite = MilitiaRangedElite.Deserialize();

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
        }
    }
}
