using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Migration.Legacy
{
    /// <summary>
    /// Mirrors the v1 <c>SaveDefinitions : SaveableTypeDefiner(090_787)</c>.
    /// Registers the same type IDs and container signatures so the BL save
    /// system can deserialize old-format partitions when loading a v1 save.
    /// The data-only mirror classes live in this namespace but carry the same
    /// numeric type IDs – BL matches by ID, not by fully-qualified class name.
    /// </summary>
    public sealed class LegacySaveDefinitions : SaveableTypeDefiner
    {
        public LegacySaveDefinitions()
            : base(090_787) { }

        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();

            AddClassDefinition(typeof(TroopSaveData), 070_910);
            AddClassDefinition(typeof(TroopBodySaveData), 070_911);
            AddClassDefinition(typeof(TroopEquipmentData), 070_912);
            AddClassDefinition(typeof(TroopSkillData), 070_913);

            AddClassDefinition(typeof(FactionSaveData), 070_920);
            AddClassDefinition(typeof(TroopCombatStats), 070_930);

            AddClassDefinition(typeof(PendingTrainData), 070_001);
            AddClassDefinition(typeof(PendingEquipData), 070_002);
            AddClassDefinition(typeof(EquipmentPolicy), 200901);
            AddClassDefinition(typeof(LegacyTroopSaveData), 070_992);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();

            ConstructContainerDefinition(typeof(Dictionary<string, PendingTrainData>));
            ConstructContainerDefinition(typeof(Dictionary<string, PendingEquipData>));
            ConstructContainerDefinition(
                typeof(Dictionary<string, Dictionary<string, PendingTrainData>>)
            );
            ConstructContainerDefinition(
                typeof(Dictionary<string, Dictionary<string, PendingEquipData>>)
            );

            ConstructContainerDefinition(typeof(List<TroopSaveData>));
            ConstructContainerDefinition(typeof(List<string>));
            ConstructContainerDefinition(typeof(List<FactionSaveData>));

            ConstructContainerDefinition(typeof(Dictionary<string, TroopCombatStats>));

            ConstructContainerDefinition(typeof(Dictionary<int, byte>));
            ConstructContainerDefinition(typeof(Dictionary<string, Dictionary<int, byte>>));

            ConstructContainerDefinition(typeof(Dictionary<int, EquipmentPolicy>));
            ConstructContainerDefinition(
                typeof(Dictionary<string, Dictionary<int, EquipmentPolicy>>)
            );

            ConstructContainerDefinition(typeof(Dictionary<string, int>));

            ConstructContainerDefinition(typeof(List<LegacyTroopSaveData>));
        }
    }
}
